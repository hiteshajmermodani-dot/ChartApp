using System.Windows.Media;

namespace ChartApp.Models
{
    public class Line3DSeries
    {
        /// <summary>Unique identifier for this 3D line series.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>X-axis coordinate values.</summary>
        public double[] XValues { get; set; } = [];

        /// <summary>Y-axis coordinate values.</summary>
        public double[] YValues { get; set; } = [];

        /// <summary>Z-axis coordinate values.</summary>
        public double[] ZValues { get; set; } = [];

        /// <summary>Color of the 3D line.</summary>
        public Color Color { get; set; } = Colors.DodgerBlue;

        /// <summary>Thickness of the 3D line.</summary>
        public double Thickness { get; set; } = 0.02;
    }

    public class Line3DData
    {
        /// <summary>Collection of 3D line series.</summary>
        public List<Line3DSeries> Series { get; set; } = [];

        /// <summary>Label for the X axis.</summary>
        public string XLabel { get; set; } = "X";

        /// <summary>Label for the Y axis.</summary>
        public string YLabel { get; set; } = "Y";

        /// <summary>Label for the Z axis.</summary>
        public string ZLabel { get; set; } = "Z";

        /// <summary>Minimum X value displayed on axis.</summary>
        public double DisplayXMin { get; set; }

        /// <summary>Maximum X value displayed on axis.</summary>
        public double DisplayXMax { get; set; }

        /// <summary>Minimum Y value displayed on axis.</summary>
        public double DisplayYMin { get; set; }

        /// <summary>Maximum Y value displayed on axis.</summary>
        public double DisplayYMax { get; set; }

        /// <summary>Minimum Z value displayed on axis.</summary>
        public double DisplayZMin { get; set; }

        /// <summary>Maximum Z value displayed on axis.</summary>
        public double DisplayZMax { get; set; }
    }
}