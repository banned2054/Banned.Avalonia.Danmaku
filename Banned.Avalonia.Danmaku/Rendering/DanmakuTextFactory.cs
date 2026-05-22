using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Banned.Avalonia.Danmaku.Models;

namespace Banned.Avalonia.Danmaku.Rendering;

internal static class DanmakuTextFactory
{
    public static Control Create(
        DanmakuItem       item,
        double            fontScale,
        double            opacity,
        string            fontFamilyName,
        bool              isBold,
        DanmakuTextEffect textEffect)
    {
        var fontSize   = Math.Max(10, item.FontSize * fontScale);
        var foreground = new SolidColorBrush(ToColor(item.Color));
        var fontFamily = new FontFamily(fontFamilyName);
        var fontWeight = isBold ? FontWeight.Bold : FontWeight.Normal;
        var padding    = Math.Max(2, fontSize * 0.12);

        var panel = new Grid
        {
            IsHitTestVisible = false,
            Opacity          = opacity
        };

        foreach (var shadow in ShadowOffsetsFor(textEffect))
        {
            panel.Children.Add(CreateTextLayer(
                                               item.Text,
                                               new SolidColorBrush(Color.FromArgb(shadow.Alpha, 0, 0, 0)),
                                               fontSize,
                                               fontFamily,
                                               fontWeight,
                                               padding,
                                               shadow.X,
                                               shadow.Y));
        }

        panel.Children.Add(CreateTextLayer(item.Text, foreground, fontSize, fontFamily, fontWeight, padding, 0, 0));
        return panel;
    }

    private static TextBlock CreateTextLayer(
        string     text,
        IBrush     foreground,
        double     fontSize,
        FontFamily fontFamily,
        FontWeight fontWeight,
        double     padding,
        double     offsetX,
        double     offsetY)
    {
        return new TextBlock
        {
            Text             = text,
            Foreground       = foreground,
            FontSize         = fontSize,
            FontFamily       = fontFamily,
            FontWeight       = fontWeight,
            TextWrapping     = TextWrapping.NoWrap,
            IsHitTestVisible = false,
            Margin           = new Thickness(padding),
            RenderTransform  = new TranslateTransform(offsetX, offsetY)
        };
    }

    private static IEnumerable<ShadowOffset> ShadowOffsetsFor(DanmakuTextEffect effect)
    {
        return effect switch
        {
            DanmakuTextEffect.Outline =>
            [
                new ShadowOffset(-1.5, -1.5, 230),
                new ShadowOffset(0, -1.5, 230),
                new ShadowOffset(1.5, -1.5, 230),
                new ShadowOffset(-1.5, 0, 230),
                new ShadowOffset(1.5, 0, 230),
                new ShadowOffset(-1.5, 1.5, 230),
                new ShadowOffset(0, 1.5, 230),
                new ShadowOffset(1.5, 1.5, 230)
            ],
            DanmakuTextEffect.Shadow45 => [new ShadowOffset(2.4, 2.4, 210)],
            _ =>
            [
                new ShadowOffset(-1, 0, 245),
                new ShadowOffset(1, 0, 245),
                new ShadowOffset(0, -1, 245),
                new ShadowOffset(0, 1, 245)
            ]
        };
    }

    private static Color ToColor(uint raw)
    {
        if ((raw & 0xFF000000) == 0)
        {
            return Color.FromRgb((byte)((raw >> 16) & 0xFF), (byte)((raw >> 8) & 0xFF), (byte)(raw & 0xFF));
        }

        return Color.FromArgb((byte)((raw >> 24) & 0xFF), (byte)((raw >> 16) & 0xFF), (byte)((raw >> 8) & 0xFF),
                              (byte)(raw         & 0xFF));
    }

    private readonly record struct ShadowOffset(double X, double Y, byte Alpha);
}