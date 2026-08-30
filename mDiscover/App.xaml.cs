using mDiscover.Core.Interfaces;
using mDiscover.Core.Models;
using mDiscover.Core.Services;
using mDiscover.Providers.Win32;
using mDiscover.Providers.WinRt;
using mDiscover.Services;
using mDiscover.ViewModels;
using mDiscover.ViewModels.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage;
using NLog;
using NLog.Extensions.Logging;

namespace mDiscover;

public partial class App : Application
{
    public static new App Current => (App)Application.Current;

    public IServiceProvider Services { get; }
    public MainWindow? MainWindow { get; private set; }
    public ApplicationData AppData { get; } =
#if UNPACKAGED
        ApplicationData.GetForUnpackaged(mDiscover.Core.Common.AppPaths.PublisherName, mDiscover.Core.Common.AppPaths.AppName);
#else
        ApplicationData.GetDefault();
#endif

    public App()
    {
        InitializeLogging();
        Services = ConfigureServices();
        InitializeTheme();
        InitializeComponent();
    }

    private void InitializeTheme()
    {
        var savedTheme = Services
            .GetRequiredService<LocalSettingsService>()
            .ReadSetting(SettingDefinitions.AppTheme);
        if (savedTheme == mDiscover.Core.Models.AppTheme.Light)
        {
            RequestedTheme = ApplicationTheme.Light;
        }
        else if (savedTheme == mDiscover.Core.Models.AppTheme.Dark)
        {
            RequestedTheme = ApplicationTheme.Dark;
        }
    }

    private void InitializeLogging()
    {
        AppDomain.CurrentDomain.ProcessExit += (s, e) => LogManager.Shutdown();
        UnhandledException += OnAppUnhandledException;

        mDiscover.Core.Common.AppPaths.Initialize(AppData.LocalFolder.Path);
        var logDir = mDiscover.Core.Common.AppPaths.LogFolder;
        Directory.CreateDirectory(logDir);
        var logFile = Path.Combine(logDir, mDiscover.Core.Common.AppPaths.LogFileName);

        var config = new NLog.Config.LoggingConfiguration();

        var fileTarget = new NLog.Targets.FileTarget("file")
        {
            FileName = logFile,
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            MaxArchiveFiles = 7,
            Layout = "${longdate}\t${level:uppercase=true:padding=-5}\t${logger:shortName=true:padding=-30}\t${message:withexception=true}",
        };

        var debugTarget = new NLog.Targets.DebuggerTarget("debugger")
        {
            Layout = "${level:uppercase=true:padding=-5}\t${logger:shortName=true:padding=-30}\t${message:withexception=true}",
        };

        config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, fileTarget);
        config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, debugTarget);

        LogManager.Configuration = config;
        LogManager.GetCurrentClassLogger().Info("mDiscover logging initialized in {0}", logDir);
    }

    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog();
        });

        // Settings & Path Services
        services.AddSingleton<ApplicationData>(s => AppData);
        services.AddSingleton<IAppPathService, AppPathService>();
        services.AddSingleton<LocalSettingsService>();
        services.AddSingleton<ISettingsService>(sp => sp.GetRequiredService<LocalSettingsService>());
        services.AddSingleton<WindowPlacementService>();

        // WinUI Abstraction Services
        services.AddSingleton<IDispatcherService, WinUiDispatcherService>();
        services.AddSingleton<IClipboardService, WinUiClipboardService>();
        services.AddSingleton<IUriLauncherService, WinUiLauncherService>();
        services.AddSingleton<IAppLifecycleService, WinUiAppLifecycleService>();

        // Register Active Discovery Providers (Win32 Native & WinRT DeviceWatcher)
        services.AddSingleton<IDnsSdDiscoveryProvider, Win32DnsSdProvider>();
        services.AddSingleton<IDnsSdDiscoveryProvider, WinRtDeviceWatcherProvider>();
#if DEBUG
        services.AddSingleton<IDnsSdDiscoveryProvider, FakeDnsSdProvider>();
#endif

        // Register Discovery Engine
        services.AddSingleton<IServiceDiscoveryEngine, ServiceDiscoveryEngine>();

        // Register Core Singletons
        services.AddSingleton(TimeProvider.System);

        // Register Localized Export Service
        services.AddSingleton<IExportService>(sp =>
        {
            var options = new ExportLocalizationOptions
            {
                ServiceLabel = Strings.Resources.Export_Label_Service,
                TypeLabel = Strings.Resources.Export_Label_Type,
                HostLabel = Strings.Resources.Export_Label_Host,
                EndpointLabel = Strings.Resources.Export_Label_Endpoint,
                IPv4Label = Strings.Resources.Export_Label_IPv4,
                IPv6Label = Strings.Resources.Export_Label_IPv6,
                TxtAttributesLabel = Strings.Resources.Export_Label_TxtAttributes,
                UnspecifiedLabel = Strings.Resources.Export_Label_Unspecified,
                MarkdownDocumentTitle = Strings.Resources.Export_Markdown_Title,
                ExportedOnPrefix = Strings.Resources.Export_Markdown_ExportedOn,
                TableHeaderServiceName = Strings.Resources.Export_Table_ServiceName,
                TableHeaderType = Strings.Resources.Export_Table_Type,
                TableHeaderHost = Strings.Resources.Export_Table_Host,
                TableHeaderEndpoint = Strings.Resources.Export_Table_Endpoint,
                TableHeaderIpAddresses = Strings.Resources.Export_Table_IpAddresses,
                TableHeaderTxtAttributes = Strings.Resources.Export_Table_TxtAttributes
            };
            return new ExportService(options, sp.GetRequiredService<TimeProvider>());
        });

        // Register ViewModels & Registries
        services.AddSingleton<DiscoveredServiceRegistry>();
        services.AddSingleton<MainViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var logger = Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation("App launching OnLaunched");

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();
    }

    private void OnAppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var logger = Services?.GetService<ILogger<App>>();
        logger?.LogCritical(e.Exception, "Unhandled application exception occurred: {Message}", e.Message);
    }
}
