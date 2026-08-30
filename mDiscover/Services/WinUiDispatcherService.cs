using Microsoft.UI.Dispatching;
using mDiscover.Core.Interfaces;

namespace mDiscover.Services;

public class WinUiDispatcherService(DispatcherQueue? dispatcherQueue = null) : IDispatcherService
{
    private readonly DispatcherQueue _dispatcherQueue = dispatcherQueue ?? DispatcherQueue.GetForCurrentThread();

    public void Enqueue(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
    }
}
