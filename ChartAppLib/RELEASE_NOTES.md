# ChartApp Release Notes

## Version 1.0.1 (2026-05-19)

### 🐞 Bug Fixes

- Fixed an issue where **3D charts appeared empty** when switching from a **2D chart to a 3D chart**.
- Improved chart rendering lifecycle to ensure **3D visualizations load correctly after chart type transitions**.

### 🔧 Improvements

- Enhanced chart type switching stability between **2D and 3D chart modes**.

---

## Version 1.0.0 (2026-05-07)

### ✨ Features

**Initial Public Release**

#### Chart Types
- **Line Plot** - Classic line charts with multiple series support
- **Scatter Plot** - Point-based data visualization with customizable markers
- **Bubble Plot** - Two-dimensional scatter with bubble size as third dimension
- **Box Plot** - Statistical visualization showing quartiles, median, and outliers
- **Histogram** - Frequency distribution with customizable bin ranges
- **3D Charts** - Three-dimensional line and surface plots for complex data

#### Multi-Axis Support
- Independent X and Y axis configurations
- Support for multiple Y-axes (left and right positioning)
- Zoom and pan independently per axis
- Automatic margin calculation based on axis labels

#### Interactive Features
- **Live Data Streaming** - Real-time data updates with smooth animations
- **Zoom & Pan** - Intuitive mouse-based zooming and panning
- **Interactive Tracker** - Hover to see exact data point values
- **Tooltips** - Context-aware information popups
- **Annotations** - Add text, arrows, and shapes to highlight data

#### Visualization
- **Grid Lines** - Major and minor grid line support
- **Themes** - Light and Dark theme support
- **Brushes** - Full customization of colors and styles
- **Transparency** - Alpha blending support for overlapping elements

#### Performance
- **Efficient Rendering** - Optimized canvas rendering with caching
- **Large Datasets** - Handles thousands of data points smoothly
- **Memory Management** - Proper resource cleanup and disposal

### 🎯 Target Framework
- **.NET 10.0 Windows** - Full compatibility with latest .NET framework

### 📚 Documentation
- Comprehensive API documentation
- XML comments on all public members
- Sample application with common usage patterns
- Integration guide for WPF applications

### 🔧 Development Features
- Full source code availability
- Symbol packages (.snupkg) for debugging
- MIT License for open-source projects

---

**Copyright © 2026 ChartApp Contributors. All rights reserved.**