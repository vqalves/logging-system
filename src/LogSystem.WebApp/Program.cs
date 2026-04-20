using LogSystem.Core.Services.Azure;
using LogSystem.Core.Services.Database;
using LogSystem.WebApp;
using LogSystem.WebApp.BackgroundServices.Persistence;

var configBuilder = new LogSystemConfigurationBuilder();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register configurations
var azureConfig = configBuilder.GetAzureConfig();
var databaseConfig = configBuilder.GetDatabaseConfig();
var logSystemConfig = configBuilder.GetLogSystemConfig();
var persistenceConfig = configBuilder.GetPersistenceBackgroundServiceConfig();

builder.Services.AddSingleton(azureConfig);
builder.Services.AddSingleton(databaseConfig);
builder.Services.AddSingleton(logSystemConfig);
builder.Services.AddSingleton(persistenceConfig);

// Register services
builder.Services.AddSingleton<AzureService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<BatchPersistenceService>();

// Register caches
builder.Services.AddSingleton<LogCollectionCache>();
builder.Services.AddSingleton<LogAttributeCache>();

// Register background service
builder.Services.AddHostedService<PersistenceBackgroundService>();

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

app.Run();
