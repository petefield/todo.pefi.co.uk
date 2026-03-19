using System.Text.Json;
using TodoApp.Models;

namespace TodoApp.Services;

public class TodoService
{
    private readonly string _filePath;
    private readonly Lock _lock = new();
    private List<TodoItem> _items = [];

    public TodoService(IConfiguration configuration)
    {
        var dataDir = configuration.GetValue<string>("DataDirectory") ?? "/data";
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "todos.json");
        Load();
    }

    public List<TodoItem> GetAll()
    {
        lock (_lock)
        {
            return _items.OrderBy(t => t.Order).ToList();
        }
    }

    public void Add(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        lock (_lock)
        {
            var maxOrder = _items.Count > 0 ? _items.Max(t => t.Order) : 0;
            _items.Add(new TodoItem { Title = title.Trim(), Order = maxOrder + 1 });
            Save();
        }
    }

    public void SetStatus(Guid id, TodoStatus status)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(t => t.Id == id);
            if (item != null)
            {
                item.Status = status;
                Save();
            }
        }
    }

    public void MoveUp(Guid id)
    {
        lock (_lock)
        {
            var sorted = _items.OrderBy(t => t.Order).ToList();
            var index = sorted.FindIndex(t => t.Id == id);
            if (index > 0)
            {
                (sorted[index].Order, sorted[index - 1].Order) = (sorted[index - 1].Order, sorted[index].Order);
                Save();
            }
        }
    }

    public void MoveDown(Guid id)
    {
        lock (_lock)
        {
            var sorted = _items.OrderBy(t => t.Order).ToList();
            var index = sorted.FindIndex(t => t.Id == id);
            if (index >= 0 && index < sorted.Count - 1)
            {
                (sorted[index].Order, sorted[index + 1].Order) = (sorted[index + 1].Order, sorted[index].Order);
                Save();
            }
        }
    }

    public void Reorder(List<Guid> orderedIds)
    {
        lock (_lock)
        {
            // Get the current order values of the items being reordered
            var orderSlots = orderedIds
                .Select(id => _items.FirstOrDefault(t => t.Id == id))
                .Where(t => t != null)
                .Select(t => t!.Order)
                .OrderBy(o => o)
                .ToList();

            // Assign the sorted order slots to the items in their new sequence
            for (int i = 0; i < orderedIds.Count && i < orderSlots.Count; i++)
            {
                var item = _items.FirstOrDefault(t => t.Id == orderedIds[i]);
                if (item != null)
                    item.Order = orderSlots[i];
            }
            Save();
        }
    }

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            _items.RemoveAll(t => t.Id == id);
            Save();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new TodoStatusConverter() }
    };

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            _items = JsonSerializer.Deserialize<List<TodoItem>>(json, JsonOptions) ?? [];
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_items, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}

// Handles backward compatibility: "Active" in JSON → Pending enum
public class TodoStatusConverter : System.Text.Json.Serialization.JsonConverter<TodoStatus>
{
    public override TodoStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase))
            return TodoStatus.Pending;
        return Enum.TryParse<TodoStatus>(value, true, out var status) ? status : TodoStatus.Pending;
    }

    public override void Write(Utf8JsonWriter writer, TodoStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
