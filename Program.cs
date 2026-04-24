using Microsoft.AspNetCore.Mvc;
using pefi.persistence;
using TodoApp.Components;
using TodoApp.Models;
using TodoApp.Services;

var builder = WebApplication.CreateBuilder(args);

// MongoDB setup
var mongoConnectionString = builder.Configuration.GetValue<string>("MongoDb:ConnectionString")
    ?? throw new InvalidOperationException("MongoDb:ConnectionString is required");

builder.Services.AddPeFiPersistance(options =>
{
    options.ConnectionString = mongoConnectionString;
});
builder.Services.AddSingleton<TodoService>();
builder.Services.AddSingleton<PushNotificationService>();
builder.Services.AddHostedService<DailyNotificationService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

// Push notification endpoints
app.MapGet("/api/push/vapid-public-key", (PushNotificationService pushService) =>
    Results.Ok(pushService.GetPublicKey()));

app.MapPost("/api/push/subscribe", async (PushSubscriptionInfo subscription, PushNotificationService pushService) =>
{
    await pushService.SaveSubscriptionAsync(subscription);
    return Results.Ok();
});

app.MapDelete("/api/push/subscribe", async ([FromBody] PushSubscriptionInfo subscription, [FromServices] PushNotificationService pushService) =>
{
    await pushService.RemoveSubscriptionAsync(subscription.Endpoint);
    return Results.Ok();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
