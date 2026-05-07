using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ChartApp.Models;

namespace ChartApp.Controls
{
    public partial class ChartControl
    {
        private void DrawAnnotations(double width, double height,
                                     Dictionary<string, (double min, double max, double range)> yAxisRanges,
                                     Dictionary<string, (double min, double max, int maxPoints)> xAxisRanges)
        {
            if (Annotations == null || Annotations.Count == 0)
            {
                return;
            }

            foreach (var item in Annotations)
            {
                if (item is LineAnnotation annotation)
                {
                    DrawLineAnnotation(annotation, width, height, yAxisRanges, xAxisRanges);
                }
                else if (item is BoxAnnotation box)
                {
                    DrawBoxAnnotation(box, width, height, yAxisRanges, xAxisRanges);
                }
            }
        }

        private void DrawLineAnnotation(LineAnnotation annotation, double width, double height,
                                        Dictionary<string, (double min, double max, double range)> yAxisRanges,
                                        Dictionary<string, (double min, double max, int maxPoints)> xAxisRanges)
        {
            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));

            if (annotation.Orientation == AnnotationOrientation.Horizontal)
            {
                string yAxisId = annotation.YAxisId ?? YAxes[0].Id;

                if (!yAxisRanges.ContainsKey(yAxisId))
                {
                    return;
                }

                var (min, max, range) = yAxisRanges[yAxisId];
                double y = height - (annotation.Value - min) / range * height;

                var line = new Line
                           {
                               X1 = 0,
                               X2 = width,
                               Y1 = y,
                               Y2 = y,
                               Stroke = annotation.Stroke,
                               StrokeThickness = annotation.StrokeThickness / zoomScale,
                               StrokeDashArray = annotation.StrokeDashArray,
                               IsHitTestVisible = annotation.IsDraggable,
                               Cursor = annotation.IsDraggable ? Cursors.SizeNS : Cursors.Arrow,
                               Tag = annotation
                           };

                Panel.SetZIndex(line, 500);

                if (annotation.IsDraggable)
                {
                    line.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                }

                PART_Canvas.Children.Add(line);

                if (!string.IsNullOrEmpty(annotation.Label))
                {
                    var label = new TextBlock
                                {
                                    Text = $"{annotation.Label}: {annotation.Value:0.##}",
                                    FontSize = 10 / zoomScale,
                                    Foreground = annotation.Stroke,
                                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                                    Padding = new Thickness(2)
                                };

                    Canvas.SetLeft(label, 5);
                    Canvas.SetTop(label, y - 18 / zoomScale);
                    Panel.SetZIndex(label, 501);
                    PART_Canvas.Children.Add(label);
                }
            }
            else // Vertical
            {
                string xAxisId = annotation.XAxisId ?? XAxes[0].Id;

                if (!xAxisRanges.ContainsKey(xAxisId))
                {
                    return;
                }

                var (min, max, maxPoints) = xAxisRanges[xAxisId];
                double xRange = max - min;
                double x = (annotation.Value - min) / xRange * width;

                var line = new Line
                           {
                               X1 = x,
                               X2 = x,
                               Y1 = 0,
                               Y2 = height,
                               Stroke = annotation.Stroke,
                               StrokeThickness = annotation.StrokeThickness / zoomScale,
                               StrokeDashArray = annotation.StrokeDashArray,
                               IsHitTestVisible = annotation.IsDraggable,
                               Cursor = annotation.IsDraggable ? Cursors.SizeWE : Cursors.Arrow,
                               Tag = annotation
                           };

                Panel.SetZIndex(line, 500);

                if (annotation.IsDraggable)
                {
                    line.MouseLeftButtonDown += Line_MouseLeftButtonDown;
                }

                PART_Canvas.Children.Add(line);

                if (!string.IsNullOrEmpty(annotation.Label))
                {
                    var label = new TextBlock
                                {
                                    Text = $"{annotation.Label}: {annotation.Value:0.##}",
                                    FontSize = 10 / zoomScale,
                                    Foreground = annotation.Stroke,
                                    Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                                    Padding = new Thickness(2),
                                    RenderTransform = new RotateTransform(-90)
                                };

                    Canvas.SetLeft(label, x     - 18 / zoomScale);
                    Canvas.SetTop(label, height - 5);
                    Panel.SetZIndex(label, 501);
                    PART_Canvas.Children.Add(label);
                }
            }
        }

        private void DrawBoxAnnotation(BoxAnnotation box, double width, double height,
                                       Dictionary<string, (double min, double max, double range)> yAxisRanges,
                                       Dictionary<string, (double min, double max, int maxPoints)> xAxisRanges)
        {
            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var yAxisId = box.YAxisId ?? YAxes[0].Id;
            var xAxisId = box.XAxisId ?? XAxes[0].Id;

            if (!yAxisRanges.ContainsKey(yAxisId) || !xAxisRanges.ContainsKey(xAxisId))
            {
                return;
            }

            var (yMin, yMax, yRange) = yAxisRanges[yAxisId];
            var (xMin, xMax, maxPoints) = xAxisRanges[xAxisId];
            var xRange = xMax - xMin;

            var pixelX1 = (box.X1 - xMin)                   / xRange * width;
            var pixelX2 = (box.X2 - xMin)                   / xRange * width;
            var pixelY1 = height - (box.Y1 - yMin) / yRange * height;
            var pixelY2 = height - (box.Y2 - yMin) / yRange * height;

            var left = Math.Min(pixelX1, pixelX2);
            var top = Math.Min(pixelY1, pixelY2);
            var rectWidth = Math.Abs(pixelX2  - pixelX1);
            var rectHeight = Math.Abs(pixelY2 - pixelY1);

            var rect = new Rectangle
                       {
                           Width = rectWidth,
                           Height = rectHeight,
                           Fill = box.Fill,
                           Stroke = box.Stroke,
                           StrokeThickness = box.StrokeThickness / zoomScale,
                           StrokeDashArray = box.StrokeDashArray,
                           IsHitTestVisible = false
                       };

            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);
            Panel.SetZIndex(rect, 400);
            PART_Canvas.Children.Add(rect);

            if (!string.IsNullOrEmpty(box.Label))
            {
                var label = new TextBlock
                            {
                                Text = box.Label,
                                FontSize = 10 / zoomScale,
                                Foreground = box.Stroke,
                                Background = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                                Padding = new Thickness(2)
                            };

                Canvas.SetLeft(label, left + 4);
                Canvas.SetTop(label, top   + 2);
                Panel.SetZIndex(label, 401);
                PART_Canvas.Children.Add(label);
            }
        }

        private void Line_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Line { Tag: LineAnnotation annotation } line)
            {
                _isDraggingLine = true;
                _draggedLine = annotation;
                _draggedLineShape = line;
                PART_Canvas.CaptureMouse();

                PART_Canvas.Cursor = annotation.Orientation == AnnotationOrientation.Horizontal
                                         ? Cursors.SizeNS
                                         : Cursors.SizeWE;

                e.Handled = true;
            }
        }

        private void HandleLineDrag(MouseEventArgs e)
        {
            if (!_isDraggingLine || _draggedLine == null)
            {
                return;
            }

            var pos = e.GetPosition(PART_Canvas);
            var width = PART_Canvas.ActualWidth;
            var height = PART_Canvas.ActualHeight;

            if (_draggedLine.Orientation == AnnotationOrientation.Horizontal)
            {
                var yAxisId = _draggedLine.YAxisId ?? (YAxes is { Count: > 0 } ? YAxes[0].Id : "");
                var yAxis = YAxes?.FirstOrDefault(a => a.Id == yAxisId);

                if (yAxis != null)
                {
                    var min = yAxis.MinValue ?? 0;
                    var max = yAxis.MaxValue ?? 1;
                    var range = max - min;
                    var newValue = min + (height - pos.Y) / height * range;
                    newValue = Math.Max(min, Math.Min(max, newValue));
                    _draggedLine.Value = newValue;

                    // Move the line shape directly — no full redraw needed during drag
                    if (_draggedLineShape != null)
                    {
                        var y = height - (newValue - min) / range * height;
                        _draggedLineShape.Y1 = y;
                        _draggedLineShape.Y2 = y;
                    }
                }
            }
            else
            {
                var xAxisId = _draggedLine.XAxisId ?? (XAxes is { Count: > 0 } ? XAxes[0].Id : "");
                var xAxis = XAxes?.FirstOrDefault(a => a.Id == xAxisId);

                if (xAxis != null)
                {
                    var min = xAxis.MinValue ?? 0;
                    var max = xAxis.MaxValue ?? Series?.Max(s => s.YValues.Count) - 1 ?? 1;
                    var range = max - min;
                    var newValue = min + pos.X / width * range;
                    newValue = Math.Max(min, Math.Min(max, newValue));
                    _draggedLine.Value = newValue;

                    // Move the line shape directly — no full redraw needed during drag
                    if (_draggedLineShape != null)
                    {
                        var x = (newValue - min) / range * width;
                        _draggedLineShape.X1 = x;
                        _draggedLineShape.X2 = x;
                    }
                }
            }

            e.Handled = true;
        }

        private void StopLineDrag()
        {
            if (!_isDraggingLine)
            {
                return;
            }

            _isDraggingLine = false;
            _draggedLine = null;
            _draggedLineShape = null;
            PART_Canvas.ReleaseMouseCapture();
            PART_Canvas.Cursor = Cursors.Arrow;

            // Finalize: rebuild annotation labels and any dependent elements
            DrawChart();
        }
    }
}
