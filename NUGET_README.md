# ChartApp WPF Chart Control Library

A powerful, feature-rich WPF charting library for .NET 10 applications with support for multiple chart types, real-time data updates, and advanced visualization capabilities.

## Features

### Chart Types
- **Line Plot** - Connect data points with smooth or segmented lines
- **Scatter Plot** - Visualize data point clouds and distributions
- **Bubble Plot** - Show relationships between three variables with bubble size
- **Box Plot** - Display statistical distributions with quartiles and whiskers
- **Histogram** - Analyze frequency distributions with customizable bins
- **3D Line Chart** - Render 3D parametric curves and paths
- **3D Surface Plot** - Visualize complex 3D mathematical surfaces

### Interactive Features
- **Zoom & Pan** - Click-drag to zoom; mouse wheel for zoom levels up to 50x
- **Live Data Streaming** - Append data in real-time with automatic axis scaling
- **Annotations** - Add lines, rectangles, and text overlays
- **Multi-Axis Support** - Independent Y-axis definitions for different series
- **Tracker** - Interactive vertical line with value tooltips following cursor
- **Drag-Based Axis Adjustment** - Shift data view using X/Y axis drag areas

### Performance Optimizations
- **DrawingGroup/DrawingImage** - High-performance rendering for 100K+ points
- **Throttled Updates** - Adaptive tracking intervals based on data size
- **Caching** - Axis range caching to avoid repeated min/max calculations
- **Clipping** - Efficient off-screen geometry culling

### Developer Experience
- **MVVM-Friendly** - Bindable properties for all chart parameters
- **Data Generator Helpers** - Built-in functions for sine, cosine, random walk, Gaussian data
- **Customizable Themes** - Dark and Light theme support via ResourceDictionary
- **Extensive Documentation** - Inline XML comments and API documentation

## Installation

### Via NuGet Package Manager
```
Install-Package ChartApp.WPF
```

### Via .NET CLI
```
dotnet add package ChartApp.WPF
```

## Quick Start

### 1. Add ChartControl to XAML

```xaml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:chart="clr-namespace:ChartApp.Controls;assembly=ChartApp">
    <Grid>
        <chart:ChartControl 
            x:Name="MyChart"
            ChartType="ScatterPlot"
            Series="{Binding ChartSeries}"
            YAxes="{Binding YAxisDefinitions}"
            XAxes="{Binding XAxisDefinitions}"
            ShowTrackerLine="True" />
    </Grid>
</Window>
```

### 2. Populate Data in ViewModel

```csharp
using ChartApp.Models;
using ChartApp.Helpers;

public class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DataSeries> ChartSeries { get; set; }
    public ObservableCollection<YAxisDefinition> YAxisDefinitions { get; set; }
    public ObservableCollection<XAxisDefinition> XAxisDefinitions { get; set; }

    public void LoadData()
    {
        // Generate sample data
        var xValues = ChartDataGenerator.GetUniformXData(100, 0, 1.0);
        var yValues = ChartDataGenerator.GetSinewaveYData(10, 0, 100, 5);

        ChartSeries = new ObservableCollection<DataSeries>
        {
            new DataSeries
            {
                Name = "Sine Wave",
                XValues = xValues.ToList(),
                YValues = yValues.ToList(),
                Stroke = Brushes.Blue,
                Thickness = 1.5,
                YAxisId = "Y1",
                XAxisId = "X1"
            }
        };

        YAxisDefinitions = new ObservableCollection<YAxisDefinition>
        {
            new YAxisDefinition
            {
                Id = "Y1",
                Label = "Value",
                Position = YAxisPosition.Left,
                ShowGridLines = true,
                MajorTickCount = 10
            }
        };

        XAxisDefinitions = new ObservableCollection<XAxisDefinition>
        {
            new XAxisDefinition
            {
                Id = "X1",
                Label = "Time",
                Position = XAxisPosition.Bottom,
                ShowGridLines = true
            }
        };
    }
}
```

## Core Classes

### ChartControl
Main WPF control for rendering charts.

**Key Properties:**
- `ChartType` - Chart visualization type (LinePlot, ScatterPlot, etc.)
- `Series` - Observable collection of data series
- `YAxes` - Y-axis definitions for multi-axis support
- `XAxes` - X-axis definitions
- `ShowTrackerLine` - Enable/disable interactive tracker
- `ShowLegend` - Toggle legend visibility
- `Annotations` - Collection of visual annotations

**Key Commands:**
- `ResetViewCommand` - Reset zoom/pan to default
- `UndoCommand` / `RedoCommand` - Undo/redo zoom states
- `ToggleLockCommand` - Lock/unlock interaction

### DataSeries
Represents a single data series in the chart.

```csharp
public class DataSeries
{
    public string Name { get; set; }
    public List<double> XValues { get; set; }
    public List<double> YValues { get; set; }
    public List<double> ZValues { get; set; }  // For bubble plots
    public Brush Stroke { get; set; }
    public double Thickness { get; set; }
    public string YAxisId { get; set; }
    public string XAxisId { get; set; }
    public bool HasExplicitXValues { get; set; }
}
```

### YAxisDefinition / XAxisDefinition
Define axis appearance and behavior.

```csharp
public class YAxisDefinition
{
    public string Id { get; set; }
    public string Label { get; set; }
    public YAxisPosition Position { get; set; }  // Left or Right
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public int MajorTickCount { get; set; }
    public bool ShowGridLines { get; set; }
    public Brush GridLineBrush { get; set; }
    public Brush LabelBrush { get; set; }
}
```

### ChartDataGenerator
Static utility class for generating test data.

**Methods:**
- `GetRandomDoubleData()` - Random values in a range
- `GetStraightLineYData()` - Linear trend
- `GetSinewaveYData()` - Sine wave with configurable frequency
- `GetNoisySinewaveYData()` - Sine wave with noise
- `GetCosineWaveYData()` - Cosine wave
- `GetRandomWalkData()` - Random walk simulation
- `GetGaussianData()` - Gaussian distribution
- `GetExponentialData()` - Exponential decay/growth
- `GetUniformXData()` - Uniformly spaced X values
- `GetDateTimeXData()` - DateTime-based X axis
- `GetFourierYData()` - Fourier series composition

## Advanced Usage

### Live Data Streaming

```csharp
public void StartLiveUpdates()
{
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
    timer.Tick += (s, e) =>
    {
        // Append new data point
        ChartSeries[0].YValues.Add(Math.Sin(DateTime.Now.Millisecond * 0.01));
        
        // Trigger chart redraw
        MyChart.InvalidateVisual();
    };
    timer.Start();
}
```

### Custom Annotations

```csharp
Annotations = new ObservableCollection<Annotation>
{
    new LineAnnotation
    {
        Name = "Threshold",
        X1 = 10, Y1 = 75,
        X2 = 100, Y2 = 75,
        StrokeColor = Colors.Red,
        StrokeThickness = 2
    },
    new RectangleAnnotation
    {
        Name = "Alert Zone",
        X = 20, Y = 60,
        Width = 30, Height = 20,
        FillColor = Color.FromArgb(50, 255, 0, 0)
    }
};
```

### Multi-Axis Chart

```csharp
YAxisDefinitions = new ObservableCollection<YAxisDefinition>
{
    new YAxisDefinition { Id = "Y1", Label = "Temperature (°C)", Position = YAxisPosition.Left },
    new YAxisDefinition { Id = "Y2", Label = "Humidity (%)", Position = YAxisPosition.Right }
};

ChartSeries = new ObservableCollection<DataSeries>
{
    new DataSeries { Name = "Temp", YAxisId = "Y1", ... },
    new DataSeries { Name = "Humidity", YAxisId = "Y2", ... }
};
```

## Performance Considerations

- **Large Datasets (>10K points)**: Use `ThrottledTrackerInterval` to reduce update frequency
- **Multiple Series**: Combine series on the same axis when possible
- **Real-Time Updates**: Append data in batches (e.g., 5-10 points per frame)
- **3D Rendering**: Keep grid size ≤100×100 for Surface3D plots

## Theming

Change theme at runtime:

```csharp
var uri = new Uri("pack://application:,,,/ChartApp;component/Themes/Theme.Dark.xaml", UriKind.Absolute);
var theme = new ResourceDictionary { Source = uri };
Application.Current.Resources.MergedDictionaries.Clear();
Application.Current.Resources.MergedDictionaries.Add(theme);
```

## Keyboard Shortcuts

- **Arrow Keys** - Adjust Y-axis offset
- **Ctrl + Z** - Undo zoom state
- **Ctrl + Y** - Redo zoom state
- **Double-Click** - Reset view (if enabled)

## System Requirements

- **.NET 10.0** or later
- **Windows 10** or later (WPF platform requirement)
- **Visual Studio 2022** or later recommended

## License

This package is licensed under the MIT License. See LICENSE file for details.

## Support & Issues

For bug reports, feature requests, or questions:
- GitHub Issues: https://github.com/hiteshajmermodani-dot/ChartApp/issues
- Documentation: Check inline XML comments in source code

## Changelog

### Version 1.0.0
- Initial NuGet release
- Full support for 7 chart types
- Multi-axis and live streaming
- Advanced tracking and annotation features
- DrawingImage-based performance optimization for 100K+ points
