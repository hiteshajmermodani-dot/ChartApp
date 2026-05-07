using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace ChartApp.Controls
{
    public partial class ChartControl
    {
        private bool _isDragging3D;
        private AxisAngleRotation3D? _rot3DX;
        private AxisAngleRotation3D? _rot3DY;
        private Point _surface3DDragStart;
        private ModelVisual3D? _surfaceVisual3D;

        private void DrawSurface3D()
        {
            if (Surface3DSeries == null)
            {
                return;
            }

            var data = Surface3DSeries;
            int rows = data.ZValues.GetLength(0);
            int cols = data.ZValues.GetLength(1);

            if (rows < 2 || cols < 2)
            {
                return;
            }

            // Hide the 2D canvas and overlay
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

            // Find Z range for color normalization
            var zMin = double.MaxValue;
            var zMax = double.MinValue;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var z = data.ZValues[r, c];

                    if (z < zMin)
                    {
                        zMin = z;
                    }

                    if (z > zMax)
                    {
                        zMax = z;
                    }
                }
            }

            if (Math.Abs(zMax - zMin) < 1e-10)
            {
                zMax = zMin + 1;
            }

            // Build mesh: all axes normalized to [-1,1]
            var positions = new Point3DCollection(rows   * cols);
            var uvs = new PointCollection(rows           * cols);
            var indices = new Int32Collection((rows - 1) * (cols - 1) * 6);

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < cols; c++)
                {
                    var x = -1.0 + 2.0 * c / (cols - 1);
                    var y = -1.0 + 2.0 * r / (rows - 1);
                    var zNorm = (data.ZValues[r, c] - zMin) / (zMax - zMin); // 0..1 for UV / color
                    var yPos = -1.0 + 2.0 * zNorm;                           // -1..1 for 3D position

                    // WPF 3D: X = right, Y = up, Z = toward viewer
                    positions.Add(new Point3D(x, yPos, y));

                    // U maps to normalised height for gradient colour (keep 0..1)
                    uvs.Add(new Point(zNorm, 0.5));
                }
            }

            for (var r = 0; r < rows - 1; r++)
            {
                for (var c = 0; c < cols - 1; c++)
                {
                    var i00 = r       * cols + c;
                    var i10 = r       * cols + c + 1;
                    var i01 = (r + 1) * cols + c;
                    var i11 = (r + 1) * cols + c + 1;

                    // Two triangles per quad (consistent winding)
                    indices.Add(i00);
                    indices.Add(i01);
                    indices.Add(i11);
                    indices.Add(i00);
                    indices.Add(i11);
                    indices.Add(i10);
                }
            }

            var mesh = new MeshGeometry3D
                       {
                           Positions = positions,
                           TriangleIndices = indices,
                           TextureCoordinates = uvs
                       };

            // Height-mapped gradient: blue → cyan → green → yellow → red
            var gradient = new LinearGradientBrush
                           {
                               StartPoint = new Point(0, 0),
                               EndPoint = new Point(1, 0)
                           };

            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 0, 200), 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 180, 220), 0.25));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0, 200, 80), 0.5));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(230, 200, 0), 0.75));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(200, 0, 0), 1.0));
            gradient.Freeze();

            var material = new DiffuseMaterial(gradient);
            var surfaceModel = new GeometryModel3D(mesh, material) { BackMaterial = material };

            // Rotation transforms (drag to rotate)
            _rot3DX = new AxisAngleRotation3D(new Vector3D(1, 0, 0), -25);
            _rot3DY = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 30);

            var transforms = new Transform3DGroup();
            transforms.Children.Add(new RotateTransform3D(_rot3DX));
            transforms.Children.Add(new RotateTransform3D(_rot3DY));

            var modelGroup = new Model3DGroup();
            modelGroup.Children.Add(surfaceModel);
            modelGroup.Children.Add(new AmbientLight(Color.FromRgb(70, 70, 70)));
            modelGroup.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -2, -1.5)));
            modelGroup.Children.Add(new DirectionalLight(Color.FromRgb(80, 80, 160), new Vector3D(1, 1, 1)));

            _surfaceVisual3D = new ModelVisual3D
                               {
                                   Content = modelGroup,
                                   Transform = transforms
                               };

            PART_Viewport3D.Camera = new PerspectiveCamera
                                     {
                                         Position = new Point3D(0, 2.2, 4.5),
                                         LookDirection = new Vector3D(0, -0.5, -1),
                                         UpDirection = new Vector3D(0, 1, 0),
                                         FieldOfView = 45
                                     };

            PART_Viewport3D.Children.Add(_surfaceVisual3D);

            // Store display ranges for axis overlay
            // In Surface3D model coords: X→data.X, Y(up)→data.Z (height), Z(depth)→data.Y
            _disp3DXMin = data.XMin;
            _disp3DXMax = data.XMax;
            _disp3DXLabel = data.XLabel;
            _disp3DYMin = zMin;
            _disp3DYMax = zMax;
            _disp3DYLabel = data.ZLabel;
            _disp3DZMin = data.YMin;
            _disp3DZMax = data.YMax;
            _disp3DZLabel = data.YLabel;
            _norm3DXMin = 0;
            _norm3DXRange = 1;
            _norm3DYMin = 0;
            _norm3DYRange = 1;
            _norm3DZMin = 0;
            _norm3DZRange = 1;

            Dispatcher.InvokeAsync(Draw3DAxisOverlay, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void HideSurface3D()
        {
            PART_Viewport3D.Visibility = Visibility.Collapsed;
            PART_Viewport3D.Margin = new Thickness(0);
            PART_3DMouseLayer.Visibility = Visibility.Collapsed;
            PART_Canvas.Visibility = Visibility.Visible;
            PART_Overlay.Visibility = Visibility.Visible;

            if (_3dLabelCanvas is { Parent: Grid parentGrid })
            {
                parentGrid.Children.Remove(_3dLabelCanvas);
                _3dLabelCanvas = null;
            }
        }

        // ── Mouse interaction ────────────────────────────────────────────────

        private void Viewport3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging3D = true;
            _surface3DDragStart = e.GetPosition(PART_3DMouseLayer);
            PART_3DMouseLayer.CaptureMouse();
        }

        private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging3D || _rot3DX == null || _rot3DY == null)
            {
                return;
            }

            var pos = e.GetPosition(PART_3DMouseLayer);
            var delta = pos - _surface3DDragStart;
            _surface3DDragStart = pos;

            _rot3DY.Angle += delta.X * 0.5;
            _rot3DX.Angle = Math.Clamp(_rot3DX.Angle + delta.Y * 0.5, -89, 89);

            Draw3DAxisOverlay();
        }

        private void Viewport3D_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging3D = false;
            PART_3DMouseLayer.ReleaseMouseCapture();
        }

        private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (PART_Viewport3D.Camera is not PerspectiveCamera cam)
            {
                return;
            }

            var dir = cam.LookDirection;
            dir.Normalize();
            var step = e.Delta > 0 ? 0.2 : -0.2;

            cam.Position = new Point3D(
                                       cam.Position.X + dir.X * step,
                                       cam.Position.Y + dir.Y * step,
                                       cam.Position.Z + dir.Z * step);
        }
    }
}