using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using ChartApp.Helpers;
using ChartApp.Models;

namespace SampleApplicationChartApp.ViewModels
{
    /// <summary>Main view model providing chart data, commands, and live-update support.</summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Annotation>? _annotations;
        private ObservableCollection<DataSeries>? _chartData;
        private bool _isDataLoaded;
        private bool _isLiveRunning;
        private bool _isLoading;
        private int _liveIndex;
        private Random? _liveRng;
        private System.Windows.Threading.DispatcherTimer? _liveTimer;
        private ChartType _selectedChartType;
        private ObservableCollection<XAxisDefinition>? _xAxes;
        private ObservableCollection<YAxisDefinition>? _yAxes;

        public MainViewModel()
        {
            LoadLinePlotCommand = new RelayCommand(async () => await LoadLinePlotAsync());

            ToggleLiveDataCommand = new RelayCommand(() =>
                                                     {
                                                         ToggleLiveData();

                                                         return Task.CompletedTask;
                                                     });

            LoadBoxPlotCommand = new RelayCommand(async () => await LoadBoxPlotAsync());
            LoadHistogramCommand = new RelayCommand(async () => await LoadHistogramAsync());
            LoadScatterPlotCommand = new RelayCommand(async () => await LoadScatterPlotAsync());
            LoadBubblePlotCommand = new RelayCommand(async () => await LoadBubblePlotAsync());
            LoadSurface3DCommand = new RelayCommand(async () => await LoadSurface3DAsync());
            LoadLine3DCommand = new RelayCommand(async () => await LoadLine3DAsync());
        }

        /// <summary>Gets or sets the chart data series collection.</summary>
        public ObservableCollection<DataSeries>? ChartData
        {
            get => _chartData;
            set
            {
                _chartData = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets or sets the X-axis definitions.</summary>
        public ObservableCollection<XAxisDefinition>? XAxes
        {
            get => _xAxes;
            set
            {
                _xAxes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets or sets the Y-axis definitions.</summary>
        public ObservableCollection<YAxisDefinition>? YAxes
        {
            get => _yAxes;
            set
            {
                _yAxes = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets or sets the chart annotations collection.</summary>
        public ObservableCollection<Annotation>? Annotations
        {
            get => _annotations;
            set
            {
                _annotations = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets or sets whether chart data has been loaded.</summary>
        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set
            {
                _isDataLoaded = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets or sets whether data is currently being loaded.</summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Command to load the line plot sample data.</summary>
        public ICommand LoadLinePlotCommand { get; }

        /// <summary>Command to load the box plot sample data.</summary>
        public ICommand LoadBoxPlotCommand { get; }

        /// <summary>Command to load the histogram sample data.</summary>
        public ICommand LoadHistogramCommand { get; }

        /// <summary>Command to load the scatter plot sample data.</summary>
        public ICommand LoadScatterPlotCommand { get; }

        /// <summary>Command to load the bubble plot sample data.</summary>
        public ICommand LoadBubblePlotCommand { get; }

        /// <summary>Command to load the 3D surface plot sample data.</summary>
        public ICommand LoadSurface3DCommand { get; }

        /// <summary>Command to load the 3D line chart sample data.</summary>
        public ICommand LoadLine3DCommand { get; }

        /// <summary>Command to toggle live data streaming on or off.</summary>
        public ICommand ToggleLiveDataCommand { get; }

        /// <summary>
        /// Callback set by the View to trigger a chart redraw when live data is appended.
        /// </summary>
        public Action? RequestRefresh { get; set; }

        /// <summary>Gets or sets whether live data streaming is active.</summary>
        public bool IsLiveRunning
        {
            get => _isLiveRunning;
            set
            {
                _isLiveRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LiveButtonText));
            }
        }

        /// <summary>Gets or sets the currently selected chart type.</summary>
        public ChartType SelectedChartType
        {
            get => _selectedChartType;
            set
            {
                _selectedChartType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Gets the display text for the live data toggle button.</summary>
        public string LiveButtonText => _isLiveRunning ? "⏹ Stop Live" : "▶ Start Live";

        /// <summary>Gets or sets the box plot data collection.</summary>
        public ObservableCollection<BoxPlotData>? BoxPlotSeries { get; set; }

        /// <summary>Gets or sets the 3D surface plot data.</summary>
        public Surface3DData? Surface3DSeries { get; set; }

        /// <summary>Gets or sets the 3D line chart data.</summary>
        public Line3DData? Line3DData { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task LoadLinePlotAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;
            SelectedChartType = ChartType.LinePlot;

            // Generate data on background thread to keep UI responsive
            var (s1, s2, s3, s4, s5, xValues) = await Task.Run(() =>
                                                               {
                                                                   const int pointCount = 5000;

                                                                   // Demonstrate the various ChartDataGenerator helpers
                                                                   var sine =
                                                                       ChartDataGenerator
                                                                           .GetNoisySinewaveYData(8, 0, pointCount, 5,
                                                                            0.5, 1);

                                                                   var cosine =
                                                                       ChartDataGenerator
                                                                           .GetNoisySinewaveYData(10, Math.PI / 2,
                                                                            pointCount, 8, 0.8, 2);

                                                                   var line =
                                                                       ChartDataGenerator
                                                                           .GetStraightLineYData(0.05, 70, pointCount);

                                                                   var walk =
                                                                       ChartDataGenerator.GetRandomWalkData(pointCount,
                                                                        40, 0.7, 3);

                                                                   var gauss =
                                                                       ChartDataGenerator
                                                                           .GetGaussianData(12, pointCount);

                                                                   // OA-date X axis — one sample every 12 hours over ~500 days
                                                                   var baseDate = new DateTime(2024, 1, 1);

                                                                   var xVals =
                                                                       ChartDataGenerator.GetDateTimeXData(baseDate,
                                                                        TimeSpan.FromHours(12), pointCount);

                                                                   return (sine.ToList(), cosine.ToList(),
                                                                           line.ToList(), walk.ToList(), gauss.ToList(),
                                                                           xVals.ToList());
                                                               });

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1", Label = "Sine", ShowLabel = true, Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Red, MinValue = s1.Min() - 2, MaxValue = s1.Max() + 2,
                    MajorTickCount = 10, ShowGridLines = true, GridLineBrush = Brushes.LightGray
                },
                new YAxisDefinition
                {
                    Id = "Y2", Label = "Cosine", ShowLabel = true, Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Blue, MinValue = s2.Min() - 2, MaxValue = s2.Max() + 2,
                    MajorTickCount = 10, ShowGridLines = true
                },
                new YAxisDefinition
                {
                    Id = "Y3", Label = "Line", ShowLabel = true, Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Green, MinValue = s3.Min() - 2, MaxValue = s3.Max() + 2,
                    MajorTickCount = 10, ShowGridLines = true
                },
                new YAxisDefinition
                {
                    Id = "Y4", Label = "Walk", ShowLabel = true, Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Orange, MinValue = s4.Min() - 2, MaxValue = s4.Max() + 2,
                    MajorTickCount = 10, ShowGridLines = true
                },
                new YAxisDefinition
                {
                    Id = "Y5", Label = "Gauss", ShowLabel = true, Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Purple, MinValue = s5.Min() - 2, MaxValue = s5.Max() + 2,
                    MajorTickCount = 10, ShowGridLines = true
                }
            ];

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1", Label = "Date", ShowLabel = true,
                    Position = XAxisPosition.Bottom, LabelBrush = Brushes.Black,
                    MajorTickCount = 5, ShowGridLines = true, GridLineBrush = Brushes.LightGray,
                    AxisType = AxisType.DateTime, DateTimeFormat = "MMM dd"
                }
            ];

            ChartData =
            [
                new DataSeries
                {
                    Name = "Sine Wave", Stroke = Brushes.Red, Thickness = 1.5, XValues = xValues, YValues = s1,
                    YAxisId = "Y1", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Cosine Wave", Stroke = Brushes.Blue, Thickness = 1.5, XValues = xValues, YValues = s2,
                    YAxisId = "Y2", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Straight Line", Stroke = Brushes.Green, Thickness = 1.5, XValues = xValues, YValues = s3,
                    YAxisId = "Y3", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Random Walk", Stroke = Brushes.Orange, Thickness = 1.5, XValues = xValues, YValues = s4,
                    YAxisId = "Y4", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Gaussian", Stroke = Brushes.Purple, Thickness = 1.5, XValues = xValues, YValues = s5,
                    YAxisId = "Y5", XAxisId = "X1"
                }
            ];

            Annotations = null;
            IsDataLoaded = true;
            IsLoading = false;
        }

        private async Task LoadBoxPlotAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            // Reset axes and data first to clear previous layout state
            ChartData = null;
            YAxes = null;
            XAxes = null;

            // Example: create two box plot groups with random data
            var rng = new Random(42);

            var groupA = new BoxPlotData
                         {
                             Category = "Group A",
                             Values = GenerateSeries(200, 50, 20, 5, 0.02, rng)
                         };

            var groupB = new BoxPlotData
                         {
                             Category = "Group B",
                             Values = GenerateSeries(200, 60, 15, 7, 0.015, rng)
                         };

            BoxPlotSeries = new ObservableCollection<BoxPlotData>
                            {
                                groupA,
                                groupB
                            };

            OnPropertyChanged(nameof(BoxPlotSeries));

            SelectedChartType = ChartType.BoxPlot;

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1", Label = "Value", ShowLabel = true,
                    Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Black,
                    MinValue = BoxPlotSeries.SelectMany(b => b.Values).Min() - 5,
                    MaxValue = BoxPlotSeries.SelectMany(b => b.Values).Max() + 5,
                    MajorTickCount = 5, ShowGridLines = true, GridLineBrush = Brushes.LightGray
                }
            ];

            OnPropertyChanged(nameof(YAxes)); // Force margin recalculation

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1", Label = "Sample", ShowLabel = false,
                    Position = XAxisPosition.Bottom, LabelBrush = Brushes.Black,
                    MajorTickCount = BoxPlotSeries.Count, ShowGridLines = false
                }
            ];

            OnPropertyChanged(nameof(XAxes)); // Force margin recalculation

            Annotations = null;

            var allY = new List<double>(BoxPlotSeries.SelectMany(b => b.Values));
            var allX = Enumerable.Range(0, allY.Count).Select(i => (double)i).ToList();

            ChartData = new ObservableCollection<DataSeries>
                        {
                            new DataSeries
                            {
                                Name = "BoxPlot",
                                YValues = allY,
                                XValues = allX,
                                YAxisId = "Y1",
                                XAxisId = "X1",
                                Stroke = Brushes.Transparent,
                                Thickness = 0.0
                            }
                        };

            IsDataLoaded = true;
            IsLoading = false;
        }

        private async Task LoadHistogramAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            var values = await Task.Run(() =>
                                        {
                                            var rng = new Random(99);

                                            return GenerateSeries(1000, 100, 30, 10, 0.01, rng);
                                        });

            SelectedChartType = ChartType.Histogram;

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1", Label = "Frequency", ShowLabel = false,
                    Position = YAxisPosition.Left, LabelBrush = Brushes.Black,
                    MinValue = 0, MaxValue = values.Count,
                    MajorTickCount = 5, ShowGridLines = true, GridLineBrush = Brushes.LightGray
                }
            ];

            OnPropertyChanged(nameof(YAxes)); // Force margin recalculation

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1", Label = "Bins", ShowLabel = false,
                    Position = XAxisPosition.Bottom, LabelBrush = Brushes.Black,
                    MajorTickCount = 8, ShowGridLines = false
                }
            ];

            OnPropertyChanged(nameof(XAxes)); // Force margin recalculation

            Annotations = null;

            ChartData =
            [
                new DataSeries
                {
                    Name = "Measurement Distribution", Stroke = Brushes.DarkOrange,
                    Thickness = 1.5, YValues = values, YAxisId = "Y1", XAxisId = "X1"
                }
            ];

            IsDataLoaded = true;
            IsLoading = false;
        }

        private async Task LoadScatterPlotAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            // Generate scatter plot data on background thread
            var (xValues, yValues1, yValues2, yValues3) = await Task.Run(() =>
                                                                         {
                                                                             var rng = new Random(42);
                                                                             const int pointCount = 200;

                                                                             // Generate 3 datasets with different distributions
                                                                             var xVals = new List<double>();
                                                                             var y1Vals = new List<double>();
                                                                             var y2Vals = new List<double>();
                                                                             var y3Vals = new List<double>();

                                                                             for (int i = 0; i < pointCount; i++)
                                                                             {
                                                                                 var x = i / 20.0;
                                                                                 xVals.Add(x);

                                                                                 // Dataset 1: Linear trend with noise
                                                                                 y1Vals.Add(30 + 5 * x +
                                                                                     (rng.NextDouble() * 8 - 4));

                                                                                 // Dataset 2: Quadratic trend with noise
                                                                                 y2Vals.Add(50 + 2 * x * x +
                                                                                     (rng.NextDouble() * 10 - 5));

                                                                                 // Dataset 3: Sine curve with noise
                                                                                 y3Vals.Add(60              +
                                                                                     15 * Math.Sin(x * 0.5) +
                                                                                     (rng.NextDouble() * 6 - 3));
                                                                             }

                                                                             return (xVals, y1Vals, y2Vals, y3Vals);
                                                                         });

            SelectedChartType = ChartType.ScatterPlot;

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1",
                    Label = "Value",
                    ShowLabel = true,
                    Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Black,
                    MinValue = 0,
                    MaxValue = 150,
                    MajorTickCount = 6,
                    ShowGridLines = true,
                    GridLineBrush = Brushes.LightGray
                }
            ];

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1",
                    Label = "X Value",
                    ShowLabel = true,
                    Position = XAxisPosition.Bottom,
                    LabelBrush = Brushes.Black,
                    MajorTickCount = 5,
                    ShowGridLines = true,
                    GridLineBrush = Brushes.LightGray
                }
            ];

            ChartData =
            [
                new DataSeries
                {
                    Name = "Linear Trend",
                    Stroke = Brushes.Blue,
                    Thickness = 0,
                    XValues = xValues,
                    YValues = yValues1,
                    YAxisId = "Y1",
                    XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Quadratic Trend",
                    Stroke = Brushes.Red,
                    Thickness = 0,
                    XValues = xValues,
                    YValues = yValues2,
                    YAxisId = "Y1",
                    XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Sine Wave",
                    Stroke = Brushes.Green,
                    Thickness = 0,
                    XValues = xValues,
                    YValues = yValues3,
                    YAxisId = "Y1",
                    XAxisId = "X1"
                }
            ];

            Annotations = null;

            IsDataLoaded = true;
            IsLoading = false;
        }

        private async Task LoadBubblePlotAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            var (xValues, yValues1, zValues1, yValues2, zValues2) = await Task.Run(() =>
                                                                    {
                                                                        var rng = new Random(42);
                                                                        const int pointCount = 1000;

                                                                        var xVals = new List<double>();
                                                                        var y1Vals = new List<double>();
                                                                        var z1Vals = new List<double>();
                                                                        var y2Vals = new List<double>();
                                                                        var z2Vals = new List<double>();

                                                                        for (var i = 0; i < pointCount; i++)
                                                                        {
                                                                            var x = i / 10.0;
                                                                            xVals.Add(x);

                                                                            // Dataset 1: Y = 30 + 2x, bubble size = 5 + 3x
                                                                            y1Vals.Add(30 + 2 * x +
                                                                                (rng.NextDouble() * 10 -
                                                                                        5));

                                                                            z1Vals.Add(5 + 3 * x +
                                                                                (rng.NextDouble() * 2 -
                                                                                        1));

                                                                            // Dataset 2: Y = 80 - 1.5x, bubble size = 15 - 2x
                                                                            y2Vals.Add(80 - 1.5 * x +
                                                                                (rng.NextDouble() * 10 -
                                                                                        5));

                                                                            z2Vals.Add(Math.Max(3, 15 - 2 * x +
                                                                                (rng.NextDouble() *
                                                                                        2 - 1)));
                                                                        }

                                                                        return (xVals, y1Vals, z1Vals, y2Vals,
                                                                                       z2Vals);
                                                                    });

            SelectedChartType = ChartType.BubblePlot;

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1",
                    Label = "Value",
                    ShowLabel = true,
                    Position = YAxisPosition.Left,
                    LabelBrush = Brushes.Black,
                    MinValue = 0,
                    MaxValue = 100,
                    MajorTickCount = 5,
                    ShowGridLines = true,
                    GridLineBrush = Brushes.LightGray
                }
            ];

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1",
                    Label = "X Value",
                    ShowLabel = true,
                    Position = XAxisPosition.Bottom,
                    LabelBrush = Brushes.Black,
                    MajorTickCount = 5,
                    ShowGridLines = true,
                    GridLineBrush = Brushes.LightGray
                }
            ];

            ChartData =
            [
                new DataSeries
                {
                    Name = "Uptrend",
                    Stroke = Brushes.Blue,
                    Thickness = 0,
                    XValues = xValues,
                    YValues = yValues1,
                    ZValues = zValues1,
                    YAxisId = "Y1",
                    XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Downtrend",
                    Stroke = Brushes.Red,
                    Thickness = 0,
                    XValues = xValues,
                    YValues = yValues2,
                    ZValues = zValues2,
                    YAxisId = "Y1",
                    XAxisId = "X1"
                }
            ];

            Annotations = null;

            IsDataLoaded = true;
            IsLoading = false;
        }

        private void ToggleLiveData()
        {
            if (_isLiveRunning)
            {
                _liveTimer?.Stop();
                IsLiveRunning = false;

                return;
            }

            // Clear any previously loaded data and start fresh
            _liveRng = new Random(123);
            _liveIndex = 0;

            YAxes =
            [
                new YAxisDefinition
                {
                    Id = "Y1", Label = "Signal", ShowLabel = false,
                    Position = YAxisPosition.Left, LabelBrush = Brushes.Red,
                    MajorTickCount = 4, ShowGridLines = true, GridLineBrush = Brushes.LightGray
                }
            ];

            XAxes =
            [
                new XAxisDefinition
                {
                    Id = "X1", Label = "Time", ShowLabel = false,
                    Position = XAxisPosition.Bottom, LabelBrush = Brushes.Black,
                    MajorTickCount = 8, ShowGridLines = true, GridLineBrush = Brushes.LightGray
                }
            ];

            Annotations = null;

            ChartData =
            [
                new DataSeries
                {
                    Name = "Temperature", Stroke = Brushes.Red, Thickness = 1.5, YValues = new List<double>(),
                    YAxisId = "Y1", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Humidity", Stroke = Brushes.Blue, Thickness = 1.5, YValues = new List<double>(),
                    YAxisId = "Y1", XAxisId = "X1"
                },
                new DataSeries
                {
                    Name = "Pressure", Stroke = Brushes.Green, Thickness = 1.5, YValues = new List<double>(),
                    YAxisId = "Y1", XAxisId = "X1"
                }
            ];

            _liveTimer ??= new System.Windows.Threading.DispatcherTimer();
            _liveTimer.Interval = TimeSpan.FromMilliseconds(50);
            _liveTimer.Tick += LiveTimer_Tick;
            IsLiveRunning = true;
            _liveTimer.Start();
        }

        private void LiveTimer_Tick(object? sender, EventArgs e)
        {
            if (ChartData == null || _liveRng == null)
            {
                return;
            }

            const int pointsPerTick = 5;

            for (var p = 0; p < pointsPerTick; p++)
            {
                double i = _liveIndex++;

                if (ChartData.Count > 0)
                {
                    ChartData[0].YValues
                                .Add(Math.Round(25 + 8 * Math.Sin(0.01 * i) + 0.5 * (_liveRng.NextDouble() * 2 - 1),
                                                2));
                }

                if (ChartData.Count > 1)
                {
                    ChartData[1].YValues
                                .Add(Math.Round(62 + 10 * Math.Sin(0.008 * i) + 0.8 * (_liveRng.NextDouble() * 2 - 1),
                                                2));
                }

                if (ChartData.Count > 2)
                {
                    ChartData[2].YValues
                                .Add(Math.Round(40 + 15 * Math.Sin(0.005 * i) + 1.0 * (_liveRng.NextDouble() * 2 - 1),
                                                2));
                }
            }

            // Update Y-axis range dynamically
            if (YAxes is { Count: > 0 } && ChartData.Count > 0)
            {
                double allMin = double.MaxValue, allMax = double.MinValue;

                foreach (var s in ChartData)
                {
                    foreach (var v in s.YValues)
                    {
                        if (v < allMin)
                        {
                            allMin = v;
                        }

                        if (v > allMax)
                        {
                            allMax = v;
                        }
                    }
                }

                YAxes[0].MinValue = allMin - 2;
                YAxes[0].MaxValue = allMax + 2;
            }

            RequestRefresh?.Invoke();
        }

        private async Task LoadSurface3DAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            ChartData = null;
            YAxes = null;
            XAxes = null;
            Annotations = null;

            const int gridSize = 60;
            const double range = Math.PI * 2;

            var zValues = await Task.Run(() =>
                                         {
                                             var grid = new double[gridSize, gridSize];

                                             for (int r = 0; r < gridSize; r++)
                                             {
                                                 for (int c = 0; c < gridSize; c++)
                                                 {
                                                     double x = -range + 2 * range * c / (gridSize - 1);
                                                     double y = -range + 2 * range * r / (gridSize - 1);
                                                     double dist = Math.Sqrt(x * x                 + y * y);

                                                     grid[r, c] = dist < 1e-10
                                                                      ? 1.0
                                                                      : Math.Sin(dist) / dist
                                                                        + 0.3          * Math.Sin(x) * Math.Cos(y);
                                                 }
                                             }

                                             return grid;
                                         });

            Surface3DSeries = new Surface3DData
                              {
                                  ZValues = zValues,
                                  XMin = -range,
                                  XMax = range,
                                  YMin = -range,
                                  YMax = range,
                                  XLabel = "X",
                                  YLabel = "Y",
                                  ZLabel = "Z"
                              };

            OnPropertyChanged(nameof(Surface3DSeries));
            SelectedChartType = ChartType.Surface3DPlot;

            IsDataLoaded = true;
            IsLoading = false;
        }

        private async Task LoadLine3DAsync()
        {
            IsDataLoaded = false;
            IsLoading = true;

            ChartData = null;
            YAxes = null;
            XAxes = null;
            Annotations = null;

            const int pointCount = 500;
            const int cycles = 3;

            // Build a 3D helix: X advances linearly, Y = sin, Z = cos.
            // All three axes span their full range so the series is centred
            // inside the bounding box, not collapsed onto one face.
            var (xVals, yVals, zVals) = await Task.Run(() =>
                                                       {
                                                           var x = ChartDataGenerator.GetUniformXData(pointCount, 0,
                                                            5.0 / (pointCount - 1));

                                                           var y =
                                                               ChartDataGenerator.GetSinewaveYData(1.0, 0, pointCount,
                                                                cycles);

                                                           var z =
                                                               ChartDataGenerator.GetCosineWaveYData(1.0, 0, pointCount,
                                                                cycles);

                                                           return (x, y, z);
                                                       });

            Line3DData = new Line3DData
                         {
                             Series =
                             [
                                 new Line3DSeries
                                 {
                                     Name = "Helix",
                                     Color = Colors.DodgerBlue,
                                     Thickness = 0.02,
                                     XValues = xVals,
                                     YValues = yVals,
                                     ZValues = zVals
                                 }
                             ],
                             XLabel = "Time",
                             YLabel = "Sin",
                             ZLabel = "Cos",
                             DisplayXMin = 0,  DisplayXMax = 5,
                             DisplayYMin = -1, DisplayYMax = 1,
                             DisplayZMin = -1, DisplayZMax = 1
                         };

            OnPropertyChanged(nameof(Line3DData));
            SelectedChartType = ChartType.Line3DPlot;

            IsDataLoaded = true;
            IsLoading = false;
        }

        private static List<double> GenerateSeries(int count, double baseValue, double amplitude, double noiseLevel,
                                                   double frequency, Random rng)
        {
            var values = new List<double>(count);

            for (var i = 0; i < count; i++)
            {
                var signal = baseValue
                             + amplitude  * Math.Sin(2       * Math.PI * frequency * i)
                             + amplitude  * 0.3 * Math.Sin(2 * Math.PI * frequency * 3.7 * i)
                             + noiseLevel * (rng.NextDouble() * 2 - 1);

                values.Add(Math.Round(signal, 2));
            }

            return values;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke() ?? true;
        }

        public async void Execute(object? parameter)
        {
            await execute();
        }
    }
}
