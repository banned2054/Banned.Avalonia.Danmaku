namespace Banned.Avalonia.Danmaku.Models;

public sealed class DanmakuItem
{
    public TimeSpan    Time     { get; init; }
    public DanmakuMode Mode     { get; init; } = DanmakuMode.ScrollRightToLeft;
    public string      Text     { get; init; } = "";
    public uint        Color    { get; init; } = 0x00FFFFFF;
    public double      FontSize { get; init; } = 25;
    public TimeSpan    Duration { get; init; } = TimeSpan.Zero;
}