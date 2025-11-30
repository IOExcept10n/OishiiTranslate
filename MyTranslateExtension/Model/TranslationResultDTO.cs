using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTranslateExtension.Model
{
    public class TranslationResultDTO
    {
        [JsonIgnore]
        public string TargetLangCode { get; set; } = string.Empty;

        [JsonPropertyName("translations")]
        public List<TranslationDTO> Translations { get; set; } = [];
    }
}
