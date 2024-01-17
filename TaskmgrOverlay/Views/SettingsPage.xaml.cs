using System.Windows.Controls;

using TaskmgrOverlay.ViewModels;

namespace TaskmgrOverlay.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
