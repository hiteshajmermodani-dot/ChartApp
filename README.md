# ChartApp - WPF Chart Control Library

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![NuGet](https://img.shields.io/badge/NuGet-ChartApp.WPF-blue)

A powerful, feature-rich **WPF charting library** for .NET 9 and above developers. Create stunning interactive charts with smooth animations, real-time data streaming, and advanced visualization capabilities.

<img width="1917" height="992" alt="Screenshot 2026-05-04 231746" src="https://github.com/user-attachments/assets/8586cc2c-56eb-4fad-9247-03b0104545fe" />


## 🎯 Features Overview

### 📊 7 Chart Types
- **Line Plot** - Connect data points with customizable line styles
- **Scatter Plot** - Visualize data distributions and correlations
- **Bubble Plot** - Show 3-variable relationships with bubble sizing
- **Box Plot** - Display statistical distributions with quartiles
- **Histogram** - Analyze frequency distributions
- **3D Line Chart** - Parametric 3D curves with rotation
- **3D Surface Plot** - Complex mathematical surfaces

### 🎮 Interactive Controls
- **Zoom & Pan** - Click-drag to zoom up to 50x magnification
- **Tracker** - Vertical line follows cursor with live value tooltips
- **Live Updates** - Stream real-time data with auto-scaling
- **Annotations** - Add lines, rectangles, and text overlays
- **Undo/Redo** - Navigate zoom history seamlessly

### 📈 Multi-Axis Support
- Independent Y-axis definitions (left/right)
- Multiple X-axis options (top/bottom)
- Per-series axis assignment
- Automatic range scaling

### ⚡ Performance Optimized
- **100K+ points** with smooth rendering
- DrawingImage-based GPU acceleration
- Adaptive throttling for large datasets
- Intelligent caching and culling

## 🚀 Quick Start

### Installation

```bash
# NuGet Package Manager
Install-Package ChartAppLib

# .NET CLI
dotnet add package ChartAppLib

# Package Manager Console
Install-Package ChartAppLib
```

### Basic Usage

**XAML:**
```xaml
<Window xmlns:chart="clr-namespace:ChartApp.Controls;assembly=ChartApp">
    <chart:ChartControl 
        ChartType="LinePlot"
        Series="{Binding ChartSeries}"
        YAxes="{Binding YAxes}"
        XAxes="{Binding XAxes}"
        ShowTrackerLine="True" />
</Window>
```

**C# ViewModel:**
```csharp
using ChartApp.Models;
using ChartApp.Helpers;

public class ChartViewModel
{
    public ObservableCollection<DataSeries> ChartSeries { get; set; }
    public ObservableCollection<YAxisDefinition> YAxes { get; set; }
    public ObservableCollection<XAxisDefinition> XAxes { get; set; }

    public void LoadData()
    {
        // Generate sample data
        var x = ChartDataGenerator.GetUniformXData(100, 0, 1.0).ToList();
        var y = ChartDataGenerator.GetSinewaveYData(10, 0, 100, 5).ToList();

        ChartSeries = new()
        {
            new DataSeries
            {
                Name = "Sine Wave",
                XValues = x,
                YValues = y,
                Stroke = Brushes.Blue,
                Thickness = 1.5,
                YAxisId = "Y1",
                XAxisId = "X1"
            }
        };

        YAxes = new()
        {
            new YAxisDefinition
            {
                Id = "Y1",
                Label = "Value",
                Position = YAxisPosition.Left,
                ShowGridLines = true
            }
        };

        XAxes = new()
        {
            new XAxisDefinition
            {
                Id = "X1",
                Label = "Time",
                Position = XAxisPosition.Bottom
            }
        };
    }
}
```

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| [NUGET_README.md](ChartApp/NUGET_README.md) | NuGet package documentation |
| [API_DOCUMENTATION.md](ChartApp/API_DOCUMENTATION.md) | Detailed API reference |
| [LICENSE](LICENSE) | MIT License (free for all developers) |

## 🔧 Sample Application

The **SampleApplication** project demonstrates all features:

```bash
# Navigate to sample app
cd SampleApplication

# Run the demo
dotnet run
```

**Includes examples of:**
- Line charts with 5+ series
- Live data streaming
- Scatter/Bubble plots
- 3D visualization
- Real-time updates
- Theme switching

## 💻 System Requirements

- **.NET 10.0** or later
- **Windows 10** or later (WPF platform)
- **Visual Studio 2022** recommended
- **4GB RAM** minimum (8GB+ for large datasets)

## 📖 Core Classes

### ChartControl
Main WPF control for rendering charts.

```csharp
// Key Properties
ChartType                // LinePlot, ScatterPlot, etc.
Series                   // ObservableCollection<DataSeries>
YAxes                    // Multi-axis definitions
XAxes                    // Multi-axis definitions
ShowTrackerLine          // Enable interactive tracker
ShowLegend               // Toggle legend visibility
Annotations              // Visual overlays
ZoomTransform            // Current zoom/pan state

// Key Commands
ResetViewCommand         // Reset to default zoom
UndoCommand             // Undo zoom state
RedoCommand             // Redo zoom state
ToggleLockCommand       // Lock/unlock interaction
```

### DataSeries
Represents a single data series.

```csharp
public class DataSeries
{
    public string Name { get; set; }
    public List<double> XValues { get; set; }
    public List<double> YValues { get; set; }
    public List<double> ZValues { get; set; }  // Bubble size
    public Brush Stroke { get; set; }
    public double Thickness { get; set; }
    public string YAxisId { get; set; }
    public string XAxisId { get; set; }
}
```

### ChartDataGenerator
12 built-in data generation helpers.

```csharp
// Utility methods
ChartDataGenerator.GetSinewaveYData(amplitude, phase, count, frequency);
ChartDataGenerator.GetRandomWalkData(count, startValue, stepSize);
ChartDataGenerator.GetGaussianData(amplitude, count);
ChartDataGenerator.GetFourierYData(amplitude, phase, count, harmonics);
// ... and 8 more helpers
```

## 🎨 Themes

Built-in Light and Dark themes:

```csharp
// Switch theme at runtime
var darkTheme = new ResourceDictionary 
{ 
    Source = new Uri("pack://application:,,,/ChartApp;component/Themes/Theme.Dark.xaml") 
};
Application.Current.Resources.MergedDictionaries.Clear();
Application.Current.Resources.MergedDictionaries.Add(darkTheme);
```

## 🔌 Advanced Usage

### Live Data Streaming

```csharp
private void StartLiveUpdates()
{
    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
    timer.Tick += (s, e) =>
    {
        ChartSeries[0].YValues.Add(GetNextValue());
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
        X1 = 10, Y1 = 100, X2 = 100, Y2 = 100,
        StrokeColor = Colors.Red,
        StrokeThickness = 2
    }
};
```

### Multi-Axis Charts

```csharp
YAxes = new()
{
    new YAxisDefinition { Id = "Y1", Label = "Temperature (°C)" },
    new YAxisDefinition { Id = "Y2", Label = "Humidity (%)" }
};

ChartSeries = new()
{
    new DataSeries { Name = "Temp", YAxisId = "Y1", ... },
    new DataSeries { Name = "Humidity", YAxisId = "Y2", ... }
};
```

## ⚙️ Building the NuGet Package

```powershell
# Method 1: dotnet CLI
dotnet pack ChartAppLib/ChartAppLib.csproj -c Release -o ./nupkg --include-symbols

# Method 2: Visual Studio
# Right-click ChartAppLib project → Pack
```

## 📊 Performance Benchmarks

| Chart Type | Data Points | Performance |
|-----------|-------------|-------------|
| Line Plot | 50,000 | 60 FPS |
| Scatter Plot | 10,000 | 60 FPS |
| Bubble Plot | 5,000 | 60 FPS |
| 3D Surface | 60×60 grid | 30 FPS |
| 3D Line | 500 points | 60 FPS |

*Benchmarks on Intel i7, 16GB RAM, NVIDIA GTX 1080*

## 🐛 Troubleshooting

### Chart not rendering?
- Ensure `UseWPF=true` in project file
- Check that data collections are not null
- Verify axis IDs match series assignments

### Performance issues?
- Reduce point count or use aggregation
- Enable adaptive throttling for tracker
- Use DrawingImage for 10K+ points

### Zoom/Pan not working?
- Verify `ShowTrackerLine` is not conflicting
- Check if interaction is locked (`IsLocked` property)
- Ensure chart has focus and correct event handlers

## 🤝 Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing`)
3. Commit changes (`git commit -m 'Add feature'`)
4. Push branch (`git push origin feature/amazing`)
5. Open a Pull Request

**Code Standards:**
- Follow C# naming conventions
- Add XML documentation comments
- Include unit tests for new features
- Ensure 80% code coverage minimum

## 📝 License

This project is licensed under the **MIT License** - completely **free for all developers**.

See [LICENSE](LICENSE) for details.

**You can use ChartApp for:**
- ✅ Commercial projects
- ✅ Open source projects
- ✅ Educational purposes
- ✅ Personal projects
- ✅ Modifications and derivative works

## 🌟 Support & Feedback

- **Bug Reports**: [GitHub Issues](https://github.com/hiteshajmermodani-dot/ChartApp/issues)
- **Feature Requests**: [GitHub Discussions](https://github.com/hiteshajmermodani-dot/ChartApp/discussions)
- **Documentation**: See [ChartApp/API_DOCUMENTATION.md](ChartApp/API_DOCUMENTATION.md)
- **Examples**: Check [SampleApplication](SampleApplication)

## 📦 NuGet Package

Published on **[NuGet.org](https://www.nuget.org/packages/ChartAppLib/)**

```bash
Install-Package ChartAppLib
```

## 🎓 Learning Resources

- **Quick Start**: [NUGET_README.md](ChartApp/NUGET_README.md)
- **API Reference**: [API_DOCUMENTATION.md](ChartApp/API_DOCUMENTATION.md)
- **Examples**: Run [SampleApplication](SampleApplication)
- **Source Code**: All classes include XML documentation

## 🗂️ Project Structure

```
ChartAppLib/
├── Controls/              # Chart control components
├── Models/                # Data models (DataSeries, Annotations)
├── Helpers/               # ChartDataGenerator utility class
├── ViewportManagers/      # Zoom/pan state management
├── Themes/                # Light and Dark XAML themes
├── API_DOCUMENTATION.md   # Detailed API reference
└── NUGET_README.md       # NuGet package documentation

SampleApplication/        # Working examples of all features
├── ViewModels/
├── MainWindow.xaml
└── MainWindow.xaml.cs
```

## 🎯 Roadmap

**Version 1.1.0** (Q3 2026)
- [ ] Stacked bar charts
- [ ] Pie/Donut charts
- [ ] OHLC/Candlestick
- [ ] Heatmaps
- [ ] Data export (CSV/Excel)

## 💡 Tips & Best Practices

1. **Use MVVM Pattern** - Bind data via properties
2. **Throttle Updates** - Append data in batches for live streams
3. **Lazy Load Data** - Load large datasets asynchronously
4. **Test Performance** - Profile with your data sizes
5. **Cache Axis Ranges** - Avoid repeated calculations

## 📞 Contact & Community

- **GitHub**: [hiteshajmermodani-dot/ChartApp](https://github.com/hiteshajmermodani-dot/ChartApp)
- **Email**: hitesh.ajmer.modani@gmail.com

---

**Made with ❤️ for developers**

**ChartApp** - Fast, free, and feature-rich charting for .NET

![ChartApp Logo](https://img.shields.io/badge/ChartApp-WPF-blue?style=flat-square&logo=dotnet)
