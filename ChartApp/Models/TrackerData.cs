using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace ChartApp.Models
{
    public class TrackerData : INotifyPropertyChanged
    {
        private string _xText = string.Empty;

        /// <summary>Formatted X-axis label and value (e.g., "Time: 3.14").</summary>
        public string XText
        {
            get => _xText;
            set
            {
                if (_xText != value)
                {
                    _xText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Collection of tracker items, one per visible series.</summary>
        public ObservableCollection<TrackerSeriesItem> Items { get; } = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class TrackerSeriesItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private double _value;
        private string _formattedValue = string.Empty;
        private Brush _stroke = Brushes.Transparent;

        /// <summary>Series name.</summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Raw interpolated Y value.</summary>
        public double Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Pre-formatted display string (e.g., "Temperature: 23.45").</summary>
        public string FormattedValue
        {
            get => _formattedValue;
            set
            {
                if (_formattedValue != value)
                {
                    _formattedValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Series stroke brush — use as the colour swatch in tooltip templates.</summary>
        public Brush Stroke
        {
            get => _stroke;
            set
            {
                if (_stroke != value)
                {
                    _stroke = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
