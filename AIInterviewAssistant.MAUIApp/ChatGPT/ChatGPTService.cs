using AIInterviewAssistant.MAUIApp.ChatGPT.Interfaces;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;

namespace AIInterviewAssistant.MAUIApp.ChatGPT;

//UNUSED
public class ChatGPTService : IAIService
{
    private readonly IOpenAi _openAi;

    public ChatGPTService(IOpenAiFactory openAiFactory)
    {
        _openAi = openAiFactory.Create();
    }

    public async Task<string> SendQuestionAsync(string question)
    {
        var response = await _openAi.Chat.Request(new ChatMessage(){Role = ChatRole.Assistant, Content = question})
            .WithModel(ChatModelType.Gpt35Turbo).WithTemperature(0.2).ExecuteAndCalculateCostAsync(true);
        return response.Result.Choices[0].Message.Content;
    }

    public Task<bool> AuthAsync()
    {
        throw new NotImplementedException();
    }
}