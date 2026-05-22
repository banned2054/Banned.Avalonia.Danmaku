using Avalonia;

namespace Banned.Avalonia.Danmaku.Demo;

internal class Program
{
    public static string DanmakuXmlPath { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, "Danmaku", "comments.xml");

    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            DanmakuXmlPath = Path.GetFullPath(args[0]);
        }

        BuildAvaloniaApp()
           .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .LogToTrace();
}