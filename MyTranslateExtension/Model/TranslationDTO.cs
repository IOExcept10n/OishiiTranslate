using System.Text.Json.Serialization;

namespace MyTranslateExtension.Model
{
    public class TranslationDTO
    {
        [JsonIgnore]
        public string ProviderCode { get; set; } = string.Empty;

        [JsonPropertyName("detected_source_language")]
        public string DetectedSourceLanguage { get; set; } = string.Empty;

        [JsonIgnore]
        public string OriginalText { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
