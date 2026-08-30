using Windows.Storage;
using Windows.System;
using mDiscover.Core.Interfaces;

namespace mDiscover.Services;

public class WinUiLauncherService : IUriLauncherService
{
    public async Task<bool> LaunchUriAsync(Uri uri)
    {
        return await Launcher.LaunchUriAsync(uri);
    }

    public async Task<bool> LaunchFolderPathAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            return await Launcher.LaunchFolderAsync(folder);
        }
        catch
        {
            return false;
        }
    }
}
