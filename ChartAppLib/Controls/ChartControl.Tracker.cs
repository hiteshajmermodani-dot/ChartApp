using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChartAppLib.Models;

namespace ChartAppLib.Controls
{
    public partial class ChartControl
    {
        // Cached tracker tooltip item panels to avoid recreating UI elements every mouse move
        private readonly List<StackPanel> _trackerItemPanels = [];
        private readonly List<Rectangle> _trackerItemRects = [];
        private readonly List<TextBlock> _trackerItemTexts = [];

        // Tracker dot markers placed on the overlay at each series intersection
        private readonly List<Ellipse> _trackerMarkers = [];

        // X-axis band marker shown on the axis below the tracker line
        private Border? _trackerXAxisMarker;
        private TextBlock? _trackerXAxisMarkerText;

        // X-axis label shown as first row in the tracker tooltip
        private TextBlock? _trackerXText;

        private void Canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            _lastTrackerPosition = null;
            HideTracker();
        }

        private void HideTracker()
        {
            _trackerLineVisible = false;
            _trackerLine?.Visibility = Visibility.Collapsed;
            _trackerTooltip?.Visibility = Visibility.Collapsed;

            if (_trackerContentPresenter != null)
            {
                _trackerContentPresenter.Visibility = Visibility.Collapsed;
            }

            if (_trackerXAxisMarker != null)
            {
                _trackerXAxisMarker.Visibility = Visibility.Collapsed;
            }

            foreach (var marker in _trackerMarkers)
            {
                marker.Visibility = Visibility.Collapsed;
            }

            // Cancel any pending deferred tooltip update so it cannot resurrect the tooltip
            if (_trackerThrottleTimer != null)
            {
                _trackerThrottleTimer.Stop();
                _trackerUpdatePending = false;
            }
        }

        /// <summary>
        /// Converts a canvas pixel X coordinate back to a fractional data index.
        /// Points are drawn at canvasX = (i - _xAxisOffset) * step, so the inverse is:
        ///   index = dataX / step + _xAxisOffset
        /// XValues (if any) are data-domain values used only for display, not for positioning.
        /// </summary>
        private double FindNearestPointIndex(double dataX, double step, int count)
        {
            var index = dataX / step + _xAxisOffset;

            return Math.Clamp(index, 0, count - 1);
        }

        private void EnsureTrackerItems(int count)
        {
            while (_trackerItemPanels.Count < count)
            {
                var rect = new Rectangle
                           {
                               Width = 10,
                               Height = 10,
                               Margin = new Thickness(0, 2, 5, 0)
                           };

                var text = new TextBlock { FontSize = 11 };
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                panel.Children.Add(rect);
                panel.Children.Add(text);
                _trackerItemPanels.Add(panel);
                _trackerItemRects.Add(rect);
                _trackerItemTexts.Add(text);
            }
        }

        private void EnsureTrackerMarkers(int count)
        {
            while (_trackerMarkers.Count < count)
            {
                var marker = new Ellipse
                             {
                                 Width = 10,
                                 Height = 10,
                                 IsHitTestVisible = false,
                                 Visibility = Visibility.Collapsed
                             };

                Panel.SetZIndex(marker, 902);
                PART_Overlay.Children.Add(marker);
                _trackerMarkers.Add(marker);
            }
        }

        /// <summary>
        /// Re-runs the tracker tooltip update at the last known mouse position.
        /// Called after live data refresh so the tooltip reflects new values.
        /// </summary>
        private void UpdateTrackerAtLastPosition()
        {
            if (_lastTrackerPosition.HasValue && _trackerLine != null && _trackerLine.Visibility == Visibility.Visible)
            {
                // Update both line position and values immediately
                UpdateTrackerLineOnly(_lastTrackerPosition.Value);
                UpdateTrackerValuesOnly(_lastTrackerPosition.Value);
            }
        }

        private void EnsureXAxisMarker()
        {
            if (_trackerXAxisMarker != null)
            {
                return;
            }

            _trackerXAxisMarkerText = new TextBlock
                                      {
                                          FontSize = 10,
                                          FontWeight = FontWeights.Bold,
                                          Foreground = Brushes.White,
                                          TextAlignment = TextAlignment.Center
                                      };

            _trackerXAxisMarker = new Border
                                  {
                                      Background = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                                      CornerRadius = new CornerRadius(2),
                                      Padding = new Thickness(4, 2, 4, 2),
                                      IsHitTestVisible = false,
                                      Visibility = Visibility.Collapsed,
                                      Child = _trackerXAxisMarkerText
                                  };

            Panel.SetZIndex(_trackerXAxisMarker, 903);
            PART_AxesCanvas.Children.Add(_trackerXAxisMarker);
        }

        private void UpdateTracker(MouseEventArgs e)
        {
            // This method is no longer used - tracker line is updated directly in Canvas_MouseMove
            // Keep for compatibility if called from elsewhere
            Point overlayPos = e.GetPosition(PART_Overlay);
            _lastTrackerPosition = overlayPos;
            _pendingTrackerPosition = overlayPos;

            QueueTrackerValueUpdate();
        }

        /// <summary>
        /// Queue tracker value update to run every 50ms (throttled).
        /// This separates instant line movement from deferred value calculations.
        /// Must be called AFTER _pendingTrackerPosition is set.
        /// </summary>
        private void QueueTrackerValueUpdate()
        {
            if (!_trackerUpdatePending)
            {
                _trackerUpdatePending = true;

                if (_trackerThrottleTimer == null)
                {
                    _trackerThrottleTimer = new System.Windows.Threading.DispatcherTimer
                                            {
                                                Interval = TimeSpan.FromMilliseconds(GetAdaptiveTrackerInterval())
                                            };

                    _trackerThrottleTimer.Tick += (_, _) =>
                                                  {
                                                      if (_pendingTrackerPosition.HasValue)
                                                      {
                                                          UpdateTrackerValuesOnly(_pendingTrackerPosition.Value);
                                                      }

                                                      _trackerUpdatePending = false;
                                                  };
                }
                else
                {
                    // Update interval in case data size changed
                    _trackerThrottleTimer.Interval = TimeSpan.FromMilliseconds(GetAdaptiveTrackerInterval());
                }

                _trackerThrottleTimer.Stop();
                _trackerThrottleTimer.Start();
            }
        }

        /// <summary>
        /// Returns an adaptive throttle interval (ms) based on total data point count.
        /// Small datasets get near-instant updates; huge datasets get longer intervals
        /// to avoid blocking the UI thread with value calculations.
        /// </summary>
        private int GetAdaptiveTrackerInterval()
        {
            var totalPoints = GetTotalDataPointCount();

            return totalPoints switch
                   {
                       > 1_000_000 => 150, // 1M+ points: aggressive throttle
                       > 500_000   => 100, // 500K+ points
                       > 100_000   => 75,  // 100K+ points
                       _           => 50   // Default: responsive
                   };
        }

        /// <summary>
        /// Updates only the tracker line position (instant feedback).
        /// Does NOT calculate series values or axis ranges.
        /// Called directly from Canvas_MouseMove for immediate visual feedback.
        /// </summary>
        private void UpdateTrackerLineOnly(Point overlayPos)
        {
            if (!ShowTrackerLine || _trackerLine == null || PART_Overlay.ActualWidth <= 0 ||
                PART_Overlay.ActualHeight        <= 0    ||
                Series                           == null || Series.Count == 0)
            {
                HideTracker();

                return;
            }

            var width = PART_Overlay.ActualWidth;
            var height = PART_Overlay.ActualHeight;
            var mouseX = overlayPos.X;
            var mouseY = overlayPos.Y;

            if (mouseX < 0 || mouseX > width || mouseY < 0 || mouseY > height)
            {
                HideTracker();

                return;
            }

            // Position the vertical tracker line at cursor X position
            _trackerLine.X1 = mouseX;
            _trackerLine.X2 = mouseX;
            _trackerLine.Y1 = 0;
            _trackerLine.Y2 = height;
            _trackerLine.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Updates the tracker tooltip with calculated values (deferred, throttled).
        /// Assumes the tracker line is already positioned by UpdateTrackerLineOnly.
        /// Optimized with binary search and caching to handle 100K+ points efficiently.
        /// </summary>
        private void UpdateTrackerValuesOnly(Point overlayPos)
        {
            if (!ShowTrackerLine || Series == null || Series.Count == 0 ||
                YAxes                      == null || YAxes.Count  == 0 || _trackerTooltip == null)
            {
                return;
            }

            var width = PART_Overlay.ActualWidth;
            var height = PART_Overlay.ActualHeight;
            var mouseX = overlayPos.X;
            var mouseY = overlayPos.Y;

            if (width <= 0 || height <= 0 || mouseX < 0 || mouseX > width || mouseY < 0 || mouseY > height)
            {
                return;
            }

            var cx = ZoomTransform.CenterX;
            var cy = ZoomTransform.CenterY;
            var scaleX = ZoomTransform.ScaleX;
            var scaleY = ZoomTransform.ScaleY;
            var panX = PanTransform.X;
            var panY = PanTransform.Y;

            // Convert overlay mouse position back to canvas coordinate space
            // Inverse of: overlayX = canvasX * scaleX + cx * (1 - scaleX) + panX
            var dataX = (mouseX - panX - cx * (1 - scaleX)) / scaleX;

            var content = (StackPanel)_trackerTooltip.Child;

            // Ensure X-axis label is the first child of the tooltip content
            if (_trackerXText == null)
            {
                _trackerXText = new TextBlock
                                {
                                    FontSize = 11,
                                    FontWeight = FontWeights.Bold,
                                    Margin = new Thickness(0, 0, 0, 4)
                                };

                content.Children.Insert(0, _trackerXText);
            }

            var isScatterOrBubble = _chartType == ChartType.ScatterPlot || _chartType == ChartType.BubblePlot;
            var visibleIndex = 0;
            bool xLabelSet = false;

            // Build axis range cache ONCE per value update if data version changed
            RebuildAxisRangesCacheIfNeeded();
            var axisRanges = _cachedAxisRanges ?? new Dictionary<string, (double min, double max, double range)>();

            foreach (var series in Series)
            {
                if (series.YValues == null || series.YValues.Count < 2)
                {
                    continue;
                }

                var yAxisId = series.YAxisId ?? YAxes[0].Id;

                // Look up pre-computed axis range instead of recalculating
                if (!axisRanges.TryGetValue(yAxisId, out var axisRange))
                {
                    axisRange = ComputeAxisRange(yAxisId);
                    axisRanges[yAxisId] = axisRange;
                }

                var count = series.YValues.Count;
                var step = width / (count - 1);

                // Convert canvas pixel coordinate to fractional data index
                // Scatter/bubble use absolute positions (i * step), not offset-shifted
                var dataIndex = isScatterOrBubble
                                    ? Math.Clamp(dataX / step, 0, count - 1)
                                    : FindNearestPointIndex(dataX, step, count);

                if (dataIndex < 0 || dataIndex > count - 1)
                {
                    continue;
                }

                int i0;
                double displayValue;

                if (isScatterOrBubble)
                {
                    // Snap to nearest discrete point — scatter dots are not connected
                    i0 = Math.Clamp((int)Math.Round(dataIndex), 0, count - 1);
                    displayValue = series.YValues[i0];
                }
                else
                {
                    i0 = (int)Math.Floor(dataIndex);
                    var i1Interp = Math.Min(i0 + 1, count - 1);
                    var frac = dataIndex - i0;
                    displayValue = series.YValues[i0] * (1 - frac) + series.YValues[i1Interp] * frac;
                }

                // Set X-axis label from the first valid series
                if (!xLabelSet && _trackerXText != null)
                {
                    double xDisplayValue;

                    // Check if we have explicit X values AND index is within bounds
                    if (series.HasExplicitXValues && i0 < series.XValues!.Count)
                    {
                        if (isScatterOrBubble)
                        {
                            // For scatter/bubble use the snapped index directly — no interpolation
                            xDisplayValue = series.XValues[i0];
                        }
                        else
                        {
                            var i1X = Math.Min(i0 + 1, count - 1);
                            var fracX = dataIndex - i0;
                            xDisplayValue = series.XValues[i0] * (1 - fracX) +
                                            (i1X < series.XValues!.Count ? series.XValues[i1X] : series.XValues[i0]) * fracX;
                        }
                    }
                    else
                    {
                        // Fallback: Use the actual data index
                        xDisplayValue = isScatterOrBubble ? i0 : dataIndex;
                    }

                    var xAxisDef = XAxes?.FirstOrDefault(a => a.Id == (series.XAxisId ?? XAxes[0].Id));
                    var xAxisLabel = xAxisDef?.Label;
                    var formattedX = xAxisDef?.FormatLabel(xDisplayValue) ?? xDisplayValue.ToString("F2");

                    _trackerXText.Text = string.IsNullOrEmpty(xAxisLabel)
                                             ? $"X: {formattedX}"
                                             : $"{xAxisLabel}: {formattedX}";

                    _trackerData.XText = _trackerXText.Text;

                    // Show X-axis band marker on the axis below the plot area (in PART_AxesCanvas)
                    EnsureXAxisMarker();
                    _trackerXAxisMarkerText!.Text = formattedX;
                    // Use previously measured width to avoid expensive synchronous UpdateLayout()
                    var markerW = _trackerXAxisMarker!.ActualWidth > 0 ? _trackerXAxisMarker.ActualWidth : 40;
                    var plotMargin = PART_PlotArea.Margin;
                    var plotHeight = PART_PlotArea.ActualHeight;
                    Canvas.SetLeft(_trackerXAxisMarker, plotMargin.Left + mouseX - markerW / 2);
                    Canvas.SetTop(_trackerXAxisMarker, plotMargin.Top            + plotHeight + 2);
                    _trackerXAxisMarker.Visibility = Visibility.Visible;

                    xLabelSet = true;
                }

                // Position dot marker on the overlay at the series Y value
                EnsureTrackerMarkers(visibleIndex + 1);
                var marker = _trackerMarkers[visibleIndex];
                marker.Fill = series.Stroke;

                var canvasY = height - (displayValue + _yAxisOffset - axisRange.min) / axisRange.range * height;

                // Convert canvas coordinates to overlay coordinates
                var markerOverlayY = canvasY * scaleY + cy * (1 - scaleY) + panY;
                var markerOverlayX = isScatterOrBubble
                                         ? i0 * step * scaleX + cx * (1 - scaleX) + panX
                                         : mouseX;

                if (markerOverlayY >= 0 && markerOverlayY <= height)
                {
                    Canvas.SetLeft(marker, markerOverlayX - marker.Width  / 2);
                    Canvas.SetTop(marker, markerOverlayY  - marker.Height / 2);
                    marker.Visibility = Visibility.Visible;
                }
                else
                {
                    marker.Visibility = Visibility.Collapsed;
                }

                // Reuse cached tooltip row elements
                EnsureTrackerItems(visibleIndex + 1);
                var itemPanel = _trackerItemPanels[visibleIndex];
                _trackerItemRects[visibleIndex].Fill = series.Stroke;
                _trackerItemTexts[visibleIndex].Text = $"{series.Name}: {displayValue:F2}";

                // Sync to TrackerData for custom template support
                while (_trackerData.Items.Count <= visibleIndex)
                {
                    _trackerData.Items.Add(new TrackerSeriesItem());
                }

                var trackerItem = _trackerData.Items[visibleIndex];
                trackerItem.Name = series.Name ?? string.Empty;
                trackerItem.Value = displayValue;
                trackerItem.Stroke = series.Stroke;
                trackerItem.FormattedValue = $"{series.Name}: {displayValue:F2}";

                var tooltipChildIndex = visibleIndex + 1; // +1 for X label at index 0

                if (tooltipChildIndex < content.Children.Count)
                {
                    if (content.Children[tooltipChildIndex] != itemPanel)
                    {
                        content.Children[tooltipChildIndex] = itemPanel;
                    }
                }
                else
                {
                    content.Children.Add(itemPanel);
                }

                itemPanel.Visibility = Visibility.Visible;
                visibleIndex++;
            }

            // Hide extra marker ellipses
            for (var i = visibleIndex; i < _trackerMarkers.Count; i++)
            {
                _trackerMarkers[i].Visibility = Visibility.Collapsed;
            }

            // Hide extra tooltip rows (skip index 0 which is the X label)
            for (var i = visibleIndex + 1; i < content.Children.Count; i++)
            {
                content.Children[i].Visibility = Visibility.Collapsed;
            }

            // Trim extra TrackerData items
            while (_trackerData.Items.Count > visibleIndex)
            {
                _trackerData.Items.RemoveAt(_trackerData.Items.Count - 1);
            }

            if (visibleIndex == 0)
            {
                return;
            }

            // Determine which tooltip element to position: custom template or built-in border
            FrameworkElement tooltipElement;

            if (TrackerTooltipTemplate != null && _trackerContentPresenter != null)
            {
                _trackerTooltip.Visibility = Visibility.Collapsed;
                _trackerContentPresenter.Visibility = Visibility.Visible;
                tooltipElement = _trackerContentPresenter;
            }
            else
            {
                if (_trackerContentPresenter != null)
                {
                    _trackerContentPresenter.Visibility = Visibility.Collapsed;
                }

                _trackerTooltip.Visibility = Visibility.Visible;
                tooltipElement = _trackerTooltip;
            }

            var tooltipX = mouseX + 15;
            var tooltipWidth = tooltipElement.ActualWidth > 0 ? tooltipElement.ActualWidth : 120;

            if (tooltipX + tooltipWidth > width)
            {
                tooltipX = mouseX - tooltipWidth - 15;
            }

            var tooltipY = Math.Max(0, Math.Min(overlayPos.Y - 20, height - tooltipElement.ActualHeight));

            Canvas.SetLeft(tooltipElement, tooltipX);
            Canvas.SetTop(tooltipElement, tooltipY);
        }

        /// <summary>
        /// Rebuilds the axis range cache if the data version has changed.
        /// This avoids the O(n*m) recalculation of all series min/max on every tracker update.
        /// </summary>
        private void RebuildAxisRangesCacheIfNeeded()
        {
            // Invalidate cache when series data changes
            if (_cachedAxisRangesVersion != _currentDataVersion || _cachedAxisRanges == null)
            {
                _cachedAxisRanges = new Dictionary<string, (double min, double max, double range)>();

                if (Series != null && YAxes != null)
                {
                    foreach (var yAxis in YAxes)
                    {
                        var axisRange = ComputeAxisRange(yAxis.Id);
                        _cachedAxisRanges[yAxis.Id] = axisRange;
                    }
                }

                _cachedAxisRangesVersion = _currentDataVersion;
            }
        }

        /// <summary>
        /// Computes min/max range for a specific Y-axis.
        /// Called once per axis during cache rebuild instead of on every tracker update.
        /// </summary>
        private (double min, double max, double range) ComputeAxisRange(string yAxisId)
        {
            if (Series == null || YAxes == null)
            {
                return (0, 1, 1);
            }

            var yAxis = YAxes.FirstOrDefault(a => a.Id == yAxisId) ?? YAxes[0];
            double axMin = double.MaxValue, axMax = double.MinValue;

            foreach (var s in Series)
            {
                if ((s.YAxisId ?? YAxes[0].Id) != yAxisId || s.YValues == null)
                {
                    continue;
                }

                // Optimized: Initialize from first value instead of double.MaxValue/MinValue
                if (s.YValues.Count > 0)
                {
                    axMin = Math.Min(axMin, s.YValues[0]);
                    axMax = Math.Max(axMax, s.YValues[0]);

                    for (var i = 1; i < s.YValues.Count; i++)
                    {
                        axMin = Math.Min(axMin, s.YValues[i]);
                        axMax = Math.Max(axMax, s.YValues[i]);
                    }
                }
            }

            // Use explicit axis bounds if set, otherwise use computed values
            axMin = yAxis.MinValue ?? (axMin == double.MaxValue ? 0 : axMin);
            axMax = yAxis.MaxValue ?? (axMax == double.MinValue ? 1 : axMax);
            var axRange = axMax - axMin      == 0 ? 1 : axMax - axMin;

            return (axMin, axMax, axRange);
        }

        /// <summary>
        /// Invalidates the axis range cache when data changes.
        /// Called by parent ChartControl when Series property changes.
        /// </summary>
        private void InvalidateAxisRangesCache()
        {
            _currentDataVersion++;
        }
    }
}