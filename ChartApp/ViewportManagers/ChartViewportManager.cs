using System.Windows;

namespace ChartApp.ViewportManagers
{
    public abstract class ChartViewportManager
    {
        /// <summary>Horizontal zoom scale factor (1 = no zoom).</summary>
        public double ScaleX { get; protected set; } = 1;

        /// <summary>Vertical zoom scale factor (1 = no zoom).</summary>
        public double ScaleY { get; protected set; } = 1;

        /// <summary>X coordinate of the zoom origin point.</summary>
        public double CenterX { get; protected set; } = 0;

        /// <summary>Y coordinate of the zoom origin point.</summary>
        public double CenterY { get; protected set; } = 0;

        /// <summary>Horizontal pan offset in pixels.</summary>
        public double PanX { get; protected set; } = 0;

        /// <summary>Vertical pan offset in pixels.</summary>
        public double PanY { get; protected set; } = 0;

        /// <summary>Logical data-index offset along the X axis.</summary>
        public double XAxisOffset { get; protected set; } = 0;

        /// <summary>Logical value offset along the Y axis.</summary>
        public double YAxisOffset { get; protected set; } = 0;

        /// <summary>Handles mouse wheel zoom events.</summary>
        public abstract void OnMouseWheel(Point position, int delta);

        /// <summary>Handles pan delta during Ctrl+drag interaction.</summary>
        public abstract void OnPanDelta(Vector delta);

        /// <summary>Handles rubber-band zoom rectangle selection.</summary>
        public abstract void OnZoomRect(Rect selectionRect, Size canvasSize);

        /// <summary>Handles X-axis drag interaction.</summary>
        public abstract void OnXAxisDrag(double pixelDelta, int maxDataPoints, double canvasWidth,
                                         double offsetAtDragStart);

        /// <summary>Handles Y-axis drag interaction.</summary>
        public abstract void OnYAxisDrag(double pixelDelta, double canvasHeight, double valueRange,
                                         double offsetAtDragStart);

        /// <summary>Handles trackpad pinch and two-finger pan gestures.</summary>
        public virtual void OnManipulationDelta(Point origin, double scaleX, double scaleY, Vector translation)
        {
            if (Math.Abs(scaleX - 1.0) > 0.001 || Math.Abs(scaleY - 1.0) > 0.001)
            {
                CenterX = origin.X;
                CenterY = origin.Y;
                ScaleX *= scaleX;
                ScaleY *= scaleY;
            }

            if (Math.Abs(translation.X) > 0.5 || Math.Abs(translation.Y) > 0.5)
            {
                PanX += translation.X;
                PanY += translation.Y;
            }

            RaiseViewportChanged();
        }

        /// <summary>Shifts the X axis by the specified data amount.</summary>
        public virtual void ShiftXOffset(double delta)
        {
            XAxisOffset += delta;
            RaiseViewportChanged();
        }

        /// <summary>Shifts the Y axis by the specified data amount.</summary>
        public virtual void ShiftYOffset(double delta)
        {
            YAxisOffset += delta;
            RaiseViewportChanged();
        }

        /// <summary>Resets all viewport state to default identity values.</summary>
        public abstract void Reset();

        /// <summary>Zooms to fit data extents with optional growth factor.</summary>
        public abstract void ZoomExtents(double xDataMin, double xDataMax, double yDataMin, double yDataMax,
                                         double xGrowBy, double yGrowBy, double canvasHeight);

        /// <summary>Restores viewport state from a previously captured state (undo/redo).</summary>
        public virtual void RestoreState(double scaleX, double scaleY, double centerX, double centerY,
                                         double panX, double panY, double xAxisOffset, double yAxisOffset)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
            CenterX = centerX;
            CenterY = centerY;
            PanX = panX;
            PanY = panY;
            XAxisOffset = xAxisOffset;
            YAxisOffset = yAxisOffset;
        }

        /// <summary>Raised whenever viewport state changes.</summary>
        public event EventHandler? ViewportChanged;

        protected void RaiseViewportChanged()
        {
            ViewportChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
