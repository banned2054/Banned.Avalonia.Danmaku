# Demo Danmaku Assets

`comments.xml` is the bundled sample danmaku file used by the demo app.

The file is kept as a sample from the Java `DanmakuFlameMaster` project. During
build, the demo project copies it to `Danmaku/comments.xml` next to the
executable, and `Program.DanmakuXmlPath` loads that runtime copy by default.
