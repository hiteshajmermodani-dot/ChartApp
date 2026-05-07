using System.Windows;
using SampleApplicationChartApp.ViewModels;

namespace SampleApplicationChartApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel
                     {
                         RequestRefresh = () => ChartControl.RefreshChart()
                     };

            DataContext = vm;

            // Ensure the chart can receive keyboard focus for axis movement
            ChartControl.Focus();
        }
    }
}