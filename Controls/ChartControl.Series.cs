using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ChartApp.Models;

namespace ChartApp.Controls
{
    public partial class ChartControl
    {
        private const int MaxPointMarkersCount = 200;
        private const double Tolerance = 1e-8;

        private void DrawLineSeries(DataSeries series, double width, double height, double min, double range)
        {
            var count = series.YValues.Count;

            if (count < 2 || width <= 0 || height <= 0)
            {
                return;
            }

            var step = width / (count - 1);
            var drawMarkers = count <= MaxPointMarkersCount;

            // Use StreamGeometry for much faster rendering with large point sets
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                if (step >= 1.0)
                {
                    // Enough pixels for every point — draw only the visible range.
                    var startIdx = Math.Max(0, (int)_xAxisOffset - 1);
                    var endIdx = Math.Min(count                  - 1, (int)(_xAxisOffset + width / step) + 2);

                    if (startIdx >= count)
                    {
                        return;
                    }

                    var x0 = (startIdx - _xAxisOffset) * step;
                    var y0 = height - (series.YValues[startIdx] + _yAxisOffset - min) / range * height;
                    ctx.BeginFigure(new Point(x0, y0), false, false);

                    for (var i = startIdx + 1; i <= endIdx; i++)
                    {
                        var xPos = (i - _xAxisOffset) * step;
                        var y = height - (series.YValues[i] + _yAxisOffset - min) / range * height;
                        ctx.LineTo(new Point(xPos, y), true, false);
                    }
                }
                else
                {
                    var pixelW = Math.Max(1, (int)width);
                    var started = false;

                    for (var px = 0; px < pixelW; px++)
                    {
                        var iStart = (int)(px     / step + _xAxisOffset);
                        var iEnd = (int)((px + 1) / step + _xAxisOffset);
                        iStart = Math.Clamp(iStart, 0, count - 1);
                        iEnd = Math.Clamp(iEnd, 0, count     - 1);

                        if (iStart > iEnd || iStart >= count)
                        {
                            continue;
                        }

                        var yMinVal = series.YValues[iStart];
                        var yMaxVal = series.YValues[iStart];
                        var yMinIdx = iStart;
                        var yMaxIdx = iStart;

                        for (var i = iStart + 1; i <= iEnd; i++)
                        {
                            var v = series.YValues[i];

                            if (v < yMinVal)
                            {
                                yMinVal = v;
                                yMinIdx = i;
                            }

                            if (v > yMaxVal)
                            {
                                yMaxVal = v;
                                yMaxIdx = i;
                            }
                        }

                        var xCanvas = (iStart - _xAxisOffset) * step;
                        var yMinPx = height - (yMinVal + _yAxisOffset - min) / range * height;
                        var yMaxPx = height - (yMaxVal + _yAxisOffset - min) / range * height;

                        if (!started)
                        {
                            ctx.BeginFigure(new Point(xCanvas, yMinIdx <= yMaxIdx ? yMinPx : yMaxPx), false, false);
                            started = true;
                        }

                        // Emit min then max (or max then min) to preserve the visual envelope.
                        if (yMinIdx <= yMaxIdx)
                        {
                            ctx.LineTo(new Point(xCanvas, yMinPx), true, false);

                            if (yMinIdx != yMaxIdx)
                            {
                                ctx.LineTo(new Point(xCanvas, yMaxPx), true, false);
                            }
                        }
                        else
                        {
                            ctx.LineTo(new Point(xCanvas, yMaxPx), true, false);

                            if (yMinIdx != yMaxIdx)
                            {
                                ctx.LineTo(new Point(xCanvas, yMinPx), true, false);
                            }
                        }
                    }

                    if (!started)
                    {
                        return;
                    }
                }
            }

            geometry.Freeze();

            // Use default line thickness if series thickness is 0 (e.g., when switching from scatter/bubble plots)
            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var lineThickness = (series.Thickness > 0 ? series.Thickness : 1.5) / zoomScale;

            var path = new Path
                       {
                           Data = geometry,
                           Stroke = series.Stroke,
                           StrokeThickness = lineThickness,
                           IsHitTestVisible = false
                       };

            var targetCanvas = _seriesCanvas ?? PART_Canvas;
            targetCanvas.Children.Add(path);
            _seriesPaths.Add(path);

            // Only draw markers for reasonable dataset sizes to avoid performance degradation
            if (drawMarkers && count <= MaxPointMarkersCount)
            {
                // Pre-calculate visible range to avoid rendering off-screen markers
                var visibleStart = Math.Max(0, (int)(_xAxisOffset - 50 / step));
                var visibleEnd = Math.Min(count - 1, (int)(_xAxisOffset + (width + 50) / step) + 1);

                for (var i = visibleStart; i <= visibleEnd; i++)
                {
                    var xPosition = (i - _xAxisOffset) * step;

                    var adjustedValue = series.YValues[i] + _yAxisOffset;
                    var y = height                        - (adjustedValue - min) / range * height;

                    var markerSize = 8.0 / zoomScale;

                    var point = new Ellipse
                                {
                                    Width = markerSize,
                                    Height = markerSize,
                                    Fill = series.Stroke,
                                    Stroke = Brushes.White,
                                    StrokeThickness = 1.5 / zoomScale,
                                    IsHitTestVisible = false
                                };

                    Panel.SetZIndex(point, 1000);
                    Canvas.SetLeft(point, xPosition - markerSize / 2);
                    Canvas.SetTop(point, y          - markerSize / 2);
                    targetCanvas.Children.Add(point);
                }
            }
        }

        private void DrawBoxPlot(DataSeries series, double width, double height, double min, double range)
        {
            var count = series.YValues.Count;

            if (count < 1)
            {
                return;
            }

            var sorted = series.YValues.OrderBy(v => v).ToList();
            var median = Percentile(sorted, 0.5);
            var q1 = Percentile(sorted, 0.25);
            var q3 = Percentile(sorted, 0.75);
            var iqr = q3 - q1;
            var lowerWhisker = sorted.FirstOrDefault(v => v >= q1 - 1.5 * iqr, sorted.First());
            var upperWhisker = sorted.LastOrDefault(v => v  <= q3 + 1.5 * iqr, sorted.Last());

            // Box width and position
            var boxWidth = width  * 0.15;
            var boxCenter = width / 2;

            // Draw box (Q1 to Q3)
            var box = new Rectangle
                      {
                          Width = boxWidth,
                          Height = Math.Abs(Y(q3) - Y(q1)),
                          Stroke = series.Stroke,
                          StrokeThickness = 2,
                          Fill = new SolidColorBrush(Color.FromArgb(40, ((SolidColorBrush)series.Stroke).Color.R,
                                                                    ((SolidColorBrush)series.Stroke).Color.G,
                                                                    ((SolidColorBrush)series.Stroke).Color.B)),
                          IsHitTestVisible = false
                      };

            Canvas.SetLeft(box, boxCenter - boxWidth / 2);
            Canvas.SetTop(box, Math.Min(Y(q1), Y(q3)));
            Panel.SetZIndex(box, 10); // Lower than axis
            PART_Canvas.Children.Add(box);

            // Median line
            var medianLine = new Line
                             {
                                 X1 = boxCenter - boxWidth / 2,
                                 X2 = boxCenter + boxWidth / 2,
                                 Y1 = Y(median),
                                 Y2 = Y(median),
                                 Stroke = series.Stroke,
                                 StrokeThickness = 2
                             };

            Panel.SetZIndex(medianLine, 10);
            PART_Canvas.Children.Add(medianLine);

            // Whiskers
            var lowerWhiskerLine = new Line
                                   {
                                       X1 = boxCenter,
                                       X2 = boxCenter,
                                       Y1 = Y(lowerWhisker),
                                       Y2 = Y(q1),
                                       Stroke = series.Stroke,
                                       StrokeThickness = 1.5
                                   };

            Panel.SetZIndex(lowerWhiskerLine, 10);
            PART_Canvas.Children.Add(lowerWhiskerLine);

            var upperWhiskerLine = new Line
                                   {
                                       X1 = boxCenter,
                                       X2 = boxCenter,
                                       Y1 = Y(q3),
                                       Y2 = Y(upperWhisker),
                                       Stroke = series.Stroke,
                                       StrokeThickness = 1.5
                                   };

            Panel.SetZIndex(upperWhiskerLine, 10);
            PART_Canvas.Children.Add(upperWhiskerLine);

            // Whisker caps
            var lowerCap = new Line
                           {
                               X1 = boxCenter - boxWidth / 4,
                               X2 = boxCenter + boxWidth / 4,
                               Y1 = Y(lowerWhisker),
                               Y2 = Y(lowerWhisker),
                               Stroke = series.Stroke,
                               StrokeThickness = 1.5
                           };

            Panel.SetZIndex(lowerCap, 10);
            PART_Canvas.Children.Add(lowerCap);

            var upperCap = new Line
                           {
                               X1 = boxCenter - boxWidth / 4,
                               X2 = boxCenter + boxWidth / 4,
                               Y1 = Y(upperWhisker),
                               Y2 = Y(upperWhisker),
                               Stroke = series.Stroke,
                               StrokeThickness = 1.5
                           };

            Panel.SetZIndex(upperCap, 10);
            PART_Canvas.Children.Add(upperCap);

            return;

            // Y coordinate helpers
            double Y(double v)
            {
                return height - (v + _yAxisOffset - min) / range * height;
            }
        }

        private void DrawHistogram(DataSeries series, double width, double height, double min, double range)
        {
            int count = series.YValues.Count;

            if (count < 1)
            {
                return;
            }

            // Single pass: find min and max together
            double valMin = series.YValues[0], valMax = series.YValues[0];

            for (var i = 1; i < count; i++)
            {
                var v = series.YValues[i];

                if (v < valMin)
                {
                    valMin = v;
                }
                else if (v > valMax)
                {
                    valMax = v;
                }
            }

            if (Math.Abs(valMax - valMin) < Tolerance)
            {
                valMax = valMin + 1;
            }

            var binCount = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));
            var binWidth = (valMax - valMin) / binCount;
            var bins = new int[binCount];

            foreach (var v in series.YValues)
            {
                var bin = (int)((v - valMin) / binWidth);

                if (bin >= binCount)
                {
                    bin = binCount - 1;
                }

                bins[bin]++;
            }

            var maxBin = 0;

            foreach (var b in bins)
            {
                if (b > maxBin)
                {
                    maxBin = b;
                }
            }

            if (maxBin == 0)
            {
                return;
            }

            // Create brush and pen once — avoids one allocation per bar
            var strokeColor = ((SolidColorBrush)series.Stroke).Color;

            var fillBrush =
                new SolidColorBrush(Color.FromArgb((byte)(0.6 * 255), strokeColor.R, strokeColor.G, strokeColor.B));

            fillBrush.Freeze();
            var borderPen = new Pen(series.Stroke, 1.0);
            borderPen.Freeze();

            var barWidth = width / binCount;
            var drawingGroup = new DrawingGroup();

            using (var ctx = drawingGroup.Open())
            {
                // Anchor coordinate space to full canvas area so DrawingImage renders at the correct position
                ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

                for (var i = 0; i < binCount; i++)
                {
                    if (bins[i] == 0)
                    {
                        continue;
                    }

                    var barHeight = (double)bins[i] / maxBin * height * 0.9;
                    var rectX = i                   * barWidth + 1;
                    var rectY = height                         - barHeight;
                    var rectW = Math.Max(1, barWidth - 2);

                    ctx.DrawRectangle(fillBrush, borderPen, new Rect(rectX, rectY, rectW, barHeight));
                }
            }

            drawingGroup.Freeze();

            var image = new Image
                        {
                            Source = new DrawingImage(drawingGroup),
                            IsHitTestVisible = false
                        };

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);
            (_seriesCanvas ?? PART_Canvas).Children.Add(image);
            _seriesImages.Add(image);
        }

        private void DrawScatterPlot(DataSeries series, double width, double height, double min, double range)
        {
            var count = series.YValues.Count;

            if (count < 1)
            {
                return;
            }

            var step = count > 1 ? width / (count - 1) : 0;

            // Compensate each axis independently so circles stay circular under asymmetric zoom
            var scaleX = ZoomTransform.ScaleX > 0 ? ZoomTransform.ScaleX : 1.0;
            var scaleY = ZoomTransform.ScaleY > 0 ? ZoomTransform.ScaleY : 1.0;
            const double baseRadius = 4.0;
            var radiusX = baseRadius / scaleX;
            var radiusY = baseRadius / scaleY;
            var penThickness = 1.5 / Math.Sqrt(scaleX * scaleY);

            var targetCanvas = _seriesCanvas ?? PART_Canvas;

            // Use DrawingVisual for high-performance rendering of large point sets
            var drawingGroup = new DrawingGroup();

            using (var ctx = drawingGroup.Open())
            {
                // Anchor coordinate space to full canvas area so DrawingImage renders at the correct position
                ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

                var brush = series.Stroke;
                var pen = new Pen(Brushes.White, penThickness);

                // Pre-calculate visible range to avoid rendering off-screen points
                var visibleStart = Math.Max(0, (int)(_xAxisOffset - 50 / step));
                var visibleEnd = Math.Min(count - 1, (int)(_xAxisOffset + (width + 50) / step) + 1);

                for (var i = visibleStart; i <= visibleEnd; i++)
                {
                    if (i >= series.YValues.Count)
                    {
                        break;
                    }

                    var xPosition = i * step;
                    var adjustedValue = series.YValues[i] + _yAxisOffset;
                    var y = height                        - (adjustedValue - min) / range * height;

                    ctx.DrawEllipse(brush, pen, new Point(xPosition, y), radiusX, radiusY);
                }
            }

            drawingGroup.Freeze();

            // Offset Image by DrawingGroup bounds so that drawing coordinate (0,0)
            // aligns with canvas coordinate (0,0). Ellipses at edges extend beyond
            // the anchor rect, enlarging the bounds — without this offset the entire
            // scatter plot shifts right/down by the ellipse radius.
            var bounds = drawingGroup.Bounds;

            var image = new Image
                        {
                            Source = new DrawingImage(drawingGroup),
                            IsHitTestVisible = false
                        };

            Panel.SetZIndex(image, 1000);
            Canvas.SetLeft(image, bounds.X);
            Canvas.SetTop(image, bounds.Y);
            targetCanvas.Children.Add(image);
            _seriesImages.Add(image);
        }

        private void DrawBubblePlot(DataSeries series, double width, double height, double min, double range)
        {
            var count = series.YValues.Count;

            if (count < 1 || series.ZValues == null || series.ZValues.Count < count)
            {
                return;
            }

            var step = count > 1 ? width / (count - 1) : 0;
            var zMin = series.ZValues.Min();
            var zMax = series.ZValues.Max();
            var zRange = zMax - zMin == 0 ? 1 : zMax - zMin;

            // Compensate each axis independently so bubbles stay circular under asymmetric zoom
            var scaleX = ZoomTransform.ScaleX > 0 ? ZoomTransform.ScaleX : 1.0;
            var scaleY = ZoomTransform.ScaleY > 0 ? ZoomTransform.ScaleY : 1.0;
            var penThickness = 1.5 / Math.Sqrt(scaleX * scaleY);
            const double minRadiusBase = 3.0;
            const double maxRadiusBase = 15.0;

            var targetCanvas = _seriesCanvas ?? PART_Canvas;

            // Use DrawingVisual for high-performance rendering of large bubble sets
            var drawingGroup = new DrawingGroup();

            using (var ctx = drawingGroup.Open())
            {
                // Anchor coordinate space to full canvas area so DrawingImage renders at the correct position
                ctx.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

                var pen = new Pen(Brushes.White, penThickness);
                pen.Freeze();

                // Create brush once with opacity applied
                var bubbleBrush = new SolidColorBrush(((SolidColorBrush)series.Stroke).Color) { Opacity = 0.7 };
                bubbleBrush.Freeze();

                // Pre-calculate visible range to avoid rendering off-screen bubbles
                var visibleStart = Math.Max(0, (int)(_xAxisOffset - 50 / step));
                var visibleEnd = Math.Min(count - 1, (int)(_xAxisOffset + (width + 50) / step) + 1);

                for (var i = visibleStart; i <= visibleEnd; i++)
                {
                    if (i >= series.YValues.Count || i >= series.ZValues.Count)
                    {
                        break;
                    }

                    var xPosition = i * step;
                    var adjustedValue = series.YValues[i] + _yAxisOffset;
                    var y = height                        - (adjustedValue - min) / range * height;

                    // Normalize Z value to base radius, then divide per-axis to cancel the zoom stretch
                    var zNormalized = (series.ZValues[i] - zMin) / zRange;
                    var radiusBase = minRadiusBase + zNormalized * (maxRadiusBase - minRadiusBase);
                    var radiusX = radiusBase / scaleX;
                    var radiusY = radiusBase / scaleY;

                    ctx.DrawEllipse(bubbleBrush, pen, new Point(xPosition, y), radiusX, radiusY);
                }
            }

            drawingGroup.Freeze();

            // Offset Image by DrawingGroup bounds so drawing origin aligns with canvas origin
            var bounds = drawingGroup.Bounds;

            var image = new Image
                        {
                            Source = new DrawingImage(drawingGroup),
                            IsHitTestVisible = false
                        };

            Panel.SetZIndex(image, 1000);
            Canvas.SetLeft(image, bounds.X);
            Canvas.SetTop(image, bounds.Y);
            targetCanvas.Children.Add(image);
            _seriesImages.Add(image);
        }

        private void DrawLegend(DataSeries series)
        {
            var axisBrush = (Brush)FindResource("ChartAxisBrush");

            var legendItem = new StackPanel
                             {
                                 Orientation = Orientation.Horizontal,
                                 Margin = new Thickness(10, 0, 10, 0),
                                 Cursor = System.Windows.Input.Cursors.Hand,
                                 Tag = series.Name
                             };

            legendItem.Children.Add(new Rectangle
                                    {
                                        Width = 16,
                                        Height = 4,
                                        Fill = series.Stroke,
                                        Margin = new Thickness(0, 6, 5, 0)
                                    });

            legendItem.Children.Add(new TextBlock
                                    {
                                        Text = series.Name,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        Foreground = axisBrush
                                    });

            legendItem.MouseLeftButtonDown += LegendItem_MouseLeftButtonDown;

            PART_Legend.Children.Add(legendItem);
        }

        private void LegendItem_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is StackPanel { Tag: string seriesName })
            {
                if (_highlightedSeriesName == seriesName)
                {
                    ClearHighlight();
                }
                else
                {
                    HighlightSeries(seriesName);
                }
            }

            e.Handled = true;
        }
    }
}