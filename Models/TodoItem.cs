using System.Text.Json.Serialization;

namespace TodoApp.Models;

public class TodoItem
{
    [JsonPropertyName("id")]
    public string CosmosId
    {
        get => Id.ToString();
        set => Id = Guid.Parse(value);
    }

    [JsonIgnore]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public TodoStatus Status { get; set; } = TodoStatus.Pending;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TodoStatus
{
    Pending,
    InProgress,
    Done,
    Cancelled
}
