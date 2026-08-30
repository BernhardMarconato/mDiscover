using System.Collections.Frozen;

namespace mDiscover.Core.Models;

/// <summary>
/// Provides high-performance frozen catalog lookup and category inference for well-known DNS-SD service types.
/// </summary>
public static class WellKnownServiceCatalog
{
    private static readonly FrozenDictionary<string, ServiceDefinition> _services = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        // Web & HTTP
        ["_http._tcp"] = new("_http._tcp", "Web Server (HTTP)", ServiceCategory.WebAndApi, 80, "http"),
        ["_https._tcp"] = new("_https._tcp", "Secure Web Server (HTTPS)", ServiceCategory.WebAndApi, 443, "https"),
        ["_webdav._tcp"] = new("_webdav._tcp", "WebDAV Server", ServiceCategory.WebAndApi, 80, "http"),
        ["_webdavs._tcp"] = new("_webdavs._tcp", "Secure WebDAV Server", ServiceCategory.WebAndApi, 443, "https"),
        ["_rest._tcp"] = new("_rest._tcp", "REST API Service", ServiceCategory.WebAndApi, null, "http"),

        // Smart Home & IoT
        ["_elg._tcp"] = new("_elg._tcp", "Elgato Key Light", ServiceCategory.SmartHomeAndIot, 9123, "http"),
        ["_egstreamwrap._tcp"] = new("_egstreamwrap._tcp", "Elgato EpocCam", ServiceCategory.MediaAndAudio, null, null),
        ["_miio._udp"] = new("_miio._udp", "Xiaomi Mijia Smart Device", ServiceCategory.SmartHomeAndIot, 54321, null),
        ["_yeelight._tcp"] = new("_yeelight._tcp", "Yeelight Smart Bulb", ServiceCategory.SmartHomeAndIot, 55443, null),
        ["_nanoleafapi._tcp"] = new("_nanoleafapi._tcp", "Nanoleaf Light Panels", ServiceCategory.SmartHomeAndIot, 16021, "http"),
        ["_nanoleaf._tcp"] = new("_nanoleaf._tcp", "Nanoleaf Aurora/Canvas", ServiceCategory.SmartHomeAndIot, 16021, "http"),
        ["_wled._tcp"] = new("_wled._tcp", "WLED Addressable LED Controller", ServiceCategory.SmartHomeAndIot, 80, "http"),
        ["_shelly._tcp"] = new("_shelly._tcp", "Shelly Smart Relay", ServiceCategory.SmartHomeAndIot, 80, "http"),
        ["_tasmota._tcp"] = new("_tasmota._tcp", "Tasmota Smart Device", ServiceCategory.SmartHomeAndIot, 80, "http"),
        ["_esphomelib._tcp"] = new("_esphomelib._tcp", "ESPHome Sensor Node", ServiceCategory.SmartHomeAndIot, 6053, null),
        ["_hue._tcp"] = new("_hue._tcp", "Philips Hue Bridge", ServiceCategory.SmartHomeAndIot, 80, "http"),
        ["_hap._tcp"] = new("_hap._tcp", "Apple HomeKit", ServiceCategory.SmartHomeAndIot, null, null),
        ["_hap._udp"] = new("_hap._udp", "Apple HomeKit", ServiceCategory.SmartHomeAndIot, null, null),
        ["_matter._tcp"] = new("_matter._tcp", "Matter Smart Device", ServiceCategory.SmartHomeAndIot, null, null),
        ["_matterc._udp"] = new("_matterc._udp", "Matter Commissioning Beacon", ServiceCategory.SmartHomeAndIot, 5540, null),
        ["_matterd._udp"] = new("_matterd._udp", "Matter Operational Device", ServiceCategory.SmartHomeAndIot, 5540, null),
        ["_meshcop._udp"] = new("_meshcop._udp", "Thread Border Router", ServiceCategory.SmartHomeAndIot, null, null),
        ["_home-assistant._tcp"] = new("_home-assistant._tcp", "Home Assistant Server", ServiceCategory.SmartHomeAndIot, 8123, "http"),
        ["_mqtt._tcp"] = new("_mqtt._tcp", "MQTT Message Broker", ServiceCategory.SmartHomeAndIot, 1883, "mqtt"),
        ["_ewelink._tcp"] = new("_ewelink._tcp", "Sonoff / eWeLink Device", ServiceCategory.SmartHomeAndIot, 8081, "http"),
        ["_tuya._tcp"] = new("_tuya._tcp", "Tuya Smart Device", ServiceCategory.SmartHomeAndIot, 6668, null),

        // Remote Access & Developer Tools
        ["_ssh._tcp"] = new("_ssh._tcp", "SSH Remote Terminal", ServiceCategory.RemoteAccess, 22, "ssh"),
        ["_sftp-ssh._tcp"] = new("_sftp-ssh._tcp", "SFTP File Transfer", ServiceCategory.RemoteAccess, 22, "sftp"),
        ["_telnet._tcp"] = new("_telnet._tcp", "Telnet Terminal", ServiceCategory.RemoteAccess, 23, "telnet"),
        ["_rfb._tcp"] = new("_rfb._tcp", "VNC Screen Sharing", ServiceCategory.RemoteAccess, 5900, "vnc"),
        ["_rdp._tcp"] = new("_rdp._tcp", "Remote Desktop (RDP)", ServiceCategory.RemoteAccess, 3389, "rdp"),
        ["_adb._tcp"] = new("_adb._tcp", "Android Debug Bridge (ADB)", ServiceCategory.Developer, 5555, null),
        ["_workstation._tcp"] = new("_workstation._tcp", "Workstation Announcement", ServiceCategory.Infrastructure, null, null),

        // Media, Audio & Casting
        ["_airplay._tcp"] = new("_airplay._tcp", "Apple AirPlay Display", ServiceCategory.MediaAndAudio, 7000, "airplay"),
        ["_raop._tcp"] = new("_raop._tcp", "Apple AirPlay Audio (RAOP)", ServiceCategory.MediaAndAudio, 5000, "raop"),
        ["_spotify-connect._tcp"] = new("_spotify-connect._tcp", "Spotify Connect Speaker", ServiceCategory.MediaAndAudio, null, "spotify"),
        ["_sonos._tcp"] = new("_sonos._tcp", "Sonos Speaker System", ServiceCategory.MediaAndAudio, 1400, "http"),
        ["_googlecast._tcp"] = new("_googlecast._tcp", "Google Cast / Chromecast", ServiceCategory.MediaAndAudio, 8009, "cast"),
        ["_googlezone._tcp"] = new("_googlezone._tcp", "Google Cast Audio Group", ServiceCategory.MediaAndAudio, 8009, "cast"),
        ["_daap._tcp"] = new("_daap._tcp", "Digital Audio Access Protocol (iTunes)", ServiceCategory.MediaAndAudio, 3689, "daap"),
        ["_touch-able._tcp"] = new("_touch-able._tcp", "Apple TV Remote / Control", ServiceCategory.MediaAndAudio, null, null),
        ["_mediaremotetv._tcp"] = new("_mediaremotetv._tcp", "Apple MediaRemote TV", ServiceCategory.MediaAndAudio, null, null),
        ["_plex._tcp"] = new("_plex._tcp", "Plex Media Server", ServiceCategory.MediaAndAudio, 32400, "http"),

        // Print, Scan & 3D Printing
        ["_ipp._tcp"] = new("_ipp._tcp", "IPP Network Printer", ServiceCategory.PrintAndScan, 631, "ipp"),
        ["_ipps._tcp"] = new("_ipps._tcp", "Secure IPP Network Printer", ServiceCategory.PrintAndScan, 631, "ipps"),
        ["_printer._tcp"] = new("_printer._tcp", "LPR/LPD Print Server", ServiceCategory.PrintAndScan, 515, null),
        ["_pdl-datastream._tcp"] = new("_pdl-datastream._tcp", "Raw Port 9100 Print Stream", ServiceCategory.PrintAndScan, 9100, null),
        ["_scanner._tcp"] = new("_scanner._tcp", "Network Scanner", ServiceCategory.PrintAndScan, null, null),
        ["_uscan._tcp"] = new("_uscan._tcp", "eSCL Network Scanner", ServiceCategory.PrintAndScan, 80, "http"),
        ["_uscans._tcp"] = new("_uscans._tcp", "Secure eSCL Network Scanner", ServiceCategory.PrintAndScan, 443, "https"),
        ["_octoprint._tcp"] = new("_octoprint._tcp", "OctoPrint 3D Print Server", ServiceCategory.PrintingAndWorkshop, 80, "http"),
        ["_klipper._tcp"] = new("_klipper._tcp", "Moonraker / Klipper 3D Printer", ServiceCategory.PrintingAndWorkshop, 7125, "http"),
        ["_bambulab._tcp"] = new("_bambulab._tcp", "Bambu Lab 3D Printer", ServiceCategory.PrintingAndWorkshop, null, null),
        ["_prusa-link._tcp"] = new("_prusa-link._tcp", "PrusaLink 3D Printer", ServiceCategory.PrintingAndWorkshop, 80, "http"),

        // Storage & File Sharing
        ["_smb._tcp"] = new("_smb._tcp", "SMB / Windows File Sharing", ServiceCategory.StorageAndFiles, 445, "smb"),
        ["_afpovertcp._tcp"] = new("_afpovertcp._tcp", "Apple Filing Protocol (AFP)", ServiceCategory.StorageAndFiles, 548, "afp"),
        ["_nfs._tcp"] = new("_nfs._tcp", "Network File System (NFS)", ServiceCategory.StorageAndFiles, 2049, "nfs"),
        ["_ftp._tcp"] = new("_ftp._tcp", "FTP Server", ServiceCategory.StorageAndFiles, 21, "ftp"),
        ["_device-info._tcp"] = new("_device-info._tcp", "macOS Finder Device Info", ServiceCategory.StorageAndFiles, null, null),

        // Apple Ecosystem & System Services
        ["_companion-link._tcp"] = new("_companion-link._tcp", "Apple Companion Link", ServiceCategory.AppleEcosystem, null, null),
        ["_airdrop._tcp"] = new("_airdrop._tcp", "Apple AirDrop Discovery", ServiceCategory.AppleEcosystem, null, null),
        ["_sleep-proxy._udp"] = new("_sleep-proxy._udp", "Bonjour Sleep Proxy", ServiceCategory.Infrastructure, null, null),
        ["_dns-sd._udp"] = new("_dns-sd._udp", "DNS-SD Service Meta-Query", ServiceCategory.Infrastructure, 5353, null),
        ["_services._dns-sd._udp"] = new("_services._dns-sd._udp", "DNS-SD Meta Discovery", ServiceCategory.Infrastructure, 5353, null),

        // Cameras & Streaming
        ["_axis-video._tcp"] = new("_axis-video._tcp", "Axis Network Camera", ServiceCategory.CamerasAndVideo, 80, "http"),
        ["_rtsp._tcp"] = new("_rtsp._tcp", "RTSP Video Stream", ServiceCategory.CamerasAndVideo, 554, "rtsp"),
        ["_onvif._tcp"] = new("_onvif._tcp", "ONVIF IP Camera", ServiceCategory.CamerasAndVideo, 80, "http"),

        // Databases & Caches
        ["_mysql._tcp"] = new("_mysql._tcp", "MySQL Database", ServiceCategory.Databases, 3306, null),
        ["_postgresql._tcp"] = new("_postgresql._tcp", "PostgreSQL Database", ServiceCategory.Databases, 5432, null),
        ["_redis._tcp"] = new("_redis._tcp", "Redis Cache", ServiceCategory.Databases, 6379, null)
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the underlying frozen dictionary containing all well-known service type definitions.
    /// </summary>
    public static FrozenDictionary<string, ServiceDefinition> Entries => _services;

    /// <summary>
    /// Gets all cataloged service definitions from the frozen dictionary.
    /// </summary>
    public static IReadOnlyCollection<ServiceDefinition> All => _services.Values;

    /// <summary>
    /// Gets the collection of common service types queried in targeted scans from the frozen dictionary keys.
    /// </summary>
    public static IReadOnlyCollection<string> CommonScanTypes => _services.Keys;

    /// <summary>
    /// Resolves the metadata definition for the specified DNS-SD service type, or infers one if not in the catalog.
    /// </summary>
    /// <param name="rawType">The DNS-SD service type (e.g. "_http._tcp").</param>
    /// <returns>A matched or inferred <see cref="ServiceDefinition"/>.</returns>
    public static ServiceDefinition GetOrInfer(string rawType)
    {
        var cleanType = rawType.TrimEnd('.');
        if (_services.TryGetValue(cleanType, out var found))
        {
            return found;
        }

        var title = FormatGenericTypeName(cleanType);
        var category = InferCategory(cleanType);

        return new ServiceDefinition(cleanType, title, category, null, null);
    }

    private static string FormatGenericTypeName(string rawType)
    {
        var parts = rawType.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var baseName = parts[0].TrimStart('_');
            var proto = parts.Length > 1 && parts[1].Equals("_udp", StringComparison.OrdinalIgnoreCase) ? "UDP" : "TCP";
            var capitalized = char.ToUpperInvariant(baseName[0]) + (baseName.Length > 1 ? baseName[1..] : "");
            return $"{capitalized} ({proto})";
        }
        return rawType;
    }

    /// <summary>
    /// Infers the functional domain category for an unknown DNS-SD service type string.
    /// </summary>
    /// <param name="serviceType">The service type name.</param>
    /// <returns>The inferred <see cref="ServiceCategory"/>.</returns>
    public static ServiceCategory InferCategory(string serviceType)
    {
        if (serviceType.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("web", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("rest", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("api", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.WebAndApi;
        }

        if (serviceType.Contains("ssh", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("telnet", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("vnc", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("rdp", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("remote", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.RemoteAccess;
        }

        if (serviceType.Contains("airplay", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("raop", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("cast", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("spotify", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("media", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("tv", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.MediaAndAudio;
        }

        if (serviceType.Contains("elg", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("light", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("hue", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("miio", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("yeelight", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("nanoleaf", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("wled", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("shelly", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("tasmota", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("hap", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("matter", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("home", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.SmartHomeAndIot;
        }

        if (serviceType.Contains("print", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("ipp", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("scan", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("pdl", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.PrintAndScan;
        }

        if (serviceType.Contains("smb", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("afp", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("nfs", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("ftp", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("disk", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("share", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.StorageAndFiles;
        }

        if (serviceType.Contains("octo", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("klipper", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("bambu", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("prusa", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.PrintingAndWorkshop;
        }

        if (serviceType.Contains("cam", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("video", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("onvif", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("rtsp", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.CamerasAndVideo;
        }

        if (serviceType.Contains("apple", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("companion", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("airdrop", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.AppleEcosystem;
        }

        if (serviceType.Contains("sql", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("redis", StringComparison.OrdinalIgnoreCase) ||
            serviceType.Contains("db", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceCategory.Databases;
        }

        return ServiceCategory.Other;
    }
}

