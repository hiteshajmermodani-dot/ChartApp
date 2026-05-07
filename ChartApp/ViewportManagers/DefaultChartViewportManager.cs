using System.Windows;

namespace ChartApp.ViewportManagers
{
    public class DefaultChartViewportManager : ChartViewportManager
    {
        private const double ZoomFactor = 1.1;
        private const double MaxZoomScale = 50.0;
        private const double MinZoomScale = 0.1;

        /// <inheritdoc />
        public override void OnMouseWheel(Point position, int delta)
        {
            var scale = delta > 0 ? ZoomFactor : 1.0 / ZoomFactor;
            var newScaleX = ScaleX * scale;
            var newScaleY = ScaleY * scale;

            if (newScaleX > MaxZoomScale || newScaleY > MaxZoomScale ||
                newScaleX < MinZoomScale || newScaleY < MinZoomScale)
            {
                return;
            }

            var normalizedPanX = PanX + CenterX * (1 - ScaleX);
            var normalizedPanY = PanY + CenterY * (1 - ScaleY);

            ScaleX = newScaleX;
            ScaleY = newScaleY;
            CenterX = 0;
            CenterY = 0;
            PanX = position.X * (1 - scale) + normalizedPanX * scale;
            PanY = position.Y * (1 - scale) + normalizedPanY * scale;

            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void OnPanDelta(Vector delta)
        {
            PanX += delta.X;
            PanY += delta.Y;
            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void OnZoomRect(Rect selectionRect, Size canvasSize)
        {
            var oldPanX = PanX + CenterX * (1 - ScaleX);
            var oldPanY = PanY + CenterY * (1 - ScaleY);

            var scaleFactorX = canvasSize.Width / selectionRect.Width;
            var scaleFactorY = canvasSize.Height / selectionRect.Height;

            var newScaleX = ScaleX * scaleFactorX;
            var newScaleY = ScaleY * scaleFactorY;

            if (newScaleX > MaxZoomScale || newScaleY > MaxZoomScale ||
                newScaleX < MinZoomScale || newScaleY < MinZoomScale)
            {
                return;
            }

            ScaleX = newScaleX;
            ScaleY = newScaleY;
            CenterX = 0;
            CenterY = 0;
            PanX = oldPanX * scaleFactorX - selectionRect.Left * scaleFactorX;
            PanY = oldPanY * scaleFactorY - selectionRect.Top * scaleFactorY;

            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void OnXAxisDrag(double pixelDelta, int maxDataPoints, double canvasWidth, double offsetAtDragStart)
        {
            var indexChange = pixelDelta / canvasWidth * (maxDataPoints - 1);
            XAxisOffset = offsetAtDragStart - indexChange;
            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void OnYAxisDrag(double pixelDelta, double canvasHeight, double valueRange, double offsetAtDragStart)
        {
            var valueChange = -(pixelDelta / canvasHeight) * valueRange;
            YAxisOffset = offsetAtDragStart + valueChange;
            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void Reset()
        {
            ScaleX = 1;
            ScaleY = 1;
            CenterX = 0;
            CenterY = 0;
            PanX = 0;
            PanY = 0;
            XAxisOffset = 0;
            YAxisOffset = 0;
            RaiseViewportChanged();
        }

        /// <inheritdoc />
        public override void ZoomExtents(double xDataMin, double xDataMax, double yDataMin, double yDataMax,
                                         double xGrowBy, double yGrowBy, double canvasHeight)
        {
            var xRange = xDataMax - xDataMin;
            var yRange = yDataMax - yDataMin;

            var xGrowAmount = xRange * xGrowBy;
            var yGrowAmount = yRange * yGrowBy;

            var xMin = xDataMin - xGrowAmount;
            var xMax = xDataMax + xGrowAmount;
            var yMin = yDataMin - yGrowAmount;
            var yMax = yDataMax + yGrowAmount;

            var xRange2 = xMax - xMin;
            var yRange2 = yMax - yMin;

            if (xRange2 <= 0 || yRange2 <= 0)
            {
                Reset();
                return;
            }

            ScaleX = 1;
            XAxisOffset = xMin;
            ScaleY = canvasHeight / yRange2;
            PanY = -(yMin * ScaleY);
            CenterX = 0;
            CenterY = 0;
            PanX = 0;

            RaiseViewportChanged();
        }
    }
}