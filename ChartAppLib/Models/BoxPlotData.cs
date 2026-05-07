namespace ChartAppLib.Models
{
    public class BoxPlotData
    {
        /// <summary>Category or group label for this box plot.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Distribution of values for this box plot.</summary>
        public List<double> Values { get; set; } = new List<double>();
    }
}
