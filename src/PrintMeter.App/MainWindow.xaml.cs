using Microsoft.UI.Xaml;
using PrintMeter.App.ViewModels;

namespace PrintMeter.App;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        RootGrid.DataContext = viewModel;
    }
}
