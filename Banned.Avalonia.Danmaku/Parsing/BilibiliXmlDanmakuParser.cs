using Banned.Avalonia.Danmaku.Models;
using System.Globalization;
using System.Xml;

namespace Banned.Avalonia.Danmaku.Parsing;

public static class BilibiliXmlDanmakuParser
{
    public static DanmakuDocument ParseFile(string path)
    {
        return Parse(File.ReadAllText(path));
    }

    public static DanmakuDocument Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return new DanmakuDocument([]);
        }

        var items    = new List<DanmakuItem>();
        var document = new XmlDocument();
        document.LoadXml(xml);

        var nodes = document.SelectNodes("//i/d");
        if (nodes == null)
        {
            return new DanmakuDocument(items);
        }

        foreach (XmlNode node in nodes)
        {
            var item = ParseNode(node);
            if (item != null)
            {
                items.Add(item);
            }
        }

        return new DanmakuDocument(items);
    }

    private static DanmakuItem? ParseNode(XmlNode node)
    {
        var p    = node.Attributes?["p"]?.Value;
        var text = node.InnerText?.Trim();
        if (string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = p.Split(',');
        if (parts.Length < 4)
        {
            return null;
        }

        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return null;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawMode))
        {
            return null;
        }

        var mode = (DanmakuMode)rawMode;
        if (mode is DanmakuMode.Advanced or DanmakuMode.Code)
        {
            return null;
        }

        if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fontSize))
        {
            return null;
        }

        if (!uint.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var color))
        {
            return null;
        }

        return new DanmakuItem
        {
            Time     = TimeSpan.FromSeconds(Math.Max(0, seconds)),
            Mode     = mode,
            Text     = text,
            FontSize = fontSize,
            Color    = color
        };
    }
}