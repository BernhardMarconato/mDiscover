using mDiscover.Core.Common;
using mDiscover.Core.Interfaces;

namespace mDiscover.Services;

public class AppPathService : IAppPathService
{
    public string AppDataFolderPath => AppPaths.AppDataFolder;
    public string LogFolderPath => AppPaths.LogFolder;
}
