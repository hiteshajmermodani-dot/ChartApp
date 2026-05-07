using System.Windows.Media;

namespace ChartApp.Models
{
    /// <summary>Specifies whether an annotation is drawn horizontally or vertically.</summary>
    public enum AnnotationOrientation
    {
        Horizontal,
        Vertical
    }

    /// <summary>Base class for chart annotations (lines, boxes, etc.).</summary>
    public abstract class Annotation
    {
        /// <summary>Gets or sets the display label for the annotation.</summary>
        public string Label { get; set; } = "";

        /// <summary>Gets or sets the stroke brush.</summary>
        public Brush Stroke { get; set; } = Brushes.Red;

        /// <summary>Gets or sets the stroke thickness.</summary>
        public double StrokeThickness { get; set; } = 1.5;

        /// <summary>Gets or sets the stroke dash pattern.</summary>
        public DoubleCollection? StrokeDashArray { get; set; }

        /// <summary>Gets or sets the associated Y-axis identifier.</summary>
        public string? YAxisId { get; set; }

        /// <summary>Gets or sets the associated X-axis identifier.</summary>
        public string? XAxisId { get; set; }

        /// <summary>Gets or sets whether the annotation can be dragged interactively.</summary>
        public bool IsDraggable { get; set; } = false;
    }

    /// <summary>A line annotation (horizontal or vertical) drawn across the chart.</summary>
    public class LineAnnotation : Annotation
    {
        public LineAnnotation()
        {
            StrokeDashArray = [4, 2];
            IsDraggable = true;
        }

        /// <summary>Gets or sets the data value at which the line is drawn.</summary>
        public double Value { get; set; }

        /// <summary>Gets or sets the line orientation.</summary>
        public AnnotationOrientation Orientation { get; set; } = AnnotationOrientation.Horizontal;
    }

    /// <summary>A rectangular box annotation defined by X and Y ranges.</summary>
    public class BoxAnnotation : Annotation
    {
        public BoxAnnotation()
        {
            Stroke = Brushes.Orange;
            StrokeThickness = 1;
        }

        /// <summary>Gets or sets the left X boundary.</summary>
        public double X1 { get; set; }

        /// <summary>Gets or sets the right X boundary.</summary>
        public double X2 { get; set; }

        /// <summary>Gets or sets the bottom Y boundary.</summary>
        public double Y1 { get; set; }

        /// <summary>Gets or sets the top Y boundary.</summary>
        public double Y2 { get; set; }

        /// <summary>Gets or sets the fill brush for the box interior.</summary>
        public Brush Fill { get; set; } = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0));
    }
}
