# Changelog

All notable changes to the ChartApp WPF Chart Control Library will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-05-06

### Added
- **Chart Types**: Full support for 7 visualization types
  - Line Plot with multi-series support
  - Scatter Plot with interactive tracking
  - Bubble Plot with size-based visualization
  - Box Plot for statistical distributions
  - Histogram with customizable bins
  - 3D Line Chart for parametric curves
  - 3D Surface Plot for mathematical surfaces

- **Interactive Features**
  - Zoom and Pan (up to 50x magnification)
  - Interactive vertical tracker line with tooltips
  - Marker highlighting for data points
  - Drag-based X/Y axis adjustment
  - Undo/Redo for zoom states
  - Double-click reset view

- **Multi-Axis Support**
  - Multiple independent Y-axes (left/right positioned)
  - Multiple X-axes (top/bottom positioned)
  - Per-series axis assignment
  - Automatic axis scaling

- **Annotations**
  - Line annotations with custom styling
  - Rectangle annotations
  - Text annotations with positioning
  - Custom colors and transparency support

- **Live Data Streaming**
  - Real-time data point appending
  - Automatic axis range scaling
  - Adaptive update throttling based on data size
  - High-performance for continuous updates

- **Performance Optimizations**
  - DrawingGroup/DrawingImage rendering for 100K+ points
  - Throttled tracker updates with adaptive intervals
  - Axis range caching to avoid repeated calculations
  - Off-screen geometry culling
  - Per-axis radius compensation for scatter/bubble zoom

- **Developer Experience**
  - MVVM-friendly bindable properties
  - ChartDataGenerator utility class with 12 helper methods
  - Comprehensive XML documentation
  - Themes: Light and Dark XAML ResourceDictionary
  - Observable collection binding support

- **Data Generation Helpers**
  - `GetRandomDoubleData()` - Random values
  - `GetStraightLineYData()` - Linear trends
  - `GetSinewaveYData()` - Sine waves
  - `GetNoisySinewaveYData()` - Sine with noise
  - `GetCosineWaveYData()` - Cosine waves
  - `GetRandomWalkData()` - Random walk simulation
  - `GetGaussianData()` - Gaussian distribution
  - `GetExponentialData()` - Exponential curves
  - `GetUniformXData()` - Uniform spacing
  - `GetDateTimeXData()` - DateTime-based axes
  - `GetSquareWaveYData()` - Square waves
  - `GetFourierYData()` - Fourier series composition

- **3D Chart Features**
  - Single-series 3D line charts
  - Full mouse interaction for 3D rotation/zoom
  - Surface plot rendering with gradient colors
  - 3D axis labels perpendicular to edges
  - Proper coordinate space transformations

### Fixed
- Scatter/Bubble marker positioning precision
- Tracker dot alignment with series points
- DrawingImage bounds offset for edge circles
- 3D tick label positioning using axis-perpendicular vectors
- Per-axis radius compensation for asymmetric zoom
- Tracker marker clipping for out-of-bounds values
- Tracker line rendering (solid instead of dashed)
- 3D mouse interaction working anywhere in plot area

### Technical Details
- **Target Framework**: .NET 10.0-windows
- **Platform**: WPF (Windows Presentation Foundation)
- **Language**: C# 14.0
- **UI Pattern**: MVVM-compatible
- **Rendering**: DrawingImage/DrawingGroup for performance
- **Data Binding**: INotifyPropertyChanged, ObservableCollection

### Performance Metrics
- **Line Plot**: Handles 50,000+ points smoothly
- **Scatter Plot**: 1,000+ points with real-time tracking
- **Bubble Plot**: 1,000+ points with size variation
- **3D Surface**: 60×60 grid with interactive rotation
- **Zoom Level**: Up to 50x magnification
- **Tracker Update Rate**: Adaptive 50-150ms based on data size

### Breaking Changes
None - Initial release

### Deprecated
None - Initial release

### Security
- No known security vulnerabilities
- MIT License provides full transparency

## Future Roadmap

### Planned for 1.1.0
- [ ] Stacked bar chart type
- [ ] Pie and Donut charts
- [ ] Candlestick/OHLC charts
- [ ] Heatmap visualization
- [ ] Custom legend positioning
- [ ] Data export to CSV/Excel
- [ ] Print preview and printing support

### Planned for 1.2.0
- [ ] Touch/pen input support
- [ ] Advanced filtering UI
- [ ] Data aggregation options
- [ ] Smooth scrolling animations
- [ ] Custom color schemes
- [ ] Right-to-left (RTL) support

### Planned for 2.0.0
- [ ] Cross-platform support (.NET MAUI)
- [ ] Web component version (Blazor)
- [ ] Advanced ML visualization tools
- [ ] Real-time data pipeline integration
- [ ] Major API redesign based on feedback

---

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Support

For questions, issues, or feature requests:
- **GitHub Issues**: https://github.com/hiteshajmermodani-dot/ChartApp/issues
- **Documentation**: See README.md and API_DOCUMENTATION.md
- **Examples**: Check SampleApplication for implementation patterns

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Authors

- **ChartApp Contributors** - Initial development and maintenance

## Acknowledgments

- Built with .NET 10 and WPF
- Inspired by modern charting libraries
- Thanks to all contributors and users
