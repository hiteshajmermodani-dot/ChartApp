using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ChartAppLib.Models;

namespace ChartAppLib.Controls
{
    public partial class ChartControl
    {
        private Thickness UpdateDynamicMargins()
        {
            const double topPadding = 10;
            const double bottomBase = 48; // set to 48 for balanced space for x-axis label and tick values
            const double axisGap = 5;     // matches the gap used in DrawChartWithMultipleAxes and RedrawAxesOnly

            // Calculate left margin from Y-axes
            double leftMargin = 5;   // minimal padding
            double rightMargin = 10; // minimal padding

            // Support margin calculation for BoxPlot mode even if Series is null
            bool isBoxPlot = _chartType == ChartType.BoxPlot && BoxPlotSeries != null &&
                             BoxPlotSeries.Cast<object>().Any();

            if (YAxes is { Count: > 0 } && (Series is { Count: > 0 } || isBoxPlot))
            {
                var leftCount = 0;
                var rightCount = 0;

                foreach (var yAxis in YAxes)
                {
                    double valMin, valMax;

                    if (isBoxPlot)
                    {
                        // Use all values from all box plot groups
                        var yValues = BoxPlotSeries!
                                      .Cast<dynamic>().SelectMany((dynamic b) => (IEnumerable<double>)b.Values)
                                      .ToList();

                        if (yValues.Count == 0)
                        {
                            continue;
                        }

                        valMin = yValues.Min();
                        valMax = yValues.Max();
                    }
                    else if (Series is { Count: > 0 })
                    {
                        // Use cached axis ranges to avoid O(n) allocation and scan
                        RebuildAxisRangesCacheIfNeeded();

                        if (_cachedAxisRanges != null && _cachedAxisRanges.TryGetValue(yAxis.Id, out var cached))
                        {
                            valMin = cached.min;
                            valMax = cached.max;
                        }
                        else
                        {
                            var axisRange = ComputeAxisRange(yAxis.Id);
                            valMin = axisRange.min;
                            valMax = axisRange.max;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var min = yAxis.MinValue ?? valMin;
                    var max = yAxis.MaxValue ?? valMax;
                    var range = max - min == 0 ? 1 : max - min;

                    var labelWidth = MeasureYAxisLabelWidth(yAxis, min, max, range);
                    const int tickWidth = 6;
                    const int labelGap = 2;
                    var axisWidth = labelWidth + labelGap + tickWidth;

                    var showLabelExtra =
                        yAxis.ShowLabel && !string.IsNullOrEmpty(yAxis.Label) && ShowYAxisLabel ? 16 : 0;

                    if (yAxis.Position == YAxisPosition.Left)
                    {
                        if (leftCount > 0)
                        {
                            leftMargin += axisGap; // Use consistent 5px gap
                        }

                        leftMargin += axisWidth + showLabelExtra;
                        leftCount++;
                    }
                    else // Right
                    {
                        if (rightCount > 0)
                        {
                            rightMargin += axisGap; // Use consistent 5px gap
                        }

                        rightMargin += axisWidth + showLabelExtra;
                        rightCount++;
                    }
                }

                leftMargin += 4; // small extra buffer

                if (rightCount > 0)
                {
                    rightMargin += 4;
                }
            }

            // Calculate bottom margin from X-axes
            var bottomMargin = bottomBase;

            if (XAxes is { Count: > 0 })
            {
                var bottomXAxes = XAxes.Where(a => a.Position == XAxisPosition.Bottom).ToList();

                if (bottomXAxes.Count > 0)
                {
                    // Base space for tick marks + tick labels per axis
                    bottomMargin = 22;

                    foreach (var xAxis in bottomXAxes)
                    {
                        bottomMargin += 18; // tick mark + tick label space

                        if (xAxis.ShowLabel && !string.IsNullOrEmpty(xAxis.Label) && ShowXAxisLabel)
                        {
                            bottomMargin += 20; // axis label text space
                        }
                    }
                }
            }

            var margin = new Thickness(leftMargin, topPadding, rightMargin, bottomMargin);
            PART_PlotArea.Margin = margin;
            PART_Overlay.Margin = margin;

            // Update Y-axis interactive area
            PART_YAxisArea.Width = leftMargin;
            PART_YAxisArea.Margin = new Thickness(0, topPadding, 0, bottomMargin);

            // Update X-axis interactive area
            PART_XAxisArea.Height = bottomMargin;
            PART_XAxisArea.Margin = new Thickness(leftMargin, 0, rightMargin, 0);

            return margin;
        }

        private void DrawChartWithMultipleAxes(double width, double height)
        {
            double leftOffset = 0, rightOffset = 0;

            // Create a non-zoomed container for axis decorations (lines, ticks, labels).
            // PART_AxesCanvas has no RenderTransform so axes stay at constant visual size during zoom/pan.
            _axisContainer = new Canvas { IsHitTestVisible = false };

            _axisContainer.RenderTransform =
                new TranslateTransform(PART_PlotArea.Margin.Left, PART_PlotArea.Margin.Top);

            Panel.SetZIndex(_axisContainer, 1000);
            PART_AxesCanvas.Children.Add(_axisContainer);

            // If Series is null, only draw axes and grid (for box plot mode)
            if (Series == null)
            {
                // Draw axes and grid lines for box plot mode
                if (YAxes is { Count: > 0 } && XAxes is { Count: > 0 })
                {
                    int leftIdx = 0, rightIdx = 0;
                    var themeAxisBrush = GetAxisBrush();

                    foreach (var yAxis in YAxes)
                    {
                        var min = yAxis.MinValue ?? 0;
                        var max = yAxis.MaxValue ?? 1;
                        var range = max - min == 0 ? 1 : max - min;
                        var maxLabelWidth = MeasureYAxisLabelWidth(yAxis, min, max, range);
                        var axisWidth = maxLabelWidth + 8;

                        double showLabelExtra = yAxis.ShowLabel && !string.IsNullOrEmpty(yAxis.Label) && ShowYAxisLabel
                                                    ? 16
                                                    : 0;

                        // Always use theme axis brush for box plot Y axis
                        var yAxisBrushBox = themeAxisBrush;

                        if (yAxis.Position == YAxisPosition.Left)
                        {
                            if (leftIdx > 0)
                            {
                                leftOffset += 0.2;
                            }

                            DrawYAxis(yAxis, height, width, min, max, range, -leftOffset - 2, yAxisBrushBox,
                                      maxLabelWidth);

                            leftOffset += axisWidth + showLabelExtra;
                            leftIdx++;
                        }
                        else
                        {
                            if (rightIdx > 0)
                            {
                                rightOffset += 0.2;
                            }

                            DrawYAxis(yAxis, height, width, min, max, range, width + rightOffset + 10, yAxisBrushBox,
                                      maxLabelWidth);

                            rightOffset += axisWidth + showLabelExtra;
                            rightIdx++;
                        }
                    }

                    // Draw X-axes
                    double bottomOff = 0;
                    var xAxisBrush = GetAxisBrush();

                    foreach (var xAxis in XAxes)
                    {
                        const int maxPoints = 1;
                        var xMin = xAxis.MinValue ?? 0;
                        var xMax = xAxis.MaxValue ?? maxPoints - 1;

                        if (xAxis.Position == XAxisPosition.Bottom)
                        {
                            DrawXAxis(xAxis, width, height, maxPoints, height + bottomOff, xAxisBrush);
                            bottomOff += 50;
                        }
                    }

                    // Draw grid lines
                    if (YAxes.Count > 0 && YAxes[0].ShowGridLines)
                    {
                        var min = YAxes[0].MinValue ?? 0;
                        var max = YAxes[0].MaxValue ?? 1;
                        DrawYAxisGridLines(height, width, min, max, YAxes[0].GridLineBrush, YAxes[0].MajorTickCount);
                    }

                    if (XAxes.Count > 0 && XAxes[0].ShowGridLines)
                    {
                        DrawXAxisGridLines(width, height, XAxes[0].GridLineBrush, XAxes[0].MajorTickCount);
                    }
                }

                return;
            }

            // Calculate value ranges for each axis
            var yAxisRanges = new Dictionary<string, (double min, double max, double range)>();
            var xAxisRanges = new Dictionary<string, (double min, double max, int maxPoints)>();

            // Calculate Y-axis ranges — reuse tracker axis range cache when available
            RebuildAxisRangesCacheIfNeeded();

            foreach (var yAxis in YAxes)
            {
                if (_cachedAxisRanges != null && _cachedAxisRanges.TryGetValue(yAxis.Id, out var cached))
                {
                    yAxisRanges[yAxis.Id] = cached;
                }
                else
                {
                    var seriesForAxis = Series
                                        .Where(s => s.YAxisId == yAxis.Id ||
                                                    (string.IsNullOrEmpty(s.YAxisId) && yAxis == YAxes.First()))
                                        .ToList();

                    if (seriesForAxis.Count > 0)
                    {
                        double valMin = double.MaxValue, valMax = double.MinValue;

                        foreach (var s in seriesForAxis)
                        {
                            foreach (var v in s.YValues)
                            {
                                if (v < valMin)
                                {
                                    valMin = v;
                                }

                                if (v > valMax)
                                {
                                    valMax = v;
                                }
                            }
                        }

                        var min = yAxis.MinValue ?? valMin;
                        var max = yAxis.MaxValue ?? valMax;
                        var range = max - min == 0 ? 1 : max - min;
                        yAxisRanges[yAxis.Id] = (min, max, range);
                    }
                }
            }

            // Calculate X-axis ranges
            foreach (var xAxis in XAxes)
            {
                var seriesForAxis = Series
                                    .Where(s => s.XAxisId == xAxis.Id ||
                                                (string.IsNullOrEmpty(s.XAxisId) && xAxis == XAxes.First())).ToList();

                if (seriesForAxis.Count > 0)
                {
                    var maxPoints = seriesForAxis.Max(s => s.YValues.Count);
                    var min = xAxis.MinValue ?? 0;
                    var max = xAxis.MaxValue ?? maxPoints - 1;
                    xAxisRanges[xAxis.Id] = (min, max, maxPoints);
                }
            }

            // Draw Y-axes with adequate margin between stacked axes (5px ensures good spacing even when labels are hidden)
            const double axisGap = 5;
            var leftAxisIndex = 0;
            var rightAxisIndex = 0;

            foreach (var yAxis in YAxes)
            {
                if (yAxisRanges.ContainsKey(yAxis.Id))
                {
                    var (min, max, range) = yAxisRanges[yAxis.Id];

                    // Find the first series for this Y axis
                    var seriesForAxis = Series
                                        .Where(s => s.YAxisId == yAxis.Id ||
                                                    (string.IsNullOrEmpty(s.YAxisId) && yAxis == YAxes.First()))
                                        .ToList();

                    var seriesBrush = seriesForAxis.Count > 0 ? seriesForAxis[0].Stroke : null;

                    var yAxisBrush = seriesBrush == null || seriesBrush == Brushes.Transparent
                                         ? GetAxisBrush()
                                         : seriesBrush;

                    // Measure the widest tick label for this axis to determine spacing
                    var maxLabelWidth = MeasureYAxisLabelWidth(yAxis, min, max, range);
                    const double tickWidth = 6;
                    const double labelGap = 2; // tiny gap between label text and tick
                    var axisWidth = maxLabelWidth + labelGap + tickWidth;

                    double showLabelExtra =
                        yAxis.ShowLabel && !string.IsNullOrEmpty(yAxis.Label) && ShowYAxisLabel ? 16 : 0;

                    var totalAxisWidth = axisWidth + showLabelExtra;

                    if (yAxis.Position == YAxisPosition.Left)
                    {
                        if (leftAxisIndex > 0)
                        {
                            leftOffset += axisGap;
                        }

                        var xPosition = -leftOffset - 2;
                        DrawYAxis(yAxis, height, width, min, max, range, xPosition, yAxisBrush, maxLabelWidth);
                        leftOffset += totalAxisWidth;
                        leftAxisIndex++;
                    }
                    else // Right
                    {
                        if (rightAxisIndex > 0)
                        {
                            rightOffset += axisGap;
                        }

                        DrawYAxis(yAxis, height, width, min, max, range, width + rightOffset + 10, yAxisBrush,
                                  maxLabelWidth);

                        rightOffset += totalAxisWidth;
                        rightAxisIndex++;
                    }
                }
            }

            // Draw X-axes
            double bottomOffset = 0;
            double topOffset = 0;
            var axisBrush = (Brush)FindResource("ChartAxisBrush");

            foreach (var xAxis in XAxes)
            {
                if (xAxisRanges.ContainsKey(xAxis.Id))
                {
                    var (min, max, maxPoints) = xAxisRanges[xAxis.Id];

                    // Get XValues from the first series on this axis (for tick label display)
                    var xSeries = Series.FirstOrDefault(s => s.XAxisId == xAxis.Id ||
                                                             (string.IsNullOrEmpty(s.XAxisId) &&
                                                              xAxis == XAxes.First()));

                    var seriesXValues = xSeries?.HasExplicitXValues == true ? xSeries.XValues : null;

                    // Use themed axis brush for all X axes
                    if (xAxis.Position == XAxisPosition.Bottom)
                    {
                        DrawXAxis(xAxis, width, height, maxPoints, height + bottomOffset, axisBrush, seriesXValues);
                        bottomOffset += 50; // Space for next bottom axis
                    }
                    else // Top
                    {
                        DrawXAxis(xAxis, width, height, maxPoints, -topOffset - 50, axisBrush, seriesXValues);
                        topOffset += 50; // Space for next top axis
                    }
                }
            }

            // Draw grid lines for primary axes only
            if (YAxes.Count > 0 && yAxisRanges.ContainsKey(YAxes[0].Id))
            {
                var (min, max, range) = yAxisRanges[YAxes[0].Id];

                if (YAxes[0].ShowGridLines)
                {
                    DrawYAxisGridLines(height, width, min, max, YAxes[0].GridLineBrush, YAxes[0].MajorTickCount);
                }
            }

            if (XAxes.Count > 0 && xAxisRanges.ContainsKey(XAxes[0].Id))
            {
                if (XAxes[0].ShowGridLines)
                {
                    DrawXAxisGridLines(width, height, XAxes[0].GridLineBrush, XAxes[0].MajorTickCount);
                }
            }

            // Draw series
            foreach (var series in Series)
            {
                var yAxisId = series.YAxisId ?? YAxes[0].Id;
                var xAxisId = series.XAxisId ?? XAxes[0].Id;

                if (yAxisRanges.ContainsKey(yAxisId) && xAxisRanges.ContainsKey(xAxisId))
                {
                    var (yMin, yMax, yRange) = yAxisRanges[yAxisId];

                    switch (_chartType)
                    {
                        case ChartType.BoxPlot:
                            DrawBoxPlot(series, width, height, yMin, yRange);

                            break;
                        case ChartType.Histogram:
                            DrawHistogram(series, width, height, yMin, yRange);

                            break;
                        case ChartType.ScatterPlot:
                            DrawScatterPlot(series, width, height, yMin, yRange);

                            break;
                        case ChartType.BubblePlot:
                            DrawBubblePlot(series, width, height, yMin, yRange);

                            break;
                        case ChartType.LinePlot:
                        default:
                            DrawLineSeries(series, width, height, yMin, yRange);

                            break;
                    }

                    if (PART_Legend.Visibility == Visibility.Visible)
                    {
                        DrawLegend(series);
                    }
                }
            }

            // Draw annotations
            DrawAnnotations(width, height, yAxisRanges, xAxisRanges);
        }

        private double MeasureYAxisLabelWidth(YAxisDefinition yAxis, double min, double max, double range)
        {
            // Account for zoom when measuring label widths
            var scaleY = ZoomTransform.ScaleY;
            var panY = PanTransform.Y;
            var centerY = ZoomTransform.CenterY;
            var height = PART_Canvas.ActualHeight > 0 ? PART_Canvas.ActualHeight : 300;

            var canvasTop = (0         - panY - centerY * (1 - scaleY)) / scaleY;
            var canvasBottom = (height - panY - centerY * (1 - scaleY)) / scaleY;
            var visibleMax = min          + (height - canvasTop)    / height * range;
            var visibleMin = min          + (height - canvasBottom) / height * range;
            var visibleRange = visibleMax - visibleMin;
            var majorStepValue = visibleRange / yAxis.MajorTickCount;

            double maxWidth = 0;
            var typeface = new Typeface("Segoe UI");
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            for (var i = 0; i <= yAxis.MajorTickCount; i++)
            {
                var value = visibleMin + i * majorStepValue + _yAxisOffset;

                var ft = new FormattedText(
                                           yAxis.FormatLabel(value),
                                           CultureInfo.CurrentCulture,
                                           FlowDirection.LeftToRight,
                                           typeface,
                                           10,
                                           Brushes.Black,
                                           dpi);

                if (ft.Width > maxWidth)
                {
                    maxWidth = ft.Width;
                }
            }

            return maxWidth;
        }

        private void DrawYAxis(YAxisDefinition yAxis, double height, double width, double min, double max, double range,
                               double xPosition, Brush labelBrush, double measuredLabelWidth)
        {
            var majorStepPixels = height / yAxis.MajorTickCount;

            // Compute zoom-adjusted value range visible on screen
            var scaleY = ZoomTransform.ScaleY;
            var panY = PanTransform.Y;
            var centerY = ZoomTransform.CenterY;

            // Screen pixel 0 (top) and height (bottom) map to these canvas-space Y coordinates:
            var canvasTop = (0         - panY - centerY * (1 - scaleY)) / scaleY;
            var canvasBottom = (height - panY - centerY * (1 - scaleY)) / scaleY;

            // Data values at visible top/bottom
            var visibleMax = min          + (height - canvasTop)    / height * range;
            var visibleMin = min          + (height - canvasBottom) / height * range;
            var visibleRange = visibleMax - visibleMin;
            var majorStepValue = visibleRange / yAxis.MajorTickCount;

            // Use the provided labelBrush (series color) for axis line and tick labels

            // Draw axis line
            var axisLine = new Line
                           {
                               X1 = xPosition,
                               X2 = xPosition,
                               Y1 = 0,
                               Y2 = height,
                               Stroke = labelBrush,
                               StrokeThickness = 1
                           };

            Panel.SetZIndex(axisLine, 1000);
            _axisContainer!.Children.Add(axisLine);

            const double tickLength = 6;
            var labelOffset = tickLength + 2 + measuredLabelWidth; // tick + gap + label width

            for (var i = 0; i <= yAxis.MajorTickCount; i++)
            {
                var y = height         - i * majorStepPixels;
                var value = visibleMin + i * majorStepValue + _yAxisOffset;

                // Major tick
                var tickLine = new Line
                               {
                                   X1 = xPosition,
                                   X2 = xPosition + (yAxis.Position == YAxisPosition.Left ? -tickLength : tickLength),
                                   Y1 = y,
                                   Y2 = y,
                                   Stroke = labelBrush,
                                   StrokeThickness = 1
                               };

                Panel.SetZIndex(tickLine, 1000);
                _axisContainer!.Children.Add(tickLine);

                // Label
                var label = new TextBlock
                            {
                                Text = yAxis.FormatLabel(value),
                                FontSize = 10,
                                Foreground = labelBrush
                            };

                if (yAxis.Position == YAxisPosition.Left)
                {
                    Canvas.SetLeft(label, xPosition - labelOffset);
                }
                else
                {
                    Canvas.SetLeft(label, xPosition + tickLength + 2);
                }

                Canvas.SetTop(label, y - 8);
                Panel.SetZIndex(label, 1000);
                _axisContainer!.Children.Add(label);

                // Minor ticks
                if (i < yAxis.MajorTickCount)
                {
                    var minorStepPixels = majorStepPixels / (yAxis.MinorTickCount + 1);

                    for (var m = 1; m <= yAxis.MinorTickCount; m++)
                    {
                        var minorTick = new Line
                                        {
                                            X1 = xPosition,
                                            X2 = xPosition + (yAxis.Position == YAxisPosition.Left ? -3 : 3),
                                            Y1 = y         - m * minorStepPixels,
                                            Y2 = y         - m * minorStepPixels,
                                            Stroke = Brushes.Gray,
                                            StrokeThickness = 0.5
                                        };

                        Panel.SetZIndex(minorTick, 1000);
                        _axisContainer!.Children.Add(minorTick);
                    }
                }
            }

            // Axis label
            if (yAxis.ShowLabel && !string.IsNullOrEmpty(yAxis.Label) && ShowYAxisLabel)
            {
                var axisLabel = new TextBlock
                                {
                                    Text = yAxis.Label,
                                    FontWeight = FontWeights.Bold,
                                    FontSize = 11,
                                    Foreground = labelBrush,
                                    RenderTransform = new RotateTransform(-90)
                                };

                if (yAxis.Position == YAxisPosition.Left)
                {
                    Canvas.SetLeft(axisLabel, xPosition - labelOffset - 14);
                }
                else
                {
                    Canvas.SetLeft(axisLabel, xPosition + labelOffset + 5);
                }

                Canvas.SetTop(axisLabel, height / 2 + 50);
                Panel.SetZIndex(axisLabel, 1000);
                _axisContainer!.Children.Add(axisLabel);
            }
        }

        private void DrawXAxis(XAxisDefinition xAxis, double width, double height, int maxPoints, double yPosition,
                               Brush labelBrush, IList<double>? seriesXValues = null)
        {
            var majorStepPixels = width / xAxis.MajorTickCount;

            // Compute zoom-adjusted visible X range
            var scaleX = ZoomTransform.ScaleX;
            var panX = PanTransform.X;
            var centerX = ZoomTransform.CenterX;

            // Screen pixel 0 (left) and width (right) map to these canvas-space X coordinates:
            var canvasLeft = (0      - panX - centerX * (1 - scaleX)) / scaleX;
            var canvasRight = (width - panX - centerX * (1 - scaleX)) / scaleX;

            // Data indices at visible left/right
            var visibleLeftIndex = canvasLeft   / width * (maxPoints - 1);
            var visibleRightIndex = canvasRight / width * (maxPoints - 1);
            var visibleMajorStep = (visibleRightIndex                - visibleLeftIndex) / xAxis.MajorTickCount;

            // Draw solid baseline across full width
            _axisContainer!.Children.Add(new Line
                                         {
                                             X1 = 0,
                                             X2 = width,
                                             Y1 = yPosition,
                                             Y2 = yPosition,
                                             Stroke = labelBrush, // Use theme brush
                                             StrokeThickness = 1
                                         });

            for (var i = 0; i <= xAxis.MajorTickCount; i++)
            {
                var x = i * majorStepPixels;
                // Use zoom-adjusted data index for this tick position
                var dataIndex = visibleLeftIndex + i * visibleMajorStep + _xAxisOffset;
                double value;

                if (seriesXValues != null && seriesXValues.Count > 0)
                {
                    var idx0 = (int)Math.Floor(dataIndex);
                    var idx1 = Math.Min(idx0 + 1, seriesXValues.Count - 1);
                    idx0 = Math.Clamp(idx0, 0, seriesXValues.Count - 1);
                    var frac = dataIndex - Math.Floor(dataIndex);
                    value = seriesXValues[idx0] * (1 - frac) + seriesXValues[idx1] * frac;
                }
                else
                {
                    value = dataIndex;
                }

                // Major tick
                var tickLine = new Line
                               {
                                   X1 = x,
                                   X2 = x,
                                   Y1 = yPosition,
                                   Y2 = yPosition + (xAxis.Position == XAxisPosition.Bottom ? 8 : -8),
                                   Stroke = labelBrush, // Use theme brush
                                   StrokeThickness = 1
                               };

                _axisContainer!.Children.Add(tickLine);

                // Label
                var label = new TextBlock
                            {
                                Text = xAxis.FormatLabel(value),
                                FontSize = 10,
                                Foreground = labelBrush // Use theme brush
                            };

                Canvas.SetLeft(label, x - 10);

                if (xAxis.Position == XAxisPosition.Bottom)
                {
                    Canvas.SetTop(label, yPosition + 12);
                }
                else
                {
                    Canvas.SetTop(label, yPosition - 22);
                }

                _axisContainer!.Children.Add(label);

                // Minor ticks
                if (i < xAxis.MajorTickCount)
                {
                    var minorStepPixels = majorStepPixels / (xAxis.MinorTickCount + 1);

                    for (var m = 1; m <= xAxis.MinorTickCount; m++)
                    {
                        var minorTick = new Line
                                        {
                                            X1 = x + m * minorStepPixels,
                                            X2 = x + m * minorStepPixels,
                                            Y1 = yPosition,
                                            Y2 = yPosition + (xAxis.Position == XAxisPosition.Bottom ? 4 : -4),
                                            Stroke = labelBrush, // Use theme brush for minor ticks too
                                            StrokeThickness = 0.5
                                        };

                        _axisContainer!.Children.Add(minorTick);
                    }
                }
            }

            // Axis label
            if (xAxis.ShowLabel && !string.IsNullOrEmpty(xAxis.Label) && ShowXAxisLabel)
            {
                var axisLabel = new TextBlock
                                {
                                    Text = xAxis.Label,
                                    FontWeight = FontWeights.Bold,
                                    FontSize = 12,
                                    Foreground = labelBrush // Use theme brush
                                };

                Canvas.SetLeft(axisLabel, width / 2 - 40);

                if (xAxis.Position == XAxisPosition.Bottom)
                {
                    Canvas.SetTop(axisLabel, yPosition + 12 + 16 + 6);
                }
                else
                {
                    Canvas.SetTop(axisLabel, yPosition - 42);
                }

                _axisContainer!.Children.Add(axisLabel);
            }
        }

        private void DrawYAxisGridLines(double height, double width, double min, double max, Brush gridBrush,
                                        int majorTickCount = -1)
        {
            if (!ShowMajorGridLines && !ShowMinorGridLines)
            {
                return;
            }

            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var ticks = majorTickCount > 0 ? majorTickCount : YMajorTickCount;
            var majorStepPixels = height / ticks;

            // Draw alternating bands between major grid lines first (behind lines and series)
            if (ShowMajorGridLines)
            {
                for (var i = 0; i < ticks; i++)
                {
                    if (i % 2 != 0)
                    {
                        continue;
                    }

                    var bandTop = height - (i + 1) * majorStepPixels;

                    var band = new Rectangle
                               {
                                   Width = width,
                                   Height = majorStepPixels,
                                   Fill = MajorGridBandBrush,
                                   IsHitTestVisible = false
                               };

                    Canvas.SetLeft(band, 0);
                    Canvas.SetTop(band, bandTop);
                    Panel.SetZIndex(band, 0);
                    PART_Canvas.Children.Add(band);
                }
            }

            for (var i = 0; i <= ticks; i++)
            {
                var y = height - i * majorStepPixels;

                // Major grid line
                if (ShowMajorGridLines)
                {
                    PART_Canvas.Children.Add(new Line
                                             {
                                                 X1 = 0,
                                                 X2 = width,
                                                 Y1 = y,
                                                 Y2 = y,
                                                 Stroke = gridBrush,
                                                 StrokeThickness = 1 / zoomScale,
                                                 StrokeDashArray = [4, 2],
                                                 IsHitTestVisible = false
                                             });
                }

                // Minor grid lines
                if (ShowMinorGridLines && i < ticks)
                {
                    var minorStepPixels = majorStepPixels / (YMinorTickCount + 1);

                    for (var m = 1; m <= YMinorTickCount; m++)
                    {
                        PART_Canvas.Children.Add(new Line
                                                 {
                                                     X1 = 0,
                                                     X2 = width,
                                                     Y1 = y - m * minorStepPixels,
                                                     Y2 = y - m * minorStepPixels,
                                                     Stroke = MinorGridLineBrush,
                                                     StrokeThickness = 0.5 / zoomScale,
                                                     StrokeDashArray = [2, 4],
                                                     IsHitTestVisible = false
                                                 });
                    }
                }
            }
        }

        private void DrawXAxisGridLines(double width, double height, Brush gridBrush, int majorTickCount = -1)
        {
            if (!ShowMajorGridLines && !ShowMinorGridLines)
            {
                return;
            }

            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var ticks = majorTickCount > 0 ? majorTickCount : XMajorTickCount;
            var majorStepPixels = width / ticks;

            for (var i = 0; i <= ticks; i++)
            {
                var x = i * majorStepPixels;

                // Major grid line
                if (ShowMajorGridLines)
                {
                    PART_Canvas.Children.Add(new Line
                                             {
                                                 X1 = x,
                                                 X2 = x,
                                                 Y1 = 0,
                                                 Y2 = height,
                                                 Stroke = gridBrush,
                                                 StrokeThickness = 1 / zoomScale,
                                                 StrokeDashArray = [4, 2],
                                                 IsHitTestVisible = false
                                             });
                }

                // Minor grid lines
                if (ShowMinorGridLines && i < ticks)
                {
                    var minorStepPixels = majorStepPixels / (XMinorTickCount + 1);

                    for (var m = 1; m <= XMinorTickCount; m++)
                    {
                        PART_Canvas.Children.Add(new Line
                                                 {
                                                     X1 = x + m * minorStepPixels,
                                                     X2 = x + m * minorStepPixels,
                                                     Y1 = 0,
                                                     Y2 = height,
                                                     Stroke = MinorGridLineBrush,
                                                     StrokeThickness = 0.5 / zoomScale,
                                                     StrokeDashArray = [2, 4],
                                                     IsHitTestVisible = false
                                                 });
                    }
                }
            }
        }
    }
}