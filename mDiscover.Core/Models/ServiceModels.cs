namespace mDiscover.Core.Models;

/// <summary>
/// Represents a key-value attribute pair from a DNS-SD TXT record.
/// </summary>
/// <param name="Key">The attribute key name.</param>
/// <param name="Value">The attribute value string.</param>
public record TxtRecordItem(string Key, string Value);

/// <summary>
/// Categorizes discovered services into functional domain groups.
/// </summary>
public enum ServiceCategory
{
    /// <summary>
    /// Uncategorized or generic service.
    /// </summary>
    Other = 0,

    /// <summary>
    /// HTTP, HTTPS, REST APIs, and web management interfaces.
    /// </summary>
    WebAndApi,

    /// <summary>
    /// Smart home hubs, IoT sensors, bridges, and lighting (HomeKit, Matter, MQTT, Zigbee).
    /// </summary>
    SmartHomeAndIot,

    /// <summary>
    /// Audio and media receivers (AirPlay, Google Cast, Spotify Connect, Sonos).
    /// </summary>
    MediaAndAudio,

    /// <summary>
    /// Remote administration tools (SSH, SFTP, Remote Desktop, VNC).
    /// </summary>
    RemoteAccess,

    /// <summary>
    /// Developer tools, local debugging endpoints, and IDE bridges.
    /// </summary>
    Developer,

    /// <summary>
    /// Workshop, 3D printers, OctoPrint, and CNC controllers.
    /// </summary>
    PrintingAndWorkshop,

    /// <summary>
    /// Network IP cameras, RTSP streams, and video surveillance.
    /// </summary>
    CamerasAndVideo,

    /// <summary>
    /// Network printers, multi-function scanners, and AirPrint devices.
    /// </summary>
    PrintAndScan,

    /// <summary>
    /// Network-attached storage (SMB, NFS, AFP, WebDAV).
    /// </summary>
    StorageAndFiles,

    /// <summary>
    /// Core network infrastructure, routers, DNS, DHCP, and mesh nodes.
    /// </summary>
    Infrastructure,

    /// <summary>
    /// Database servers and cluster nodes.
    /// </summary>
    Databases,

    /// <summary>
    /// Apple ecosystem protocols (AirDrop, Companion Link, Continuity).
    /// </summary>
    AppleEcosystem
}

/// <summary>
/// Metadata definition for a well-known DNS-SD service type.
/// </summary>
/// <param name="ServiceType">The DNS-SD service type identifier (e.g. "_http._tcp").</param>
/// <param name="DisplayName">The user-friendly display name (e.g. "Web Server").</param>
/// <param name="Category">The functional domain category.</param>
/// <param name="DefaultPort">The conventional default TCP/UDP port.</param>
/// <param name="Transport">The underlying transport protocol ("tcp" or "udp").</param>
public record ServiceDefinition(
    string ServiceType,
    string DisplayName,
    ServiceCategory Category,
    int? DefaultPort = null,
    string? Transport = "tcp"
);

