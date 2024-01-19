using AutoUpdateViaGitHubRelease;
using System.IO;
using System.Windows;
using TaskmgrOverlay.Contracts.Services;
using TaskmgrOverlay.Properties;

namespace TaskmgrOverlay.Services;

public class AppAutoUpdate(IApplicationInfoService applicationInfoService) : IAppAutoUpdate
{
    private const string GitHubUser = "HuangZhilue";
    private const string GitHubRepo = "TaskmgrOverlay";
    private Update Update { get; } = new();


    public void CheckUpdates()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), nameof(TaskmgrOverlay));
        Directory.CreateDirectory(tempDir);
        string updateArchive = Path.Combine(tempDir, "update.zip");

        Update.PropertyChanged += Update_PropertyChanged;
        Version version = applicationInfoService.GetVersion();
        Update.CheckDownloadNewVersionAsync(GitHubUser, GitHubRepo, version, updateArchive);
    }

    private void Update_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Update.Available)
        {
            MessageBoxResult r = MessageBox.Show(Resources.新版本可用是否进行更新, "", MessageBoxButton.YesNo);
            if (r != MessageBoxResult.Yes) return;
            string destinationDir = AppContext.BaseDirectory;
            Update.StartInstall(destinationDir);
            Environment.Exit(0);//.Close();
        }
    }
}
