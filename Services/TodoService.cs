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

    public void Delete(Guid id)
    {
        lock (_lock)
        {
            _items.RemoveAll(t => t.Id == id);
            Save();
        }
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            _items = JsonSerializer.Deserialize<List<TodoItem>>(json) ?? [];
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
