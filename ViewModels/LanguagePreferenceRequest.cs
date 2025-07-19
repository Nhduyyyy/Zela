using System.Text.Json.Serialization;

namespace Zela.ViewModels
{
    public class LanguagePreferenceRequest
    {
        [JsonPropertyName("sessionId")]
        public Guid SessionId { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;
    }
} 