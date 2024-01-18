using Microsoft.Extensions.Hosting;
using TaskmgrOverlay.Contracts.Activation;
using TaskmgrOverlay.Contracts.Services;
using TaskmgrOverlay.Contracts.Views;
using TaskmgrOverlay.ViewModels;

namespace TaskmgrOverlay.Services;

public class ApplicationHostService(IServiceProvider serviceProvider, IEnumerable<IActivationHandler> activationHandlers, INavigationService navigationService, IThemeSelectorService themeSelectorService, IPersistAndRestoreService persistAndRestoreService) : IHostedService
{
    private IShellWindow _shellWindow;
    private bool _isInitialized;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize services that you need before app activation
        await InitializeAsync();

        await HandleActivationAsync();

        // Tasks after activation
        await StartupAsync();
        _isInitialized = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        persistAndRestoreService.PersistData();
        await Task.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            persistAndRestoreService.RestoreData();
            themeSelectorService.InitializeTheme();
            await Task.CompletedTask;
        }
    }

    private async Task StartupAsync()
    {
        if (!_isInitialized)
        {
            await Task.CompletedTask;
        }
    }

    private async Task HandleActivationAsync()
    {
        var activationHandler = activationHandlers.FirstOrDefault(h => h.CanHandle());

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync();
        }

        await Task.CompletedTask;

        if (!System.Windows.Application.Current.Windows.OfType<IShellWindow>().Any())
        {
            // Default activation that navigates to the apps default page
            _shellWindow = serviceProvider.GetService(typeof(IShellWindow)) as IShellWindow;
            navigationService.Initialize(_shellWindow.GetNavigationFrame());
            _shellWindow.ShowWindow();
            navigationService.NavigateTo(typeof(MainViewModel).FullName);
            await Task.CompletedTask;
        }
    }
}
