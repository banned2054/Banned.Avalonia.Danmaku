namespace Banned.Avalonia.Danmaku.Models;

public sealed class DanmakuDocument
{
    public DanmakuDocument(IEnumerable<DanmakuItem> items)
    {
        Items = items
               .OrderBy(static item => item.Time)
               .ToArray();

        Duration = Items.Count == 0
            ? TimeSpan.Zero
            : Items.Max(static item =>
                            item.Time + (item.Duration > TimeSpan.Zero ? item.Duration : TimeSpan.FromSeconds(5)));
    }

    public IReadOnlyList<DanmakuItem> Items { get; }

    public TimeSpan Duration { get; }
}