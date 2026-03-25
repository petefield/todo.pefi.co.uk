using System.Text.Json.Serialization;

namespace TodoApp.Models;

public class PushSubscriptionDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "push-subscriptions";

    public List<PushSubscriptionInfo> Subscriptions { get; set; } = new();
}

public class PushSubscriptionInfo
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}
