using Windows.ApplicationModel.DataTransfer;
using mDiscover.Core.Interfaces;

namespace mDiscover.Services;

public class WinUiClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }
}
