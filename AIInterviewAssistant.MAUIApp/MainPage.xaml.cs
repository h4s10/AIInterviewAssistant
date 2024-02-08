using AIInterviewAssistant.MAUIApp.ViewModels;

namespace AIInterviewAssistant.MAUIApp;

public partial class MainPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}