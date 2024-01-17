using MahApps.Metro.Controls;
using System.Windows.Controls;
using TaskmgrOverlay.Contracts.Views;
using TaskmgrOverlay.ViewModels;

namespace TaskmgrOverlay.Views;

public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public Frame GetNavigationFrame()
        => shellFrame;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();
}
