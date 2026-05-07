using System.Windows;

namespace PrintMeter.App;

public partial class MainWindow : Window
{
    public MainWindow(ViewModels.MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
