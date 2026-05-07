using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ChartAppLib.Models;

namespace ChartAppLib.Controls
{
    public partial class ChartControl
    {
        // Shared 3D overlay state (accessible from Surface3D.cs via partial class)
        internal Canvas? _3dLabelCanvas;
        internal string _disp3DXLabel = "X";
        internal double _disp3DXMin, _disp3DXMax;
        internal string _disp3DYLabel = "Y";
        internal double _disp3DYMin, _disp3DYMax;
        internal string _disp3DZLabel = "Z";
        internal double _disp3DZMin, _disp3DZMax;

        // Normalization ranges: how model coords [-1,1] map to actual data values
        internal double _norm3DXMin, _norm3DXRange;
        internal double _norm3DYMin, _norm3DYRange;
        internal double _norm3DZMin, _norm3DZRange;

        private void DrawLine3D()
        {
            if (Line3DData == null || Line3DData.Series.Count == 0)
            {
                return;
            }

            PART_Canvas.Visibility = Visibility.Collapsed;
            PART_Overlay.Visibility = Visibility.Collapsed;
            PART_Viewport3D.Margin = new Thickness(75, 20, 20, 55);
            PART_Viewport3D.Visibility = Visibility.Visible;
            PART_Viewport3D.Children.Clear();

            // Show the transparent hit-test layer so drag works from anywhere in the plot
            PART_3DMouseLayer.Margin = PART_Viewport3D.Margin;
            PART_3DMouseLayer.Visibility = Visibility.Visible;

            // Wire up mouse events to the overlay layer (Viewport3D only hits on geometry)
            PART_3DMouseLayer.MouseLeftButtonDown -= Viewport3D_MouseLeftButtonDown;
            PART_3DMouseLayer.MouseMove -= Viewport3D_MouseMove;
            PART_3DMouseLayer.MouseLeftButtonUp -= Viewport3D_MouseLeftButtonUp;
            PART_3DMouseLayer.MouseWheel -= Viewport3D_MouseWheel;
            PART_3DMouseLayer.MouseLeftButtonDown += Viewport3D_MouseLeftButtonDown;
            PART_3DMouseLayer.MouseMove += Viewport3D_MouseMove;
            PART_3DMouseLayer.MouseLeftButtonUp += Viewport3D_MouseLeftButtonUp;
            PART_3DMouseLayer.MouseWheel += Viewport3D_MouseWheel;

            // Compute global min/max across all series for normalization
            double xMin = double.MaxValue, xMax = double.MinValue;
            double yMin = double.MaxValue, yMax = double.MinValue;
            double zMin = double.MaxValue, zMax = double.MinValue;

            foreach (var s in Line3DData.Series)
            {
                foreach (var v in s.XValues)
                {
                    if (v < xMin)
                    {
                        xMin = v;
                    }

                    if (v > xMax)
                    {
                        xMax = v;
                    }
                }

                foreach (var v in s.YValues)
                {
                    if (v < yMin)
                    {
                        yMin = v;
                    }

                    if (v > yMax)
                    {
                        yMax = v;
                    }
                }

                foreach (var v in s.ZValues)
                {
                    if (v < zMin)
                    {
                        zMin = v;
                    }

                    if (v > zMax)
                    {
                        zMax = v;
                    }
                }
            }

            double xRange = Math.Abs(xMax - xMin) < 1e-10 ? 1 : xMax - xMin;
            double yRange = Math.Abs(yMax - yMin) < 1e-10 ? 1 : yMax - yMin;
            double zRange = Math.Abs(zMax - zMin) < 1e-10 ? 1 : zMax - zMin;

            // Store for overlay projection inverse mapping
            _norm3DXMin = xMin;
            _norm3DXRange = xRange;
            _norm3DYMin = yMin;
            _norm3DYRange = yRange;
            _norm3DZMin = zMin;
            _norm3DZRange = zRange;

            // Store display ranges (from Line3DData overrides)
            _disp3DXMin = Line3DData.DisplayXMin;
            _disp3DXMax = Line3DData.DisplayXMax;
            _disp3DYMin = Line3DData.DisplayYMin;
            _disp3DYMax = Line3DData.DisplayYMax;
            _disp3DZMin = Line3DData.DisplayZMin;
            _disp3DZMax = Line3DData.DisplayZMax;
            _disp3DXLabel = Line3DData.XLabel;
            _disp3DYLabel = Line3DData.YLabel;
            _disp3DZLabel = Line3DData.ZLabel;

            Point3D Norm(double x, double y, double z)
            {
                return new Point3D(
                                   -1.0 + 2.0 * (x - xMin) / xRange,
                                   -1.0 + 2.0 * (y - yMin) / yRange,
                                   -1.0 + 2.0 * (z - zMin) / zRange);
            }

            var modelGroup = new Model3DGroup();

            // Bounding box wireframe
            Point3D[] corners = new Point3D[8];
            double[] bxs = [-1, 1];
            double[] bys = [-1, 1];
            double[] bzs = [-1, 1];

            for (int i = 0; i < 8; i++)
            {
                corners[i] = new Point3D(bxs[(i >> 2) & 1], bys[(i >> 1) & 1], bzs[i & 1]);
            }

            int[][] edges =
                [[0, 1], [2, 3], [4, 5], [6, 7], [0, 2], [1, 3], [4, 6], [5, 7], [0, 4], [1, 5], [2, 6], [3, 7]];

            foreach (var e in edges)
            {
                AddTubeToGroup(modelGroup, corners[e[0]], corners[e[1]], Color.FromRgb(160, 160, 160), 0.003);
            }

            // X axis (Red), Y axis (Green), Z axis (Blue)
            AddTubeToGroup(modelGroup, new Point3D(-1, -1, -1), new Point3D(1.1, -1, -1), Colors.Red, 0.007);
            AddTubeToGroup(modelGroup, new Point3D(-1, -1, -1), new Point3D(-1, 1.1, -1), Colors.Green, 0.007);
            AddTubeToGroup(modelGroup, new Point3D(-1, -1, -1), new Point3D(-1, -1, 1.1), Colors.Blue, 0.007);

            // Arrowhead cones at axis ends
            AddConeToGroup(modelGroup, new Point3D(1.22, -1, -1), new Vector3D(1, 0, 0), Colors.Red, 0.035, 0.12);
            AddConeToGroup(modelGroup, new Point3D(-1, 1.22, -1), new Vector3D(0, 1, 0), Colors.Green, 0.035, 0.12);
            AddConeToGroup(modelGroup, new Point3D(-1, -1, 1.22), new Vector3D(0, 0, 1), Colors.Blue, 0.035, 0.12);

            // Tick marks on each axis
            const double tickHalf = 0.05;

            for (int t = 0; t <= 4; t++)
            {
                double coord = -1.0 + 0.5 * t;

                AddTubeToGroup(modelGroup, new Point3D(coord, -1 - tickHalf, -1), new Point3D(coord, -1 + tickHalf, -1),
                               Color.FromRgb(140, 140, 140), 0.004);

                AddTubeToGroup(modelGroup, new Point3D(-1 - tickHalf, coord, -1), new Point3D(-1 + tickHalf, coord, -1),
                               Color.FromRgb(140, 140, 140), 0.004);

                AddTubeToGroup(modelGroup, new Point3D(-1, -1 - tickHalf, coord), new Point3D(-1, -1 + tickHalf, coord),
                               Color.FromRgb(140, 140, 140), 0.004);
            }

            // Series tube mesh — only the first series is rendered
            var series = Line3DData.Series[0];

            if (series.XValues.Length >= 2)
            {
                var pts = new List<Point3D>(series.XValues.Length);

                for (int i = 0; i < series.XValues.Length; i++)
                {
                    pts.Add(Norm(series.XValues[i], series.YValues[i], series.ZValues[i]));
                }

                var mesh = CreateTubeMesh(pts, series.Thickness);
                var brush = new SolidColorBrush(series.Color);
                brush.Freeze();
                var mat = new DiffuseMaterial(brush);
                modelGroup.Children.Add(new GeometryModel3D(mesh, mat) { BackMaterial = mat });
            }

            // Lighting
            modelGroup.Children.Add(new AmbientLight(Color.FromRgb(80, 80, 80)));
            modelGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -2, -1.5)));
            modelGroup.Children.Add(new DirectionalLight(Color.FromRgb(60, 60, 120), new Vector3D(1, 1, 1)));

            _rot3DX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -25);
            _rot3DY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 30);
            var transforms = new Transform3DGroup();
            transforms.Children.Add(new RotateTransform3D(_rot3DX));
            transforms.Children.Add(new RotateTransform3D(_rot3DY));

            _surfaceVisual3D = new ModelVisual3D { Content = modelGroup, Transform = transforms };

            PART_Viewport3D.Camera = new PerspectiveCamera
                                     {
                                         Position = new Point3D(0, 2.2, 4.5),
                                         LookDirection = new Vector3D(0, -0.5, -1),
                                         UpDirection = new Vector3D(0, 1, 0),
                                         FieldOfView = 45
                                     };

            PART_Viewport3D.Children.Add(_surfaceVisual3D);

            Dispatcher.InvokeAsync(Draw3DAxisOverlay, System.Windows.Threading.DispatcherPriority.Background);
        }

        internal void Draw3DAxisOverlay()
        {
            if (_surfaceVisual3D == null || PART_Viewport3D.Camera == null)
            {
                return;
            }

            if (PART_Viewport3D.ActualWidth <= 0 || PART_Viewport3D.ActualHeight <= 0)
            {
                return;
            }

            if (PART_Viewport3D.Parent is not Grid parentGrid)
            {
                return;
            }

            if (_3dLabelCanvas == null)
            {
                _3dLabelCanvas = new Canvas { IsHitTestVisible = false };
                parentGrid.Children.Add(_3dLabelCanvas);
            }
            else
            {
                _3dLabelCanvas.Children.Clear();
            }

            // Offsets to convert viewport-local coords → parent canvas coords
            double ml = PART_Viewport3D.Margin.Left;
            double mt = PART_Viewport3D.Margin.Top;

            var axisBrush = GetAxisBrush();

            // Project the scene centre so we can compute "outward" screen directions
            var ctrRaw = ProjectPoint3D(new Point3D(0, 0, 0));

            if (!ctrRaw.HasValue)
            {
                return;
            }

            double cx = ctrRaw.Value.X, cy = ctrRaw.Value.Y;

            // Pushes a label outward from the projected scene centre — used for axis name labels.
            Point Outward(Point rawPt, double pixelDist)
            {
                double dx = rawPt.X - cx, dy = rawPt.Y - cy;
                double len = Math.Sqrt(dx * dx + dy * dy);

                if (len > 0.5)
                {
                    dx /= len;
                    dy /= len;
                }
                else
                {
                    dx = 0;
                    dy = 1;
                }

                return new Point(rawPt.X + dx * pixelDist + ml,
                                 rawPt.Y + dy * pixelDist + mt);
            }

            // Returns the screen-space unit vector perpendicular to an axis edge, pointing
            // away from the projected scene centre.  Tick labels always sit beside their
            // ticks regardless of the current viewing angle.
            Vector AxisPerp(Point3D edgeFrom, Point3D edgeTo)
            {
                var pFrom = ProjectPoint3D(edgeFrom);
                var pTo   = ProjectPoint3D(edgeTo);

                if (!pFrom.HasValue || !pTo.HasValue)
                {
                    return new Vector(0, 1);
                }

                double dx = pTo.Value.X - pFrom.Value.X;
                double dy = pTo.Value.Y - pFrom.Value.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);

                if (len < 0.5)
                {
                    return new Vector(0, 1);
                }

                dx /= len;
                dy /= len;

                // Perpendicular candidate (rotate 90°)
                double p1x = -dy, p1y = dx;

                // Midpoint of axis edge in screen space
                double midX = (pFrom.Value.X + pTo.Value.X) / 2;
                double midY = (pFrom.Value.Y + pTo.Value.Y) / 2;

                // Pick the candidate pointing away from the projected scene centre
                return (midX - cx) * p1x + (midY - cy) * p1y >= 0
                           ? new Vector(p1x, p1y)
                           : new Vector(-p1x, -p1y);
            }

            const double tickOut = 24; // pixels perpendicular to axis for tick values
            const double nameOut = 44; // pixels outward for axis name labels
            const double fs = 9.5;     // tick font size

            var xPerp = AxisPerp(new Point3D(-1, -1, -1), new Point3D( 1, -1, -1));
            var yPerp = AxisPerp(new Point3D(-1, -1, -1), new Point3D(-1,  1, -1));
            var zPerp = AxisPerp(new Point3D(-1, -1, -1), new Point3D(-1, -1,  1));

            // ── X axis ticks (bottom-front edge, from (-1,-1,-1) to (1,-1,-1)) ───────
            for (int i = 0; i <= 4; i++)
            {
                double t = i / 4.0;
                var raw = ProjectPoint3D(new Point3D(-1.0 + 2.0 * t, -1, -1));

                if (!raw.HasValue)
                {
                    continue;
                }

                double dv = _disp3DXMin + t * (_disp3DXMax - _disp3DXMin);
                PlaceLabel(_3dLabelCanvas, Format3DTick(dv),
                           raw.Value.X + xPerp.X * tickOut + ml - 12,
                           raw.Value.Y + xPerp.Y * tickOut + mt - 6,
                           axisBrush, fs);
            }

            // X axis name
            var xNameRaw = ProjectPoint3D(new Point3D(0, -1, -1));

            if (xNameRaw.HasValue)
            {
                var pos = Outward(xNameRaw.Value, nameOut);
                PlaceLabel(_3dLabelCanvas, _disp3DXLabel, pos.X - 22, pos.Y - 6, axisBrush, 10.5, true);
            }

            // ── Y axis ticks (left-front edge, from (-1,-1,-1) to (-1,1,-1)) ─────────
            for (int i = 0; i <= 4; i++)
            {
                double t = i / 4.0;
                var raw = ProjectPoint3D(new Point3D(-1, -1.0 + 2.0 * t, -1));

                if (!raw.HasValue)
                {
                    continue;
                }

                double dv = _disp3DYMin + t * (_disp3DYMax - _disp3DYMin);
                PlaceLabel(_3dLabelCanvas, Format3DTick(dv),
                           raw.Value.X + yPerp.X * tickOut + ml - 30,
                           raw.Value.Y + yPerp.Y * tickOut + mt - 6,
                           axisBrush, fs);
            }

            // Y axis name
            var yNameRaw = ProjectPoint3D(new Point3D(-1, 0, -1));

            if (yNameRaw.HasValue)
            {
                var pos = Outward(yNameRaw.Value, nameOut);
                PlaceLabel(_3dLabelCanvas, _disp3DYLabel, pos.X - 20, pos.Y - 6, axisBrush, 10.5, true);
            }

            // ── Z axis ticks (bottom-left edge, from (-1,-1,-1) to (-1,-1,1)) ────────
            for (int i = 0; i <= 4; i++)
            {
                double t = i / 4.0;
                var raw = ProjectPoint3D(new Point3D(-1, -1, -1.0 + 2.0 * t));

                if (!raw.HasValue)
                {
                    continue;
                }

                double dv = _disp3DZMin + t * (_disp3DZMax - _disp3DZMin);
                PlaceLabel(_3dLabelCanvas, Format3DTick(dv),
                           raw.Value.X + zPerp.X * tickOut + ml - 12,
                           raw.Value.Y + zPerp.Y * tickOut + mt - 6,
                           axisBrush, fs);
            }

            // Z axis name
            var zNameRaw = ProjectPoint3D(new Point3D(-1, -1, 0));

            if (zNameRaw.HasValue)
            {
                var pos = Outward(zNameRaw.Value, nameOut);
                PlaceLabel(_3dLabelCanvas, _disp3DZLabel, pos.X - 15, pos.Y - 6, axisBrush, 10.5, true);
            }

            // ── Series name label at the last projected point of the 3D line ────────
            if (Line3DData != null && ChartType == ChartType.Line3DPlot && _norm3DXRange > 0)
            {
                var s = Line3DData.Series[0];

                if (s.XValues.Length > 0)
                {
                    int li = s.XValues.Length - 1;
                    double nx = -1.0 + 2.0 * (s.XValues[li] - _norm3DXMin) / _norm3DXRange;
                    double ny = -1.0 + 2.0 * (s.YValues[li] - _norm3DYMin) / _norm3DYRange;
                    double nz = -1.0 + 2.0 * (s.ZValues[li] - _norm3DZMin) / _norm3DZRange;
                    var raw = ProjectPoint3D(new Point3D(nx, ny, nz));

                    if (raw.HasValue)
                    {
                        var pos = Outward(raw.Value, 18);
                        var sb = new SolidColorBrush(s.Color);
                        PlaceLabel(_3dLabelCanvas, s.Name, pos.X + 4, pos.Y - 6, sb, 10, true);
                    }
                }
            }
        }

        internal Point? ProjectPoint3D(Point3D modelPoint)
        {
            if (PART_Viewport3D.Camera is not PerspectiveCamera cam || _surfaceVisual3D == null)
            {
                return null;
            }

            var world = _surfaceVisual3D.Transform.Transform(modelPoint);
            var look = cam.LookDirection;
            look.Normalize();
            var up = cam.UpDirection;
            up.Normalize();
            var right = Vector3D.CrossProduct(look, up);
            right.Normalize();
            var camUp = Vector3D.CrossProduct(right, look);

            var toPoint = world - cam.Position;
            double vx = Vector3D.DotProduct(toPoint, right);
            double vy = Vector3D.DotProduct(toPoint, camUp);
            double vz = Vector3D.DotProduct(toPoint, look);

            if (vz <= 0.01)
            {
                return null;
            }

            double tanHalf = Math.Tan(cam.FieldOfView * Math.PI / 360.0);
            double aspect = PART_Viewport3D.ActualWidth           / PART_Viewport3D.ActualHeight;
            double sx = (vx / (vz  * tanHalf * aspect) + 1) / 2.0 * PART_Viewport3D.ActualWidth;
            double sy = (1.0 - (vy / (vz * tanHalf) + 1) / 2.0)   * PART_Viewport3D.ActualHeight;

            return new Point(sx, sy);
        }

        private static void PlaceLabel(Canvas canvas, string text, double x, double y,
                                       Brush? brush, double size, bool bold = false)
        {
            var tb = new TextBlock
                     {
                         Text = text,
                         FontSize = size,
                         Foreground = brush,
                         FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                         IsHitTestVisible = false
                     };

            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }

        private static string Format3DTick(double v)
        {
            if (Math.Abs(v) >= 1000)
            {
                return v.ToString("0");
            }

            if (Math.Abs(v) >= 100)
            {
                return v.ToString("0.#");
            }

            return v.ToString("0.##");
        }

        private static void AddTubeToGroup(Model3DGroup group, Point3D from, Point3D to, Color color, double radius)
        {
            var mesh = CreateTubeMesh([from, to], radius);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            var mat = new DiffuseMaterial(brush);
            group.Children.Add(new GeometryModel3D(mesh, mat) { BackMaterial = mat });
        }

        private static void AddConeToGroup(Model3DGroup group, Point3D tip, Vector3D dir,
                                           Color color, double radius, double length, int sides = 8)
        {
            dir.Normalize();

            Vector3D up = Math.Abs(Vector3D.DotProduct(dir, new Vector3D(0, 1, 0))) < 0.99
                              ? new Vector3D(0, 1, 0)
                              : new Vector3D(1, 0, 0);

            Vector3D perp = Vector3D.CrossProduct(up, dir);
            perp.Normalize();
            Vector3D perp2 = Vector3D.CrossProduct(dir, perp);
            perp2.Normalize();
            var baseCenter = tip - dir * length;

            var positions = new Point3DCollection { tip };

            for (int s = 0; s < sides; s++)
            {
                double a = 2      * Math.PI     * s / sides;
                var offset = perp * Math.Cos(a) * radius + perp2 * Math.Sin(a) * radius;
                positions.Add(new Point3D(baseCenter.X + offset.X, baseCenter.Y + offset.Y, baseCenter.Z + offset.Z));
            }

            var indices = new Int32Collection();

            for (int s = 0; s < sides; s++)
            {
                indices.Add(0);
                indices.Add(1 + s);
                indices.Add(1 + (s + 1) % sides);
            }

            var mesh = new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            group.Children.Add(new GeometryModel3D(mesh, new DiffuseMaterial(brush))
                               { BackMaterial = new DiffuseMaterial(brush) });
        }

        private static MeshGeometry3D CreateTubeMesh(IList<Point3D> points, double radius, int sides = 8)
        {
            var positions = new Point3DCollection();
            var indices = new Int32Collection();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3D dir = i < points.Count - 1
                                   ? points[i + 1] - points[i]
                                   : points[i]     - points[i - 1];

                if (dir.LengthSquared < 1e-20)
                {
                    dir = new Vector3D(0, 0, 1);
                }
                else
                {
                    dir.Normalize();
                }

                Vector3D up = Math.Abs(Vector3D.DotProduct(dir, new Vector3D(0, 1, 0))) < 0.99
                                  ? new Vector3D(0, 1, 0)
                                  : new Vector3D(1, 0, 0);

                Vector3D perp = Vector3D.CrossProduct(up, dir);
                perp.Normalize();
                Vector3D perp2 = Vector3D.CrossProduct(dir, perp);
                perp2.Normalize();

                for (int s = 0; s < sides; s++)
                {
                    double angle = 2  * Math.PI         * s / sides;
                    var offset = perp * Math.Cos(angle) * radius + perp2 * Math.Sin(angle) * radius;
                    positions.Add(new Point3D(points[i].X + offset.X, points[i].Y + offset.Y, points[i].Z + offset.Z));
                }
            }

            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = i * sides + s, b = i * sides + (s + 1) % sides;
                    int c = (i                                + 1) * sides + s, d = (i + 1) * sides + (s + 1) % sides;
                    indices.Add(a);
                    indices.Add(c);
                    indices.Add(b);
                    indices.Add(b);
                    indices.Add(c);
                    indices.Add(d);
                }
            }

            return new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        }
    }
}