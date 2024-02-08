namespace AIInterviewAssistant.MAUIApp.ChatGPT.Interfaces;

public interface IAIService
{
    Task<string> SendQuestionAsync(string question);
    Task<bool> AuthAsync();
}