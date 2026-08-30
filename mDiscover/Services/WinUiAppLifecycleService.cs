using Microsoft.Windows.AppLifecycle;
using mDiscover.Core.Interfaces;

namespace mDiscover.Services;

public class WinUiAppLifecycleService : IAppLifecycleService
{
    public void Restart()
    {
        AppInstance.Restart(string.Empty);
    }
}
