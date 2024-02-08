namespace AIInterviewAssistant.MAUIApp.Services.Interfaces;

public interface IRecognizeService
{
    void LoadModel(string modelPath);
    Task<string> RecognizeSpeechAsync(string filePath);
}