using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using AIInterviewAssistant.MAUIApp.ChatGPT.Interfaces;
using AIInterviewAssistant.MAUIApp.Models;
using AIInterviewAssistant.MAUIApp.Services.Interfaces;
using NAudio.Wave;
using SharpHook;
using SharpHook.Native;

namespace AIInterviewAssistant.MAUIApp.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly IRecognizeService _recognizeService;
    private readonly IAIService _aiService;
    private IWaveIn _micCapture;
    private WasapiLoopbackCapture _desktopCapture;
    private WaveFileWriter? _desktopAudioWriter;
    private WaveFileWriter? _micAudioWriter;
    private bool _inProgress;

    public MainPageViewModel(IRecognizeService recognizeService, IAIService aiService)
    {
        LoadModelCommand = new Command(execute: () =>
        {
            ModelLoading = true;
            RefreshCanExecute();
            LoadModel();
        }, canExecute: () => !ModelLoading && !string.IsNullOrWhiteSpace(Position) && !string.IsNullOrWhiteSpace(ModelPath));
        SendPromptCommand = new Command(execute: () =>
            {
                SendQuestionAsync();
            });
        var hook = new TaskPoolGlobalHook();
        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        hook.RunAsync();
        _recognizeService = recognizeService;
        _aiService = aiService;
        PrepareDesktopRecording();
        PrepareMicRecording();
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _inputPrompt, _outputPrompt, _modelPath, _statusText, _position;
    private bool _modelLoading, _modelLoaded;

    public ICommand LoadModelCommand { get; }
    public ICommand SendPromptCommand { get; }
    
    public bool ModelLoading
    {
        get => _modelLoading;
        set
        {
            if (_modelLoading != value)
                SetField(ref _modelLoading, value);
        }
    }
    
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
                SetField(ref _statusText, value);
        }
    }
    
    public string ModelPath
    {
        get => _modelPath;
        set
        {
            if (_modelPath != value)
                SetField(ref _modelPath, value);
        }
    }
    
    public string InputPrompt
    {
        get => _inputPrompt;
        set
        {
            if (_inputPrompt != value)
                SetField(ref _inputPrompt, value);
        }
    }

    public string OutputPrompt
    {
        get => _outputPrompt;
        set
        {
            if (_outputPrompt != value)
                SetField(ref _outputPrompt, value);
        }
    }
    
    public string Position
    {
        get => _position;
        set
        {
            if (_position != value)
                SetField(ref _position, value);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        RefreshCanExecute();
        return true;
    }

    private async Task SendQuestionAsync()
    {
        if (!string.IsNullOrWhiteSpace(InputPrompt) && _modelLoaded)
            OutputPrompt = await _aiService.SendQuestionAsync(InputPrompt);
    }
    
    private async Task LoadModel()
    {
        if (!string.IsNullOrWhiteSpace(ModelPath))
        {
            try
            {
                StatusText = "model is loading, please wait...";
                await Task.Run(() => _recognizeService.LoadModel(ModelPath));
                StatusText = "model loaded";
                var authSuccess = await _aiService.AuthAsync();
                if (!authSuccess)
                {
                    StatusText = "AI helper auth fail";
                }
                else
                {
                    await _aiService.SendQuestionAsync(
                        string.Format(Preferences.Default.Get("InitialPromptTemplate", string.Empty), Position));
                }

                _modelLoaded = true;
            }
            catch
            {
                _modelLoaded = false;
                RefreshCanExecute();
            }

            ModelLoading = false;
        }
    }

    private void RefreshCanExecute()
    {
        ((LoadModelCommand as Command)!).ChangeCanExecute();
    }

    public void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!_modelLoaded)
            return;
        
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeft:
                //record audio desktop
                if (_inProgress)
                    break;
                
                _inProgress = true;
                PrepareDesktopRecording();
                var outputDesktopFilePath = GetTempFileName();
                _desktopAudioWriter = new WaveFileWriter(outputDesktopFilePath, _desktopCapture.WaveFormat);
                _desktopCapture.DataAvailable += (sender, e) =>
                {
                    _desktopAudioWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    if (_desktopAudioWriter.Position > _desktopCapture.WaveFormat.AverageBytesPerSecond * Preferences.Default.Get("MaximumRecordLengthInSeconds", 0))
                    {
                        _desktopCapture.StopRecording();
                    }
                };

                _desktopCapture.RecordingStopped += async (sender, e) =>
                {
                    _desktopCapture.Dispose();
                    await _desktopAudioWriter.DisposeAsync();
                    _desktopAudioWriter = null;
                    await RecognizeSpeechAsync(outputDesktopFilePath);
                };

                _desktopCapture.StartRecording();
                break;
            case KeyCode.VcRight:
                //record audio mic
                if (_inProgress)
                    break;
                
                _inProgress = true;
                PrepareMicRecording();
                var outputMicFilePath = GetTempFileName();
                _micAudioWriter = new WaveFileWriter(outputMicFilePath, _micCapture.WaveFormat);
                _micCapture.DataAvailable += (sender, e) =>
                {
                    _micAudioWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    if (_micAudioWriter.Position > _micCapture.WaveFormat.AverageBytesPerSecond * Preferences.Default.Get("MaximumRecordLengthInSeconds", 0))
                    {
                        _micCapture.StopRecording();
                    }
                };

                _micCapture.RecordingStopped += async (sender, e) =>
                {
                    _micCapture.Dispose();
                    await _micAudioWriter.DisposeAsync();
                    _micAudioWriter = null;
                    await RecognizeSpeechAsync(outputMicFilePath);
                };

                _micCapture.StartRecording();
                break;
        }
    }

    private async Task RecognizeSpeechAsync(string audioFilePath)
    {
        var result = await _recognizeService.RecognizeSpeechAsync(audioFilePath);
        var recognizeSpeech = JsonSerializer.Deserialize<RecognizeSpeechDto>(result);
        try
        {
            InputPrompt = recognizeSpeech?.Text ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(InputPrompt))
                await SendQuestionAsync();
        }
        catch
        {
            // ignored
        }
    }

    public void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeft:
                //stop audio desktop
                _inProgress = false;
                _desktopCapture.StopRecording();
                break;
            case KeyCode.VcRight:
                //stop audio mic
                _inProgress = false;
                _micCapture.StopRecording();
                break;
        }
    }
    
    private string GetTempFileName()
    {
        var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
        Directory.CreateDirectory(outputFolder);
        return Path.Combine(outputFolder, $"{Guid.NewGuid()}.wav");
    }

    private void PrepareMicRecording()
    {
        var waveIn = new WaveInEvent();
        waveIn.DeviceNumber = -1; // default system device
        waveIn.WaveFormat = new WaveFormat(16000, 1);
        _micCapture = waveIn;
    }

    private void PrepareDesktopRecording()
    {
        var capture = new WasapiLoopbackCapture();
        capture.WaveFormat = new WaveFormat(16000, 1);
        _desktopCapture = capture;
    }
}