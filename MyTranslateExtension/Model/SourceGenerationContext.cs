using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTranslateExtension.Model
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(TranslationEntity))]
    [JsonSerializable(typeof(List<TranslationEntity>))]
    [JsonSerializable(typeof(TranslationDTO))]
    [JsonSerializable(typeof(TranslationResultDTO))]
    [JsonSerializable(typeof(DeeplRequestDto))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }

    internal class DeeplRequestDto
    {
        public string[] text { get; set; } = [];

        public string target_lang { get; set; } = string.Empty;
    }
}
