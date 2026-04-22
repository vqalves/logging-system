using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.WebApp;
using LogSystem.WebApp.BackgroundServices.Persistence;
using LogSystem.WebApp.BackgroundServices.Cleanup;
using LogSystem.WebApp.Endpoints.LogCollection;
using LogSystem.WebApp.Endpoints.LogAttribute;
using LogSystem.WebApp.Endpoints.LogData;
using LogSystem.WebApp.Services;

var configBuilder = new LogSystemConfigurationBuilder();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// TODO: Move service injections to a extension method inside LogSystemConfigurationBuilder
// Register configurations
var azureConfig = configBuilder.GetAzureConfig();
var databaseConfig = configBuilder.GetDatabaseConfig();
var logSystemConfig = configBuilder.GetLogSystemConfig();
var persistenceConfig = configBuilder.GetPersistenceBackgroundServiceConfig();
var cleanupConfig = configBuilder.GetCleanupBackgroundServiceConfig();
var publishConfig = configBuilder.GetPublishServiceConfig();

builder.Services.AddSingleton(azureConfig);
builder.Services.AddSingleton(databaseConfig);
builder.Services.AddSingleton(logSystemConfig);
builder.Services.AddSingleton(persistenceConfig);
builder.Services.AddSingleton(cleanupConfig);
builder.Services.AddSingleton(publishConfig);

// Register services
builder.Services.AddSingleton<AzureService>();
builder.Services.AddSingleton<LogAttributeService>();
builder.Services.AddSingleton<LogCollectionService>();
builder.Services.AddSingleton<LogDataService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<BatchPersistenceService>();
builder.Services.AddSingleton<RabbitMqPublisher>();

// Register caches
builder.Services.AddSingleton<LogCollectionCache>(p =>
{
    var databaseService = p.GetRequiredService<DatabaseService>();

    return new LogCollectionCache(
        cacheDuration: logSystemConfig.CacheDurationMinutes,
        databaseService: databaseService
    );
});

builder.Services.AddSingleton<LogAttributeCache>(p =>
{
    var databaseService = p.GetRequiredService<DatabaseService>();

    return new LogAttributeCache(
        cacheDuration: logSystemConfig.CacheDurationMinutes,
        databaseService: databaseService
    );
});

// Register background services
builder.Services.AddHostedService<PersistenceBackgroundService>();
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
CreateOrUpdateLogCollectionEndpoint.MapEndpoint(app);
DeleteLogCollectionEndpoint.MapEndpoint(app);

// Register LogAttribute endpoints
CreateLogAttributeEndpoint.MapEndpoint(app);
UpdateLogAttributeEndpoint.MapEndpoint(app);
GetLogAttributesByCollectionEndpoint.MapEndpoint(app);
DeleteLogAttributeEndpoint.MapEndpoint(app);

// Register Log endpoints
AddLogEndpoint.MapEndpoint(app);

app.Run();
