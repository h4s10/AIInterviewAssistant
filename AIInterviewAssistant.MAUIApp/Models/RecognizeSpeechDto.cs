using System.Text.Json.Serialization;

namespace AIInterviewAssistant.MAUIApp.Models;

public class RecognizeSpeechDto
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}