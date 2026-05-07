using System.Windows;
using System.Windows.Controls;

namespace ChartApp.Controls
{
    /// <summary>Toolbar control providing toggle buttons and selectors for chart features.</summary>
    public partial class ChartToolbar : UserControl
    {
        public static readonly DependencyProperty ShowTrackerButtonProperty =
            DependencyProperty.Register(nameof(ShowTrackerButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowLegendButtonProperty =
            DependencyProperty.Register(nameof(ShowLegendButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowLabelsButtonProperty =
            DependencyProperty.Register(nameof(ShowLabelsButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowMajorGridButtonProperty =
            DependencyProperty.Register(nameof(ShowMajorGridButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowMinorGridButtonProperty =
            DependencyProperty.Register(nameof(ShowMinorGridButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowResetButtonProperty =
            DependencyProperty.Register(nameof(ShowResetButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowUndoRedoButtonsProperty =
            DependencyProperty.Register(nameof(ShowUndoRedoButtons), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowLockButtonProperty =
            DependencyProperty.Register(nameof(ShowLockButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowDoubleClickResetButtonProperty =
            DependencyProperty.Register(nameof(ShowDoubleClickResetButton), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowChartTypeSelectorProperty =
            DependencyProperty.Register(nameof(ShowChartTypeSelector), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty ShowThemeSelectorProperty =
            DependencyProperty.Register(nameof(ShowThemeSelector), typeof(bool), typeof(ChartToolbar),
                                        new PropertyMetadata(true));

        public static readonly DependencyProperty TargetChartProperty =
            DependencyProperty.Register(nameof(TargetChart),
                                        typeof(ChartControl),
                                        typeof(ChartToolbar),
                                        new PropertyMetadata(null, OnTargetChartChanged));

        public ChartToolbar()
        {
            InitializeComponent();
        }

        /// <summary>Gets or sets the chart control this toolbar is bound to.</summary>
        public ChartControl? TargetChart
        {
            get => (ChartControl?)GetValue(TargetChartProperty);
            set => SetValue(TargetChartProperty, value);
        }

        private static void OnTargetChartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartToolbar toolbar)
            {
                if (e.OldValue is ChartControl oldChart)
                {
                    oldChart.ToolbarStateChanged -= toolbar.OnChartToolbarStateChanged;

                    // Detach commands so buttons revert to always-enabled
                    toolbar.PART_Undo.Command = null;
                    toolbar.PART_Redo.Command = null;
                }

                if (e.NewValue is ChartControl newChart)
                {
                    newChart.ToolbarStateChanged += toolbar.OnChartToolbarStateChanged;

                    // Bind Command so WPF evaluates CanExecute and disables the
                    // buttons automatically when the undo/redo stacks are empty.
                    toolbar.PART_Undo.Command = newChart.UndoCommand;
                    toolbar.PART_Redo.Command = newChart.RedoCommand;

                    toolbar.SyncFromChart(newChart);
                }
            }
        }

        private void SyncFromChart(ChartControl chart)
        {
            PART_ToggleTracker.IsChecked = chart.ShowTrackerLine;
            PART_ToggleLegend.IsChecked = chart.ShowLegend;
            PART_ToggleMajorGrid.IsChecked = chart.ShowMajorGridLines;
            PART_ToggleMinorGrid.IsChecked = chart.ShowMinorGridLines;
            PART_ToggleLock.IsChecked = chart.IsLocked;
            PART_ToggleLock.Content = chart.IsLocked ? "🔒 Locked" : "🔓 Unlocked";
            PART_ToggleDoubleClickReset.IsChecked = chart.DoubleClickResetEnabled;

            // Map ChartType enum to ComboBox index
            var chartTypeIndex = chart.ChartType switch
                                 {
                                     Models.ChartType.LinePlot => 0,
                                     Models.ChartType.ScatterPlot => 1,
                                     Models.ChartType.BubblePlot => 2,
                                     Models.ChartType.BoxPlot => 3,
                                     Models.ChartType.Histogram => 4,
                                     _ => 0 // Default to LinePlot
                                 };

            PART_ChartType.SelectedIndex = chartTypeIndex;
            PART_ToggleLabels.IsChecked = chart is { ShowXAxisLabel: true, ShowYAxisLabel: true };
        }

        private void OnChartToolbarStateChanged(object? sender, EventArgs e)
        {
            if (TargetChart != null)
            {
                SyncFromChart(TargetChart);
            }
        }

        private void ToggleTracker_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.ShowTrackerLine = PART_ToggleTracker.IsChecked == true;
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ToggleLegend_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.ShowLegend = PART_ToggleLegend.IsChecked == true;
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ToggleMajorGrid_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.ShowMajorGridLines = PART_ToggleMajorGrid.IsChecked == true;
                TargetChart.RefreshChart();
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ToggleMinorGrid_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.ShowMinorGridLines = PART_ToggleMinorGrid.IsChecked == true;
                TargetChart.RefreshChart();
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ResetView_Click(object sender, RoutedEventArgs e)
        {
            TargetChart?.ResetViewCommand.Execute(null);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            TargetChart?.UndoCommand.Execute(null);
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            TargetChart?.RedoCommand.Execute(null);
        }

        private void ToggleLock_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.IsLocked = PART_ToggleLock.IsChecked == true;
                PART_ToggleLock.Content = TargetChart.IsLocked ? "🔒 Locked" : "🔓 Unlocked";
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ToggleDoubleClickReset_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                TargetChart.DoubleClickResetEnabled = PART_ToggleDoubleClickReset.IsChecked == true;
                TargetChart.NotifyToolbarStateChanged();
            }
        }

        private void ChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetChart != null && PART_ChartType.SelectedIndex >= 0)
            {
                // Explicit mapping from ComboBox index to ChartType enum
                var chartTypeMap = new[]
                                   {
                                       Models.ChartType.LinePlot,        // Index 0
                                       Models.ChartType.ScatterPlot,     // Index 1
                                       Models.ChartType.BubblePlot,      // Index 2
                                       Models.ChartType.BoxPlot,         // Index 3
                                       Models.ChartType.Histogram        // Index 4
                                   };

                if (PART_ChartType.SelectedIndex < chartTypeMap.Length)
                {
                    TargetChart.ChartType = chartTypeMap[PART_ChartType.SelectedIndex];
                    TargetChart.NotifyToolbarStateChanged();
                }
            }
        }

        private void ToggleLabels_Click(object sender, RoutedEventArgs e)
        {
            if (TargetChart != null)
            {
                var show = PART_ToggleLabels.IsChecked == true;
                TargetChart.ShowXAxisLabel = show;
                TargetChart.ShowYAxisLabel = show;
            }
        }

        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PART_ThemeSelector.SelectedIndex == 1)
            {
                // Dark
                ApplyTheme("Themes/Theme.Dark.xaml");
            }
            else
            {
                // Light
                ApplyTheme("Themes/Theme.Light.xaml");
            }

            TargetChart?.RefreshChart(); // Force redraw to update legend and axis colors
        }

        private void ApplyTheme(string themePath)
        {
            var dict = new ResourceDictionary
                       { Source = new Uri($"pack://application:,,,/ChartApp;component/{themePath}", UriKind.Absolute) };

            // Remove any previous theme dictionaries
            for (var i = Application.Current.Resources.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var md = Application.Current.Resources.MergedDictionaries[i];

                if (md.Source != null && (md.Source.OriginalString.Contains("Theme.Light.xaml") ||
                                          md.Source.OriginalString.Contains("Theme.Dark.xaml")))
                {
                    Application.Current.Resources.MergedDictionaries.RemoveAt(i);
                }
            }

            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        #region Visibility DependencyProperties

        /// <summary>Gets or sets whether the tracker toggle button is visible.</summary>
        public bool ShowTrackerButton
        {
            get => (bool)GetValue(ShowTrackerButtonProperty);
            set => SetValue(ShowTrackerButtonProperty, value);
        }

        /// <summary>Gets or sets whether the legend toggle button is visible.</summary>
        public bool ShowLegendButton
        {
            get => (bool)GetValue(ShowLegendButtonProperty);
            set => SetValue(ShowLegendButtonProperty, value);
        }

        /// <summary>Gets or sets whether the axis labels toggle button is visible.</summary>
        public bool ShowLabelsButton
        {
            get => (bool)GetValue(ShowLabelsButtonProperty);
            set => SetValue(ShowLabelsButtonProperty, value);
        }

        /// <summary>Gets or sets whether the major grid toggle button is visible.</summary>
        public bool ShowMajorGridButton
        {
            get => (bool)GetValue(ShowMajorGridButtonProperty);
            set => SetValue(ShowMajorGridButtonProperty, value);
        }

        /// <summary>Gets or sets whether the minor grid toggle button is visible.</summary>
        public bool ShowMinorGridButton
        {
            get => (bool)GetValue(ShowMinorGridButtonProperty);
            set => SetValue(ShowMinorGridButtonProperty, value);
        }

        /// <summary>Gets or sets whether the reset view button is visible.</summary>
        public bool ShowResetButton
        {
            get => (bool)GetValue(ShowResetButtonProperty);
            set => SetValue(ShowResetButtonProperty, value);
        }

        /// <summary>Gets or sets whether the undo/redo buttons are visible.</summary>
        public bool ShowUndoRedoButtons
        {
            get => (bool)GetValue(ShowUndoRedoButtonsProperty);
            set => SetValue(ShowUndoRedoButtonsProperty, value);
        }

        /// <summary>Gets or sets whether the lock toggle button is visible.</summary>
        public bool ShowLockButton
        {
            get => (bool)GetValue(ShowLockButtonProperty);
            set => SetValue(ShowLockButtonProperty, value);
        }

        /// <summary>Gets or sets whether the double-click reset toggle button is visible.</summary>
        public bool ShowDoubleClickResetButton
        {
            get => (bool)GetValue(ShowDoubleClickResetButtonProperty);
            set => SetValue(ShowDoubleClickResetButtonProperty, value);
        }

        /// <summary>Gets or sets whether the chart type selector combo box is visible.</summary>
        public bool ShowChartTypeSelector
        {
            get => (bool)GetValue(ShowChartTypeSelectorProperty);
            set => SetValue(ShowChartTypeSelectorProperty, value);
        }

        /// <summary>Gets or sets whether the theme selector combo box is visible.</summary>
        public bool ShowThemeSelector
        {
            get => (bool)GetValue(ShowThemeSelectorProperty);
            set => SetValue(ShowThemeSelectorProperty, value);
        }

        #endregion
    }
}