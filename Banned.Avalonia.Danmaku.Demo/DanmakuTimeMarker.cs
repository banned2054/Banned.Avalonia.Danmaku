using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Banned.Avalonia.Danmaku.Demo;

public class DanmakuTimeMarker : Control
{
    public static readonly StyledProperty<IReadOnlyList<long>> DanmakuTimesProperty =
        AvaloniaProperty.Register<DanmakuTimeMarker, IReadOnlyList<long>>(nameof(DanmakuTimes));

    public IReadOnlyList<long> DanmakuTimes
    {
        get => GetValue(DanmakuTimesProperty);
        set => SetValue(DanmakuTimesProperty, value);
    }

    public static readonly StyledProperty<long> TotalTimeProperty =
        AvaloniaProperty.Register<DanmakuTimeMarker, long>(nameof(TotalTime));

    public long TotalTime
    {
        get => GetValue(TotalTimeProperty);
        set => SetValue(TotalTimeProperty, value);
    }

    static DanmakuTimeMarker()
    {
        AffectsRender<DanmakuTimeMarker>(DanmakuTimesProperty, TotalTimeProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var times = DanmakuTimes;
        var total = TotalTime;

        if (times == null || times.Count == 0 || total <= 0)
            return;

        var width  = Bounds.Width;
        var height = Bounds.Height;
        var pen    = new Pen(new SolidColorBrush(Colors.Red, 0.5), 1);

        foreach (var time in times)
        {
            if (time < 0 || time > total) continue;

            var x = (time / (double)total) * width;
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }
    }
}
