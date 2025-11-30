using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyTranslateExtension.Model;
using MyTranslateExtension.Translation.Providers;

namespace MyTranslateExtension.Translation
{
    internal class TranslatorWorker(SettingsManager settings)
    {
        private readonly Dictionary<string, ITranslationProvider> _providers = InitializeProviders(settings);
        private readonly SettingsManager _settings = settings;

        private static Dictionary<string, ITranslationProvider> InitializeProviders(SettingsManager settings)
        {
            var providers = new Dictionary<string, ITranslationProvider>(StringComparer.OrdinalIgnoreCase)
            {
                { "google", new GoogleTranslationProvider(() => settings.GoogleTranslateEnabled) },
                { "bing", new BingTranslationProvider(() => settings.BingTranslateEnabled) },
                { "yandex", new YandexTranslationProvider(() => settings.YandexTranslateEnabled) },
                { "azure", new AzureTranslationProvider(() => settings.MicrosoftAzureTranslateEnabled) },
                { "deepl", new DeepLProvider(settings.DeeplApiKey, () => settings.DeepLTranslateEnabled) }
            };

            return providers;
        }

        /// <summary>
        /// Translates text using a specific provider or all available providers
        /// </summary>
        public async Task<TranslationResultDTO> TranslateAsync(LangCode targetCode, string text, string? provider = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                // No provider specified - use all available providers and aggregate results
                return await AggregateTranslationAsync(text, targetCode);
            }

            // Specific provider requested
            if (_providers.TryGetValue(provider, out var selectedProvider) && selectedProvider.IsAvailable)
            {
                return await TranslateWithTimeoutAsync(selectedProvider, text, targetCode, _settings.TranslationTimeout);
            }

            // Provider not found or not available
            return CreateErrorResult($"Provider ''{provider}'' is not available", targetCode);
        }

        /// <summary>
        /// Aggregates translations from all available providers
        /// </summary>
        private async Task<TranslationResultDTO> AggregateTranslationAsync(string text, LangCode targetCode)
        {
            var availableProviders = _providers
                .Where(p => p.Value.IsAvailable)
                .ToList();

            if (availableProviders.Count == 0)
            {
                return CreateErrorResult("No translation providers available", targetCode, "no providers", text);
            }

            var targetLangCode = targetCode.ToString();
            var result = new TranslationResultDTO
            {
                Translations = [],
                TargetLangCode = targetLangCode
            };

            // Run all translations concurrently with timeout
            var tasks = availableProviders
                .Select(async p =>
                {
                    try
                    {
                        var translation = await TranslateWithTimeoutAsync(p.Value, text, targetCode, timeoutMs: _settings.TranslationTimeout);
                        return (Provider: p.Key, Translation: translation);
                    }
                    catch (Exception ex)
                    {
                        return new() { Provider = p.Key, Translation = CreateErrorResult($"Error: {ex.Message}", targetCode, p.Key, text) };
                    }
                })
                .ToList();

            var completedTasks = await Task.WhenAll(tasks);

            // Aggregate all results
            foreach (var (providerName, translation) in completedTasks)
            {
                foreach (var item in translation.Translations)
                {
                    // Add provider name to subtitle for identification
                    result.Translations.Add(new TranslationDTO
                    {
                        OriginalText = item.OriginalText,
                        ProviderCode = providerName,
                        Text = item.Text,
                        DetectedSourceLanguage = item.DetectedSourceLanguage
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Executes translation with timeout
        /// </summary>
        private static async Task<TranslationResultDTO> TranslateWithTimeoutAsync(
            ITranslationProvider translator,
            string text,
            LangCode targetCode,
            int timeoutMs)
        {
            using var cts = new System.Threading.CancellationTokenSource(timeoutMs);
            try
            {
                var task = translator.TranslateAsync(text, targetCode, cts.Token);
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return CreateErrorResult($"Translation timeout ({translator.Name})", targetCode, translator.Name, text);
            }
        }

        private static TranslationResultDTO CreateErrorResult(string message, LangCode targetCode, string provider = "", string originalText = "")
        {
            return new TranslationResultDTO
            {
                Translations =
                [
                    new TranslationDTO
                    {
                        ProviderCode = provider,
                        OriginalText = originalText,
                        DetectedSourceLanguage = LangCode.Unknown.ToNormalizedString(),
                        Text = message,
                    }
                ],
                TargetLangCode = targetCode.ToNormalizedString(),
            };
        }

        public void Dispose()
        {
            foreach (var provider in _providers.Values.OfType<IDisposable>())
            {
                provider?.Dispose();
            }
        }
    }
}
