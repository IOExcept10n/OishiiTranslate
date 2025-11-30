using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyTranslateExtension.Model;
using MyTranslateExtension.Properties;

namespace MyTranslateExtension.Translation.Providers
{
    internal partial class DeepLProvider(string apiKey, Func<bool> availability) : ITranslationProvider, IDisposable
    {
        private readonly string _apiKey = apiKey;

        public string Name => "deepl";

        public void Dispose() => GC.SuppressFinalize(this);

        public bool IsAvailable => availability() && !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<TranslationResultDTO> TranslateAsync(string text, LangCode targetLanguage, CancellationToken cancellationToken)
        {
            if (!IsAvailable)
                throw new InvalidOperationException("DeepL API key is not configured");

            // Use existing JobHttp.Translation method
            return await JobHttp.Translation(targetLanguage, text, _apiKey, cancellationToken);
        }

        public class JobHttp
        {
            private static HttpClient? httpClient;
            private static string? oldAPIKey;
            private const int MaxRetries = 3;
            private const int InitialRetryDelayMs = 1000;
            private static readonly Random Random = new();

            public static async Task<TranslationResultDTO> Translation(LangCode targetCode, string text, string apiKey, CancellationToken cancellationToken)
            {
                if (httpClient == null || oldAPIKey != apiKey)
                {
                    Init(apiKey);
                }

                if (httpClient != null)
                {
                    int retryCount = 0;
                    while (true)
                    {
                        try
                        {
                            var body = new DeeplRequestDto
                            {
                                text = [text],
                                target_lang = targetCode.ToNormalizedString()
                            };

                            using StringContent jsonContent = new(
                                JsonSerializer.Serialize(body, SourceGenerationContext.Default.DeeplRequestDto),
                                Encoding.UTF8,
                                "application/json"
                            );

                            HttpResponseMessage response = await httpClient.PostAsync("translate", jsonContent, cancellationToken);

                            if (response.IsSuccessStatusCode)
                            {
                                string responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                                if (responseString != null)
                                {
                                    var result = JsonSerializer.Deserialize(responseString, SourceGenerationContext.Default.TranslationResultDTO);
                                    if (result != null)
                                    {
                                        result.TargetLangCode = targetCode.ToNormalizedString();
                                        return result;
                                    }
                                }
                            }

                            if (response.StatusCode == HttpStatusCode.Forbidden)
                            {
                                return CreateErrorResult(Localization.InvalidApiKey, targetCode);
                            }
                            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                if (retryCount >= MaxRetries)
                                {
                                    return CreateErrorResult(Localization.TooManyRequestsMessage, targetCode);
                                }

                                int delayMs = InitialRetryDelayMs * (int)Math.Pow(2, retryCount);
                                await Task.Delay(delayMs, cancellationToken);
                                retryCount++;
                                continue;
                            }

                            return CreateErrorResult(Localization.TranslationErrorMessage, targetCode);
                        }
                        catch (Exception ex)
                        {
                            if (retryCount >= MaxRetries)
                            {
                                return CreateErrorResult($"Error: {ex.Message}", targetCode);
                            }
                            int delayMs = CalculateDelayWithJitter(retryCount);
                            await Task.Delay(delayMs, cancellationToken);
                            retryCount++;
                        }
                    }
                }

                return CreateErrorResult(Localization.TranslationErrorMessage, targetCode);
            }

            private static TranslationResultDTO CreateErrorResult(string message, LangCode targetCode)
            {
                return new TranslationResultDTO
                {
                    Translations = [
                        new TranslationDTO
                        {
                            DetectedSourceLanguage = LangCode.Unknown.ToNormalizedString(),
                            Text = message
                        }
                    ],
                    TargetLangCode = targetCode.ToNormalizedString()
                };
            }

            private static int CalculateDelayWithJitter(int retryCount)
            {
                double baseDelay = 1000 * Math.Pow(2, retryCount);
                const double jitterPercentage = 0.23;
                double jitter = (Random.NextDouble() * 2 - 1) * jitterPercentage * baseDelay;
                return (int)Math.Min(baseDelay + jitter, 120000);
            }


            private static void Init(string apiKey)
            {
                oldAPIKey = apiKey;

                httpClient = new HttpClient
                {
                    BaseAddress = new Uri("https://api-free.deepl.com/v2/"),
                    Timeout = TimeSpan.FromMinutes(2)
                };
                httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
            }
        }
    }
}
