using Microsoft.Extensions.Options;
using System.Collections;
using System.IO;
using TaskmgrOverlay.Contracts.Services;
using TaskmgrOverlay.Models;

namespace TaskmgrOverlay.Services;

public class PersistAndRestoreService(IFileService fileService, IOptions<AppConfig> appConfig) : IPersistAndRestoreService
{
    private readonly AppConfig _appConfig = appConfig.Value;
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public void PersistData()
    {
        if (System.Windows.Application.Current.Properties == null) return;

        string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        string fileName = _appConfig.AppPropertiesFileName;
        fileService.Save(folderPath, fileName, App.Current.Properties);
    }

    public void RestoreData()
    {
        string folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        string fileName = _appConfig.AppPropertiesFileName;
        IDictionary properties = fileService.Read<IDictionary>(folderPath, fileName);
        if (properties == null) return;

        foreach (DictionaryEntry property in properties)
        {
            System.Windows.Application.Current.Properties.Add(property.Key, property.Value);
        }
    }
}
