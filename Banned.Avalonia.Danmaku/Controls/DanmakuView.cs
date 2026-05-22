using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Banned.Avalonia.Danmaku.Models;
using Banned.Avalonia.Danmaku.Rendering;

namespace Banned.Avalonia.Danmaku.Controls;

public sealed class DanmakuView : Canvas
{
    private const double LaneGap                        = 8;
    private const double DefaultFixedDurationMs         = 4000;
    private const double BiliPlayerWidth                = 682;
    private const double CommonScrollDurationMs         = 3800;
    private const double MaxHighDensityScrollDurationMs = 9000;
    private const long   PreferNextLaneWindowMs         = 900;

    private readonly List<DanmakuItem>    _items   = [];
    private readonly HashSet<DanmakuItem> _spawned = [];
    private readonly List<ActiveDanmaku>  _active  = [];

    private DanmakuDocument?  _source;
    private long              _lastPosition;
    private bool              _lastPlaying;
    private bool              _hasPosition;
    private int               _nextIndex;
    private double            _lanePitch = 40;
    private int               _nextRollingLane;
    private int               _nextReverseRollingLane;
    private double            _displayAreaRatio = 1.0;
    private double            _opacityRatio     = 1.0;
    private double            _fontScale        = 1.0;
    private double            _scrollSpeed      = 1.0;
    private double            _playbackRate     = 1.0;
    private string            _fontFamilyName   = "SimHei";
    private bool              _isBold           = true;
    private DanmakuTextEffect _textEffect       = DanmakuTextEffect.Heavy;

    private LaneState[] _rollingLanes        = [];
    private LaneState[] _reverseRollingLanes = [];
    private LaneState[] _topLanes            = [];
    private LaneState[] _bottomLanes         = [];

    public DanmakuView()
    {
        ClipToBounds     = true;
        Background       = Brushes.Transparent;
        IsHitTestVisible = false;
    }

    public DanmakuDocument? Source
    {
        get => _source;
        set
        {
            _source = value;
            _items.Clear();
            if (value != null)
            {
                _items.AddRange(value.Items.OrderBy(static item => item.Time));
            }

            _nextIndex = 0;
            Seek(TimeSpan.FromMilliseconds(_lastPosition));
        }
    }

    public double DisplayAreaRatio
    {
        get => _displayAreaRatio;
        set => SetPlaybackOption(ref _displayAreaRatio, Math.Clamp(value, 0.1, 1.0), affectsLayout : true);
    }

    public double OpacityRatio
    {
        get => _opacityRatio;
        set
        {
            if (SetPlaybackOption(ref _opacityRatio, Math.Clamp(value, 0.05, 1.0), affectsLayout : false))
            {
                foreach (var active in _active)
                {
                    active.Element.Opacity = _opacityRatio;
                }
            }
        }
    }

    public double FontScale
    {
        get => _fontScale;
        set => SetPlaybackOption(ref _fontScale, Math.Clamp(value, 0.5, 2.0), affectsLayout : false);
    }

    public double ScrollSpeed
    {
        get => _scrollSpeed;
        set => SetPlaybackOption(ref _scrollSpeed, Math.Clamp(value, 0.25, 4.0), affectsLayout : false);
    }

    public string FontFamilyName
    {
        get => _fontFamilyName;
        set => SetPlaybackOption(ref _fontFamilyName, string.IsNullOrWhiteSpace(value) ? "SimHei" : value,
                                 affectsLayout : false);
    }

    public bool IsBold
    {
        get => _isBold;
        set => SetPlaybackOption(ref _isBold, value, affectsLayout : false);
    }

    public DanmakuTextEffect TextEffect
    {
        get => _textEffect;
        set => SetPlaybackOption(ref _textEffect, value, affectsLayout : false);
    }

    public int DanmakuCount => _items.Count;

    public void Sync(TimeSpan position, bool isPlaying, double playbackRate = 1.0)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            _lastPosition = ToMilliseconds(position);
            _lastPlaying  = isPlaying;
            _hasPosition  = true;
            return;
        }

        var now = ToMilliseconds(position);
        _playbackRate = Math.Clamp(playbackRate, 0.05, 8.0);

        var jumped           = _hasPosition && (now < _lastPosition - 150 || Math.Abs(now - _lastPosition) > 1500);
        var playStateChanged = _hasPosition && isPlaying != _lastPlaying;

        if (!isPlaying)
        {
            if (!_hasPosition || playStateChanged || now != _lastPosition)
            {
                RebuildAt(now, animate : false);
            }
        }
        else if (jumped || playStateChanged)
        {
            RebuildAt(now, isPlaying);
        }
        else
        {
            RemoveExpired(now);
            SpawnDueDanmakus(now, animateOverride : true);
        }

        if (_nextIndex >= _items.Count && _active.Count == 0)
        {
            DrawingFinished?.Invoke(this, EventArgs.Empty);
        }

        _lastPosition = now;
        _lastPlaying  = isPlaying;
        _hasPosition  = true;
    }

    public void Seek(TimeSpan position)
    {
        var now = ToMilliseconds(position);
        _lastPosition = now;
        _hasPosition  = true;
        RebuildAt(now, _lastPlaying);
    }

    public void Clear()
    {
        Children.Clear();
        _active.Clear();
        _spawned.Clear();
        ResetLanes();
    }

    public void AddDanmaku(DanmakuItem item)
    {
        var insertAt = _items.BinarySearch(item, DanmakuTimeComparer.Instance);
        if (insertAt < 0)
        {
            insertAt = ~insertAt;
        }

        _items.Insert(insertAt, item);
        _nextIndex = Math.Min(_nextIndex, insertAt);

        if (_lastPlaying)
        {
            SpawnDueDanmakus(_lastPosition, animateOverride : true);
        }
    }

    public event EventHandler<DanmakuShownEventArgs>? DanmakuShown;
    public event EventHandler?                        DrawingFinished;

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        ResetLanes();
        RebuildAt(_lastPosition, _lastPlaying);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Clear();
    }

    private bool SetPlaybackOption<T>(ref T field, T value, bool affectsLayout)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        if (affectsLayout)
        {
            RefreshLaneAvailability();
        }

        return true;
    }

    private void RebuildAt(long position, bool animate)
    {
        Children.Clear();
        _active.Clear();
        _spawned.Clear();
        ResetLanes();
        _nextIndex = LowerBound(position - MaxDuration());

        SpawnDueDanmakus(position, animateOverride : animate);
    }

    private void SpawnDueDanmakus(long now, bool? animateOverride = null)
    {
        while (_nextIndex < _items.Count)
        {
            var item  = _items[_nextIndex];
            var start = ToMilliseconds(item.Time);
            if (start > now)
            {
                break;
            }

            _nextIndex++;

            if (_spawned.Contains(item))
            {
                continue;
            }

            var elapsed  = now - start;
            var duration = DurationOf(item);
            if (elapsed < 0 || elapsed >= duration)
            {
                continue;
            }

            _spawned.Add(item);
            Spawn(item, elapsed, animateOverride ?? _lastPlaying);
        }
    }

    private void Spawn(DanmakuItem item, long elapsed, bool animate)
    {
        if (string.IsNullOrWhiteSpace(item.Text) || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var presenter = DanmakuTextFactory.Create(item, FontScale, OpacityRatio, FontFamilyName, IsBold, TextEffect);
        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var size = presenter.DesiredSize;
        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var duration = DurationOf(item);
        var lane     = ResolveLane(item, size, duration);
        if (lane < 0)
        {
            return;
        }

        var y        = TopForLane(item.Mode, lane, size.Height);
        var movement = MovementFor(item.Mode, size.Width, y, elapsed, duration);

        SetLeft(presenter, movement.Current.X);
        SetTop(presenter, movement.Current.Y);
        Children.Add(presenter);

        _active.Add(new ActiveDanmaku(item, presenter, ToMilliseconds(item.Time) + duration));
        DanmakuShown?.Invoke(this, new DanmakuShownEventArgs(item));

        ApplyMovement(presenter, movement, animate, _playbackRate, retryIfVisualMissing : true);
    }

    private static void ApplyMovement(Control presenter, Movement movement, bool animate, double playbackRate,
                                      bool    retryIfVisualMissing)
    {
        var visual = ElementComposition.GetElementVisual(presenter);
        if (visual == null)
        {
            presenter.RenderTransform = null;
            if (retryIfVisualMissing && animate && movement.RemainingMs > 0 && movement.Current != movement.End)
            {
                Dispatcher.UIThread.Post(() => ApplyMovement(presenter, movement, animate, playbackRate,
                                                             retryIfVisualMissing : false));
            }

            return;
        }

        presenter.RenderTransform = null;
        visual.Translation        = new Vector3D(0, 0, 0);

        if (!animate || movement.RemainingMs <= 0 || movement.Current == movement.End)
        {
            return;
        }

        var delta     = movement.End - movement.Current;
        var animation = visual.Compositor.CreateVector3DKeyFrameAnimation();
        animation.Duration  = TimeSpan.FromMilliseconds(movement.RemainingMs / Math.Max(0.05, playbackRate));
        animation.Direction = PlaybackDirection.Normal;
        var linear = new LinearEasing();
        animation.InsertKeyFrame(0f, new Vector3D(0, 0, 0), linear);
        animation.InsertKeyFrame(1f, new Vector3D(delta.X, delta.Y, 0), linear);
        visual.StartAnimation("Translation", animation);
    }

    private int ResolveLane(DanmakuItem item, Size size, long duration)
    {
        return item.Mode switch
        {
            DanmakuMode.Top    => PickFixedLane(_topLanes, ToMilliseconds(item.Time), duration),
            DanmakuMode.Bottom => PickFixedLane(_bottomLanes, ToMilliseconds(item.Time), duration),
            DanmakuMode.ScrollLeftToRight => PickRollingLane(_reverseRollingLanes, ToMilliseconds(item.Time),
                                                             size.Width, duration, item.Mode,
                                                             ref _nextReverseRollingLane),
            _ => PickRollingLane(_rollingLanes, ToMilliseconds(item.Time), size.Width, duration, item.Mode,
                                 ref _nextRollingLane)
        };
    }

    private int PickFixedLane(LaneState[] lanes, long start, long duration)
    {
        var availableCount = AvailableLaneCount(lanes);
        for (var i = 0; i < availableCount; i++)
        {
            if (lanes[i].EndTime <= start)
            {
                lanes[i] = lanes[i] with { EndTime = start + duration };
                return i;
            }
        }

        return -1;
    }

    private int PickRollingLane(LaneState[] lanes, long start, double width, long duration, DanmakuMode mode,
                                ref int     nextLane)
    {
        var availableCount = AvailableLaneCount(lanes);
        if (availableCount <= 0)
        {
            return -1;
        }

        var viewportWidth = Math.Max(1, Bounds.Width);
        var speed         = (viewportWidth + width) / duration;

        var fallback = -1;
        var earliest = long.MaxValue;

        if (nextLane >= 0 && nextLane < availableCount)
        {
            var lane = lanes[nextLane];
            if (lane.Width             > 0                       &&
                start - lane.StartTime <= PreferNextLaneWindowMs &&
                lane.NextEntryTime     <= start                  &&
                !WillHitPrevious(lane, start, width, duration, speed, mode))
            {
                SetRollingLane(nextLane, start, width, duration, speed);
                nextLane = (nextLane + 1) % availableCount;
                return nextLane == 0 ? availableCount - 1 : nextLane - 1;
            }
        }

        for (var i = 0; i < availableCount; i++)
        {
            var lane = lanes[i];
            if (lane.EndTime <= start || lane.Width <= 0)
            {
                SetRollingLane(i, start, width, duration, speed);
                nextLane = (i + 1) % availableCount;
                return i;
            }
        }

        for (var offset = 0; offset < availableCount; offset++)
        {
            var i    = (nextLane + offset) % availableCount;
            var lane = lanes[i];
            if (lane.NextEntryTime <= start && !WillHitPrevious(lane, start, width, duration, speed, mode))
            {
                SetRollingLane(i, start, width, duration, speed);
                nextLane = (i + 1) % availableCount;
                return i;
            }

            if (lane.NextEntryTime < earliest)
            {
                earliest = lane.NextEntryTime;
                fallback = i;
            }
        }

        if (fallback >= 0 && earliest <= start + 120)
        {
            SetRollingLane(fallback, start, width, duration, speed);
            nextLane = fallback;
            return fallback;
        }

        return -1;

        void SetRollingLane(int index, long itemStart, double itemWidth, long itemDuration, double itemSpeed)
        {
            var gapTime = (long)Math.Ceiling((itemWidth + LaneGap) / Math.Max(0.01, itemSpeed));
            lanes[index] = new LaneState(itemStart, itemStart + itemDuration, itemStart + gapTime, itemWidth,
                                         itemSpeed);
        }
    }

    private bool WillHitPrevious(LaneState   previous, long start, double width, long duration, double speed,
                                 DanmakuMode mode)
    {
        if (start <= previous.StartTime)
        {
            return true;
        }

        if (start - previous.StartTime >= Math.Min(duration, previous.EndTime - previous.StartTime))
        {
            return false;
        }

        return HitAt(start) || HitAt(previous.EndTime);

        bool HitAt(long time)
        {
            if (time < start)
            {
                return false;
            }

            var viewportWidth   = Math.Max(1, Bounds.Width);
            var previousElapsed = Math.Max(0, time - previous.StartTime);
            var currentElapsed  = Math.Max(0, time - start);

            if (mode == DanmakuMode.ScrollLeftToRight)
            {
                var previousLeft = -previous.Width + previousElapsed * previous.Speed;
                var currentRight = -width          + currentElapsed  * speed + width;
                return currentRight > previousLeft;
            }

            var previousRight = viewportWidth - previousElapsed * previous.Speed + previous.Width;
            var currentLeft   = viewportWidth                                    - currentElapsed * speed;
            return currentLeft < previousRight;
        }
    }

    private Movement MovementFor(DanmakuMode mode, double width, double y, long elapsed, long duration)
    {
        var viewportWidth = Bounds.Width;
        var progress      = Math.Clamp(elapsed / (double)duration, 0, 1);

        if (mode is DanmakuMode.Top or DanmakuMode.Bottom)
        {
            var x = Math.Max(0, (viewportWidth - width) / 2);
            return new Movement(new Vector(x, y), new Vector(x, y), duration - elapsed);
        }

        var fromX    = mode == DanmakuMode.ScrollLeftToRight ? -width : viewportWidth;
        var toX      = mode == DanmakuMode.ScrollLeftToRight ? viewportWidth : -width;
        var currentX = fromX                                                      + (toX - fromX) * progress;
        return new Movement(new Vector(currentX, y), new Vector(toX, y), duration - elapsed);
    }

    private double TopForLane(DanmakuMode mode, int lane, double height)
    {
        var laneHeight = Math.Max(_lanePitch, height + LaneGap);
        if (mode == DanmakuMode.Bottom)
        {
            return Math.Max(0, VisibleHeight() - laneHeight * (lane + 1));
        }

        return lane * laneHeight;
    }

    private void RemoveExpired(long now)
    {
        for (var i = _active.Count - 1; i >= 0; i--)
        {
            var active = _active[i];
            if (active.EndTime > now)
            {
                continue;
            }

            Children.Remove(active.Element);
            _active.RemoveAt(i);
        }
    }

    private void ResetLanes()
    {
        var height      = Math.Max(1, VisibleHeight());
        var maxTextSize = _items.Count == 0 ? 25 : _items.Max(d => Math.Max(10, d.FontSize * FontScale));
        _lanePitch = Math.Max(32 + LaneGap, maxTextSize + 16);
        var laneCount = Math.Max(1, (int)(height / _lanePitch));

        _rollingLanes           = CreateLanes(laneCount);
        _reverseRollingLanes    = CreateLanes(laneCount);
        _topLanes               = CreateLanes(laneCount);
        _bottomLanes            = CreateLanes(laneCount);
        _nextRollingLane        = 0;
        _nextReverseRollingLane = 0;
    }

    private void RefreshLaneAvailability()
    {
        if (_rollingLanes.Length == 0)
        {
            ResetLanes();
            return;
        }

        _nextRollingLane = Math.Min(_nextRollingLane, Math.Max(0, AvailableLaneCount(_rollingLanes) - 1));
        _nextReverseRollingLane =
            Math.Min(_nextReverseRollingLane, Math.Max(0, AvailableLaneCount(_reverseRollingLanes) - 1));
    }

    private double VisibleHeight()
    {
        return Math.Max(1, Bounds.Height * DisplayAreaRatio);
    }

    private int AvailableLaneCount(LaneState[] lanes)
    {
        if (lanes.Length == 0)
        {
            return 0;
        }

        return Math.Clamp((int)(VisibleHeight() / Math.Max(1, _lanePitch)), 1, lanes.Length);
    }

    private static LaneState[] CreateLanes(int count)
    {
        return Enumerable.Range(0, count).Select(_ => LaneState.Empty).ToArray();
    }

    private int LowerBound(long time)
    {
        var left  = 0;
        var right = _items.Count;

        while (left < right)
        {
            var mid = left + ((right - left) / 2);
            if (ToMilliseconds(_items[mid].Time) < time)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }

    private long MaxDuration()
    {
        return _items.Count == 0 ? (long)DefaultFixedDurationMs : _items.Max(DurationOf);
    }

    private long DurationOf(DanmakuItem item)
    {
        if (!IsScrolling(item.Mode))
        {
            return ToMilliseconds(item.Duration > TimeSpan.Zero
                                      ? item.Duration
                                      : TimeSpan.FromMilliseconds(DefaultFixedDurationMs));
        }

        var viewportWidth = Math.Max(BiliPlayerWidth, Bounds.Width);
        var duration      = CommonScrollDurationMs * viewportWidth / BiliPlayerWidth;
        var adjusted      = duration                               / ScrollSpeed;
        return (long)Math.Clamp(adjusted, CommonScrollDurationMs / 4, MaxHighDensityScrollDurationMs * 4);
    }

    private static bool IsScrolling(DanmakuMode mode)
    {
        return mode is DanmakuMode.ScrollRightToLeft or DanmakuMode.ScrollLeftToRight;
    }

    private static long ToMilliseconds(TimeSpan value)
    {
        return Math.Max(0, (long)Math.Round(value.TotalMilliseconds));
    }

    private sealed class DanmakuTimeComparer : IComparer<DanmakuItem>
    {
        public static readonly DanmakuTimeComparer Instance = new();

        public int Compare(DanmakuItem? x, DanmakuItem? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return x.Time.CompareTo(y.Time);
        }
    }

    private readonly record struct ActiveDanmaku(DanmakuItem Source, Control Element, long EndTime);

    private readonly record struct Movement(Vector Current, Vector End, long RemainingMs);

    private readonly record struct LaneState(
        long   StartTime,
        long   EndTime,
        long   NextEntryTime,
        double Width,
        double Speed)
    {
        public static readonly LaneState Empty = new(0, 0, 0, 0, 0);
    }
}

public sealed class DanmakuShownEventArgs(DanmakuItem danmaku) : EventArgs
{
    public DanmakuItem Danmaku { get; } = danmaku;
}