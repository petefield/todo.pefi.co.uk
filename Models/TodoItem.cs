using System.Text.Json.Serialization;

namespace TodoApp.Models;

public class TodoList
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "todo-list";

    public List<TodoItem> Items { get; set; } = new();
}

public class TodoItem
{
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
