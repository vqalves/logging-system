using LogSystem.WebApp;
using LogSystem.WebApp.Endpoints.LogCollectionEndpoints;
using LogSystem.WebApp.Endpoints.LogAttributeEndpoints;
using LogSystem.WebApp.Endpoints.LogDataEndpoints;
using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.Core.BackgroundServices.Persistence;
using LogSystem.Core.Services.RabbitMq;
using LogSystem.Core.BackgroundServices.Cleanup;
using LogSystem.Core.Metrics;
using System.Threading.Channels;
using LogSystem.Core.BackgroundServices.Persistence.DefaultMessageReceiver;
using LogSystem.Core.Caching;

var configBuilder = new LogSystemConfigurationBuilder();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure LogSystem services
configBuilder.ConfigureServices(builder.Services);

// Register services
builder.Services.AddSingleton<AzureService>();
builder.Services.AddSingleton<LogAttributeService>();
builder.Services.AddSingleton<LogCollectionService>();
builder.Services.AddSingleton<LogDataService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<RabbitMqPublisher>();

// Register metrics
builder.Services.AddSingleton<MessagesPerCollectionInTimeWindowReport>();

// Register Channel for message processing
builder.Services.AddSingleton(Channel.CreateUnbounded<IReceivedMessageModel>(new UnboundedChannelOptions
{
    SingleReader = true,
    SingleWriter = false
}));

// Register caches
builder.Services.AddSingleton<LogCollectionCache>(p =>
{
    var databaseService = p.GetRequiredService<DatabaseService>();
    var azureService = p.GetRequiredService<AzureService>();
    var logSystemConfig = p.GetRequiredService<LogSystemConfig>();

    return new LogCollectionCache(
        cacheDuration: logSystemConfig.CacheDurationMinutes,
        databaseService: databaseService,
        logSystemConfig: logSystemConfig,
        azureService: azureService
    );
});

builder.Services.AddSingleton<LogAttributeCache>(p =>
{
    var databaseService = p.GetRequiredService<DatabaseService>();
    var logSystemConfig = p.GetRequiredService<LogSystemConfig>();

    return new LogAttributeCache(
        cacheDuration: logSystemConfig.CacheDurationMinutes,
        databaseService: databaseService
    );
});

// Register background services
builder.Services.AddHostedService<DefaultMessageReceiverBackgroundService>();
builder.Services.AddHostedService<BatchPersistenceService>();
builder.Services.AddHostedService<CleanupBackgroundService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Register LogCollection endpoints
GetLogCollectionsEndpoint.MapEndpoint(app);
GetCollectionMetricsEndpoint.MapEndpoint(app);
CreateOrUpdateLogCollectionEndpoint.MapEndpoint(app);
DeleteLogCollectionEndpoint.MapEndpoint(app);

// Register LogAttribute endpoints
CreateLogAttributeEndpoint.MapEndpoint(app);
UpdateLogAttributeEndpoint.MapEndpoint(app);
GetLogAttributesByCollectionEndpoint.MapEndpoint(app);
DeleteLogAttributeEndpoint.MapEndpoint(app);

// Register Log endpoints
AddLogEndpoint.MapEndpoint(app);
AddLogBatchEndpoint.MapEndpoint(app);
SearchLogsEndpoint.MapEndpoint(app);
DownloadLogEndpoint.MapEndpoint(app);

app.Run();
