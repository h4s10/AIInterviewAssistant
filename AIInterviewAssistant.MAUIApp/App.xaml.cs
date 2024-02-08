namespace AIInterviewAssistant.MAUIApp;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(mainPage);
    }
    
    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        const int newWidth = 400;
        const int newHeight = 800;
        window.Width = newWidth;
        window.Height = newHeight;
        return window;
    }
}