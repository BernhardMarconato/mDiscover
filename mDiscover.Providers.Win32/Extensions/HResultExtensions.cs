namespace mDiscover.Providers.Win32.Extensions;

using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;

/// <summary>
/// Extension methods for CsWin32 <see cref="HRESULT"/> and <see cref="WIN32_ERROR"/> types.
/// </summary>
internal static class HResultExtensions
{
    extension(HRESULT hr)
    {
        /// <summary>
        /// Formats a CsWin32 <see cref="HRESULT"/> into a human-readable system error message with the hex code in brackets.
        /// </summary>
        public string ToFormattedString()
        {
            try
            {
                var msg = new Win32Exception(hr.Value).Message.Trim().TrimEnd('.');
                return $"{msg} (0x{hr.Value:X8})";
            }
            catch
            {
                return $"Error (0x{hr.Value:X8})";
            }
        }
    }

    extension(WIN32_ERROR win32Error)
    {
        /// <summary>
        /// Formats a <see cref="WIN32_ERROR"/> into a human-readable system error message with its <see cref="HRESULT"/> in brackets.
        /// </summary>
        public string ToFormattedString()
        {
            return PInvoke.HRESULT_FROM_WIN32(win32Error).ToFormattedString();
        }
    }
}
