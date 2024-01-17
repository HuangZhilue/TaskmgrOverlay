using System.Windows.Controls;

namespace TaskmgrOverlay.Contracts.Views;

public interface IShellWindow
{
    Frame GetNavigationFrame();

    void ShowWindow();

    void CloseWindow();
}
