using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChartAppLib.Controls
{
    public partial class ChartControl
    {
#pragma warning disable CS0414 // The field is assigned but its value is never used
        private bool _isZoomAnimationActive;
#pragma warning restore CS0414
        private static readonly Duration ZoomAnimDuration = new Duration(TimeSpan.FromMilliseconds(300));
        private static readonly IEasingFunction ZoomEase = new CubicEase { EasingMode = EasingMode.EaseInOut };

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_isLocked)
                return;

            ViewportManager.OnMouseWheel(e.GetPosition(PART_Canvas), e.Delta);
            SyncTransformsFromManager();
            Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Render);
            DebouncedZoomUndoPush();
            e.Handled = true;
        }

        private void DebouncedZoomUndoPush()
        {
            if (_zoomUndoTimer == null)
            {
                _zoomUndoTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
                _zoomUndoTimer.Tick += (_, _) =>
                {
                    _zoomUndoTimer.Stop();
                    PushUndoState();
                };
            }

            _zoomUndoTimer.Stop();
            _zoomUndoTimer.Start();
        }

        /// <summary>Resets chart view to default zoom and pan state.</summary>
        public void ResetView()
        {
            ViewportManager.Reset();
            SyncTransformsFromManager();
            DrawChart();
        }

        /// <summary>Shifts the X-axis offset by the specified delta, scrolling the data horizontally.</summary>
        public void ShiftXAxis(double delta)
        {
            ViewportManager.ShiftXOffset(delta);
            _xAxisOffset = ViewportManager.XAxisOffset;
            DrawChart();
        }

        /// <summary>Shifts the Y-axis offset by the specified delta, scrolling the data vertically.</summary>
        public void ShiftYAxis(double delta)
        {
            ViewportManager.ShiftYOffset(delta);
            _yAxisOffset = ViewportManager.YAxisOffset;
            DrawChart();
        }

        private void LineChartControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Allow Ctrl+Z/Y even when locked
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo();
                e.Handled = true;

                return;
            }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo();
                e.Handled = true;

                return;
            }

            if (_isLocked)
            {
                return;
            }

            const double shiftAmount = 1.0;
            var handled = false;

            var multiplier = 1.0;

            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                multiplier = 0.1; // Fine adjustment
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                multiplier = 5.0; // Large adjustment
            }

            switch (e.Key)
            {
                case Key.Left:
                    PushUndoState();
                    ShiftXAxis(-shiftAmount * multiplier);
                    handled = true;

                    break;
                case Key.Right:
                    PushUndoState();
                    ShiftXAxis(shiftAmount * multiplier);
                    handled = true;

                    break;
                case Key.Up:
                    PushUndoState();
                    ShiftYAxis(shiftAmount * multiplier);
                    handled = true;

                    break;
                case Key.Down:
                    PushUndoState();
                    ShiftYAxis(-shiftAmount * multiplier);
                    handled = true;

                    break;
                case Key.Home:
                    PushUndoState();
                    _xAxisOffset = 0;
                    _yAxisOffset = 0;
                    DrawChart();
                    handled = true;

                    break;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Double-click resets the view (allowed even when locked, if enabled)
            if (e.ClickCount == 2 && _doubleClickResetEnabled)
            {
                if (_isZooming)
                {
                    _isZooming = false;
                    ZoomRectangle.Visibility = Visibility.Collapsed;
                    ZoomRectangle.Width = 0;
                    ZoomRectangle.Height = 0;
                    PART_Canvas.ReleaseMouseCapture();
                }

                PushUndoState();
                ResetView();
                ClearUndoRedoHistory();
                e.Handled = true;

                return;
            }

            if (_isLocked)
            {
                return;
            }

            var position = e.GetPosition(PART_Canvas);
            var width = PART_Canvas.ActualWidth;
            var height = PART_Canvas.ActualHeight;

            // Check if clicking on Y-axis area (left side, including labels)
            if (position.X is < 0 and > -50)
            {
                _isDraggingYAxis = true;
                _axisDragStart = e.GetPosition(this);
                _yAxisOffsetStart = _yAxisOffset;
                PART_Canvas.CaptureMouse();
                PART_Canvas.Cursor = Cursors.SizeNS;
                e.Handled = true;

                return;
            }

            // Check if clicking on X-axis area (bottom, including labels)
            if (position.Y > height && position.Y < height + 50)
            {
                _isDraggingXAxis = true;
                _axisDragStart = e.GetPosition(this);
                _xAxisOffsetStart = _xAxisOffset;
                PART_Canvas.CaptureMouse();
                PART_Canvas.Cursor = Cursors.SizeWE;
                e.Handled = true;

                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // Pan mode with Control key
                PushUndoState();
                _panStart = e.GetPosition(this);
                _isPanning = true;
                PART_Canvas.CaptureMouse();
            }
            else
            {
                // Zoom rectangle mode without Control key
                // Use Mouse.GetPosition to bypass PART_Canvas's RenderTransform —
                // e.GetPosition maps through the captured element's transform, producing
                // wrong overlay coordinates when zoomed/panned.
                _zoomStart = Mouse.GetPosition(PART_Overlay);
                _isZooming = true;

                ZoomRectangle.Visibility = Visibility.Visible;
                ZoomRectangle.Width = 0;
                ZoomRectangle.Height = 0;

                // Cache zoom start position to avoid repeated Canvas.GetLeft/Top property queries
                _zoomStartLeft = _zoomStart.X;
                _zoomStartTop = _zoomStart.Y;
                Canvas.SetLeft(ZoomRectangle, _zoomStartLeft);
                Canvas.SetTop(ZoomRectangle, _zoomStartTop);

                PART_Canvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (ShowTrackerLine   && _trackerLine != null && !_isDraggingLine && !_isZooming && !_isPanning &&
                !_isDraggingXAxis && !_isDraggingYAxis)
            {
                var overlayPos = e.GetPosition(PART_Overlay);
                var width = PART_Canvas.ActualWidth;
                var height = PART_Canvas.ActualHeight;
                var hasSeries = Series != null && Series.Count > 0;

                _trackerLineVisible = hasSeries && width > 0  && height       > 0
                                      && overlayPos.X    >= 0 && overlayPos.X <= width
                                      && overlayPos.Y    >= 0 && overlayPos.Y <= height;

                if (!hasSeries)
                {
                    _trackerTooltip?.Visibility = Visibility.Collapsed;

                    if (_trackerThrottleTimer != null)
                    {
                        _trackerThrottleTimer.Stop();
                        _trackerUpdatePending = false;
                    }
                }
            }
            else if (_trackerLine != null && (Series == null || Series.Count == 0))
            {
                _trackerLineVisible = false;

                _trackerTooltip?.Visibility = Visibility.Collapsed;

                if (_trackerThrottleTimer != null)
                {
                    _trackerThrottleTimer.Stop();
                    _trackerUpdatePending = false;
                }
            }

            if (_isDraggingLine)
            {
                HandleLineDrag(e);

                return;
            }

            if (_isZooming)
            {
                // UpdateZoomRectangle is trivial (4 property sets) — no throttle needed.
                // Throttling caused a visible delay before the rectangle appeared on first move.
                // Use Mouse.GetPosition to bypass PART_Canvas's RenderTransform.
                var zoomPos = Mouse.GetPosition(PART_Overlay);
                UpdateZoomRectangle(zoomPos);

                // Stop tracker value calculations during zoom
                if (_trackerThrottleTimer != null)
                {
                    _trackerThrottleTimer.Stop();
                    _trackerUpdatePending = false;
                }
            }

            if (_isPanning)
            {
                var current = e.GetPosition(this);
                var delta = current - _panStart;

                ViewportManager.OnPanDelta(delta);
                SyncTransformsFromManager();

                // Don't call DrawChart() during pan — transforms handle the visual update
                // Axis labels only redraw when pan finishes (via debounce)
                _panStart = current;
            }

            // Axis drag via canvas area — shift series visually, throttle axis redraw
            if (_isDraggingXAxis)
            {
                var current = e.GetPosition(this);
                var pixelDelta = current.X - _axisDragStart.X;

                if (Series is { Count: > 0 })
                {
                    var width = PART_Canvas.ActualWidth;
                    var maxPoints = Series.Max(s => s.YValues.Count);

                    // Visual shift via transform (instant feedback, no redraw needed)
                    foreach (var p in _seriesPaths)
                    {
                        p.RenderTransform = new TranslateTransform(pixelDelta, 0);
                    }

                    ViewportManager.OnXAxisDrag(pixelDelta, maxPoints, width, _xAxisOffsetStart);
                    _xAxisOffset = ViewportManager.XAxisOffset;

                    // Throttle axis label redraw only (expensive labels, not series)
                    ThrottledRedrawAxesOnly();
                }
            }

            if (_isDraggingYAxis)
            {
                var current = e.GetPosition(this);
                var pixelDelta = current.Y - _axisDragStart.Y;

                if (Series is { Count: > 0 })
                {
                    var height = PART_Canvas.ActualHeight;
                    var (min, max) = GetAllValuesMinMax();
                    var range = max - min == 0 ? 1 : max - min;

                    // Visual shift via transform (instant feedback, no redraw needed)
                    foreach (var p in _seriesPaths)
                    {
                        p.RenderTransform = new TranslateTransform(0, pixelDelta);
                    }

                    ViewportManager.OnYAxisDrag(pixelDelta, height, range, _yAxisOffsetStart);
                    _yAxisOffset = ViewportManager.YAxisOffset;

                    // Throttle axis label redraw only (expensive labels, not series)
                    ThrottledRedrawAxesOnly();
                }
            }

            // Queue deferred tracker value calculation (tooltip, markers)
            // This happens asynchronously every 50ms, not blocking the line update
            if (!_isDraggingLine && !_isZooming && !_isPanning && !_isDraggingXAxis && !_isDraggingYAxis)
            {
                var overlayPos = e.GetPosition(PART_Overlay);
                _lastTrackerPosition = overlayPos;
                _pendingTrackerPosition = overlayPos;
                QueueTrackerValueUpdate();
            }
        }

        private void ApplyRectangleZoom()
        {
            // 1️⃣ Fast reject
            if (ZoomRectangle.Width < 10 || ZoomRectangle.Height < 10)
            {
                return;
            }

            PushUndoState();

            // Ensure viewport manager state is synced with current WPF transforms
            // (important if previous zoom was deferred and not yet reflected in manager)
            SyncManagerFromTransforms();

            // 2️⃣ Cache frequently accessed values
            var canvas = PART_Canvas;
            double canvasWidth = canvas.ActualWidth;
            double canvasHeight = canvas.ActualHeight;

            double rectLeft = Canvas.GetLeft(ZoomRectangle);
            double rectTop = Canvas.GetTop(ZoomRectangle);
            double rectWidth = ZoomRectangle.Width;
            double rectHeight = ZoomRectangle.Height;

            // 3️⃣ Snapshot old transform state (no layout invalidation)
            double oldScaleX = ZoomTransform.ScaleX;
            double oldScaleY = ZoomTransform.ScaleY;

            double cx = ZoomTransform.CenterX;
            double cy = ZoomTransform.CenterY;

            double oldPanX = PanTransform.X + cx * (1 - oldScaleX);
            double oldPanY = PanTransform.Y + cy * (1 - oldScaleY);

            // 4️⃣ Delegate zoom math — pass screen/overlay coords directly.
            // OnZoomRect normalizes existing CenterX/CenterY into PanX/PanY internally via:
            //   oldPanX = PanX + CenterX * (1 - ScaleX)
            // then multiplies the existing ScaleX to produce the correct cumulative scale.
            // Dividing by oldScaleX here would cause double-application of the existing zoom.
            ViewportManager.OnZoomRect(
                                       new Rect(rectLeft, rectTop, rectWidth, rectHeight),
                                       new Size(canvasWidth, canvasHeight));

            double newScaleX = ViewportManager.ScaleX;
            double newScaleY = ViewportManager.ScaleY;
            double newPanX = ViewportManager.PanX;
            double newPanY = ViewportManager.PanY;

            // 6️⃣ Reset to animation‑friendly state
            ZoomTransform.CenterX = 0;
            ZoomTransform.CenterY = 0;
            PanTransform.X = oldPanX;
            PanTransform.Y = oldPanY;

            // 7️⃣ Detect near‑no‑op zoom (skip animation entirely)
            const double epsilon = 0.0001;

            if (Math.Abs(oldScaleX - newScaleX) < epsilon &&
                Math.Abs(oldScaleY - newScaleY) < epsilon &&
                Math.Abs(oldPanX   - newPanX)   < epsilon &&
                Math.Abs(oldPanY   - newPanY)   < epsilon)
            {
                ApplyFinalTransforms(newScaleX, newScaleY, newPanX, newPanY);

                return;
            }

            // 7️⃣ Apply final transforms immediately — animation causes visible stroke
            //    thickness artifacts (series appear thick during animation, then snap thin).
            ApplyFinalTransforms(newScaleX, newScaleY, newPanX, newPanY);
        }

        private void AnimateTransform(
            double oldScaleX, double newScaleX,
            double oldScaleY, double newScaleY,
            double oldPanX, double newPanX,
            double oldPanY, double newPanY)
        {
            // Mark that animation is active to prevent DrawChart() calls during animation
            _isZoomAnimationActive = true;

            var animScaleX = new DoubleAnimation(oldScaleX, newScaleX, ZoomAnimDuration)
                             {
                                 EasingFunction = ZoomEase,
                                 FillBehavior = FillBehavior.Stop
                             };

            var animScaleY = new DoubleAnimation(oldScaleY, newScaleY, ZoomAnimDuration)
                             {
                                 EasingFunction = ZoomEase,
                                 FillBehavior = FillBehavior.Stop
                             };

            var animPanX = new DoubleAnimation(oldPanX, newPanX, ZoomAnimDuration)
                           {
                               EasingFunction = ZoomEase,
                               FillBehavior = FillBehavior.Stop
                           };

            var animPanY = new DoubleAnimation(oldPanY, newPanY, ZoomAnimDuration)
                           {
                               EasingFunction = ZoomEase,
                               FillBehavior = FillBehavior.Stop
                           };

            animPanY.Completed += (_, _) =>
                                  {
                                      _isZoomAnimationActive = false;
                                      ApplyFinalTransforms(newScaleX, newScaleY, newPanX, newPanY);
                                  };

            ZoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animScaleX, HandoffBehavior.SnapshotAndReplace);
            ZoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animScaleY, HandoffBehavior.SnapshotAndReplace);
            PanTransform.BeginAnimation(TranslateTransform.XProperty, animPanX, HandoffBehavior.SnapshotAndReplace);
            PanTransform.BeginAnimation(TranslateTransform.YProperty, animPanY, HandoffBehavior.SnapshotAndReplace);
        }

        private void ApplyFinalTransforms(
            double scaleX, double scaleY, double panX, double panY)
        {
            // Freeze values (no animations retained in the tree)
            ZoomTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ZoomTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            PanTransform.BeginAnimation(TranslateTransform.XProperty, null);
            PanTransform.BeginAnimation(TranslateTransform.YProperty, null);

            ZoomTransform.ScaleX = scaleX;
            ZoomTransform.ScaleY = scaleY;
            PanTransform.X = panX;
            PanTransform.Y = panY;

            // Keep logical zoom model in sync
            SyncManagerFromTransforms();
            UpdateScrollbars();

            // Reset zoom rectangle for next zoom operation
            ZoomRectangle.Width = 0;
            ZoomRectangle.Height = 0;
            ZoomRectangle.Visibility = Visibility.Collapsed;

            // Defer redraw at Background priority (4) so it runs after layout settles AND after
            // pending Input events (5). Using Render (7) would block mouse events and cause
            // visible delay when drawing the zoom rectangle.
            Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Updates zoom rectangle position and size (throttled via 16ms timer).
        /// Uses cached _zoomStartLeft/_zoomStartTop to avoid property queries.
        /// </summary>
        private void UpdateZoomRectangle(Point current)
        {
            var w = current.X - _zoomStartLeft;
            var h = current.Y - _zoomStartTop;

            if (w >= 0)
            {
                Canvas.SetLeft(ZoomRectangle, _zoomStartLeft);
                ZoomRectangle.Width = w;
            }
            else
            {
                Canvas.SetLeft(ZoomRectangle, current.X);
                ZoomRectangle.Width = -w;
            }

            if (h >= 0)
            {
                Canvas.SetTop(ZoomRectangle, _zoomStartTop);
                ZoomRectangle.Height = h;
            }
            else
            {
                Canvas.SetTop(ZoomRectangle, current.Y);
                ZoomRectangle.Height = -h;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingLine)
            {
                StopLineDrag();
            }
            else if (_isDraggingXAxis)
            {
                _isDraggingXAxis = false;
                PART_Canvas.ReleaseMouseCapture();
                PART_Canvas.Cursor = Cursors.Arrow;

                var current = e.GetPosition(this);
                var pixelDelta = current.X - _axisDragStart.X;

                if (Series is { Count: > 0 })
                {
                    var width = PART_Canvas.ActualWidth;
                    var maxPoints = Series.Max(s => s.YValues.Count);
                    ViewportManager.OnXAxisDrag(pixelDelta, maxPoints, width, _xAxisOffsetStart);
                    _xAxisOffset = ViewportManager.XAxisOffset;
                }

                // Dispatch at Background priority so this mouse-up returns immediately —
                // the zoom rectangle (next mouse-down) can start without blocking.
                Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);
            }
            else if (_isDraggingYAxis)
            {
                _isDraggingYAxis = false;
                PART_Canvas.ReleaseMouseCapture();
                PART_Canvas.Cursor = Cursors.Arrow;

                var current = e.GetPosition(this);
                var pixelDelta = current.Y - _axisDragStart.Y;

                if (Series is { Count: > 0 })
                {
                    var height = PART_Canvas.ActualHeight;
                    var (min, max) = GetAllValuesMinMax();
                    var range = max - min == 0 ? 1 : max - min;
                    ViewportManager.OnYAxisDrag(pixelDelta, height, range, _yAxisOffsetStart);
                    _yAxisOffset = ViewportManager.YAxisOffset;
                }

                Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);
            }
            else if (_isZooming)
            {
                _isZooming = false;
                PART_Canvas.ReleaseMouseCapture();
                ApplyRectangleZoom();
                // ZoomRectangle visibility/size reset happens in ApplyFinalTransforms()
            }
            else if (_isPanning)
            {
                _isPanning = false;
                PART_Canvas.ReleaseMouseCapture();
            }
        }

        private void Canvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!IsFocused)
            {
                Focus();
            }
        }

        // Y-Axis interactive area event handlers
        private void YAxis_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked)
            {
                return;
            }

            PushUndoState();
            _isDraggingYAxis = true;
            _axisDragStart = e.GetPosition(this);
            _yAxisOffsetStart = _yAxisOffset;
            ((Border)sender).CaptureMouse();
            e.Handled = true;
        }

        private void YAxis_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingYAxis)
            {
                return;
            }

            var current = e.GetPosition(this);
            var pixelDelta = current.Y - _axisDragStart.Y;

            if (Series is { Count: > 0 })
            {
                var height = PART_Canvas.ActualHeight;
                var (min, max) = GetAllValuesMinMax();
                var range = max - min == 0 ? 1 : max - min;

                foreach (var p in _seriesPaths)
                {
                    p.RenderTransform = new TranslateTransform(0, pixelDelta);
                }

                ViewportManager.OnYAxisDrag(pixelDelta, height, range, _yAxisOffsetStart);
                _yAxisOffset = ViewportManager.YAxisOffset;
                ThrottledRedrawAxesOnly();
            }

            e.Handled = true;
        }

        private void YAxis_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingYAxis)
            {
                return;
            }

            _isDraggingYAxis = false;
            ((Border)sender).ReleaseMouseCapture();

            var current = e.GetPosition(this);
            var pixelDelta = current.Y - _axisDragStart.Y;

            if (Series is { Count: > 0 })
            {
                var height = PART_Canvas.ActualHeight;
                var (min, max) = GetAllValuesMinMax();
                var range = max - min == 0 ? 1 : max - min;
                ViewportManager.OnYAxisDrag(pixelDelta, height, range, _yAxisOffsetStart);
                _yAxisOffset = ViewportManager.YAxisOffset;
            }

            DrawChart();
            e.Handled = true;
        }

        // X-Axis interactive area event handlers
        private void XAxis_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isLocked)
            {
                return;
            }

            PushUndoState();
            _isDraggingXAxis = true;
            _axisDragStart = e.GetPosition(this);
            _xAxisOffsetStart = _xAxisOffset;
            ((Border)sender).CaptureMouse();
            e.Handled = true;
        }

        private void XAxis_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingXAxis)
            {
                return;
            }

            var current = e.GetPosition(this);
            var pixelDelta = current.X - _axisDragStart.X;

            if (Series is { Count: > 0 })
            {
                var width = PART_Canvas.ActualWidth;
                var maxPoints = Series.Max(s => s.YValues.Count);

                foreach (var p in _seriesPaths)
                {
                    p.RenderTransform = new TranslateTransform(pixelDelta, 0);
                }

                ViewportManager.OnXAxisDrag(pixelDelta, maxPoints, width, _xAxisOffsetStart);
                _xAxisOffset = ViewportManager.XAxisOffset;
                ThrottledRedrawAxesOnly();
            }

            e.Handled = true;
        }

        private void XAxis_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingXAxis)
            {
                return;
            }

            _isDraggingXAxis = false;
            ((Border)sender).ReleaseMouseCapture();

            var current = e.GetPosition(this);
            var pixelDelta = current.X - _axisDragStart.X;

            if (Series is { Count: > 0 })
            {
                var width = PART_Canvas.ActualWidth;
                var maxPoints = Series.Max(s => s.YValues.Count);
                ViewportManager.OnXAxisDrag(pixelDelta, maxPoints, width, _xAxisOffsetStart);
                _xAxisOffset = ViewportManager.XAxisOffset;
            }

            DrawChart();
            e.Handled = true;
        }

        // Trackpad pinch-to-zoom and two-finger pan via manipulation events
        private void Canvas_ManipulationStarting(object? sender, ManipulationStartingEventArgs e)
        {
            if (_isLocked)
            {
                e.Cancel();

                return;
            }

            e.ManipulationContainer = PART_Canvas;
            e.Mode = ManipulationModes.Scale | ManipulationModes.Translate;
            _manipulationUndoPushed = false;
            e.Handled = true;
        }

        private void Canvas_ManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
        {
            if (!_manipulationUndoPushed)
            {
                PushUndoState();
                _manipulationUndoPushed = true;
            }

            var origin = e.ManipulationOrigin;
            var scale = e.DeltaManipulation.Scale;
            var translate = e.DeltaManipulation.Translation;

            ViewportManager.OnManipulationDelta(
                                                origin,
                                                scale.X, scale.Y,
                                                new Vector(translate.X, translate.Y));

            SyncTransformsFromManager();
            e.Handled = true;
        }

        private void Canvas_ManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
        {
            _manipulationUndoPushed = false;
            e.Handled = true;
        }

        // ── Scrollbar handlers ─────────────────────────────────────────────────────

        /// <summary>
        /// Handles horizontal scrollbar changes when user scrolls to pan left/right.
        /// </summary>
        private void HorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (_isScrollbarUpdating)
            {
                return;
            }

            // Convert scrollbar value (0-100%) to pan delta
            var maxPan = PART_Canvas.ActualWidth * (ZoomTransform.ScaleX - 1);

            if (maxPan > 0)
            {
                var targetPan = -(PART_HorizontalScrollBar.Value / 100.0) * maxPan;
                var delta = targetPan - PanTransform.X;

                if (Math.Abs(delta) > 0.1)
                {
                    ViewportManager.OnPanDelta(new Vector(delta, 0));
                    SyncTransformsFromManager();
                    Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Handles vertical scrollbar changes when user scrolls to pan up/down.
        /// </summary>
        private void VerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (_isScrollbarUpdating)
            {
                return;
            }

            // Convert scrollbar value to pan delta
            var maxPan = PART_Canvas.ActualHeight * (ZoomTransform.ScaleY - 1);

            if (maxPan > 0)
            {
                var targetPan = -(PART_VerticalScrollBar.Value / 100.0) * maxPan;
                var delta = targetPan - PanTransform.Y;

                if (Math.Abs(delta) > 0.1)
                {
                    ViewportManager.OnPanDelta(new Vector(0, delta));
                    SyncTransformsFromManager();
                    Dispatcher.InvokeAsync(DrawChart, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Updates scrollbar visibility and range based on current zoom level.
        /// Called whenever viewport changes (zoom, pan, reset).
        /// </summary>
        internal void UpdateScrollbars()
        {
            if (PART_HorizontalScrollBar == null || PART_VerticalScrollBar == null || PART_Canvas == null)
            {
                return;
            }

            _isScrollbarUpdating = true;

            try
            {
                var canvasWidth = PART_Canvas.ActualWidth;
                var canvasHeight = PART_Canvas.ActualHeight;

                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    return;
                }

                var scaleX = ZoomTransform.ScaleX;
                var scaleY = ZoomTransform.ScaleY;
                var panX = PanTransform.X;
                var panY = PanTransform.Y;

                // Update horizontal scrollbar
                if (scaleX > 1.0 + 0.01) // Only show if zoomed in
                {
                    PART_HorizontalScrollBar.Visibility = Visibility.Visible;
                    var maxPan = canvasWidth * (scaleX - 1);

                    PART_HorizontalScrollBar.Minimum = 0;
                    PART_HorizontalScrollBar.Maximum = 100;

                    if (maxPan > 0)
                    {
                        PART_HorizontalScrollBar.Value = Math.Clamp(-panX / maxPan * 100, 0, 100);
                    }
                    else
                    {
                        PART_HorizontalScrollBar.Value = 0;
                    }
                }
                else
                {
                    PART_HorizontalScrollBar.Visibility = Visibility.Collapsed;
                }

                // Update vertical scrollbar
                if (scaleY > 1.0 + 0.01) // Only show if zoomed in
                {
                    PART_VerticalScrollBar.Visibility = Visibility.Visible;
                    var maxPan = canvasHeight * (scaleY - 1);

                    PART_VerticalScrollBar.Minimum = 0;
                    PART_VerticalScrollBar.Maximum = 100;

                    if (maxPan > 0)
                    {
                        PART_VerticalScrollBar.Value = Math.Clamp(-panY / maxPan * 100, 0, 100);
                    }
                    else
                    {
                        PART_VerticalScrollBar.Value = 0;
                    }
                }
                else
                {
                    PART_VerticalScrollBar.Visibility = Visibility.Collapsed;
                }
            }
            finally
            {
                _isScrollbarUpdating = false;
            }
        }
    }
}