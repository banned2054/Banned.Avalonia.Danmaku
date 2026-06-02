# Changelog

## 🚀 Release v0.1.0 — Viewport-Aware Danmaku Scaling

**Release Date:** 2026-06-02

This release improves danmaku rendering behavior when the host viewport changes size, especially for video players that resize dynamically or map the danmaku layer to the actual video area.

---

### ✨ Added

* **Viewport-Based Auto Scaling**
  - Added `ReferenceViewportSize` for defining the base viewport used by danmaku scaling
  - Added `AutoScaleToViewport` to automatically scale danmaku rendering from the current `DanmakuView.Bounds`
  - Scales font size, lane pitch, lane gaps, and scroll timing from the same viewport reference

* **Readable Minimum Font Size**
  - Added `MinimumFontSize` to keep danmaku readable in small viewports
  - Applies the minimum size consistently to both text rendering and lane height estimation
  - Scales text outline and shadow effects from the final rendered font size

### 🔧 Changed

* Updated the package version to `0.1.0`
* Lowered the minimum Avalonia dependency to `12.0.0` for broader compatibility within Avalonia 12
* Updated README API listings for the new scaling options

---

## 🚀 Release v0.0.1 — Initial Avalonia Danmaku Control

**Release Date:** 2026-05-22

This is the first public release of **Banned.Avalonia.Danmaku**, an Avalonia/.NET danmaku control library inspired by the Java [`DanmakuFlameMaster`](https://github.com/bilibili/DanmakuFlameMaster) project.

---

### ✨ Added

* **Reusable Avalonia Danmaku Control**
  - Added `DanmakuView` for rendering danmaku over video or any layered Avalonia UI
  - Supports right-to-left scrolling, left-to-right scrolling, top fixed, and bottom fixed danmaku
  - Supports transparent overlay usage without taking ownership of video playback

* **Bilibili XML Parsing**
  - Added parser support for normal Bilibili XML danmaku
  - Skips advanced and code danmaku types (`type=7` and `type=8`)
  - Provides `DanmakuDocument` and `DanmakuItem` data models for application-side integration

* **Timeline Integration**
  - Added external playback synchronization through `Sync(...)`
  - Added `Seek(...)` support for rebuilding on-screen danmaku state after timeline changes
  - Added runtime `AddDanmaku(...)` support for live danmaku insertion

* **Rendering Options**
  - Added display area, opacity, font scale, scroll speed, font family, bold text, and text effect options
  - Added lane allocation for scrolling and fixed-position danmaku

* **Demo Application**
  - Added an Avalonia desktop demo project
  - Included a sample danmaku XML file from the Java `DanmakuFlameMaster` project with attribution

---

### 📦 Notes

This is an initial `0.0.1` release intended for early integration and feedback.  
The package focuses on being a reusable danmaku overlay control; media playback, timeline ownership, and video rendering remain the host application's responsibility.
