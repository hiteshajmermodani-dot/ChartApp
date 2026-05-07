using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChartAppLib.Models;
using ChartAppLib.ViewportManagers;

namespace ChartAppLib.Controls
{
    /// <summary>Custom WPF chart control supporting multiple chart types, multi-axis, zoom, pan, and annotations.</summary>
    public partial class ChartControl : UserControl
    {
        private readonly TranslateTransform _seriesDragTransform = new();

        // Cached series Image elements for scatter / bubble / histogram highlight support
        private readonly List<Image> _seriesImages = [];

        // Cached series Path elements for transform-based axis drag
        private readonly List<Path> _seriesPaths = [];
        private readonly TrackerData _trackerData = new();

        // Tracker line elements (on overlay, survive redraws)
        private readonly Line? _trackerLine;
        private readonly Border? _trackerTooltip;

        // Container for axis lines/ticks/labels — placed in PART_AxesCanvas (no zoom transform)
        private Canvas? _axisContainer;

        // Interaction state
        private Point _axisDragStart;
        private bool _axisRedrawPending;

        // Axis-drag throttling: redraws only axes/grid (cheap) while series are shifted via RenderTransform
        private System.Windows.Threading.DispatcherTimer? _axisRedrawTimer;
        private double _cachedAllValuesMax;

        // Cached series value ranges to avoid repeated SelectMany/Min/Max
        private double _cachedAllValuesMin;
        private bool _cachedAllValuesValid;

        // Cached axis ranges to avoid recalculating on every tracker update
        private Dictionary<string, (double min, double max, double range)>? _cachedAxisRanges;
        private long _cachedAxisRangesVersion;

        // Cached total data point count for adaptive throttle intervals
        private int _cachedTotalPoints;
        private long _cachedTotalPointsVersion = -1;

        // Current chart type
        private ChartType _chartType = ChartType.LinePlot;
        private long _currentDataVersion;

        // Double-click reset enabled state
        private bool _doubleClickResetEnabled = true;
        private LineAnnotation? _draggedLine;
        private Line? _draggedLineShape;
        private bool _dragRedrawPending;

        // Throttle support for axis drag redraws
        private System.Windows.Threading.DispatcherTimer? _dragRedrawTimer;

        // Highlighted series name — null means no highlight active
        private string? _highlightedSeriesName;

        // Line dragging state
        private bool _isDraggingLine;
        private bool _isDraggingXAxis;
        private bool _isDraggingYAxis;

        // Lock state — prevents zoom, pan, and axis drag
        private bool _isLocked;
        private bool _isPanning;

        // Scrollbar state
        private bool _isScrollbarUpdating = false; // Prevent feedback loop when syncing scrollbars with pan
        private bool _isZooming;
        private Point? _lastTrackerPosition;

        // Manipulation (trackpad pinch) state
        private bool _manipulationUndoPushed;
        private Point _panStart;
        private Point? _pendingTrackerPosition;
#pragma warning disable CS0169 // The field is never used
        private bool _renderingHooked;
#pragma warning restore CS0169

        // Clipping container for series paths — stays fixed while paths are shifted via RenderTransform
        private Canvas? _seriesCanvas;

        // Show/hide legend state
        private bool _showLegend = true;

        // Show/hide X-axis label state
        private bool _showXAxisLabel = true;

        // Show/hide Y-axis label state
        private bool _showYAxisLabel = true;

        // Custom-template host: shown instead of _trackerTooltip when TrackerTooltipTemplate is set
        private ContentPresenter? _trackerContentPresenter;

        // Frame-synced tracker line: stores desired X so it can be applied on CompositionTarget.Rendering
#pragma warning disable CS0414 // The field is assigned but its value is never used
        private double _trackerLineTargetX = double.NaN;
#pragma warning restore CS0414
        private bool _trackerLineVisible;

        // Tracker throttling for performance with large datasets
        private System.Windows.Threading.DispatcherTimer? _trackerThrottleTimer;
        private bool _trackerUpdatePending;

        // Axis offset properties for shifting ticks and series
        private double _xAxisOffset = 0;
        private double _xAxisOffsetStart = 0;
        private double _yAxisOffset = 0;
        private double _yAxisOffsetStart = 0;
        private Point _zoomStart;

        private double _zoomStartLeft;
        private double _zoomStartTop;

        // Debounce timer for capturing undo state at the end of a zoom gesture
        private System.Windows.Threading.DispatcherTimer? _zoomUndoTimer;

        public static readonly DependencyProperty ViewportManagerProperty =
            DependencyProperty.Register(nameof(ViewportManager),
                                        typeof(ChartViewportManager),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnViewportManagerChanged));

        public ChartControl()
        {
            // Initialize bindable commands before InitializeComponent so they're available for binding
            ToggleTrackerCommand = new ChartCommand(() => ExecuteToggleTracker());
            ToggleLegendCommand = new ChartCommand(() => ExecuteToggleLegend());
            ToggleMajorGridCommand = new ChartCommand(() => ExecuteToggleMajorGrid());
            ToggleMinorGridCommand = new ChartCommand(() => ExecuteToggleMinorGrid());
            ResetViewCommand = new ChartCommand(() => ExecuteResetView());
            UndoCommand = new ChartCommand(() => Undo(), () => _undoStack.Count > 0);
            RedoCommand = new ChartCommand(() => Redo(), () => _redoStack.Count > 0);
            ToggleLockCommand = new ChartCommand(() => ExecuteToggleLock());
            ToggleDoubleClickResetCommand = new ChartCommand(() => ExecuteToggleDoubleClickReset());

            ViewportManager = new DefaultChartViewportManager();

            InitializeComponent();

            SizeChanged +=
                (_, __) => Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);

            // Also listen to PART_PlotArea size changes to catch internal layout redistribution
            // (e.g., when legend populates and causes Row 0 to shrink on first load)
            PART_PlotArea.SizeChanged +=
                (_, __) => Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);

            PART_Canvas.MouseWheel += Canvas_MouseWheel;
            PART_Canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            PART_Canvas.MouseMove += Canvas_MouseMove;
            PART_Canvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
            PART_Canvas.MouseEnter += Canvas_MouseEnter;
            PART_Canvas.MouseLeave += Canvas_MouseLeave;
            PART_Canvas.ManipulationStarting += Canvas_ManipulationStarting;

            PART_Canvas.ManipulationDelta += Canvas_ManipulationDelta;
            PART_Canvas.ManipulationCompleted += Canvas_ManipulationCompleted;

            // Add keyboard support for axis movement
            KeyDown += LineChartControl_KeyDown;
            Focusable = true;

            // Create tracker elements on overlay (persistent across redraws)
            _trackerLine = new Line
                           {
                               Stroke = Brushes.Gray,
                               StrokeThickness = 1,
                               Visibility = Visibility.Collapsed,
                               IsHitTestVisible = false
                           };

            Panel.SetZIndex(_trackerLine, 900);

            _trackerTooltip = new Border
                              {
                                  Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                                  BorderBrush = Brushes.Gray,
                                  BorderThickness = new Thickness(1),
                                  CornerRadius = new CornerRadius(4),
                                  Padding = new Thickness(8, 5, 8, 5),
                                  Visibility = Visibility.Collapsed,
                                  IsHitTestVisible = false,
                                  Child = new StackPanel { Name = "TrackerContent" }
                              };

            Panel.SetZIndex(_trackerTooltip, 901);

            PART_Overlay.Children.Add(_trackerLine);
            PART_Overlay.Children.Add(_trackerTooltip);

            // Custom-template presenter — shown in place of _trackerTooltip when TrackerTooltipTemplate is set
            _trackerContentPresenter = new ContentPresenter
                                       {
                                           Content = _trackerData,
                                           IsHitTestVisible = false,
                                           Visibility = Visibility.Collapsed
                                       };

            Panel.SetZIndex(_trackerContentPresenter, 901);
            PART_Overlay.Children.Add(_trackerContentPresenter);

            // Hook render-frame callback for frame-perfect tracker line positioning
            CompositionTarget.Rendering += OnCompositionTargetRendering;
            Unloaded += (_, _) => CompositionTarget.Rendering -= OnCompositionTargetRendering;

            // Apply zoom rectangle DP defaults to the XAML element
            ZoomRectangle.Fill = ZoomRectangleFill;
            ZoomRectangle.Stroke = ZoomRectangleStroke;
            ZoomRectangle.StrokeThickness = ZoomRectangleStrokeThickness;
            ZoomRectangle.StrokeDashArray = ZoomRectangleStrokeDashArray;
        }

        /// <summary>Whether the chart interaction is locked.</summary>
        public bool IsLocked
        {
            get => _isLocked;
            set => _isLocked = value;
        }

        /// <summary>Whether double-click resets the chart view.</summary>
        public bool DoubleClickResetEnabled
        {
            get => _doubleClickResetEnabled;
            set => _doubleClickResetEnabled = value;
        }

        /// <summary>Whether the legend is visible.</summary>
        public bool ShowLegend
        {
            get => _showLegend;
            set
            {
                _showLegend = value;
                PART_Legend.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>Whether the X-axis label is visible.</summary>
        public bool ShowXAxisLabel
        {
            get => _showXAxisLabel;
            set
            {
                if (_showXAxisLabel != value)
                {
                    _showXAxisLabel = value;
                    Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Render);
                    NotifyToolbarStateChanged();
                }
            }
        }

        /// <summary>Whether the Y-axis label is visible.</summary>
        public bool ShowYAxisLabel
        {
            get => _showYAxisLabel;
            set
            {
                if (_showYAxisLabel != value)
                {
                    _showYAxisLabel = value;
                    Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Render);
                    NotifyToolbarStateChanged();
                }
            }
        }

        /// <summary>Gets or sets the current chart visualization type.</summary>
        public ChartType ChartType
        {
            get => _chartType;
            set
            {
                if (_chartType != value)
                {
                    _chartType = value;
                    ResetZoomAndAxis();
                    Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Render);
                    NotifyToolbarStateChanged();
                }
            }
        }

        /// <summary>Gets or sets the viewport manager that controls zoom, pan, and axis offsets.
        /// Assign a subclass of <see cref="DefaultChartViewportManager"/> to customize
        /// interaction behavior.  Defaults to <see cref="DefaultChartViewportManager"/>.</summary>
        public ChartViewportManager ViewportManager
        {
            get => (ChartViewportManager)GetValue(ViewportManagerProperty);
            set => SetValue(ViewportManagerProperty, value);
        }

        private static void OnViewportManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ChartControl chart)
            {
                return;
            }

            if (e.OldValue is ChartViewportManager oldMgr)
            {
                oldMgr.ViewportChanged -= chart.OnManagerViewportChanged;
            }

            if (e.NewValue is ChartViewportManager newMgr)
            {
                newMgr.ViewportChanged += chart.OnManagerViewportChanged;
                chart.SyncManagerFromTransforms();
            }
        }

        /// <summary>Raised whenever the viewport changes (zoom, pan, axis shift, reset).
        /// Useful for linking multiple charts or displaying a viewport indicator.</summary>
        public event EventHandler? ViewportChanged;

        private void OnManagerViewportChanged(object? sender, EventArgs e)
        {
            ViewportChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Copies manager state → WPF transforms and axis-offset fields.</summary>
        internal void SyncTransformsFromManager()
        {
            var m = ViewportManager;
            ZoomTransform.ScaleX = m.ScaleX;
            ZoomTransform.ScaleY = m.ScaleY;
            ZoomTransform.CenterX = m.CenterX;
            ZoomTransform.CenterY = m.CenterY;
            PanTransform.X = m.PanX;
            PanTransform.Y = m.PanY;
            _xAxisOffset = m.XAxisOffset;
            _yAxisOffset = m.YAxisOffset;

            // Update scrollbars to reflect new zoom/pan state
            UpdateScrollbars();
        }

        /// <summary>Copies WPF transforms and axis-offset fields → manager
        /// (e.g. after an animation has finished).</summary>
        internal void SyncManagerFromTransforms()
        {
            if (ZoomTransform == null || PanTransform == null)
            {
                return;
            }

            ViewportManager?.RestoreState(
                                          ZoomTransform.ScaleX, ZoomTransform.ScaleY,
                                          ZoomTransform.CenterX, ZoomTransform.CenterY,
                                          PanTransform.X, PanTransform.Y,
                                          _xAxisOffset, _yAxisOffset);
        }

        private void ResetZoomAndAxis()
        {
            _xAxisOffset = 0;
            _yAxisOffset = 0;

            if (YAxes != null)
            {
                foreach (var y in YAxes)
                {
                    y.MinValue = null;
                    y.MaxValue = null;
                }
            }

            if (XAxes != null)
            {
                foreach (var x in XAxes)
                {
                    x.MinValue = null;
                    x.MaxValue = null;
                }
            }

            // Reset scrollbars
            UpdateScrollbars();
        }

        /// <summary>Returns the theme-aware axis brush.</summary>
        private Brush GetAxisBrush()
        {
            return (Brush)FindResource("ChartAxisBrush");
        }

        /// <summary>Raised when toolbar-relevant state changes, so external toolbars can sync.</summary>
        public event EventHandler? ToolbarStateChanged;

        /// <summary>Notify any attached toolbar that state has changed.</summary>
        public void NotifyToolbarStateChanged()
        {
            ToolbarStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void InvalidateAllValuesCache()
        {
            _cachedAllValuesValid = false;
        }

        /// <summary>
        /// Called every frame by WPF's composition engine. Reads the LIVE mouse position
        /// (bypassing coalesced MouseMove events) and applies it to the tracker line,
        /// guaranteeing zero-lag visual feedback regardless of dispatcher backlog.
        /// </summary>
        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            if (_trackerLine == null)
            {
                return;
            }

            if (!_trackerLineVisible)
            {
                if (_trackerLine.Visibility == Visibility.Visible)
                {
                    _trackerLine.Visibility = Visibility.Collapsed;
                }

                return;
            }

            // Read the REAL mouse position at this exact render frame — bypasses event coalescing
            var mousePos = Mouse.GetPosition(PART_Overlay);
            var width = PART_Overlay.ActualWidth;
            var height = PART_Overlay.ActualHeight;

            if (height <= 0 || width      <= 0
                            || mousePos.X < 0 || mousePos.X > width
                            || mousePos.Y < 0 || mousePos.Y > height)
            {
                if (_trackerLineVisible)
                {
                    _trackerLineVisible = false;
                    HideTracker();
                }

                return;
            }

            _trackerLine.X1 = mousePos.X;
            _trackerLine.X2 = mousePos.X;
            _trackerLine.Y1 = 0;
            _trackerLine.Y2 = height;
            _trackerLine.Visibility = Visibility.Visible;
        }

        private (double min, double max) GetAllValuesMinMax()
        {
            if (!_cachedAllValuesValid && Series is { Count: > 0 })
            {
                var min = double.MaxValue;
                var max = double.MinValue;

                foreach (var s in Series)
                {
                    foreach (var v in s.YValues)
                    {
                        if (v < min)
                        {
                            min = v;
                        }

                        if (v > max)
                        {
                            max = v;
                        }
                    }
                }

                _cachedAllValuesMin = min;
                _cachedAllValuesMax = max;
                _cachedAllValuesValid = true;
            }

            return (_cachedAllValuesMin, _cachedAllValuesMax);
        }

        private void ThrottledDrawChart()
        {
            if (_dragRedrawPending)
            {
                return;
            }

            _dragRedrawPending = true;

            _dragRedrawTimer ??= new System.Windows.Threading.DispatcherTimer
                                 {
                                     Interval = TimeSpan.FromMilliseconds(16) // ~60fps
                                 };

            _dragRedrawTimer.Tick += OnDragRedrawTick;
            _dragRedrawTimer.Start();
        }

        private void OnDragRedrawTick(object? sender, EventArgs e)
        {
            _dragRedrawTimer!.Stop();
            _dragRedrawTimer.Tick -= OnDragRedrawTick;
            _dragRedrawPending = false;
            DrawChart();
        }

        private void ThrottledRedrawAxesOnly()
        {
            if (_axisRedrawPending)
            {
                return;
            }

            _axisRedrawPending = true;

            var interval = GetAdaptiveAxisRedrawInterval();

            if (_axisRedrawTimer == null)
            {
                _axisRedrawTimer = new System.Windows.Threading.DispatcherTimer
                                   {
                                       Interval = TimeSpan.FromMilliseconds(interval)
                                   };
            }
            else
            {
                _axisRedrawTimer.Interval = TimeSpan.FromMilliseconds(interval);
            }

            _axisRedrawTimer.Tick += OnAxisRedrawTick;
            _axisRedrawTimer.Start();
        }

        /// <summary>
        /// Returns an adaptive axis redraw interval (ms) based on total data point count.
        /// </summary>
        private int GetAdaptiveAxisRedrawInterval()
        {
            var totalPoints = GetTotalDataPointCount();

            return totalPoints switch
                   {
                       > 1_000_000 => 64, // 1M+ points: ~15fps
                       > 500_000   => 48, // 500K+ points: ~20fps
                       > 100_000   => 40, // 100K+ points: ~25fps
                       _           => 32  // Default: ~30fps
                   };
        }

        /// <summary>
        /// Returns the total number of data points across all series.
        /// Cached per data version to avoid repeated enumeration.
        /// </summary>
        private int GetTotalDataPointCount()
        {
            if (_cachedTotalPointsVersion != _currentDataVersion)
            {
                _cachedTotalPoints = 0;

                if (Series is { Count: > 0 })
                {
                    foreach (var s in Series)
                    {
                        _cachedTotalPoints += s.YValues.Count;
                    }
                }

                _cachedTotalPointsVersion = _currentDataVersion;
            }

            return _cachedTotalPoints;
        }

        private void OnAxisRedrawTick(object? sender, EventArgs e)
        {
            _axisRedrawTimer!.Stop();
            _axisRedrawTimer.Tick -= OnAxisRedrawTick;
            _axisRedrawPending = false;
            RedrawAxesOnly();
        }

        /// <summary>
        /// Lightweight redraw that only updates axes, grid lines, and annotations.
        /// Series Path elements are preserved and shifted via RenderTransform during drag.
        /// </summary>
        private void RedrawAxesOnly()
        {
            if (Series == null || Series.Count == 0 || YAxes == null || XAxes == null)
            {
                return;
            }

            var width = PART_Canvas.ActualWidth;
            var height = PART_Canvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Remove only non-series elements (axes, grid, annotations, markers)
            // Keep the cached _seriesPaths and their clipping container
            var keepSet = new HashSet<UIElement>(_seriesPaths);

            if (_seriesCanvas != null)
            {
                keepSet.Add(_seriesCanvas);
            }

            var toRemove = new List<UIElement>();

            foreach (UIElement child in PART_Canvas.Children)
            {
                if (!keepSet.Contains(child))
                {
                    toRemove.Add(child);
                }
            }

            foreach (var item in toRemove)
            {
                PART_Canvas.Children.Remove(item);
            }

            // Redraw axes, grid, annotations (lightweight — no StreamGeometry rebuild)
            if (YAxes.Count > 0 && XAxes.Count > 0)
            {
                var yAxisRanges = new Dictionary<string, (double min, double max, double range)>();
                var xAxisRanges = new Dictionary<string, (double min, double max, int maxPoints)>();

                // Reuse cached axis ranges to avoid O(n) scans during interactive axis drags
                RebuildAxisRangesCacheIfNeeded();

                foreach (var yAxis in YAxes)
                {
                    if (_cachedAxisRanges != null && _cachedAxisRanges.TryGetValue(yAxis.Id, out var cached))
                    {
                        yAxisRanges[yAxis.Id] = cached;
                    }
                    else
                    {
                        var axisRange = ComputeAxisRange(yAxis.Id);
                        yAxisRanges[yAxis.Id] = axisRange;
                    }
                }

                foreach (var xAxis in XAxes)
                {
                    var seriesForAxis = Series
                                        .Where(s => s.XAxisId == xAxis.Id ||
                                                    (string.IsNullOrEmpty(s.XAxisId) && xAxis == XAxes.First()))
                                        .ToList();

                    if (seriesForAxis.Count > 0)
                    {
                        var maxPoints = seriesForAxis.Max(s => s.YValues.Count);
                        var xMin = xAxis.MinValue ?? 0;
                        var xMax = xAxis.MaxValue ?? maxPoints - 1;
                        xAxisRanges[xAxis.Id] = (xMin, xMax, maxPoints);
                    }
                }

                // Draw Y-axes
                double leftOffset = 0, rightOffset = 0;
                int leftIdx = 0, rightIdx = 0;

                foreach (var yAxis in YAxes)
                {
                    if (!yAxisRanges.ContainsKey(yAxis.Id))
                    {
                        continue;
                    }

                    var (min, max, range) = yAxisRanges[yAxis.Id];
                    var maxLabelWidth = MeasureYAxisLabelWidth(yAxis, min, max, range);
                    var axisWidth = maxLabelWidth + 8;
                    double showLabelExtra = yAxis.ShowLabel && !string.IsNullOrEmpty(yAxis.Label) ? 16 : 0;

                    if (yAxis.Position == YAxisPosition.Left)
                    {
                        if (leftIdx > 0)
                        {
                            leftOffset += 5; // 5px gap ensures adequate spacing even when labels are hidden
                        }

                        DrawYAxis(yAxis, height, width, min, max, range, -leftOffset - 2, yAxis.LabelBrush,
                                  maxLabelWidth);

                        leftOffset += axisWidth + showLabelExtra;
                        leftIdx++;
                    }
                    else
                    {
                        if (rightIdx > 0)
                        {
                            rightOffset += 5; // 5px gap ensures adequate spacing even when labels are hidden
                        }

                        DrawYAxis(yAxis, height, width, min, max, range, width + rightOffset + 10, yAxis.LabelBrush,
                                  maxLabelWidth);

                        rightOffset += axisWidth + showLabelExtra;
                        rightIdx++;
                    }
                }

                // Draw X-axes
                double bottomOff = 0;

                foreach (var xAxis in XAxes)
                {
                    if (!xAxisRanges.ContainsKey(xAxis.Id))
                    {
                        continue;
                    }

                    var (_, _, maxPts) = xAxisRanges[xAxis.Id];

                    if (xAxis.Position == XAxisPosition.Bottom)
                    {
                        DrawXAxis(xAxis, width, height, maxPts, height + bottomOff, xAxis.LabelBrush);
                        bottomOff += 50;
                    }
                }

                // Redraw grid
                if (YAxes.Count > 0 && yAxisRanges.ContainsKey(YAxes[0].Id) && YAxes[0].ShowGridLines)
                {
                    DrawYAxisGridLines(height, width, yAxisRanges[YAxes[0].Id].min, yAxisRanges[YAxes[0].Id].max,
                                       YAxes[0].GridLineBrush, YAxes[0].MajorTickCount);
                }

                if (XAxes.Count > 0 && xAxisRanges.ContainsKey(XAxes[0].Id) && XAxes[0].ShowGridLines)
                {
                    DrawXAxisGridLines(width, height, XAxes[0].GridLineBrush, XAxes[0].MajorTickCount);
                }

                // Redraw annotations
                DrawAnnotations(width, height, yAxisRanges, xAxisRanges);
            }
        }

        /// <summary>
        /// Public method to trigger a chart redraw (e.g., after live data is appended to series).
        /// </summary>
        public void RefreshChart()
        {
            DrawChart();
            UpdateTrackerAtLastPosition();
        }

        /// <summary>
        /// Highlights a series by name, dimming all others.
        /// The highlight persists across redraws until <see cref="ClearHighlight"/> is called.
        /// </summary>
        /// <param name="seriesName">The <see cref="DataSeries.Name"/> to highlight.</param>
        public void HighlightSeries(string seriesName)
        {
            _highlightedSeriesName = seriesName;
            ApplyHighlight();
        }

        /// <summary>
        /// Highlights a series by its zero-based index, dimming all others.
        /// The highlight persists across redraws until <see cref="ClearHighlight"/> is called.
        /// </summary>
        /// <param name="seriesIndex">Zero-based index into <see cref="Series"/>.</param>
        public void HighlightSeries(int seriesIndex)
        {
            if (Series == null || seriesIndex < 0 || seriesIndex >= Series.Count)
            {
                return;
            }

            _highlightedSeriesName = Series[seriesIndex].Name;
            ApplyHighlight();
        }

        /// <summary>
        /// Restores all series to full opacity, clearing any active highlight.
        /// </summary>
        public void ClearHighlight()
        {
            _highlightedSeriesName = null;
            ApplyHighlight();
        }

        private void ApplyHighlight()
        {
            if ((_seriesPaths.Count == 0 && _seriesImages.Count == 0) || Series == null)
            {
                return;
            }

            for (var i = 0; i < _seriesPaths.Count && i < Series.Count; i++)
            {
                _seriesPaths[i].Opacity = _highlightedSeriesName == null || Series[i].Name == _highlightedSeriesName
                                              ? 1.0
                                              : 0.15;
            }

            for (var i = 0; i < _seriesImages.Count && i < Series.Count; i++)
            {
                _seriesImages[i].Opacity = _highlightedSeriesName == null || Series[i].Name == _highlightedSeriesName
                                               ? 1.0
                                               : 0.15;
            }

            // Sync legend item opacity to match series highlight state
            foreach (FrameworkElement child in PART_Legend.Children)
            {
                if (child is StackPanel { Tag: string name })
                {
                    child.Opacity = _highlightedSeriesName == null || name == _highlightedSeriesName ? 1.0 : 0.4;
                }
            }
        }

        /// <summary>
        /// Copies the chart as a bitmap image to the clipboard.
        /// </summary>
        public void CopyChartImageToClipboard()
        {
            var bmp = RenderChartToBitmap();
            Clipboard.SetImage(bmp);
        }

        /// <summary>
        /// Renders the chart to a RenderTargetBitmap.
        /// </summary>
        public RenderTargetBitmap RenderChartToBitmap()
        {
            var width = (int)ActualWidth;
            var height = (int)ActualHeight;

            if (width <= 0 || height <= 0)
            {
                width = height = 1;
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(this);

            return rtb;
        }

        /// <summary>
        /// Copies all X and Y values of all series to the clipboard as CSV.
        /// </summary>
        public void CopyXYValuesToClipboard()
        {
            if (Series == null || Series.Count == 0)
            {
                return;
            }

            var sb = new StringBuilder();

            foreach (var series in Series)
            {
                sb.AppendLine($"Series: {series.Name}");
                sb.AppendLine("X,Y");

                for (int i = 0; i < series.YValues.Count; i++)
                {
                    var x = series.XValues != null && i < series.XValues.Count ? series.XValues[i] : i;
                    var y = series.YValues[i];
                    sb.AppendLine($"{x},{y}");
                }

                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private void DrawChart()
        {
            PART_Canvas.Children.Clear();
            PART_AxesCanvas.Children.Clear();
            PART_Legend.Children.Clear();
            _seriesPaths.Clear();
            _seriesImages.Clear();
            _seriesDragTransform.X = 0;
            _seriesDragTransform.Y = 0;

            // The X-axis tracker marker was in PART_AxesCanvas — reset so it gets recreated
            _trackerXAxisMarker = null;
            _trackerXAxisMarkerText = null;

            // Draw 3D surface plot
            if (ChartType == ChartType.Surface3DPlot && Surface3DSeries != null)
            {
                DrawSurface3D();

                return;
            }

            // Draw 3D line chart
            if (ChartType == ChartType.Line3DPlot && Line3DData != null)
            {
                DrawLine3D();

                return;
            }

            HideSurface3D();

            var outerSize = GetOuterGridSize();

            if (outerSize.Width <= 0 || outerSize.Height <= 0)
            {
                return;
            }

            double width, height;

            // Draw axes and grid for box plot, then draw the boxes
            if (ChartType == ChartType.BoxPlot && BoxPlotSeries != null && YAxes != null && XAxes != null)
            {
                // Compute margins and derive canvas size from outer grid minus margins.
                var boxMargin = UpdateDynamicMargins();
                var boxParentSize = GetOuterGridSize();
                width = boxParentSize.Width   - boxMargin.Left - boxMargin.Right;
                height = boxParentSize.Height - boxMargin.Top  - boxMargin.Bottom;

                if (width <= 0 || height <= 0)
                {
                    return;
                }

                // Draw axes and grid lines
                DrawChartWithMultipleAxes(width, height);
                // Draw box plots
                DrawBoxPlotSeries();

                return;
            }

            if (Series == null || Series.Count == 0)
            {
                return;
            }

            // Compute margins and derive canvas size from outer grid minus margins.
            var seriesMargin = UpdateDynamicMargins();
            var canvasParentSize = GetOuterGridSize();
            width = canvasParentSize.Width   - seriesMargin.Left - seriesMargin.Right;
            height = canvasParentSize.Height - seriesMargin.Top  - seriesMargin.Bottom;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            // Create a clipping container for series so they don't overflow during axis drag
            _seriesCanvas = new Canvas
                            {
                                Width = width,
                                Height = height,
                                ClipToBounds = true
                            };

            // ZIndex 50: above grid lines/bands (ZIndex 0) but below annotations (ZIndex 400+)
            Panel.SetZIndex(_seriesCanvas, 50);
            PART_Canvas.Children.Add(_seriesCanvas);

            // Use multiple axes if defined, otherwise fall back to legacy single-axis behavior
            if (YAxes is { Count: > 0 } && XAxes is { Count: > 0 })
            {
                DrawChartWithMultipleAxes(width, height);
            }
            else
            {
                DrawChartLegacy(width, height);
            }

            ApplyHighlight();
        }

        /// <summary>
        /// Returns the size of the outer grid that hosts PART_PlotArea, used to compute
        /// canvas dimensions from margins without requiring a layout pass.
        /// </summary>
        private Size GetOuterGridSize()
        {
            if (PART_PlotArea.Parent is FrameworkElement { ActualWidth: > 0 } parent)
            {
                return new Size(parent.ActualWidth, parent.ActualHeight);
            }

            // Fallback: use the control's own size (less accurate but never blocks layout)
            return new Size(ActualWidth, ActualHeight);
        }

        private void DrawBoxPlotSeries()
        {
            if (BoxPlotSeries == null || YAxes == null || YAxes.Count == 0)
            {
                return;
            }

            var boxPlotList = BoxPlotSeries.Cast<BoxPlotData>().ToList();
            var width = PART_Canvas.ActualWidth;
            var height = PART_Canvas.ActualHeight;
            var boxWidth = width / boxPlotList.Count * 0.6; // 60% of slot width
            var slotWidth = width                    / boxPlotList.Count;
            var yAxis = YAxes[0];
            var yMin = yAxis.MinValue ?? 0;
            var yMax = yAxis.MaxValue ?? 1;
            var yRange = yMax - yMin;
            // Use theme axis brush for category label
            var categoryLabelBrush = GetAxisBrush();

            for (var i = 0; i < boxPlotList.Count; i++)
            {
                var box = boxPlotList[i];
                var sorted = box.Values.OrderBy(v => v).ToList();

                if (sorted.Count == 0)
                {
                    continue;
                }

                var min = sorted.First();
                var max = sorted.Last();
                var q1 = Percentile(sorted, 0.25);
                var median = Percentile(sorted, 0.5);
                var q3 = Percentile(sorted, 0.75);
                var iqr = q3 - q1;
                var whiskerLow = sorted.FirstOrDefault(v => v >= q1 - 1.5 * iqr);
                var whiskerHigh = sorted.LastOrDefault(v => v <= q3 + 1.5 * iqr);

                var centerX = i * slotWidth + slotWidth                     / 2;
                var yQ1 = height            - (q1          - yMin) / yRange * height;
                var yQ3 = height            - (q3          - yMin) / yRange * height;
                var yMed = height           - (median      - yMin) / yRange * height;
                var yLow = height           - (whiskerLow  - yMin) / yRange * height;
                var yHigh = height          - (whiskerHigh - yMin) / yRange * height;

                // Box
                var boxRect = new Rectangle
                              {
                                  Width = boxWidth,
                                  Height = Math.Abs(yQ1 - yQ3),
                                  Stroke = Brushes.SteelBlue,
                                  StrokeThickness = 2,
                                  Fill = new SolidColorBrush(Colors.SteelBlue) { Opacity = 0.2 },
                                  IsHitTestVisible = false
                              };

                Canvas.SetLeft(boxRect, centerX - boxWidth / 2);
                Canvas.SetTop(boxRect, Math.Min(yQ1, yQ3));
                PART_Canvas.Children.Add(boxRect);

                // Median line
                var medLine = new Line
                              {
                                  X1 = centerX - boxWidth / 2,
                                  X2 = centerX + boxWidth / 2,
                                  Y1 = yMed,
                                  Y2 = yMed,
                                  Stroke = Brushes.SteelBlue,
                                  StrokeThickness = 3,
                                  IsHitTestVisible = false
                              };

                PART_Canvas.Children.Add(medLine);

                // Whiskers
                var whiskerLine = new Line
                                  {
                                      X1 = centerX,
                                      X2 = centerX,
                                      Y1 = yHigh,
                                      Y2 = yQ3,
                                      Stroke = Brushes.SteelBlue,
                                      StrokeThickness = 2,
                                      StrokeDashArray = [2, 2],
                                      IsHitTestVisible = false
                                  };

                PART_Canvas.Children.Add(whiskerLine);

                var whiskerLine2 = new Line
                                   {
                                       X1 = centerX,
                                       X2 = centerX,
                                       Y1 = yLow,
                                       Y2 = yQ1,
                                       Stroke = Brushes.SteelBlue,
                                       StrokeThickness = 2,
                                       StrokeDashArray = [2, 2],
                                       IsHitTestVisible = false
                                   };

                PART_Canvas.Children.Add(whiskerLine2);

                // Whisker caps
                var capW = boxWidth * 0.4;

                PART_Canvas.Children.Add(new Line
                                         {
                                             X1 = centerX - capW / 2,
                                             X2 = centerX + capW / 2,
                                             Y1 = yHigh,
                                             Y2 = yHigh,
                                             Stroke = Brushes.SteelBlue,
                                             StrokeThickness = 2,
                                             IsHitTestVisible = false
                                         });

                PART_Canvas.Children.Add(new Line
                                         {
                                             X1 = centerX - capW / 2,
                                             X2 = centerX + capW / 2,
                                             Y1 = yLow,
                                             Y2 = yLow,
                                             Stroke = Brushes.SteelBlue,
                                             StrokeThickness = 2,
                                             IsHitTestVisible = false
                                         });

                // Outliers
                var outliers = sorted.Where(v => v < whiskerLow || v > whiskerHigh).ToList();

                foreach (var v in outliers)
                {
                    var yOut = height - (v - yMin) / yRange * height;

                    var dot = new Ellipse
                              {
                                  Width = 6,
                                  Height = 6,
                                  Fill = Brushes.SteelBlue,
                                  IsHitTestVisible = false
                              };

                    Canvas.SetLeft(dot, centerX - 3);
                    Canvas.SetTop(dot, yOut     - 3);
                    PART_Canvas.Children.Add(dot);
                }

                // Category label
                var label = new TextBlock
                            {
                                Text = box.Category,
                                FontSize = 12,
                                Foreground = categoryLabelBrush,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };

                Canvas.SetLeft(label, centerX - 30);
                Canvas.SetTop(label, height   - 20);
                PART_Canvas.Children.Add(label);
            }
        }

        private void ContextMenu_SaveScreenshot_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
                      {
                          Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|Bitmap Image (*.bmp)|*.bmp",
                          FileName = "ChartScreenshot.png"
                      };

            if (dlg.ShowDialog() == true)
            {
                var bmp = RenderChartToBitmap();

                BitmapEncoder encoder = dlg.FilterIndex switch
                                        {
                                            2 => new JpegBitmapEncoder(),
                                            3 => new BmpBitmapEncoder(),
                                            _ => new PngBitmapEncoder()
                                        };

                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using var fs = System.IO.File.OpenWrite(dlg.FileName);
                encoder.Save(fs);
            }
        }

        private void ContextMenu_CopyImage_Click(object sender, RoutedEventArgs e)
        {
            CopyChartImageToClipboard();
        }

        private void ContextMenu_CopyXY_Click(object sender, RoutedEventArgs e)
        {
            CopyXYValuesToClipboard();
        }

        private void UpdateTrackerTooltip(Point position)
        {
            if (_trackerTooltip == null)
            {
                return;
            }

            var contentPanel = _trackerTooltip.Child as StackPanel;
            contentPanel?.Children.Clear();

            if (ChartType == ChartType.BoxPlot)
            {
                var boxPlotSeries = BoxPlotSeries;

                if (boxPlotSeries != null)
                {
                    var boxPlotList = boxPlotSeries.Cast<BoxPlotData>().ToList();
                    var width = PART_Canvas.ActualWidth;
                    var boxWidth = width / boxPlotList.Count;
                    var boxIndex = (int)(position.X / boxWidth);

                    if (boxIndex >= 0 && boxIndex < boxPlotList.Count)
                    {
                        var box = boxPlotList[boxIndex];
                        var sorted = box.Values.OrderBy(v => v).ToList();
                        var min = sorted.FirstOrDefault();
                        var max = sorted.LastOrDefault();
                        var q1 = Percentile(sorted, 0.25);
                        var median = Percentile(sorted, 0.5);
                        var q3 = Percentile(sorted, 0.75);
                        var iqr = q3 - q1;
                        var whiskerLow = sorted.FirstOrDefault(v => v >= q1 - 1.5 * iqr);
                        var whiskerHigh = sorted.LastOrDefault(v => v <= q3 + 1.5 * iqr);
                        var outliers = sorted.Where(v => v < whiskerLow || v > whiskerHigh).ToList();

                        contentPanel?.Children.Add(new TextBlock { Text = $"Category: {box.Category}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Min: {min:0.##}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Q1: {q1:0.##}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Median: {median:0.##}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Q3: {q3:0.##}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Max: {max:0.##}" });

                        if (outliers.Count > 0)
                        {
                            contentPanel?.Children.Add(new TextBlock
                                                       {
                                                           Text =
                                                               $"Outliers: {string.Join(", ", outliers.Select(o => o.ToString("0.##")))}"
                                                       });
                        }
                    }
                }
            }
            else
            {
                // Standard chart: find nearest data point
                if (Series is { Count: > 0 })
                {
                    var minDist = double.MaxValue;
                    double? nearestX = null, nearestY = null;

                    foreach (var series in Series)
                    {
                        for (var i = 0; i < series.YValues.Count; i++)
                        {
                            var x = series.XValues != null && i < series.XValues.Count ? series.XValues[i] : i;
                            var y = series.YValues[i];
                            // You may want to convert data X to screen X here
                            var dist = Math.Abs(position.X - x); // Simplified: assumes X is pixel

                            if (dist < minDist)
                            {
                                minDist = dist;
                                nearestX = x;
                                nearestY = y;
                            }
                        }
                    }

                    if (nearestX != null && nearestY != null)
                    {
                        contentPanel?.Children.Add(new TextBlock { Text = $"X: {nearestX:0.##}" });
                        contentPanel?.Children.Add(new TextBlock { Text = $"Y: {nearestY:0.##}" });
                    }
                }
            }

            _trackerTooltip.Visibility = Visibility.Visible;
        }

        // Helper for box plot percentiles
        private static double Percentile(List<double> sorted, double p)
        {
            var index = (sorted.Count - 1) * p;
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);

            if (lower == upper)
            {
                return sorted[lower];
            }

            return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
        }

        #region Dependency Properties

        public static readonly DependencyProperty SeriesProperty =
            DependencyProperty.Register(nameof(Series),
                                        typeof(ObservableCollection<DataSeries>),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnSeriesChanged));

        public static readonly DependencyProperty ChartTypeProperty =
            DependencyProperty.Register(nameof(ChartType),
                                        typeof(ChartType),
                                        typeof(ChartControl),
                                        new PropertyMetadata(ChartType.LinePlot, OnChartTypeChanged));

        private static void OnChartTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (ChartControl)d;
            chart._chartType = (ChartType)e.NewValue;

            chart.Dispatcher.InvokeAsync(() =>
                                         {
                                             chart.DrawChart();
                                             chart.NotifyToolbarStateChanged();
                                         }, System.Windows.Threading.DispatcherPriority.Render);
        }

        public static readonly DependencyProperty XAxesProperty =
            DependencyProperty.Register(nameof(XAxes),
                                        typeof(ObservableCollection<XAxisDefinition>),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnAxesChanged));

        public static readonly DependencyProperty YAxesProperty =
            DependencyProperty.Register(nameof(YAxes),
                                        typeof(ObservableCollection<YAxisDefinition>),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnAxesChanged));

        public static readonly DependencyProperty XAxisLabelProperty =
            DependencyProperty.Register(nameof(XAxisLabel), typeof(string),
                                        typeof(ChartControl), new PropertyMetadata("X Axis"));

        public static readonly DependencyProperty YAxisLabelProperty =
            DependencyProperty.Register(nameof(YAxisLabel), typeof(string),
                                        typeof(ChartControl), new PropertyMetadata("Y Axis"));

        public static readonly DependencyProperty AnnotationsProperty =
            DependencyProperty.Register(nameof(Annotations),
                                        typeof(ObservableCollection<Annotation>),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnSeriesChanged));

        public static readonly DependencyProperty ShowTrackerLineProperty =
            DependencyProperty.Register(nameof(ShowTrackerLine),
                                        typeof(bool),
                                        typeof(ChartControl),
                                        new PropertyMetadata(true, OnShowTrackerLineChanged));

        public static readonly DependencyProperty ZoomRectangleFillProperty =
            DependencyProperty.Register(nameof(ZoomRectangleFill),
                                        typeof(Brush),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)),
                                                             OnZoomRectangleFillChanged));

        public static readonly DependencyProperty ZoomRectangleStrokeProperty =
            DependencyProperty.Register(nameof(ZoomRectangleStroke),
                                        typeof(Brush),
                                        typeof(ChartControl),
                                        new PropertyMetadata(Brushes.Gray, OnZoomRectangleStrokeChanged));

        public static readonly DependencyProperty ZoomRectangleStrokeThicknessProperty =
            DependencyProperty.Register(nameof(ZoomRectangleStrokeThickness),
                                        typeof(double),
                                        typeof(ChartControl),
                                        new PropertyMetadata(2.0, OnZoomRectangleStrokeThicknessChanged));

        public static readonly DependencyProperty ZoomRectangleStrokeDashArrayProperty =
            DependencyProperty.Register(nameof(ZoomRectangleStrokeDashArray),
                                        typeof(DoubleCollection),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnZoomRectangleStrokeDashArrayChanged));

        public static readonly DependencyProperty BoxPlotSeriesProperty =
            DependencyProperty.Register(
                                        nameof(BoxPlotSeries),
                                        typeof(System.Collections.IEnumerable),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null));

        public static readonly DependencyProperty Surface3DSeriesProperty =
            DependencyProperty.Register(
                                        nameof(Surface3DSeries),
                                        typeof(Surface3DData),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnSeriesChanged));

        public static readonly DependencyProperty Line3DDataProperty =
            DependencyProperty.Register(
                                        nameof(Line3DData),
                                        typeof(Line3DData),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnSeriesChanged));

        public static readonly DependencyProperty TrackerTooltipBackgroundProperty =
            DependencyProperty.Register(nameof(TrackerTooltipBackground),
                                        typeof(Brush),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
                                                             OnTrackerTooltipStylePropertyChanged));

        public static readonly DependencyProperty TrackerTooltipBorderBrushProperty =
            DependencyProperty.Register(nameof(TrackerTooltipBorderBrush),
                                        typeof(Brush),
                                        typeof(ChartControl),
                                        new PropertyMetadata(Brushes.Gray, OnTrackerTooltipStylePropertyChanged));

        public static readonly DependencyProperty TrackerTooltipBorderThicknessProperty =
            DependencyProperty.Register(nameof(TrackerTooltipBorderThickness),
                                        typeof(Thickness),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new Thickness(1), OnTrackerTooltipStylePropertyChanged));

        public static readonly DependencyProperty TrackerTooltipCornerRadiusProperty =
            DependencyProperty.Register(nameof(TrackerTooltipCornerRadius),
                                        typeof(CornerRadius),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new CornerRadius(4),
                                                             OnTrackerTooltipStylePropertyChanged));

        public static readonly DependencyProperty TrackerTooltipPaddingProperty =
            DependencyProperty.Register(nameof(TrackerTooltipPadding),
                                        typeof(Thickness),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new Thickness(8, 5, 8, 5),
                                                             OnTrackerTooltipStylePropertyChanged));

        public static readonly DependencyProperty TrackerLineStrokeProperty =
            DependencyProperty.Register(nameof(TrackerLineStroke),
                                        typeof(Brush),
                                        typeof(ChartControl),
                                        new PropertyMetadata(Brushes.Gray, OnTrackerLineStylePropertyChanged));

        public static readonly DependencyProperty TrackerLineStrokeThicknessProperty =
            DependencyProperty.Register(nameof(TrackerLineStrokeThickness),
                                        typeof(double),
                                        typeof(ChartControl),
                                        new PropertyMetadata(1.0, OnTrackerLineStylePropertyChanged));

        public static readonly DependencyProperty TrackerLineDashArrayProperty =
            DependencyProperty.Register(nameof(TrackerLineDashArray),
                                        typeof(DoubleCollection),
                                        typeof(ChartControl),
                                        new PropertyMetadata(new DoubleCollection([4, 2]),
                                                             OnTrackerLineStylePropertyChanged));

        public static readonly DependencyProperty TrackerTooltipTemplateProperty =
            DependencyProperty.Register(nameof(TrackerTooltipTemplate),
                                        typeof(DataTemplate),
                                        typeof(ChartControl),
                                        new PropertyMetadata(null, OnTrackerTooltipTemplateChanged));

        #endregion

        #region CLR Properties

        /// <summary>Gets or sets the number of major ticks on the Y-axis.</summary>
        public int YMajorTickCount { get; set; } = 5;

        /// <summary>Gets or sets the number of minor ticks between Y-axis major ticks.</summary>
        public int YMinorTickCount { get; set; } = 4;

        /// <summary>Gets or sets the number of major ticks on the X-axis.</summary>
        public int XMajorTickCount { get; set; } = 5;

        /// <summary>Gets or sets the number of minor ticks between X-axis major ticks.</summary>
        public int XMinorTickCount { get; set; } = 4;

        /// <summary>Gets or sets whether major grid lines are displayed.</summary>
        public bool ShowMajorGridLines { get; set; } = true;

        /// <summary>Gets or sets whether minor grid lines are displayed.</summary>
        public bool ShowMinorGridLines { get; set; } = true;

        /// <summary>Gets or sets the brush for major grid lines.</summary>
        public Brush MajorGridLineBrush { get; set; } = Brushes.LightGray;

        /// <summary>Gets or sets the brush for minor grid lines.</summary>
        public Brush MinorGridLineBrush { get; set; } = Brushes.Silver;

        /// <summary>Fill brush applied to every other band between major grid lines.
        /// The alternating band is transparent. Default is a subtle semi-transparent gray.</summary>
        public Brush MajorGridBandBrush { get; set; } =
            new SolidColorBrush(Color.FromArgb(18, 128, 128, 128));

        /// <summary>Gets or sets the collection of data series to plot.</summary>
        public ObservableCollection<DataSeries> Series
        {
            get => (ObservableCollection<DataSeries>)GetValue(SeriesProperty);
            set => SetValue(SeriesProperty, value);
        }

        /// <summary>Gets or sets the X-axis definitions for multi-axis support.</summary>
        public ObservableCollection<XAxisDefinition> XAxes
        {
            get => (ObservableCollection<XAxisDefinition>)GetValue(XAxesProperty);
            set => SetValue(XAxesProperty, value);
        }

        /// <summary>Gets or sets the Y-axis definitions for multi-axis support.</summary>
        public ObservableCollection<YAxisDefinition> YAxes
        {
            get => (ObservableCollection<YAxisDefinition>)GetValue(YAxesProperty);
            set => SetValue(YAxesProperty, value);
        }

        /// <summary>Gets or sets the X-axis label text.</summary>
        public string XAxisLabel
        {
            get => (string)GetValue(XAxisLabelProperty);
            set => SetValue(XAxisLabelProperty, value);
        }

        /// <summary>Gets or sets the Y-axis label text.</summary>
        public string YAxisLabel
        {
            get => (string)GetValue(XAxisLabelProperty);
            set => SetValue(XAxisLabelProperty, value);
        }

        /// <summary>Gets or sets the collection of chart annotations.</summary>
        public ObservableCollection<Annotation> Annotations
        {
            get => (ObservableCollection<Annotation>)GetValue(AnnotationsProperty);
            set => SetValue(AnnotationsProperty, value);
        }

        /// <summary>Gets or sets whether the vertical tracker line is shown on hover.</summary>
        public bool ShowTrackerLine
        {
            get => (bool)GetValue(ShowTrackerLineProperty);
            set => SetValue(ShowTrackerLineProperty, value);
        }

        /// <summary>Background brush of the tracker tooltip. Default is semi-transparent white.</summary>
        public Brush TrackerTooltipBackground
        {
            get => (Brush)GetValue(TrackerTooltipBackgroundProperty);
            set => SetValue(TrackerTooltipBackgroundProperty, value);
        }

        /// <summary>Border brush of the tracker tooltip.</summary>
        public Brush TrackerTooltipBorderBrush
        {
            get => (Brush)GetValue(TrackerTooltipBorderBrushProperty);
            set => SetValue(TrackerTooltipBorderBrushProperty, value);
        }

        /// <summary>Border thickness of the tracker tooltip.</summary>
        public Thickness TrackerTooltipBorderThickness
        {
            get => (Thickness)GetValue(TrackerTooltipBorderThicknessProperty);
            set => SetValue(TrackerTooltipBorderThicknessProperty, value);
        }

        /// <summary>Corner radius of the tracker tooltip border.</summary>
        public CornerRadius TrackerTooltipCornerRadius
        {
            get => (CornerRadius)GetValue(TrackerTooltipCornerRadiusProperty);
            set => SetValue(TrackerTooltipCornerRadiusProperty, value);
        }

        /// <summary>Padding inside the tracker tooltip border.</summary>
        public Thickness TrackerTooltipPadding
        {
            get => (Thickness)GetValue(TrackerTooltipPaddingProperty);
            set => SetValue(TrackerTooltipPaddingProperty, value);
        }

        /// <summary>Stroke brush of the vertical tracker line.</summary>
        public Brush TrackerLineStroke
        {
            get => (Brush)GetValue(TrackerLineStrokeProperty);
            set => SetValue(TrackerLineStrokeProperty, value);
        }

        /// <summary>Stroke thickness of the vertical tracker line.</summary>
        public double TrackerLineStrokeThickness
        {
            get => (double)GetValue(TrackerLineStrokeThicknessProperty);
            set => SetValue(TrackerLineStrokeThicknessProperty, value);
        }

        /// <summary>Dash array of the vertical tracker line. Default is [4,2] (dashed). Set to null for a solid line.</summary>
        public DoubleCollection? TrackerLineDashArray
        {
            get => (DoubleCollection?)GetValue(TrackerLineDashArrayProperty);
            set => SetValue(TrackerLineDashArrayProperty, value);
        }

        /// <summary>
        /// Optional custom <see cref="DataTemplate"/> for the tracker tooltip.
        /// When set, replaces the built-in tooltip with this template.
        /// The template's <c>DataContext</c> is a <see cref="TrackerData"/> instance that is
        /// updated on every tracker tick via <see cref="INotifyPropertyChanged"/>.
        /// When <c>null</c> (default), the built-in tooltip is used.
        /// </summary>
        public DataTemplate? TrackerTooltipTemplate
        {
            get => (DataTemplate?)GetValue(TrackerTooltipTemplateProperty);
            set => SetValue(TrackerTooltipTemplateProperty, value);
        }

        /// <summary>Fill brush of the zoom selection rectangle. Default is semi-transparent gray.</summary>
        public Brush ZoomRectangleFill
        {
            get => (Brush)GetValue(ZoomRectangleFillProperty);
            set => SetValue(ZoomRectangleFillProperty, value);
        }

        /// <summary>Stroke brush of the zoom selection rectangle. Default is <c>Gray</c>.</summary>
        public Brush ZoomRectangleStroke
        {
            get => (Brush)GetValue(ZoomRectangleStrokeProperty);
            set => SetValue(ZoomRectangleStrokeProperty, value);
        }

        /// <summary>Stroke thickness of the zoom selection rectangle. Default is <c>2</c>.</summary>
        public double ZoomRectangleStrokeThickness
        {
            get => (double)GetValue(ZoomRectangleStrokeThicknessProperty);
            set => SetValue(ZoomRectangleStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Dash array of the zoom selection rectangle border.
        /// Default is <c>null</c> (solid line). Example: <c>4,2</c> for a dashed border.
        /// </summary>
        public DoubleCollection? ZoomRectangleStrokeDashArray
        {
            get => (DoubleCollection?)GetValue(ZoomRectangleStrokeDashArrayProperty);
            set => SetValue(ZoomRectangleStrokeDashArrayProperty, value);
        }

        /// <summary>Gets or sets the box plot data source.</summary>
        public System.Collections.IEnumerable? BoxPlotSeries
        {
            get => (System.Collections.IEnumerable?)GetValue(BoxPlotSeriesProperty);
            set => SetValue(BoxPlotSeriesProperty, value);
        }

        /// <summary>Gets or sets the 3D surface plot data.</summary>
        public Surface3DData? Surface3DSeries
        {
            get => (Surface3DData?)GetValue(Surface3DSeriesProperty);
            set => SetValue(Surface3DSeriesProperty, value);
        }

        /// <summary>Gets or sets the 3D line chart data.</summary>
        public Line3DData? Line3DData
        {
            get => (Line3DData?)GetValue(Line3DDataProperty);
            set => SetValue(Line3DDataProperty, value);
        }

        #endregion

        #region DP Callbacks

        private static void OnShowTrackerLineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart && !(bool)e.NewValue)
            {
                chart.HideTracker();
            }
        }

        private static void OnTrackerTooltipStylePropertyChanged(DependencyObject d,
                                                                 DependencyPropertyChangedEventArgs e)
        {
            if (d is not ChartControl chart || chart._trackerTooltip == null)
            {
                return;
            }

            chart._trackerTooltip.Background = chart.TrackerTooltipBackground;
            chart._trackerTooltip.BorderBrush = chart.TrackerTooltipBorderBrush;
            chart._trackerTooltip.BorderThickness = chart.TrackerTooltipBorderThickness;
            chart._trackerTooltip.CornerRadius = chart.TrackerTooltipCornerRadius;
            chart._trackerTooltip.Padding = chart.TrackerTooltipPadding;
        }

        private static void OnTrackerLineStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ChartControl chart || chart._trackerLine == null)
            {
                return;
            }

            chart._trackerLine.Stroke = chart.TrackerLineStroke;
            chart._trackerLine.StrokeThickness = chart.TrackerLineStrokeThickness;
            chart._trackerLine.StrokeDashArray = chart.TrackerLineDashArray;
        }

        private static void OnTrackerTooltipTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ChartControl chart || chart._trackerContentPresenter == null)
            {
                return;
            }

            chart._trackerContentPresenter.ContentTemplate = e.NewValue as DataTemplate;

            // When template is cleared, ensure the presenter is hidden
            if (e.NewValue == null)
            {
                chart._trackerContentPresenter.Visibility = Visibility.Collapsed;
            }
        }

        private static void OnZoomRectangleFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart && e.NewValue is Brush brush)
            {
                chart.ZoomRectangle.Fill = brush;
            }
        }

        private static void OnZoomRectangleStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart && e.NewValue is Brush brush)
            {
                chart.ZoomRectangle.Stroke = brush;
            }
        }

        private static void OnZoomRectangleStrokeThicknessChanged(DependencyObject d,
                                                                  DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart && e.NewValue is double thickness)
            {
                chart.ZoomRectangle.StrokeThickness = thickness;
            }
        }

        private static void OnZoomRectangleStrokeDashArrayChanged(DependencyObject d,
                                                                  DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart)
            {
                chart.ZoomRectangle.StrokeDashArray = e.NewValue as DoubleCollection;
            }
        }

        private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart)
            {
                chart.InvalidateAllValuesCache();
                chart.InvalidateAxisRangesCache();
                chart.DrawChart();
            }
        }

        private static void OnAxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartControl chart)
            {
                chart.DrawChart();
            }
        }

        #endregion
    }
}