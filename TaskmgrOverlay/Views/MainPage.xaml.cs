using System.Windows.Controls;

using TaskmgrOverlay.ViewModels;

namespace TaskmgrOverlay.Views;

public partial class MainPage : Page
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
