# ChartApp Control Library - API Documentation

## Overview

ChartApp is a powerful, feature-rich WPF charting control library with support for multiple chart types, interactive features, 3D visualization, and comprehensive customization options.

### Key Features
- **Multiple Chart Types**: Line, scatter, bubble, box plots, histograms, 3D surface, 3D line charts
- **Interactive Features**: Mouse-wheel zoom, rectangle zoom, pan, axis dragging, annotation dragging, tracker line
- **Multi-Axis Support**: Multiple Y-axes on left/right, X-axes on top/bottom
- **Theming**: Built-in light and dark themes
- **Viewport Management**: Customizable zoom/pan behavior via `ChartViewportManager`
- **Undo/Redo**: Full viewport state history (`Ctrl+Z` / `Ctrl+Y`)
- **Series Highlighting**: Click-to-highlight series via legend
- **Grid Lines**: Major/minor grid lines with alternating horizontal bands

---

## Quick Start

### 1. Add ChartApp to XAML

```xaml
<Window xmlns:controls="clr-namespace:ChartAppLib.Controls;assembly=ChartAppLib"
        xmlns:models="clr-namespace:ChartAppLib.Models;assembly=ChartAppLib">
		
<Window.Resources>
    <ResourceDictionary>           
        <ResourceDictionary.MergedDictionaries>
           <ResourceDictionary Source="pack://application:,,,/ChartAppLib;Component/Themes/Theme.Light.xaml"/>
        </ResourceDictionary.MergedDictionaries>              
    </ResourceDictionary>
</Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <controls:ChartToolbar Grid.Row="0" TargetChart="{Binding ElementName=MyChart}" 
		ShowThemeSelector="False" ShowChartTypeSelector="False" ShowDoubleClickResetButton="False"/>

        <controls:ChartControl x:Name="MyChart"
                               Grid.Row="1"
                               Series="{Binding ChartData}"
                               XAxes="{Binding XAxes}"
                               YAxes="{Binding YAxes}"
                               Annotations="{Binding Annotations}"
                               ChartType="{Binding SelectedChartType}"
                               ShowTrackerLine="True" />
    </Grid>
</Window>
```

### 2. Populate with Data (ViewModel)

```csharp
using ChartAppLib.Models;
using System.Collections.ObjectModel;
using System.Windows.Media;

public class MyViewModel
{
    public ObservableCollection<DataSeries> ChartData { get; set; }
    public ObservableCollection<XAxisDefinition> XAxes { get; set; }
    public ObservableCollection<YAxisDefinition> YAxes { get; set; }

    public MyViewModel()
    {
        var xValues = Enumerable.Range(0, 100).Select(i => (double)i).ToList();
        var yValues = Enumerable.Range(0, 100).Select(i => Math.Sin(i * 0.1)).ToList();

        ChartData = new ObservableCollection<DataSeries>
        {
            new DataSeries
            {
                Name = "Sine Wave",
                Stroke = Brushes.Blue,
                Thickness = 2,
                XValues = xValues,
                YValues = yValues,
                XAxisId = "X1",
                YAxisId = "Y1"
            }
        };

        XAxes = new ObservableCollection<XAxisDefinition>
        {
            new XAxisDefinition
            {
                Id = "X1",
                Label = "Time",
                ShowLabel = true,
                Position = XAxisPosition.Bottom,
                MajorTickCount = 10,
                ShowGridLines = true,
                GridLineBrush = Brushes.LightGray
            }
        };

        YAxes = new ObservableCollection<YAxisDefinition>
        {
            new YAxisDefinition
            {
                Id = "Y1",
                Label = "Amplitude",
                ShowLabel = true,
                Position = YAxisPosition.Left,
                LabelBrush = Brushes.Black,
                MajorTickCount = 5,
                ShowGridLines = true,
                GridLineBrush = Brushes.LightGray
            }
        };

        //MyChart.Refresh() method can be use in code behind or in viewmodel to refresh the chart after data load
    }
}
```

---

## Core Components

### ChartControl

The main chart rendering control.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Series` | `ObservableCollection<DataSeries>` | `null` | Data series collection |
| `XAxes` | `ObservableCollection<XAxisDefinition>` | `null` | X-axis definitions |
| `YAxes` | `ObservableCollection<YAxisDefinition>` | `null` | Y-axis definitions |
| `Annotations` | `ObservableCollection<Annotation>` | `null` | Line and box annotations |
| `BoxPlotSeries` | `IEnumerable<BoxPlotData>` | `null` | Box plot data source |
| `Surface3DSeries` | `Surface3DData` | `null` | 3D surface plot data |
| `Line3DData` | `Line3DData` | `null` | 3D line chart data |
| `ChartType` | `ChartType` | `LinePlot` | Active chart type |
| `ShowTrackerLine` | `bool` | `true` | Show vertical tracker line on hover |
| `ShowLegend` | `bool` | `true` | Show/hide legend |
| `ShowXAxisLabel` | `bool` | `true` | Show/hide X-axis label |
| `ShowYAxisLabel` | `bool` | `true` | Show/hide Y-axis label |
| `ShowMajorGridLines` | `bool` | `true` | Show/hide major grid lines |
| `ShowMinorGridLines` | `bool` | `true` | Show/hide minor grid lines |
| `MajorGridLineBrush` | `Brush` | `LightGray` | Major grid line color |
| `MajorGridBandBrush` | `Brush` | Semi-transparent gray | Alternating band fill color |
| `MinorGridLineBrush` | `Brush` | `Silver` | Minor grid line color |
| `IsLocked` | `bool` | `false` | Locks zoom, pan, and axis drag |
| `DoubleClickResetEnabled` | `bool` | `true` | Reset view on double-click |
| `ViewportManager` | `ChartViewportManager` | `DefaultChartViewportManager` | Zoom/pan behavior |
| `YMajorTickCount` | `int` | `5` | Major tick count on Y-axis |
| `YMinorTickCount` | `int` | `4` | Minor ticks per major Y interval |
| `XMajorTickCount` | `int` | `5` | Major tick count on X-axis |
| `XMinorTickCount` | `int` | `4` | Minor ticks per major X interval |
| `TrackerTooltipBackground` | `Brush` | Semi-transparent white | Tracker tooltip background |
| `TrackerTooltipBorderBrush` | `Brush` | `Gray` | Tracker tooltip border color |
| `TrackerTooltipBorderThickness` | `Thickness` | `1` | Tracker tooltip border thickness |
| `TrackerTooltipCornerRadius` | `CornerRadius` | `4` | Tracker tooltip corner radius |
| `TrackerTooltipPadding` | `Thickness` | `8,5,8,5` | Tracker tooltip padding |
| `TrackerLineStroke` | `Brush` | `Gray` | Tracker line color |
| `TrackerLineStrokeThickness` | `double` | `1.0` | Tracker line thickness |
| `TrackerLineDashArray` | `DoubleCollection` | `[4,2]` | Tracker line dash pattern |
| `TrackerTooltipTemplate` | `DataTemplate` | `null` | Custom tracker tooltip template (see [Custom Tracker Tooltip](#pattern-9-custom-tracker-tooltip)) |
| `ZoomRectangleFill` | `Brush` | Semi-transparent gray | Zoom rectangle fill |
| `ZoomRectangleStroke` | `Brush` | `Gray` | Zoom rectangle border color |
| `ZoomRectangleStrokeThickness` | `double` | `2.0` | Zoom rectangle border thickness |
| `ZoomRectangleStrokeDashArray` | `DoubleCollection` | `null` | Zoom rectangle dash pattern |

#### Methods

```csharp
// View control
public void ResetView()
public void ShiftXAxis(double delta)
public void ShiftYAxis(double delta)
public void Undo()
public void Redo()

// Series highlighting
public void HighlightSeries(string seriesName)
public void HighlightSeries(int seriesIndex)
public void ClearHighlight()

// Data refresh
public void RefreshChart()

// Export
public void CopyChartImageToClipboard()
public RenderTargetBitmap RenderChartToBitmap()
public void CopyXYValuesToClipboard()

// Toolbar integration
public void NotifyToolbarStateChanged()
```

#### Events

```csharp
// Raised on every zoom, pan, axis shift, or reset
public event EventHandler ViewportChanged;

// Raised when toolbar-relevant state changes (lock, legend, grid, etc.)
public event EventHandler ToolbarStateChanged;
```

#### Bindable Commands

```csharp
public ICommand ToggleTrackerCommand { get; }
public ICommand ToggleLegendCommand { get; }
public ICommand ToggleMajorGridCommand { get; }
public ICommand ToggleMinorGridCommand { get; }
public ICommand ResetViewCommand { get; }
public ICommand UndoCommand { get; }
public ICommand RedoCommand { get; }
public ICommand ToggleLockCommand { get; }
public ICommand ToggleDoubleClickResetCommand { get; }
```

---

### ChartToolbar

Pre-built toolbar that binds to a `ChartControl` via `TargetChart`.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TargetChart` | `ChartControl` | `null` | Chart to control |
| `ShowTrackerButton` | `bool` | `true` | Show tracker toggle button |
| `ShowLegendButton` | `bool` | `true` | Show legend toggle button |
| `ShowLabelsButton` | `bool` | `true` | Show axis labels toggle button |
| `ShowMajorGridButton` | `bool` | `true` | Show major grid toggle button |
| `ShowMinorGridButton` | `bool` | `true` | Show minor grid toggle button |
| `ShowResetButton` | `bool` | `true` | Show reset view button |
| `ShowUndoRedoButtons` | `bool` | `true` | Show undo/redo buttons |
| `ShowLockButton` | `bool` | `true` | Show lock toggle button |
| `ShowDoubleClickResetButton` | `bool` | `true` | Show double-click reset toggle |
| `ShowChartTypeSelector` | `bool` | `true` | Show chart type dropdown |
| `ShowThemeSelector` | `bool` | `true` | Show light/dark theme selector |

---

## Data Models

### DataSeries

```csharp
public class DataSeries
{
    public required string Name { get; set; }         // Series name (shown in legend)
    public Brush Stroke { get; set; }                 // Line/point color (default: Blue)
    public double Thickness { get; set; }             // Line thickness (default: 2)
    public IList<double>? XValues { get; set; }       // Explicit X coordinates (optional)
    public IList<double> YValues { get; set; }        // Y coordinates
    public string? XAxisId { get; set; }              // Associated X-axis ID
    public string? YAxisId { get; set; }              // Associated Y-axis ID
    public IList<double>? ZValues { get; set; }       // Z values for bubble/3D charts
    public bool HasExplicitXValues { get; }           // True when XValues.Count == YValues.Count

    public void AddPoint(double x, double y);         // Append a data point
}
```

### YAxisDefinition

```csharp
public class YAxisDefinition
{
    public required string Id { get; set; }           // Unique identifier
    public string Label { get; set; }                 // Axis label text
    public bool ShowLabel { get; set; }               // Show rotated label
    public YAxisPosition Position { get; set; }       // Left or Right
    public Brush LabelBrush { get; set; }             // Tick and label color
    public double? MinValue { get; set; }             // Override minimum (auto if null)
    public double? MaxValue { get; set; }             // Override maximum (auto if null)
    public int MajorTickCount { get; set; }           // Number of major ticks
    public int MinorTickCount { get; set; }           // Minor ticks per major interval
    public bool ShowGridLines { get; set; }           // Draw horizontal grid lines
    public Brush GridLineBrush { get; set; }          // Grid line color
}

public enum YAxisPosition { Left, Right }
```

### XAxisDefinition

```csharp
public class XAxisDefinition
{
    public required string Id { get; set; }           // Unique identifier
    public string Label { get; set; }                 // Axis label text
    public bool ShowLabel { get; set; }               // Show label below axis
    public XAxisPosition Position { get; set; }       // Bottom or Top
    public Brush LabelBrush { get; set; }             // Tick and label color
    public double? MinValue { get; set; }             // Override minimum (auto if null)
    public double? MaxValue { get; set; }             // Override maximum (auto if null)
    public int MajorTickCount { get; set; }           // Number of major ticks
    public int MinorTickCount { get; set; }           // Minor ticks per major interval
    public bool ShowGridLines { get; set; }           // Draw vertical grid lines
    public Brush GridLineBrush { get; set; }          // Grid line color
}

public enum XAxisPosition { Bottom, Top }
```

### Annotations

#### LineAnnotation

```csharp
public class LineAnnotation : Annotation
{
    public double Value { get; set; }                          // Data-space position
    public AnnotationOrientation Orientation { get; set; }    // Horizontal or Vertical
    // Inherited from Annotation:
    public string Label { get; set; }
    public Brush Stroke { get; set; }                         // Default: Red, dashed [4,2]
    public double StrokeThickness { get; set; }
    public DoubleCollection? StrokeDashArray { get; set; }
    public bool IsDraggable { get; set; }                     // Default: true
    public string? XAxisId { get; set; }
    public string? YAxisId { get; set; }
}

public enum AnnotationOrientation { Horizontal, Vertical }
```

#### BoxAnnotation

```csharp
public class BoxAnnotation : Annotation
{
    public double X1 { get; set; }                    // Left boundary (data value)
    public double X2 { get; set; }                    // Right boundary (data value)
    public double Y1 { get; set; }                    // Bottom boundary (data value)
    public double Y2 { get; set; }                    // Top boundary (data value)
    public Brush Fill { get; set; }                   // Interior fill (default: semi-transparent orange)
    // Inherited from Annotation:
    public string Label { get; set; }
    public Brush Stroke { get; set; }                 // Default: Orange
    public double StrokeThickness { get; set; }
    public string? XAxisId { get; set; }
    public string? YAxisId { get; set; }
}
```

### BoxPlotData

```csharp
public class BoxPlotData
{
    public required string Category { get; set; }     // Category label
    public List<double> Values { get; set; }          // Raw data (statistics computed at render time)
}
```

### TrackerData

Bound to `TrackerTooltipTemplate` when a custom tracker tooltip is used.
Implements `INotifyPropertyChanged` — updates live as the mouse moves.

```csharp
public class TrackerData
{
    public string XText { get; set; }                          // Formatted X label (e.g. "Time: 3.14")
    public ObservableCollection<TrackerSeriesItem> Items { get; } // One item per visible series
}

public class TrackerSeriesItem
{
    public string Name { get; set; }                  // Series name
    public double Value { get; set; }                 // Interpolated Y value
    public string FormattedValue { get; set; }        // Display string (e.g. "Temperature: 23.45")
    public Brush Stroke { get; set; }                 // Series color (use as swatch)
}
```

### Surface3DData

```csharp
public class Surface3DData
{
    public double[,] ZValues { get; set; }            // 2D grid of Z heights
    public double XMin { get; set; }
    public double XMax { get; set; }
    public double YMin { get; set; }
    public double YMax { get; set; }
    public string XLabel { get; set; }
    public string YLabel { get; set; }
    public string ZLabel { get; set; }
}
```

### Line3DData

```csharp
public class Line3DData
{
    public List<Line3DSeries> Series { get; set; }
    public string XLabel { get; set; }
    public string YLabel { get; set; }
    public string ZLabel { get; set; }
    public double DisplayXMin { get; set; }
    public double DisplayXMax { get; set; }
    public double DisplayYMin { get; set; }
    public double DisplayYMax { get; set; }
    public double DisplayZMin { get; set; }
    public double DisplayZMax { get; set; }
}

public class Line3DSeries
{
    public string Name { get; set; }
    public Color Color { get; set; }
    public double Thickness { get; set; }
    public double[] XValues { get; set; }
    public double[] YValues { get; set; }
    public double[] ZValues { get; set; }
}
```

### ChartType Enum

```csharp
public enum ChartType
{
    LinePlot,       // Standard line chart
    ScatterPlot,    // Scatter plot (points only)
    BubblePlot,     // Bubble chart (size from ZValues)
    BoxPlot,        // Box-and-whisker chart (uses BoxPlotSeries)
    Histogram,      // Histogram
    Surface3DPlot,  // 3D surface (uses Surface3DSeries)
    Line3DPlot      // 3D line chart (uses Line3DData)
}
```

---

## ChartDataGenerator

`ChartAppLib.Helpers.ChartDataGenerator` is a static helper class that generates `double[]` arrays for testing, demos, and prototyping.
All methods are allocation-friendly and run safely on background threads.

```csharp
using ChartAppLib.Helpers;
```

### Methods

| Method | Parameters | Description |
|--------|-----------|-------------|
| `GetRandomDoubleData` | `int pointCount, double min = 0, double max = 100, int? seed = null` | Uniformly-distributed random values in [min, max] |
| `GetStraightLineYData` | `double gradient, double yIntercept, int pointCount` | y = gradient·x + yIntercept, x = 0…pointCount−1 |
| `GetSinewaveYData` | `double amplitude, double phase, int pointCount, int freq = 10` | Pure sine wave with `freq` full cycles |
| `GetNoisySinewaveYData` | `double amplitude, double phase, int pointCount, int freq = 10, double noiseAmplitude = 0.1, int? seed = null` | Sine wave with additive uniform noise |
| `GetCosineWaveYData` | `double amplitude, double phase, int pointCount, int freq = 10` | Pure cosine wave with `freq` full cycles |
| `GetUniformXData` | `int pointCount, double start = 0, double step = 1` | Evenly-spaced X values: start, start+step, … |
| `GetDateTimeXData` | `DateTime start, TimeSpan interval, int pointCount` | OA-date X values for use with `AxisType.DateTime` |
| `GetRandomWalkData` | `int pointCount, double startValue = 0, double stepSize = 1, int? seed = null` | Cumulative random walk |
| `GetGaussianData` | `double amplitude, int pointCount, double sigma = 0.15` | Gaussian bell curve centred in the range |
| `GetExponentialData` | `double scale, double rate, int pointCount` | y = scale · e^(rate · x) |
| `GetSquareWaveYData` | `double amplitude, int pointCount, int freq = 5` | Square wave alternating ±amplitude |
| `GetFourierYData` | `double amplitude, double phaseShift, int pointCount = 5000, int harmonics = 15` | Fourier series (sum of odd harmonics); converges to a square wave as `harmonics` increases |

### Usage Example

```csharp
using ChartAppLib.Helpers;
using ChartAppLib.Models;
using System.Windows.Media;

// Generate data (safe to call on a background thread)
const int n = 500;
var xValues = ChartDataGenerator.GetDateTimeXData(DateTime.Today, TimeSpan.FromHours(1), n).ToList();
var sine    = ChartDataGenerator.GetNoisySinewaveYData(10, 0, n, freq: 5, noiseAmplitude: 0.5, seed: 1).ToList();
var trend   = ChartDataGenerator.GetStraightLineYData(0.02, 5, n).ToList();
var walk    = ChartDataGenerator.GetRandomWalkData(n, startValue: 0, stepSize: 0.5, seed: 42).ToList();

MyChart.Series = new ObservableCollection<DataSeries>
{
    new DataSeries { Name = "Sine Wave",    Stroke = Brushes.Red,   Thickness = 1.5, XValues = xValues, YValues = sine,  XAxisId = "X1", YAxisId = "Y1" },
    new DataSeries { Name = "Trend",        Stroke = Brushes.Green, Thickness = 1.5, XValues = xValues, YValues = trend, XAxisId = "X1", YAxisId = "Y2" },
    new DataSeries { Name = "Random Walk",  Stroke = Brushes.Blue,  Thickness = 1.5, XValues = xValues, YValues = walk,  XAxisId = "X1", YAxisId = "Y3" }
};

MyChart.XAxes = new ObservableCollection<XAxisDefinition>
{
    new XAxisDefinition { Id = "X1", Label = "Time", ShowLabel = true,
                          AxisType = AxisType.DateTime, DateTimeFormat = "HH:mm",
                          Position = XAxisPosition.Bottom }
};
```

---

## Common Usage Patterns

### Pattern 1: Simple Line Chart

```csharp
MyChart.Series = new ObservableCollection<DataSeries>
{
    new DataSeries
    {
        Name = "Revenue",
        Stroke = Brushes.Green,
        Thickness = 2.0,
        XValues = new List<double> { 1, 2, 3, 4, 5 },
        YValues = new List<double> { 100, 150, 120, 200, 180 },
        XAxisId = "X1",
        YAxisId = "Y1"
    }
};

MyChart.XAxes = new ObservableCollection<XAxisDefinition>
{
    new XAxisDefinition { Id = "X1", Label = "Month", ShowLabel = true }
};

MyChart.YAxes = new ObservableCollection<YAxisDefinition>
{
    new YAxisDefinition { Id = "Y1", Label = "Amount ($)", ShowLabel = true }
};

MyChart.ChartType = ChartType.LinePlot;
```

### Pattern 2: Multi-Axis Chart

```csharp
MyChart.YAxes = new ObservableCollection<YAxisDefinition>
{
    new YAxisDefinition
    {
        Id = "YLeft", Label = "Temperature (°C)",
        Position = YAxisPosition.Left, LabelBrush = Brushes.Red
    },
    new YAxisDefinition
    {
        Id = "YRight", Label = "Humidity (%)",
        Position = YAxisPosition.Right, LabelBrush = Brushes.Blue
    }
};

MyChart.Series = new ObservableCollection<DataSeries>
{
    new DataSeries { Name = "Temp",     YAxisId = "YLeft",  /* ... */ },
    new DataSeries { Name = "Humidity", YAxisId = "YRight", /* ... */ }
};
```

### Pattern 3: Box Plot

```csharp
MyChart.BoxPlotSeries = new ObservableCollection<BoxPlotData>
{
    new BoxPlotData { Category = "Group A", Values = new List<double> { /* ... */ } },
    new BoxPlotData { Category = "Group B", Values = new List<double> { /* ... */ } }
};

MyChart.YAxes = new ObservableCollection<YAxisDefinition> { /* Y-axis def */ };
MyChart.XAxes = new ObservableCollection<XAxisDefinition> { /* X-axis def */ };
MyChart.ChartType = ChartType.BoxPlot;
```

### Pattern 4: Add Annotations

```csharp
MyChart.Annotations = new ObservableCollection<Annotation>
{
    new LineAnnotation
    {
        Label = "Target",
        Value = 100,
        Orientation = AnnotationOrientation.Horizontal,
        Stroke = Brushes.Red,
        StrokeThickness = 2,
        YAxisId = "Y1",
        IsDraggable = true
    },
    new BoxAnnotation
    {
        Label = "Optimal Zone",
        X1 = 2, X2 = 4,
        Y1 = 80, Y2 = 120,
        Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)),
        Stroke = Brushes.Green,
        XAxisId = "X1",
        YAxisId = "Y1"
    }
};
```

### Pattern 5: Viewport Manager

```csharp
// Assign a custom or default viewport manager
MyChart.ViewportManager = new DefaultChartViewportManager();

// Listen to viewport changes (e.g., to sync a second chart)
MyChart.ViewportChanged += (s, e) =>
{
    var vm = MyChart.ViewportManager;
    OtherChart.ViewportManager.RestoreState(
        vm.ScaleX, vm.ScaleY, vm.CenterX, vm.CenterY,
        vm.PanX, vm.PanY, vm.XAxisOffset, vm.YAxisOffset);
};
```

### Pattern 6: Highlight Series via Code

```csharp
MyChart.HighlightSeries("Temperature");   // by name
MyChart.HighlightSeries(0);               // by index
MyChart.ClearHighlight();                 // restore all
```

### Pattern 7: Refresh Live Data

```csharp
// Append data and redraw
MyChart.Series[0].YValues.Add(newValue);
MyChart.Series[0].XValues?.Add(newX);
MyChart.RefreshChart();
```

### Pattern 8: Export Chart

```csharp
MyChart.CopyChartImageToClipboard();          // image to clipboard
MyChart.CopyXYValuesToClipboard();            // CSV to clipboard
var bmp = MyChart.RenderChartToBitmap();      // RenderTargetBitmap
```

### Pattern 9: Custom Tracker Tooltip

Define a `DataTemplate` whose `DataContext` is bound to a `TrackerData` instance.
The template receives live updates via `INotifyPropertyChanged` as the mouse moves.

```xaml
<controls:ChartControl x:Name="MyChart" ...>
    <controls:ChartControl.TrackerTooltipTemplate>
        <DataTemplate DataType="{x:Type models:TrackerData}">
            <Border Background="#CC1E1E1E" CornerRadius="4" Padding="8,5">
                <StackPanel>
                    <TextBlock Text="{Binding XText}" Foreground="White" FontWeight="Bold"/>
                    <ItemsControl ItemsSource="{Binding Items}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate DataType="{x:Type models:TrackerSeriesItem}">
                                <StackPanel Orientation="Horizontal" Margin="0,1">
                                    <Rectangle Width="10" Height="10" Margin="0,0,5,0"
                                               Fill="{Binding Stroke}" />
                                    <TextBlock Text="{Binding FormattedValue}" Foreground="White"/>
                                </StackPanel>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </Border>
        </DataTemplate>
    </controls:ChartControl.TrackerTooltipTemplate>
</controls:ChartControl>
```

---

## Advanced: Custom Viewport Manager

Extend `DefaultChartViewportManager` (or `ChartViewportManager`) to customize zoom/pan behavior.
Override only the interactions you need; call `RaiseViewportChanged()` after mutating state.

```csharp
public class MyCustomViewportManager : DefaultChartViewportManager
{
    // Restrict wheel zoom to X-axis only
    public override void OnMouseWheel(Point position, int delta)
    {
        base.OnMouseWheel(position, delta);
        ScaleY = 1.0;   // lock Y scale
        RaiseViewportChanged();
    }

    // Disable rectangle zoom
    public override void OnZoomRect(Rect selectionRect, Size canvasSize) { }
}
```

Available override points in `ChartViewportManager`:

| Method | Trigger |
|--------|---------|
| `OnMouseWheel(Point, int)` | Mouse wheel scroll |
| `OnPanDelta(Vector)` | Middle-button or Ctrl+drag pan |
| `OnZoomRect(Rect, Size)` | Rubber-band rectangle zoom |
| `OnXAxisDrag(double, int, double, double)` | X-axis label drag |
| `OnYAxisDrag(double, double, double, double)` | Y-axis label drag |
| `OnManipulationDelta(Point, double, double, Vector)` | Trackpad pinch/pan |
| `ShiftXOffset(double)` | Arrow key / `ShiftXAxis` |
| `ShiftYOffset(double)` | Arrow key / `ShiftYAxis` |
| `Reset()` | Reset view command |
| `ZoomExtents(double, double, double, double, double, double, double)` | Fit to data |
| `RestoreState(...)` | Undo/redo state restore |

---

## Theming

ChartAppLib includes built-in light and dark themes switched via `ChartToolbar` or programmatically.

```csharp
// Read theme-aware brushes
var axisColor = (Brush)chart.FindResource("ChartAxisBrush");
var gridColor = (Brush)chart.FindResource("ChartGridBrush");
```

Theme files:
- `ChartAppLib/Themes/Theme.Light.xaml`
- `ChartAppLib/Themes/Theme.Dark.xaml`

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Mouse Wheel` | Zoom in/out at cursor |
| `Left-Drag` | Rectangle zoom |
| `Ctrl+Drag` | Pan |
| `Double-Click` | Reset view (if `DoubleClickResetEnabled`) |
| `←` / `→` | Shift X-axis (hold `Shift` for fine, `Ctrl` for coarse) |
| `↑` / `↓` | Shift Y-axis (hold `Shift` for fine, `Ctrl` for coarse) |
| `Home` | Reset axis offsets |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |

---

## Troubleshooting

**Chart not rendering?**
- Ensure `Series` is not null and contains at least one point
- Verify `XAxes` and `YAxes` are populated
- Check series `XAxisId`/`YAxisId` match axis `Id` values exactly

**Grid lines not showing?**
- Ensure `ShowMajorGridLines` / `ShowMinorGridLines` on `ChartControl` are `true`
- Verify `YAxisDefinition.ShowGridLines` / `XAxisDefinition.ShowGridLines` are `true`

**Tracker tooltip empty?**
- Ensure `ShowTrackerLine` is `true`
- For a custom template, verify the `DataTemplate` `DataType` is `TrackerData`

**Box plot not showing?**
- Set `ChartType = ChartType.BoxPlot` and populate `BoxPlotSeries`
- `YAxes` and `XAxes` must also be provided

**Undo/redo buttons always disabled?**
- Bind `ChartToolbar.TargetChart` — the toolbar wires `UndoCommand`/`RedoCommand` automatically

---

## Performance Tips

1. **Large datasets (>100 K points)**: Use `Thickness = 0.5` or lower
2. **Live updates**: Batch-append data, then call `RefreshChart()` once — never per-point
3. **Many series**: Assign series to dedicated axes to reduce per-draw range scans
4. **3D surface**: Keep grid size ≤ 100×100 for smooth rendering
5. **Axis drag**: The control uses a throttled lightweight redraw during drag; avoid rebuilding `Series` during drag

---

## License & Support

ChartAppLib is provided as-is. For issues, enhancements, or questions, consult the source code documentation.

