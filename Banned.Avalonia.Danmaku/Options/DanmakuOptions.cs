using Banned.Avalonia.Danmaku.Rendering;

namespace Banned.Avalonia.Danmaku.Options;

public sealed class DanmakuOptions
{
    public double            DisplayAreaRatio { get; set; } = 1.0;
    public double            OpacityRatio     { get; set; } = 1.0;
    public double            FontScale        { get; set; } = 1.0;
    public double            ScrollSpeed      { get; set; } = 1.0;
    public string            FontFamilyName   { get; set; } = "SimHei";
    public bool              IsBold           { get; set; } = true;
    public DanmakuTextEffect TextEffect       { get; set; } = DanmakuTextEffect.Heavy;
}