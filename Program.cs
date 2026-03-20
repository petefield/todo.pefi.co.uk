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
    }
};

// For the emulator, bypass SSL validation
if (cosmosConnectionString.Contains("localhost") || cosmosConnectionString.Contains("cosmosdb"))
{
    cosmosClientOptions.HttpClientFactory = () =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler);
    };
    cosmosClientOptions.ConnectionMode = ConnectionMode.Gateway;
}

var cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);
builder.Services.AddSingleton(cosmosClient);
builder.Services.AddSingleton<TodoService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Initialize Cosmos DB database and container
await TodoService.InitializeAsync(cosmosClient, app.Configuration);

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
