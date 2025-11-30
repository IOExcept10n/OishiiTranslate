using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyTranslateExtension.Model
{
    public class TranslationEntity
    {
        [JsonPropertyName(nameof(ProviderCode))]
        public string ProviderCode { get; set; } = string.Empty;

        [JsonPropertyName(nameof(OriginalText))]
        public string OriginalText { get; set; } = string.Empty;

        [JsonPropertyName(nameof(TranslatedText))]
        public string TranslatedText { get; set; } = string.Empty;

        [JsonPropertyName(nameof(OriginalLangCode))]
        public string OriginalLangCode { get; set; } = string.Empty;

        [JsonPropertyName(nameof(TargetLangCode))]
        public string TargetLangCode { get; set; } = string.Empty;

        [JsonPropertyName(nameof(Timestamp))]
        public DateTime Timestamp { get; set; }
    }
}
