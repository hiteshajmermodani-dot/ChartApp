namespace ChartAppLib.Models
{
    /// <summary>
    /// Holds Z-value grid data for a 3D surface plot.
    /// ZValues[row, col] maps to evenly spaced (X, Y) positions within the given ranges.
    /// </summary>
    public class Surface3DData
    {
        /// <summary>Z values as a 2D grid [rowIndex, colIndex].</summary>
        public double[,] ZValues { get; set; } = new double[0, 0];

        public double XMin { get; set; } = -1;

        public double XMax { get; set; } = 1;

        public double YMin { get; set; } = -1;

        public double YMax { get; set; } = 1;

        public string XLabel { get; set; } = "X";

        public string YLabel { get; set; } = "Y";

        public string ZLabel { get; set; } = "Z";
    }
}