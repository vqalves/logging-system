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

// Minimal API endpoints for LogCollection CRUD
app.MapGet("/api/log-collections", async (DatabaseService databaseService) =>
{
    var collections = new List<object>();
    await foreach (var collection in databaseService.ListLogCollectionsAsync())
    {
        collections.Add(new
        {
            collection.ID,
            collection.Name,
            collection.TableName,
            collection.LogDurationHours
        });
    }
    return Results.Ok(collections);
});

app.MapPost("/api/log-collections", async (
    LogCollectionRequest request,
    DatabaseService databaseService,
    LogCollectionCache cache) =>
{
    // Basic validation
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required." } }
        });
    }

    if (string.IsNullOrWhiteSpace(request.TableName))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "TableName", new[] { "TableName is required." } }
        });
    }

    if (request.LogDurationHours <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "LogDurationHours", new[] { "LogDurationHours must be greater than 0." } }
        });
    }

    try
    {
        LogCollection logCollection;

        if (request.ID == 0)
        {
            // Create new collection
            logCollection = new LogCollection(request.Name, request.TableName, request.LogDurationHours);
        }
        else
        {
            // Update existing collection - fetch it first
            var existing = await databaseService.GetLogCollectionByNameAsync(request.Name);
            if (existing == null || existing.ID != request.ID)
            {
                return Results.NotFound(new { message = "LogCollection not found." });
            }

            logCollection = new LogCollection(request.Name, existing.TableName, request.LogDurationHours)
            {
                ID = request.ID
            };
        }

        await databaseService.SaveLogCollectionAsync(logCollection);

        // Invalidate cache
        cache.InvalidateCache(logCollection);

        return Results.Ok(new
        {
            logCollection.ID,
            logCollection.Name,
            logCollection.TableName,
            logCollection.LogDurationHours
        });
    }
    catch (ArgumentException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "TableName", new[] { ex.Message } }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.MapDelete("/api/log-collections/{id:long}", async (
    long id,
    DatabaseService databaseService,
    LogCollectionCache cache) =>
{
    try
    {
        // Find the collection by iterating through all collections
        LogCollection? collectionToDelete = null;
        await foreach (var collection in databaseService.ListLogCollectionsAsync())
        {
            if (collection.ID == id)
            {
                collectionToDelete = collection;
                break;
            }
        }

        if (collectionToDelete == null)
        {
            return Results.NotFound(new { message = "LogCollection not found." });
        }

        await databaseService.DeleteLogCollectionAsync(collectionToDelete);

        // Invalidate cache
        cache.InvalidateCache(collectionToDelete);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
});

app.Run();

// Request DTO for LogCollection
record LogCollectionRequest(long ID, string Name, string TableName, long LogDurationHours);
