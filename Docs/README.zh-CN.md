# Banned.Avalonia.Danmaku

[English](https://github.com/banned2054/Banned.Avalonia.Danmaku/blob/master/README.md)

用于在视频或任意分层 UI 上渲染 Bilibili 风格弹幕的 Avalonia 弹幕控件库。

本项目是受 Java[`DanmakuFlameMaster`](https://github.com/bilibili/DanmakuFlameMaster) 项目启发的Avalonia/.NET 复刻实现。

这个库被设计为可复用控件，而不是媒体播放器。你的应用负责播放、时间轴、暂停状态、seek 和视频渲染。`Banned.Avalonia.Danmaku` 负责弹幕解析、轨道分配、渲染，以及 seek 后重建屏幕上的弹幕状态。

## Features

- 从右向左滚动弹幕
- 从左向右滚动弹幕
- 顶部固定弹幕
- 底部固定弹幕
- 支持解析 Bilibili XML 中的普通弹幕
- 跳过 Bilibili 高级弹幕和代码弹幕（`type=7` 和 `type=8`）
- 透明 Avalonia 控件，适合放在 overlay 层
- 通过 `Sync(...)` 跟随外部时间轴
- 通过 `Seek(...)` 支持跳转后重建画面
- 支持运行时调整显示区域、透明度、字号缩放、速度、字体、加粗和文字效果

## Basic Usage

把控件放在视频层和 overlay/control 层之间：

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

从 Bilibili XML 加载弹幕：

```csharp
using Banned.Avalonia.Danmaku.Parsing;

DanmakuLayer.Source = BilibiliXmlDanmakuParser.ParseFile(xmlPath);
DanmakuLayer.Seek(currentPosition);
```

在播放器同步计时器中同步：

```csharp
DanmakuLayer.Sync(
    position: currentPosition,
    isPlaying: isPlaying,
    playbackRate: playbackRate);
```

用户 seek 后调用 `Seek(...)`：

```csharp
DanmakuLayer.Seek(targetPosition);
```

清空弹幕：

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

库本身只依赖 Avalonia 和标准 .NET API。它不依赖 Windows 专属 API，也不依赖特定视频播放器。

本仓库中的 demo 项目是桌面 Avalonia 应用。如果要在 Android 或 iOS 使用，可以创建 Avalonia 移动端宿主应用，然后引用这个库。

## Demo

`Banned.Avalonia.Danmaku.Demo` 是引用 `Banned.Avalonia.Danmaku` 的 demo 应用。
内置的 `Assets/Danmaku/comments.xml` 示例会被复制到 demo 可执行文件旁边的
`Danmaku/comments.xml`，该文件保留为来自 Java `DanmakuFlameMaster` 项目的示例弹幕文件。

```powershell
dotnet run --project Banned.Avalonia.Danmaku.Demo/Banned.Avalonia.Danmaku.Demo.csproj
```
