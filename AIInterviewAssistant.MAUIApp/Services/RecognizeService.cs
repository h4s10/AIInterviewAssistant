using AIInterviewAssistant.MAUIApp.Services.Interfaces;
using Vosk;

namespace AIInterviewAssistant.MAUIApp.Services;

public class RecognizeService : IRecognizeService
{
    private VoskRecognizer _recognizer;

    public void LoadModel(string modelPath)
    {
        _recognizer = new VoskRecognizer(new Model(modelPath), 16000.0f);
        _recognizer.SetMaxAlternatives(0);
        _recognizer.SetWords(true);
    }
    
    public async Task<string> RecognizeSpeechAsync(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
        {
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                _recognizer.AcceptWaveform(buffer, bytesRead);
            }
        }

        var result = _recognizer.FinalResult();
        _recognizer.Reset();
        return result;
    }
}