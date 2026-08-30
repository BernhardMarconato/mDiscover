namespace mDiscover.Core.Models;

/// <summary>
/// Defines localized strings and labels used when formatting exports in text and markdown.
/// </summary>
public record ExportLocalizationOptions
{
    public string ServiceLabel { get; init; } = "Service";
    public string TypeLabel { get; init; } = "Type";
    public string HostLabel { get; init; } = "Host";
    public string EndpointLabel { get; init; } = "Endpoint";
    public string IPv4Label { get; init; } = "IPv4";
    public string IPv6Label { get; init; } = "IPv6";
    public string TxtAttributesLabel { get; init; } = "TXT attributes";
    public string UnspecifiedLabel { get; init; } = "Unspecified";
    public string MarkdownDocumentTitle { get; init; } = "Discovered DNS-SD / Bonjour services";
    public string ExportedOnPrefix { get; init; } = "Exported on";
    public string TableHeaderServiceName { get; init; } = "Service name";
    public string TableHeaderType { get; init; } = "Type";
    public string TableHeaderHost { get; init; } = "Host";
    public string TableHeaderEndpoint { get; init; } = "Endpoint";
    public string TableHeaderIpAddresses { get; init; } = "IP addresses";
    public string TableHeaderTxtAttributes { get; init; } = "TXT attributes";
}
