using Microsoft.Azure.Cosmos;
using TodoApp.Models;
using WebPush;

namespace TodoApp.Services;

public class PushNotificationService
{
    private readonly Container _container;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;
    private const string DocumentId = "push-subscriptions";
    private const string PartitionKey = "push-subscriptions";

    public PushNotificationService(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "TodoApp";
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? "Todos";
        _container = cosmosClient.GetContainer(databaseName, containerName);

        _vapidPublicKey = configuration.GetValue<string>("Vapid:PublicKey")
            ?? throw new InvalidOperationException("Vapid:PublicKey is required");
        _vapidPrivateKey = configuration.GetValue<string>("Vapid:PrivateKey")
            ?? throw new InvalidOperationException("Vapid:PrivateKey is required");
        _vapidSubject = configuration.GetValue<string>("Vapid:Subject")
            ?? throw new InvalidOperationException("Vapid:Subject is required");
    }

    public string GetPublicKey() => _vapidPublicKey;

    public async Task SaveSubscriptionAsync(PushSubscriptionInfo subscription)
    {
        var doc = await GetOrCreateDocumentAsync();
        var existing = doc.Subscriptions.FirstOrDefault(s => s.Endpoint == subscription.Endpoint);
        if (existing == null)
            doc.Subscriptions.Add(subscription);
        else
        {
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
        }
        await SaveDocumentAsync(doc);
    }

    public async Task RemoveSubscriptionAsync(string endpoint)
    {
        var doc = await GetOrCreateDocumentAsync();
        doc.Subscriptions.RemoveAll(s => s.Endpoint == endpoint);
        await SaveDocumentAsync(doc);
    }

    public async Task SendNotificationToAllAsync(string title, string body)
    {
        var doc = await GetOrCreateDocumentAsync();
        if (doc.Subscriptions.Count == 0) return;

        var vapidDetails = new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
        var client = new WebPushClient();

        var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });
        var failedEndpoints = new List<string>();

        foreach (var sub in doc.Subscriptions)
        {
            try
            {
                var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                              || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                failedEndpoints.Add(sub.Endpoint);
            }
        }

        if (failedEndpoints.Count > 0)
        {
            doc.Subscriptions.RemoveAll(s => failedEndpoints.Contains(s.Endpoint));
            await SaveDocumentAsync(doc);
        }
    }

    private async Task<PushSubscriptionDocument> GetOrCreateDocumentAsync()
    {
        try
        {
            var response = await _container.ReadItemAsync<PushSubscriptionDocument>(
                DocumentId, new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new PushSubscriptionDocument();
        }
    }

    private async Task SaveDocumentAsync(PushSubscriptionDocument doc)
    {
        await _container.UpsertItemAsync(doc, new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
    }
}
