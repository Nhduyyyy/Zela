using System.Text.Json.Serialization;

namespace Zela.ViewModels
{
    public class TranslationRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("sourceLanguage")]
        public string? SourceLanguage { get; set; }

        [JsonPropertyName("targetLanguage")]
        public string TargetLanguage { get; set; } = string.Empty;
    }
} 