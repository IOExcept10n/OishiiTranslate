using System;
using System.Threading;
using System.Threading.Tasks;
using GTranslate.Translators;
using MyTranslateExtension.Model;

namespace MyTranslateExtension.Translation.Providers
{
    internal partial class GTranslateProviderBase<T>(string name, Func<bool> availability) : ITranslationProvider, IDisposable where T : ITranslator, new()
    {
        private readonly ITranslator _translator = new T();

        public string Name { get; } = name;

        public bool IsAvailable => availability();

        public async Task<TranslationResultDTO> TranslateAsync(string text, LangCode targetCode, CancellationToken cancellationToken)
        {
            try
            {
                var targetLangCode = targetCode.ToNormalizedString();
                var task = Task.Run(async () => await _translator.TranslateAsync(text, targetLangCode), cancellationToken);

                while (!task.IsCompleted)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(50, cancellationToken);
                }

                var response = await task;

                var sourceLanguage = ExtractLanguageCode(response.SourceLanguage);

                return new TranslationResultDTO
                {
                    Translations =
                    [
                        new TranslationDTO
                        {
                            DetectedSourceLanguage = sourceLanguage,
                            Text = response.Translation
                        }
                    ],
                    TargetLangCode = targetLangCode
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Translate error ({typeof(T).Name}): {ex.Message}", ex);
            }
        }

        private static string ExtractLanguageCode(GTranslate.ILanguage language)
        {
            if (language == null)
                return LangCode.Unknown.ToNormalizedString();

            var code = language.ISO6391.ToString();
            return code ?? LangCode.Unknown.ToNormalizedString();
        }

        public void Dispose()
        {
            (_translator as IDisposable)?.Dispose();
        }
    }

    internal partial class GoogleTranslationProvider(Func<bool> availability) : GTranslateProviderBase<GoogleTranslator2>("google", availability);
    internal partial class BingTranslationProvider(Func<bool> availability) : GTranslateProviderBase<BingTranslator>("bing", availability);
    internal partial class AzureTranslationProvider(Func<bool> availability) : GTranslateProviderBase<MicrosoftTranslator>("azure", availability);
    internal partial class YandexTranslationProvider(Func<bool> availability) : GTranslateProviderBase<YandexTranslator>("yandex", availability);
}
