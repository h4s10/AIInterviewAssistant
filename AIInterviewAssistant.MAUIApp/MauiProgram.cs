using AIInterviewAssistant.MAUIApp.ChatGPT;
using AIInterviewAssistant.MAUIApp.ChatGPT.Interfaces;
using AIInterviewAssistant.MAUIApp.Services;
using AIInterviewAssistant.MAUIApp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIInterviewAssistant.MAUIApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        
        Preferences.Default.Set("MaximumRecordLengthInSeconds", 20);
        Preferences.Default.Set("ChatpGPTToken", "");
        Preferences.Default.Set("GigaChatToken", "");
        Preferences.Default.Set("InitialPromptTemplate", "Ты профессиональный {0}. Ты проходишь собеседование. Сейчас я буду задавать вопросы, а тебе нужно на них давать ответ.");
        
        /*builder.Services.AddOpenAi(settings =>
        {
            settings.ApiKey = Settings.ChatpGPTToken;
        });*/
        builder.Services.AddSingleton<IAIService, GigaChatService>();
        builder.Services.AddSingleton<IRecognizeService, RecognizeService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ViewModels.MainPageViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}