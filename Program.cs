using Microsoft.Azure.Cosmos;
using TodoApp.Components;
using TodoApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Cosmos DB setup
var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDb:ConnectionString")
    ?? throw new InvalidOperationException("CosmosDb:ConnectionString is required");

var cosmosClientOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    },
    RequestTimeout = TimeSpan.FromSeconds(5),
    ConnectionMode = ConnectionMode.Gateway,
    LimitToEndpoint = true
};

var cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddSingleton<TodoService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Initialize Cosmos DB with retry for emulator startup
var maxRetries = 30;
Console.WriteLine("Starting Cosmos DB initialization...");
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        Console.WriteLine($"Cosmos DB init attempt {i + 1}/{maxRetries}...");
        await TodoService.InitializeAsync(cosmosClient, app.Configuration);
        Console.WriteLine("Cosmos DB initialized successfully!");
        break;
    }
    catch (Exception ex) when (i < maxRetries - 1)
    {
        Console.WriteLine($"Waiting for Cosmos DB... attempt {i + 1}/{maxRetries}: {ex.Message}");
        await Task.Delay(5000);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
