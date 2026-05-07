using System.Windows.Media;

namespace ChartApp.Models
{
    /// <summary>Determines how axis tick labels are generated.</summary>
    public enum AxisType
    {
        Numeric,
        DateTime,
        Category
    }

    /// <summary>Base class for axis definitions containing shared axis configuration.</summary>
    public abstract class AxisDefinition
    {
        /// <summary>Unique identifier for this axis.</summary>
        public required string Id { get; set; }

        /// <summary>Display label text for the axis.</summary>
        public string? Label { get; set; }

        /// <summary>Whether the axis label is visible.</summary>
        public bool ShowLabel { get; set; } = true;

        /// <summary>Brush used to render the axis label text.</summary>
        public Brush LabelBrush { get; set; } = Brushes.Black;

        /// <summary>Optional fixed minimum value for the axis range.</summary>
        public double? MinValue { get; set; }

        /// <summary>Optional fixed maximum value for the axis range.</summary>
        public double? MaxValue { get; set; }

        /// <summary>Number of major tick marks on the axis.</summary>
        public int MajorTickCount { get; set; } = 5;

        /// <summary>Number of minor tick marks between major ticks.</summary>
        public int MinorTickCount { get; set; } = 4;

        /// <summary>Whether grid lines are drawn at major ticks.</summary>
        public bool ShowGridLines { get; set; } = true;

        /// <summary>Brush used for grid lines at major ticks.</summary>
        public Brush GridLineBrush { get; set; } = Brushes.LightGray;

        /// <summary>Fraction to grow the data range on each side (e.g., 0.05 = 5%).</summary>
        public double GrowBy { get; set; } = 0.05;

        /// <summary>Tick label generation mode (Numeric, DateTime, or Category).</summary>
        public AxisType AxisType { get; set; } = AxisType.Numeric;

        /// <summary>Format string for numeric tick labels.</summary>
        public string LabelFormat { get; set; } = "0.##";

        /// <summary>Format string for DateTime tick labels.</summary>
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd";

        /// <summary>Category labels used when AxisType is Category.</summary>
        public IList<string>? Categories { get; set; }

        /// <summary>Custom label formatter that overrides all other formatting logic.</summary>
        public Func<double, string>? LabelFormatter { get; set; }

        /// <summary>Formats a tick value into a display string.</summary>
        public string FormatLabel(double value)
        {
            if (LabelFormatter != null)
            {
                return LabelFormatter(value);
            }

            return AxisType switch
                   {
                       AxisType.DateTime => DateTime.FromOADate(value).ToString(DateTimeFormat),
                       AxisType.Category => FormatCategory(value),
                       _                 => value.ToString(LabelFormat)
                   };
        }

        private string FormatCategory(double value)
        {
            if (Categories == null || Categories.Count == 0)
            {
                return value.ToString("0");
            }

            var index = (int)Math.Round(value);

            return index >= 0 && index < Categories.Count ? Categories[index] : value.ToString("0");
        }
    }

    /// <summary>Y-axis (vertical) definition with position configuration.</summary>
    public class YAxisDefinition : AxisDefinition
    {
        /// <summary>Position of the Y-axis (Left or Right).</summary>
        public YAxisPosition Position { get; set; } = YAxisPosition.Left;
    }

    /// <summary>X-axis (horizontal) definition with position configuration.</summary>
    public class XAxisDefinition : AxisDefinition
    {
        /// <summary>Position of the X-axis (Bottom or Top).</summary>
        public XAxisPosition Position { get; set; } = XAxisPosition.Bottom;
    }

    /// <summary>Y-axis position options.</summary>
    public enum YAxisPosition
    {
        Left,
        Right
    }

    /// <summary>X-axis position options.</summary>
    public enum XAxisPosition
    {
        Bottom,
        Top
    }
}