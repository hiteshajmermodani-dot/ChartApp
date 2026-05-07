namespace ChartAppLib.Helpers
{
    /// <summary>Static helpers for generating common test and demo data arrays.</summary>
    public static class ChartDataGenerator
    {
        /// <summary>Returns uniformly-distributed random values in [<paramref name="min"/>, <paramref name="max"/>].</summary>
        /// <param name="pointCount">Number of values to generate.</param>
        /// <param name="min">Lower bound (inclusive). Default 0.</param>
        /// <param name="max">Upper bound (inclusive). Default 100.</param>
        /// <param name="seed">Optional RNG seed for reproducibility.</param>
        public static double[] GetRandomDoubleData(int pointCount, double min = 0, double max = 100, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = min + rng.NextDouble() * (max - min);
            }

            return data;
        }

        /// <summary>Returns Y values for a straight line  y = gradient·x + yIntercept,
        /// where x runs from 0 to <paramref name="pointCount"/>−1.</summary>
        public static double[] GetStraightLineYData(double gradient, double yIntercept, int pointCount)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = gradient * i + yIntercept;
            }

            return data;
        }

        /// <summary>Returns Y values for a pure sine wave.</summary>
        /// <param name="amplitude">Peak deviation from zero.</param>
        /// <param name="phase">Phase offset in radians.</param>
        /// <param name="pointCount">Number of samples.</param>
        /// <param name="freq">Number of full cycles across <paramref name="pointCount"/> samples. Default 10.</param>
        public static double[] GetSinewaveYData(double amplitude, double phase, int pointCount, int freq = 10)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = amplitude * Math.Sin(2 * Math.PI * freq * i / pointCount + phase);
            }

            return data;
        }

        /// <summary>Returns Y values for a sine wave with additive Gaussian noise.</summary>
        /// <param name="amplitude">Peak amplitude of the sine component.</param>
        /// <param name="phase">Phase offset in radians.</param>
        /// <param name="pointCount">Number of samples.</param>
        /// <param name="freq">Cycles across <paramref name="pointCount"/>. Default 10.</param>
        /// <param name="noiseAmplitude">Peak-to-peak amplitude of the noise. Default 0.1.</param>
        /// <param name="seed">Optional RNG seed.</param>
        public static double[] GetNoisySinewaveYData(double amplitude, double phase, int pointCount,
                                                     int freq = 10, double noiseAmplitude = 0.1, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                var sine = amplitude * Math.Sin(2 * Math.PI * freq * i / pointCount + phase);
                var noise = noiseAmplitude * (rng.NextDouble() * 2 - 1);
                data[i] = sine + noise;
            }

            return data;
        }

        /// <summary>Returns Y values for a cosine wave.</summary>
        /// <param name="amplitude">Peak amplitude.</param>
        /// <param name="phase">Phase offset in radians.</param>
        /// <param name="pointCount">Number of samples.</param>
        /// <param name="freq">Cycles across <paramref name="pointCount"/>. Default 10.</param>
        public static double[] GetCosineWaveYData(double amplitude, double phase, int pointCount, int freq = 10)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = amplitude * Math.Cos(2 * Math.PI * freq * i / pointCount + phase);
            }

            return data;
        }

        /// <summary>Returns uniform X values: start, start+step, start+2·step, …</summary>
        /// <param name="pointCount">Number of values.</param>
        /// <param name="start">First X value. Default 0.</param>
        /// <param name="step">Spacing between values. Default 1.</param>
        public static double[] GetUniformXData(int pointCount, double start = 0, double step = 1)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = start + i * step;
            }

            return data;
        }

        /// <summary>Returns OA-date X values spaced by <paramref name="interval"/> starting from <paramref name="start"/>.
        /// Pass to a <see cref="ChartAppLib.Models.DataSeries.XValues"/> alongside
        /// <c>AxisType.DateTime</c> on the <see cref="ChartAppLib.Models.XAxisDefinition"/>.</summary>
        public static double[] GetDateTimeXData(DateTime start, TimeSpan interval, int pointCount)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = start.Add(TimeSpan.FromTicks(interval.Ticks * i)).ToOADate();
            }

            return data;
        }

        /// <summary>Returns Y values for a simple random walk starting at <paramref name="startValue"/>.</summary>
        /// <param name="pointCount">Number of steps.</param>
        /// <param name="startValue">Starting value. Default 0.</param>
        /// <param name="stepSize">Maximum step magnitude per sample. Default 1.</param>
        /// <param name="seed">Optional RNG seed.</param>
        public static double[] GetRandomWalkData(int pointCount, double startValue = 0,
                                                 double stepSize = 1, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
            var data = new double[pointCount];
            data[0] = startValue;

            for (var i = 1; i < pointCount; i++)
            {
                data[i] = data[i - 1] + stepSize * (rng.NextDouble() * 2 - 1);
            }

            return data;
        }

        /// <summary>Returns Y values for a Gaussian (bell-curve) envelope centred at the middle of the range.</summary>
        /// <param name="amplitude">Peak height.</param>
        /// <param name="pointCount">Number of samples.</param>
        /// <param name="sigma">Standard deviation as a fraction of <paramref name="pointCount"/>. Default 0.15.</param>
        public static double[] GetGaussianData(double amplitude, int pointCount, double sigma = 0.15)
        {
            var data = new double[pointCount];
            var centre = (pointCount - 1) / 2.0;
            var s = sigma * pointCount;

            for (var i = 0; i < pointCount; i++)
            {
                var d = i - centre;
                data[i] = amplitude * Math.Exp(-d * d / (2 * s * s));
            }

            return data;
        }

        /// <summary>Returns Y values for y = <paramref name="scale"/> · e^(rate · x),
        /// where x runs from 0 to <paramref name="pointCount"/>−1.</summary>
        public static double[] GetExponentialData(double scale, double rate, int pointCount)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                data[i] = scale * Math.Exp(rate * i);
            }

            return data;
        }

        /// <summary>Returns Y values for a Fourier series approximation (sum of odd harmonics):
        /// y = amplitude · Σ sin(n·2π·x + phaseShift) / n, for n = 1, 3, 5, … up to <paramref name="harmonics"/> terms.
        /// With enough harmonics this converges to a square wave.</summary>
        /// <param name="amplitude">Scaling factor applied to the full sum.</param>
        /// <param name="phaseShift">Phase offset in radians added to every harmonic.</param>
        /// <param name="pointCount">Number of samples. Default 5000.</param>
        /// <param name="harmonics">Number of odd harmonics to sum (1 = pure sine, higher = sharper edges). Default 15.</param>
        public static double[] GetFourierYData(double amplitude, double phaseShift,
                                               int pointCount = 5000, int harmonics = 15)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                var x = 2 * Math.PI * i / pointCount;
                var sum = 0.0;

                for (var h = 0; h < harmonics; h++)
                {
                    var n = 2 * h + 1; // 1, 3, 5, …
                    sum += Math.Sin(n * x + phaseShift) / n;
                }

                data[i] = amplitude * sum;
            }

            return data;
        }

        /// <summary>Returns Y values sampled from a square wave with the given <paramref name="freq"/> cycles.</summary>
        /// <param name="amplitude">Half the peak-to-peak amplitude.</param>
        /// <param name="pointCount">Number of samples.</param>
        /// <param name="freq">Cycles across <paramref name="pointCount"/>. Default 5.</param>
        public static double[] GetSquareWaveYData(double amplitude, int pointCount, int freq = 5)
        {
            var data = new double[pointCount];

            for (var i = 0; i < pointCount; i++)
            {
                var phase = (i * freq % pointCount) / (double)pointCount;
                data[i] = phase < 0.5 ? amplitude : -amplitude;
            }

            return data;
        }
    }
}
