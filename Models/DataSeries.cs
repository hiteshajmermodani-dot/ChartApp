using System.Windows.Media;

namespace ChartApp.Models
{
    /// <summary>Represents a single data series with X, Y, and optional Z values.</summary>
    public class DataSeries
    {
        /// <summary>Gets or sets the display name of the series.</summary>
        public required string Name { get; set; }

        /// <summary>Gets or sets the explicit X-axis values.</summary>
        public IList<double>? XValues { get; set; }

        /// <summary>Gets or sets the Y-axis values.</summary>
        public IList<double> YValues { get; set; } = new List<double>();

        /// <summary>Gets or sets the stroke brush used to render the series.</summary>
        public Brush Stroke { get; set; } = Brushes.Blue;

        /// <summary>Gets or sets the line thickness.</summary>
        public double Thickness { get; set; } = 2;

        /// <summary>Gets or sets the associated X-axis identifier for multi-axis charts.</summary>
        public string? XAxisId { get; set; }

        /// <summary>Gets or sets the associated Y-axis identifier for multi-axis charts.</summary>
        public string? YAxisId { get; set; }

        /// <summary>Gets or sets the Z-axis values for 3D charts.</summary>
        public IList<double>? ZValues { get; set; }

        /// <summary>Gets whether explicit X values are provided and match Y value count.</summary>
        public bool HasExplicitXValues => XValues != null && XValues.Count == YValues.Count;

        /// <summary>Adds a data point with explicit X and Y coordinates.</summary>
        public void AddPoint(double x, double y)
        {
            if (XValues == null)
            {
                XValues = new List<double>(YValues.Count);
                for (var i = 0; i < YValues.Count; i++)
                    XValues.Add(i);
            }

            XValues.Add(x);
            YValues.Add(y);
        }
    }
}