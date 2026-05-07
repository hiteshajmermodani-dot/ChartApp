using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChartApp.Controls
{
    public partial class ChartControl
    {
        private void DrawChartLegacy(double width, double height)
        {
            var allValues = Series.SelectMany(s => s.YValues).ToList();
            var min = allValues.Min();
            var max = allValues.Max();
            var range = max - min == 0 ? 1 : max - min;

            DrawAxes(width, height);

            // grid lines first
            DrawYAxisGridLines(height, width, min, max);
            DrawXAxisGridLines(width, height);

            // ticks & labels on top of grid
            DrawYAxisTicks(height, min, max);
            DrawXAxisTicks(width, height, Series.Max(s => s.YValues.Count));

            // series drawn last
            foreach (var series in Series)
            {
                DrawLineSeries(series, width, height, min, range);

                if (PART_Legend.Visibility == Visibility.Visible)
                {
                    DrawLegend(series);
                }
            }
        }

        private void DrawAxes(double width, double height)
        {
            // Y Axis
            PART_Canvas.Children.Add(new Line
                                     {
                                         X1 = 0,
                                         Y1 = 0,
                                         X2 = 0,
                                         Y2 = height,
                                         Stroke = Brushes.Black,
                                         StrokeThickness = 1
                                     });

            // X Axis
            PART_Canvas.Children.Add(new Line
                                     {
                                         X1 = 0,
                                         Y1 = height,
                                         X2 = width,
                                         Y2 = height,
                                         Stroke = Brushes.Black,
                                         StrokeThickness = 1
                                     });
        }

        private void DrawYAxisTicks(double height, double min, double max)
        {
            var range = max - min;
            var majorStepValue = range   / YMajorTickCount;
            var majorStepPixels = height / YMajorTickCount;

            for (var i = 0; i <= YMajorTickCount; i++)
            {
                var y = height - i * majorStepPixels;
                var value = min + i * majorStepValue + _yAxisOffset;

                // Major tick
                PART_Canvas.Children.Add(new Line
                                         {
                                             X1 = 0,
                                             X2 = -8,
                                             Y1 = y,
                                             Y2 = y,
                                             Stroke = Brushes.Black,
                                             StrokeThickness = 1
                                         });

                var label = new TextBlock
                            {
                                Text = value.ToString("0.##"),
                                FontSize = 11
                            };

                Canvas.SetLeft(label, -40);
                Canvas.SetTop(label, y - 8);
                PART_Canvas.Children.Add(label);

                // Minor ticks
                if (i < YMajorTickCount)
                {
                    var minorStepPixels = majorStepPixels / (YMinorTickCount + 1);

                    for (var m = 1; m <= YMinorTickCount; m++)
                    {
                        PART_Canvas.Children.Add(new Line
                                                 {
                                                     X1 = 0,
                                                     X2 = -4,
                                                     Y1 = y - m * minorStepPixels,
                                                     Y2 = y - m * minorStepPixels,
                                                     Stroke = Brushes.Gray,
                                                     StrokeThickness = 0.5
                                                 });
                    }
                }
            }
        }

        private void DrawXAxisTicks(double width, double height, int maxPoints)
        {
            var majorStepPixels = width / XMajorTickCount;

            var majorStepValue = (maxPoints - 1) /
                                 (double)XMajorTickCount;

            for (var i = 0; i <= XMajorTickCount; i++)
            {
                var x = i * majorStepPixels;
                var value = i * majorStepValue + _xAxisOffset;

                // Major tick
                PART_Canvas.Children.Add(new Line
                                         {
                                             X1 = x,
                                             X2 = x,
                                             Y1 = height,
                                             Y2 = height + 8,
                                             Stroke = Brushes.Black,
                                             StrokeThickness = 1
                                         });

                var label = new TextBlock
                            {
                                Text = value.ToString("0.##"),
                                FontSize = 11
                            };

                Canvas.SetLeft(label, x     - 8);
                Canvas.SetTop(label, height + 10);
                PART_Canvas.Children.Add(label);

                // Minor ticks
                if (i < XMajorTickCount)
                {
                    var minorStepPixels =
                        majorStepPixels / (XMinorTickCount + 1);

                    for (var m = 1; m <= XMinorTickCount; m++)
                    {
                        PART_Canvas.Children.Add(new Line
                                                 {
                                                     X1 = x + m * minorStepPixels,
                                                     X2 = x + m * minorStepPixels,
                                                     Y1 = height,
                                                     Y2 = height + 4,
                                                     Stroke = Brushes.Gray,
                                                     StrokeThickness = 0.5
                                                 });
                    }
                }
            }
        }

        private void DrawAxisLabels(double width, double height, double min, double max)
        {
            // X-axis label
            var xLabel = new TextBlock
                         {
                             Text = XAxisLabel,
                             FontWeight = FontWeights.Bold
                         };

            Canvas.SetLeft(xLabel, width / 2 - 30);
            Canvas.SetTop(xLabel, height     + 5);
            PART_Canvas.Children.Add(xLabel);

            // Y-axis label (rotated)
            var yLabel = new TextBlock
                         {
                             Text = YAxisLabel,
                             FontWeight = FontWeights.Bold,
                             RenderTransform = new RotateTransform(-90)
                         };

            Canvas.SetLeft(yLabel, -35);
            Canvas.SetTop(yLabel, height / 2 + 30);
            PART_Canvas.Children.Add(yLabel);

            // Min value
            PART_Canvas.Children.Add(CreateValueLabel(min.ToString("0.##"), -30, height - 10));

            // Max value
            PART_Canvas.Children.Add(CreateValueLabel(max.ToString("0.##"), -30, -5));
        }

        private TextBlock CreateValueLabel(string text, double x, double y)
        {
            var label = new TextBlock { Text = text };
            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, y);

            return label;
        }

        private void DrawYAxisGridLines(double height, double width, double min, double max)
        {
            if (!ShowMajorGridLines && !ShowMinorGridLines)
            {
                return;
            }

            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var majorStepPixels = height / YMajorTickCount;

            if (ShowMajorGridLines)
            {
                for (var i = 0; i < YMajorTickCount; i++)
                {
                    if (i % 2 != 0)
                    {
                        continue;
                    }

                    var band = new Rectangle
                               {
                                   Width = width,
                                   Height = majorStepPixels,
                                   Fill = MajorGridBandBrush,
                                   IsHitTestVisible = false
                               };

                    Canvas.SetLeft(band, 0);
                    Canvas.SetTop(band, height - (i + 1) * majorStepPixels);
                    Panel.SetZIndex(band, 0);
                    PART_Canvas.Children.Add(band);
                }
            }

            for (var i = 0; i <= YMajorTickCount; i++)
            {
                var y = height - i * majorStepPixels;

                if (ShowMajorGridLines)
                {
                    PART_Canvas.Children.Add(new Line
                                             {
                                                 X1 = 0,
                                                 X2 = width,
                                                 Y1 = y,
                                                 Y2 = y,
                                                 Stroke = MajorGridLineBrush,
                                                 StrokeThickness = 1 / zoomScale,
                                                 StrokeDashArray = new DoubleCollection { 4, 2 },
                                                 IsHitTestVisible = false
                                             });
                }

                if (ShowMinorGridLines && i < YMajorTickCount)
                {
                    var minorStepPixels =
                        majorStepPixels / (YMinorTickCount + 1);

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
                                                     StrokeDashArray = new DoubleCollection { 2, 4 },
                                                     IsHitTestVisible = false
                                                 });
                    }
                }
            }
        }

        private void DrawXAxisGridLines(double width, double height)
        {
            if (!ShowMajorGridLines && !ShowMinorGridLines)
            {
                return;
            }

            var zoomScale = Math.Max(1.0, Math.Sqrt(ZoomTransform.ScaleX * ZoomTransform.ScaleY));
            var majorStepPixels = width / XMajorTickCount;

            for (var i = 0; i <= XMajorTickCount; i++)
            {
                var x = i * majorStepPixels;

                if (ShowMajorGridLines)
                {
                    PART_Canvas.Children.Add(new Line
                                             {
                                                 X1 = x,
                                                 X2 = x,
                                                 Y1 = 0,
                                                 Y2 = height,
                                                 Stroke = MajorGridLineBrush,
                                                 StrokeThickness = 1 / zoomScale,
                                                 StrokeDashArray = new DoubleCollection { 4, 2 },
                                                 IsHitTestVisible = false
                                             });
                }

                if (ShowMinorGridLines && i < XMajorTickCount)
                {
                    var minorStepPixels =
                        majorStepPixels / (XMinorTickCount + 1);

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
                                                     StrokeDashArray = new DoubleCollection { 2, 4 },
                                                     IsHitTestVisible = false
                                                 });
                    }
                }
            }
        }
    }
}
