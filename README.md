# Banned.Avalonia.Danmaku

[简体中文](https://github.com/banned2054/Banned.Avalonia.Danmaku/blob/master/Docs/README.zh-CN.md)

Avalonia danmaku control library for rendering Bilibili-style comments over video or any layered UI.

This project is an Avalonia/.NET reimplementation inspired by the Java [`DanmakuFlameMaster`](https://github.com/bilibili/DanmakuFlameMaster) project.

The library is designed as a reusable control, not a media player. Your app owns playback, timeline, pause state, seek, and video rendering. `Banned.Avalonia.Danmaku` owns danmaku parsing, lane allocation, rendering, and rebuilding the on-screen danmaku state after seek.

## Features

- Right-to-left scrolling danmaku
- Left-to-right scrolling danmaku
- Top fixed danmaku
- Bottom fixed danmaku
- Bilibili XML parsing for normal danmaku
- Skips Bilibili advanced/code danmaku (`type=7` and `type=8`)
- Transparent Avalonia control suitable for overlay layers
- External timeline sync through `Sync(...)`
- Seek support through `Seek(...)`
- Runtime options for display area, opacity, font scale, speed, font family, bold text, and text effect

## Basic Usage

Add the control between your video layer and your overlay/control layer:

```xml
<Panel xmlns:danmaku="clr-namespace:Banned.Avalonia.Danmaku.Controls;assembly=Banned.Avalonia.Danmaku">
    <!-- Video layer -->
    <local:VideoView />

    <!-- Danmaku layer -->
    <danmaku:DanmakuView x:Name="DanmakuLayer"
                         IsHitTestVisible="False" />

    <!-- Input and player controls layer -->
    <local:PlayerOverlay />
</Panel>
```

Load danmaku from Bilibili XML:

```csharp
using Banned.Avalonia.Danmaku.Parsing;

DanmakuLayer.Source = BilibiliXmlDanmakuParser.ParseFile(xmlPath);
DanmakuLayer.Seek(currentPosition);
```

Sync from your player timer:

```csharp
DanmakuLayer.Sync(
    position: currentPosition,
    isPlaying: isPlaying,
    playbackRate: playbackRate);
```

Call `Seek(...)` after user-initiated seeking:

```csharp
DanmakuLayer.Seek(targetPosition);
```

Clear danmaku:

```csharp
DanmakuLayer.Clear();
DanmakuLayer.Source = null;
```

## Core API

```csharp
public sealed class DanmakuView : Canvas
{
    public DanmakuDocument? Source { get; set; }

    public double DisplayAreaRatio { get; set; }
    public double OpacityRatio { get; set; }
    public double FontScale { get; set; }
    public double MinimumFontSize { get; set; }
    public Size ReferenceViewportSize { get; set; }
    public bool AutoScaleToViewport { get; set; }
    public double ScrollSpeed { get; set; }
    public string FontFamilyName { get; set; }
    public bool IsBold { get; set; }
    public DanmakuTextEffect TextEffect { get; set; }

    public void Sync(TimeSpan position, bool isPlaying, double playbackRate = 1.0);
    public void Seek(TimeSpan position);
    public void Clear();
}
```

## Data Model

```csharp
public sealed class DanmakuDocument
{
    public IReadOnlyList<DanmakuItem> Items { get; }
    public TimeSpan Duration { get; }
}

public sealed class DanmakuItem
{
    public TimeSpan Time { get; init; }
    public DanmakuMode Mode { get; init; }
    public string Text { get; init; }
    public uint Color { get; init; }
    public double FontSize { get; init; }
    public TimeSpan Duration { get; init; }
}
```

## Platform Notes

The library itself only depends on Avalonia and standard .NET APIs. It does not depend on Windows-specific APIs or a specific video player.

The demo project in this repository is a desktop Avalonia app. For Android or iOS, create an Avalonia mobile host app and reference this library from there.

## Demo

`Banned.Avalonia.Danmaku.Demo` is a demo app that references `Banned.Avalonia.Danmaku`.
Its bundled `Assets/Danmaku/comments.xml` sample is copied to `Danmaku/comments.xml` beside the demo executable and is kept as a sample danmaku file from the Java `DanmakuFlameMaster` project.

```powershell
dotnet run --project Banned.Avalonia.Danmaku.Demo/Banned.Avalonia.Danmaku.Demo.csproj
```
