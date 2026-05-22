using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Banned.Avalonia.Danmaku.Models;
using Banned.Avalonia.Danmaku.Parsing;
using LibraryDanmakuTextEffect = Banned.Avalonia.Danmaku.Rendering.DanmakuTextEffect;

namespace Banned.Avalonia.Danmaku.Demo;

public partial class MainWindow : Window
{
    private readonly Random _random = new();
    private readonly Stopwatch _playbackClock = new();
    private System.Timers.Timer? _addDanmakuTimer;
    private DispatcherTimer? _progressTimer;
    private bool _isDraggingSlider;
    private bool _isPlaying;
    private long _positionWhenClockStarted;
    private long _pausedPosition;
    private long _totalDurationMs;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;

        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _progressTimer.Tick += ProgressTimer_Tick;
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (_isDraggingSlider)
        {
            return;
        }

        var currentTime = GetCurrentPosition();
        if (currentTime > _totalDurationMs && _totalDurationMs > 0)
        {
            currentTime = _totalDurationMs;
            PausePlayback(currentTime);
        }

        DanmakuControl.Sync(TimeSpan.FromMilliseconds(currentTime), _isPlaying);

        TimeSlider.Value = currentTime;
        CurrentTimeText.Text = FormatTime(currentTime);
    }

    private static string FormatTime(long ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
    }

    private long GetCurrentPosition()
    {
        return _isPlaying ? _positionWhenClockStarted + _playbackClock.ElapsedMilliseconds : _pausedPosition;
    }

    private void BeginPlayback(long position)
    {
        _pausedPosition = Math.Max(0, position);
        _positionWhenClockStarted = _pausedPosition;
        _playbackClock.Restart();
        _isPlaying = true;
        _progressTimer?.Start();
        DanmakuControl.Sync(TimeSpan.FromMilliseconds(_pausedPosition), isPlaying: true);
    }

    private void PausePlayback(long? position = null)
    {
        _pausedPosition = Math.Max(0, position ?? GetCurrentPosition());
        _playbackClock.Stop();
        _isPlaying = false;
        DanmakuControl.Sync(TimeSpan.FromMilliseconds(_pausedPosition), isPlaying: false);
    }

    private void TimeSlider_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _isDraggingSlider = true;
    }

    private void TimeSlider_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDraggingSlider = false;
        _pausedPosition = (long)TimeSlider.Value;
        DanmakuControl.Seek(TimeSpan.FromMilliseconds(_pausedPosition));

        if (_isPlaying)
        {
            BeginPlayback(_pausedPosition);
        }
    }

    private void TimeSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (!_isDraggingSlider)
        {
            return;
        }

        _pausedPosition = (long)e.NewValue;
        CurrentTimeText.Text = FormatTime(_pausedPosition);
        DanmakuControl.Seek(TimeSpan.FromMilliseconds(_pausedPosition));
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        LoadXmlFileAndPlay();
    }

    private void StartButton_Click(object? sender, RoutedEventArgs e)
    {
        DanmakuControl.Seek(TimeSpan.Zero);
        BeginPlayback(0);
    }

    private void PauseButton_Click(object? sender, RoutedEventArgs e)
    {
        PausePlayback();
    }

    private void ResumeButton_Click(object? sender, RoutedEventArgs e)
    {
        BeginPlayback(_pausedPosition);
    }

    private void ClearButton_Click(object? sender, RoutedEventArgs e)
    {
        PausePlayback(0);
        DanmakuControl.Source = new DanmakuDocument([]);
        SetupProgress(DanmakuControl.Source);
    }

    private void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        AddRandomDanmaku();
    }

    private void AutoAddButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            ToggleAutoAdd(button);
        }
    }

    private void LoadXmlButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadXmlFile();
    }

    private void DisplayAreaSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DanmakuControl == null || DisplayAreaText == null) return;

        DanmakuControl.DisplayAreaRatio = e.NewValue / 100.0;
        DisplayAreaText.Text = $"{e.NewValue:0}%";
    }

    private void OpacitySlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DanmakuControl == null || OpacityText == null) return;

        DanmakuControl.OpacityRatio = e.NewValue / 100.0;
        OpacityText.Text = $"{e.NewValue:0}%";
    }

    private void FontScaleSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DanmakuControl == null || FontScaleText == null) return;

        DanmakuControl.FontScale = e.NewValue / 100.0;
        FontScaleText.Text = $"{e.NewValue:0}%";
    }

    private void SpeedSlider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DanmakuControl == null || SpeedText == null) return;

        DanmakuControl.ScrollSpeed = e.NewValue / 100.0;
        SpeedText.Text = $"{e.NewValue / 100.0:0.00}x";
    }

    private void FontComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DanmakuControl == null) return;

        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Content is string fontName)
        {
            DanmakuControl.FontFamilyName = fontName;
        }
    }

    private void BoldCheckBox_Changed(object? sender, RoutedEventArgs e)
    {
        if (DanmakuControl == null) return;

        if (sender is CheckBox checkBox)
        {
            DanmakuControl.IsBold = checkBox.IsChecked == true;
        }
    }

    private void EffectComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DanmakuControl == null) return;

        var selectedIndex = sender is ComboBox comboBox ? comboBox.SelectedIndex : 0;
        DanmakuControl.TextEffect = selectedIndex switch
        {
            1 => LibraryDanmakuTextEffect.Outline,
            2 => LibraryDanmakuTextEffect.Shadow45,
            _ => LibraryDanmakuTextEffect.Heavy
        };
    }

    private void LoadXmlFileAndPlay()
    {
        LoadXmlFile();
        BeginPlayback(0);
    }

    private void SetupProgress(DanmakuDocument? document)
    {
        var times = document?.Items.Select(static item => (long)item.Time.TotalMilliseconds).ToList() ?? [];
        var maxTime = times.Count == 0 ? 0 : times.Max();

        _totalDurationMs = maxTime + 5000;
        TimeSlider.Maximum = _totalDurationMs;
        TimeSlider.Value = 0;
        TotalTimeText.Text = FormatTime(_totalDurationMs);

        TimeMarker.TotalTime = _totalDurationMs;
        TimeMarker.DanmakuTimes = times;
    }

    private void LoadSampleDanmakus()
    {
        var items = new List<DanmakuItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(CreateRandomDanmaku(i * 500));
        }

        var document = new DanmakuDocument(items);
        DanmakuControl.Source = document;
        SetupProgress(document);

        Console.WriteLine($"已加载示例弹幕，共 {DanmakuControl.DanmakuCount} 条");
    }

    private void LoadXmlFile()
    {
        var xmlPath = Program.DanmakuXmlPath;
        if (!File.Exists(xmlPath))
        {
            Console.WriteLine($"未找到XML文件: {xmlPath}");
            LoadSampleDanmakus();
            return;
        }

        try
        {
            var document = BilibiliXmlDanmakuParser.ParseFile(xmlPath);
            DanmakuControl.Source = document;
            SetupProgress(document);
            _pausedPosition = 0;
            DanmakuControl.Seek(TimeSpan.Zero);

            Console.WriteLine($"已加载XML文件: {xmlPath}");
            Console.WriteLine($"共解析出 {DanmakuControl.DanmakuCount} 条弹幕");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载XML失败: {ex.Message}");
            LoadSampleDanmakus();
        }
    }

    private DanmakuItem CreateRandomDanmaku(long timeMs)
    {
        var texts = new[]
        {
            "Hello World!",
            "Avalonia Danmaku",
            "This is a test",
            "Banned.Avalonia.Danmaku",
            "Nice to meet you!",
            "Awesome!",
            "Great job!",
            "Keep it up!",
            "Well done!",
            "Congratulations!"
        };

        var colors = new[]
        {
            0x00FFFFFFu,
            0x000000FFu,
            0x0000FF00u,
            0x00FFFF00u,
            0x00FF00FFu,
            0x0000FFFFu,
            0x00FFA500u,
            0x00800080u,
            0x00FFC0CBu
        };

        var mode = _random.Next(4) switch
        {
            1 => DanmakuMode.ScrollLeftToRight,
            2 => DanmakuMode.Top,
            3 => DanmakuMode.Bottom,
            _ => DanmakuMode.ScrollRightToLeft
        };

        return new DanmakuItem
        {
            Time = TimeSpan.FromMilliseconds(Math.Max(0, timeMs)),
            Mode = mode,
            Text = texts[_random.Next(texts.Length)],
            Color = colors[_random.Next(colors.Length)],
            FontSize = 20 + _random.Next(15),
            Duration = TimeSpan.FromMilliseconds(3000 + _random.Next(2000))
        };
    }

    private void AddRandomDanmaku(long delay = 0)
    {
        DanmakuControl.AddDanmaku(CreateRandomDanmaku(GetCurrentPosition() + delay));
    }

    private void ToggleAutoAdd(Button button)
    {
        if (_addDanmakuTimer == null)
        {
            _addDanmakuTimer = new System.Timers.Timer(500);
            _addDanmakuTimer.Elapsed += (_, _) =>
            {
                Dispatcher.UIThread.Post(() => AddRandomDanmaku());
            };
            _addDanmakuTimer.Start();
            button.Content = "Auto Add: ON";
        }
        else
        {
            _addDanmakuTimer.Stop();
            _addDanmakuTimer.Dispose();
            _addDanmakuTimer = null;
            button.Content = "Auto Add: OFF";
        }
    }
}
