using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using mDiscover.Core.Collections;
using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;

namespace mDiscover.ViewModels;

/// <summary>
/// Represents an observable ViewModel for a discovered DNS-SD service instance,
/// exposing reactive properties, resolution status, address collections, and copy/browse commands.
/// </summary>
public partial class DiscoveredServiceViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;
    private readonly IUriLauncherService _launcherService;
    private readonly IServiceDiscoveryEngine _engine;
    private readonly IExportService _exportService;

    public DiscoveredService Model { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullServicePath))]
    public partial string InstanceName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServiceDefinition))]
    [NotifyPropertyChangedFor(nameof(Category))]
    [NotifyPropertyChangedFor(nameof(DisplayType))]
    [NotifyPropertyChangedFor(nameof(FullServicePath))]
    [NotifyPropertyChangedFor(nameof(CanOpenInBrowser))]
    [NotifyPropertyChangedFor(nameof(BrowserUrl))]
    public partial string ServiceType { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullServicePath))]
    public partial string Domain { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHostName))]
    [NotifyPropertyChangedFor(nameof(FullServicePath))]
    [NotifyPropertyChangedFor(nameof(FormattedEndpoint))]
    [NotifyPropertyChangedFor(nameof(IsResolvingHost))]
    [NotifyPropertyChangedFor(nameof(IsResolvingEndpoint))]
    [NotifyPropertyChangedFor(nameof(CanOpenInBrowser))]
    [NotifyPropertyChangedFor(nameof(BrowserUrl))]
    public partial string? HostName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedEndpoint))]
    [NotifyPropertyChangedFor(nameof(CanOpenInBrowser))]
    [NotifyPropertyChangedFor(nameof(BrowserUrl))]
    [NotifyPropertyChangedFor(nameof(IsResolvingEndpoint))]
    public partial int? Port { get; set; }

    [ObservableProperty]
    public partial bool IsOnline { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public DateTimeOffset FirstSeen => Model.FirstSeen;

    [ObservableProperty]
    public partial DateTimeOffset LastSeen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResolving))]
    [NotifyPropertyChangedFor(nameof(IsResolved))]
    [NotifyPropertyChangedFor(nameof(IsResolutionFailed))]
    [NotifyPropertyChangedFor(nameof(IsResolvingIpAddresses))]
    [NotifyPropertyChangedFor(nameof(IsResolvingHost))]
    [NotifyPropertyChangedFor(nameof(IsResolvingEndpoint))]
    [NotifyPropertyChangedFor(nameof(FormattedEndpoint))]
    [NotifyPropertyChangedFor(nameof(PrimaryIp))]
    [NotifyPropertyChangedFor(nameof(CanOpenInBrowser))]
    [NotifyPropertyChangedFor(nameof(BrowserUrl))]
    public partial ResolutionStatus ResolutionStatus { get; set; }

    [ObservableProperty]
    public partial ResolutionFailureReason FailureReason { get; set; } = ResolutionFailureReason.None;

    [ObservableProperty]
    public partial string? FailureDetails { get; set; }

    [ObservableProperty]
    public partial bool IsFallbackResolution { get; set; }

    [ObservableProperty]
    public partial ExportFormat SelectedCopyFormat { get; set; } = ExportFormat.Markdown;

    [ObservableProperty]
    public partial bool IsCopyNotificationOpen { get; set; }

    public ObservableCollection<IPAddress> IPv4Addresses { get; } = [];
    public ObservableCollection<IPAddress> IPv6Addresses { get; } = [];
    public ObservableCollection<IpAddressDisplayItem> AllIpAddresses { get; } = [];
    public ObservableCollection<TxtRecordItem> TxtRecords { get; } = [];

    public ServiceDefinition ServiceDefinition => Model.ServiceDefinition;
    public ServiceCategory Category => Model.Category;
    public string DisplayType => Model.DisplayType;
    public string FullServicePath => Model.FullServicePath;
    public string PrimaryIp => Model.PrimaryIp;
    public string FormattedEndpoint => Model.FormattedEndpoint;
    public bool IsResolving => ResolutionStatus == ResolutionStatus.Resolving;
    public bool IsResolved => ResolutionStatus == ResolutionStatus.Resolved;
    public bool IsResolutionFailed => ResolutionStatus == ResolutionStatus.Failed;
    public bool HasHostName => !string.IsNullOrWhiteSpace(HostName);
    public bool HasIpAddresses => IPv4Addresses.Count > 0 || IPv6Addresses.Count > 0;
    public bool IsResolvingIpAddresses => IsResolving && !HasIpAddresses;
    public bool IsResolvingHost => IsResolving && !HasHostName;
    public bool IsResolvingEndpoint => IsResolving && string.IsNullOrWhiteSpace(FormattedEndpoint);
    public bool CanOpenInBrowser => Model.CanOpenInBrowser;
    public string? BrowserUrl => Model.BrowserUrl;

    public int TxtCount => TxtRecords.Count;
    public bool HasTxtRecords => TxtRecords.Count > 0;

    public DiscoveredServiceViewModel(
        DiscoveredService model,
        IClipboardService clipboardService,
        IUriLauncherService launcherService,
        IServiceDiscoveryEngine engine,
        IExportService exportService)
    {
        Model = model;
        _clipboardService = clipboardService;
        _launcherService = launcherService;
        _engine = engine;
        _exportService = exportService;

        InstanceName = model.InstanceName;
        ServiceType = model.ServiceType;
        Domain = model.Domain;
        HostName = model.HostName;
        Port = model.Port;
        IsOnline = model.IsOnline;
        LastSeen = model.LastSeen;
        ResolutionStatus = model.ResolutionStatus;
        FailureReason = model.FailureReason;
        FailureDetails = model.FailureDetails;
        IsFallbackResolution = model.IsFallbackResolution;

        SyncCollections();
    }

    public void UpdateFromModel()
    {
        InstanceName = Model.InstanceName;
        ServiceType = Model.ServiceType;
        Domain = Model.Domain;
        HostName = Model.HostName;
        Port = Model.Port;
        IsOnline = Model.IsOnline;
        LastSeen = Model.LastSeen;
        ResolutionStatus = Model.ResolutionStatus;
        FailureReason = Model.FailureReason;
        FailureDetails = Model.FailureDetails;
        IsFallbackResolution = Model.IsFallbackResolution;

        SyncCollections();
    }

    private void SyncCollections()
    {
        var v4Changed = IPv4Addresses.SyncTo(Model.IPv4Addresses);
        var v6Changed = IPv6Addresses.SyncTo(Model.IPv6Addresses);

        if (v4Changed || v6Changed || AllIpAddresses.Count != (IPv4Addresses.Count + IPv6Addresses.Count))
        {
            var desiredDisplayIps = new List<IpAddressDisplayItem>(Model.IPv4Addresses.Count + Model.IPv6Addresses.Count);
            foreach (var ip in Model.IPv4Addresses)
                desiredDisplayIps.Add(new IpAddressDisplayItem(ip.ToString(), "IPv4"));
            foreach (var ip in Model.IPv6Addresses)
                desiredDisplayIps.Add(new IpAddressDisplayItem(ip.ToString(), "IPv6"));

            AllIpAddresses.SyncTo(desiredDisplayIps);
        }

        TxtRecords.SyncTo(Model.TxtRecords);

        if ((IPv4Addresses.Count > 0 || IPv6Addresses.Count > 0) && ResolutionStatus == ResolutionStatus.Resolving)
        {
            ResolutionStatus = ResolutionStatus.Resolved;
            Model.ResolutionStatus = ResolutionStatus.Resolved;
        }

        OnPropertyChanged(nameof(HasIpAddresses));
        OnPropertyChanged(nameof(IsResolvingIpAddresses));
        OnPropertyChanged(nameof(TxtCount));
        OnPropertyChanged(nameof(HasTxtRecords));
        OnPropertyChanged(nameof(PrimaryIp));
        OnPropertyChanged(nameof(FormattedEndpoint));
    }

    [RelayCommand]
    public async Task RetryResolveAsync()
    {
        ResolutionStatus = ResolutionStatus.Resolving;
        FailureReason = ResolutionFailureReason.None;
        FailureDetails = null;
        await _engine.ResolveDetailsAsync(Model);
    }

    [RelayCommand]
    public async Task OpenInBrowserAsync()
    {
        if (!string.IsNullOrWhiteSpace(BrowserUrl) && Uri.TryCreate(BrowserUrl, UriKind.Absolute, out var uri))
        {
            await _launcherService.LaunchUriAsync(uri);
        }
    }

    /// <summary>
    /// Copies the service details to the clipboard in the specified format, or defaults to <see cref="SelectedCopyFormat"/>.
    /// </summary>
    [RelayCommand]
    public void Copy(ExportFormat? format = null)
    {
        var targetFormat = format ?? SelectedCopyFormat;
        SelectedCopyFormat = targetFormat;
        var text = _exportService.Export(Model, targetFormat);
        _clipboardService.SetText(text);
        IsCopyNotificationOpen = true;
    }
}

/// <summary>
/// Represents a formatted IP address display item with its protocol version label.
/// </summary>
public record IpAddressDisplayItem(string Address, string ProtocolVersion);

