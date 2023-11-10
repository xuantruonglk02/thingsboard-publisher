using MassTransit;
using Microsoft.AspNetCore.Builder;
using ThingsBoardPublisher;
using ThingsBoardPublisher.BackgroundServices;
using ThingsBoardPublisher.Configurations;
using ThingsBoardPublisher.Services;
using ThingsBoardPublisher.Settings;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;

var app = new HostBuilder()
    .UseWindowsService()
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config
              // .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
              .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName.ToLower()}.json", optional: true, reloadOnChange: true);
        AppConfigurations.SetConfiguration(config.Build());
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        services.AddWindowsService();
        services.Configure<EmailSetting>(configuration.GetSection("Email").Bind);
        services.Configure<PushSetting>(configuration.GetSection("PushSetting").Bind);
        services.Configure<TenantSetting>(configuration.GetSection("TenantSetting").Bind);
        services.AddSingleton<MailService, MailService>();
        services.AddSingleton<MqttService, MqttService>();
        services.AddSingleton<FileStorageService, FileStorageService>();
        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(Program).Assembly);
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
        services.AddSingleton<IHostedService, GcBackgroundService>();
        services.AddSingleton<IHostedService, LicenseBackgroundService>();
        services.AddSingleton<IHostedService, FileChangeBackgroundService>();
        services.AddSingleton<IHostedService, PreProcessBackgroundService>();
        services.AddSingleton<IHostedService, ProcessDataBackgroundService>();
        services.AddSingleton<IHostedService, PushDataBackgroundService>();
    })
    .ConfigureLogging((hostContext, logConfigure) =>
    {
        var configuration = hostContext.Configuration;
        logConfigure.ClearProviders();

        var logSetting = configuration.GetSection("LogSetting").Get<LogSetting>();
        var tenantSetting = configuration.GetSection("TenantSetting").Get<TenantSetting>();
        var els_options = new ElasticsearchSinkOptions(new Uri(logSetting.Host))
        {
            IndexFormat = $"log-publisher-{tenantSetting.Name.Slugify()}-{DateTime.Today:yyyy-MM-dd}",
            AutoRegisterTemplate = true,
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            ModifyConnectionSettings = (c) => c.BasicAuthentication(logSetting.UserName, logSetting.Password).ServerCertificateValidationCallback((o, certificate, arg3, arg4) => true),
            MinimumLogEventLevel = LogEventLevel.Information,
            EmitEventFailure = EmitEventFailureHandling.RaiseCallback | EmitEventFailureHandling.ThrowException,
            TypeName = null,
            InlineFields = false,
            BatchAction = ElasticOpType.Create
        };

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Elasticsearch(els_options)
            .WriteTo.Console()
            .Enrich.WithExceptionDetails();
        Log.Logger = loggerConfiguration.CreateLogger();
    })
    .UseSerilog()
    .Build();

app.Run();