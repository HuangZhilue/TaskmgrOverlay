using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using TaskmgrOverlay.Contracts.Services;
using TaskmgrOverlay.Properties;

namespace TaskmgrOverlay.ViewModels;

public partial class ShellViewModel(INavigationService navigationService) : ObservableObject
{
    [ObservableProperty]
    private HamburgerMenuItem _selectedMenuItem;
    [ObservableProperty]
    private HamburgerMenuItem _selectedOptionsMenuItem;

    // TODO: Change the icons and titles for all HamburgerMenuItems here.
    public ObservableCollection<HamburgerMenuItem> MenuItems { get; } = new ObservableCollection<HamburgerMenuItem>()
    {
        new HamburgerMenuGlyphItem() { Label = Resources.ShellMainPage, Glyph = MahApps.Metro.IconPacks.PackIconMaterialKind.DrawingBox.ToString(), TargetPageType = typeof(MainViewModel) },
    };

    public ObservableCollection<HamburgerMenuItem> OptionMenuItems { get; } = new ObservableCollection<HamburgerMenuItem>()
    {
        new HamburgerMenuGlyphItem() { Label = Resources.ShellSettingsPage, Glyph = MahApps.Metro.IconPacks.PackIconMaterialKind.Cog.ToString(), TargetPageType = typeof(SettingsViewModel) }
    };

    [RelayCommand]
    private void OnLoaded()
    {
        navigationService.Navigated += OnNavigated;
    }

    [RelayCommand]
    private void OnUnloaded()
    {
        navigationService.Navigated -= OnNavigated;
    }

    private bool CanGoBack()
        => navigationService.CanGoBack;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void OnGoBack()
        => navigationService.GoBack();

    [RelayCommand]
    private void OnMenuItemInvoked()
        => NavigateTo(SelectedMenuItem.TargetPageType);

    [RelayCommand]
    private void OnOptionsMenuItemInvoked()
        => NavigateTo(SelectedOptionsMenuItem.TargetPageType);

    private void NavigateTo(Type targetViewModel)
    {
        if (targetViewModel == null) return;

        navigationService.NavigateTo(targetViewModel.FullName);
    }

    private void OnNavigated(object sender, string viewModelName)
    {
        HamburgerMenuItem item = MenuItems
                    .OfType<HamburgerMenuItem>()
                    .FirstOrDefault(i => viewModelName == i.TargetPageType?.FullName);
        if (item != null)
        {
            SelectedMenuItem = item;
        }
        else
        {
            SelectedOptionsMenuItem = OptionMenuItems
                    .OfType<HamburgerMenuItem>()
                    .FirstOrDefault(i => viewModelName == i.TargetPageType?.FullName);
        }

        GoBackCommand.NotifyCanExecuteChanged();
    }
}
