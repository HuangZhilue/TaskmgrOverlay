using System.Reflection;
using TaskmgrOverlay.Contracts.Services;

namespace TaskmgrOverlay.Services;

public class ApplicationInfoService : IApplicationInfoService
{
    public ApplicationInfoService()
    {
    }

    public Version GetVersion()
    {
        // Set the app version in TaskmgrOverlay > Properties > Package > PackageVersion
        //string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        //var version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
        //return new Version(version);
        return Assembly.GetExecutingAssembly().GetName().Version;
    }
}
