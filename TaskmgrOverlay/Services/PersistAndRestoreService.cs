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
        if (System.Windows.Application.Current.Properties != null)
        {
            var folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
            var fileName = _appConfig.AppPropertiesFileName;
            fileService.Save(folderPath, fileName, App.Current.Properties);
        }
    }

    public void RestoreData()
    {
        var folderPath = Path.Combine(_localAppData, _appConfig.ConfigurationsFolder);
        var fileName = _appConfig.AppPropertiesFileName;
        var properties = fileService.Read<IDictionary>(folderPath, fileName);
        if (properties != null)
        {
            foreach (DictionaryEntry property in properties)
            {
                App.Current.Properties.Add(property.Key, property.Value);
            }
        }
    }
}
