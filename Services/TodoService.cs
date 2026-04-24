using pefi.persistence;
using TodoApp.Models;

namespace TodoApp.Services;

public class TodoService
{
    private readonly IDataStore _store;
    private readonly string _databaseName;
    private readonly string _collectionName;
    private const string DocumentId = "todo-list";

    public TodoService(IDataStore store, IConfiguration configuration)
    {
        _store = store;
        _databaseName = configuration.GetValue<string>("MongoDb:DatabaseName") ?? "TodoApp";
        _collectionName = configuration.GetValue<string>("MongoDb:TodosCollection") ?? "todos";
    }

    private async Task<TodoList> GetDocumentAsync()
    {
        var results = await _store.Get<TodoList>(_databaseName, _collectionName, t => t.Id == DocumentId);
        var doc = results.FirstOrDefault();
        if (doc == null)
        {
            doc = new TodoList { Id = DocumentId };
            await _store.Add(_databaseName, _collectionName, doc);
        }
        return doc;
    }

    private async Task SaveDocumentAsync(TodoList doc)
    {
        await _store.Update<TodoList>(_databaseName, _collectionName, t => t.Id == DocumentId, doc);
    }

    public async Task<List<TodoItem>> GetAllAsync()
    {
        var doc = await GetDocumentAsync();
        return doc.Items.OrderBy(t => t.Order).ToList();
    }

    public async Task AddAsync(string title, string description = "")
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var doc = await GetDocumentAsync();
        var maxOrder = doc.Items.Count > 0 ? doc.Items.Max(t => t.Order) : 0;

        doc.Items.Add(new TodoItem
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Order = maxOrder + 1
        });

        await SaveDocumentAsync(doc);
    }

    public async Task SetStatusAsync(Guid id, TodoStatus status)
    {
        var doc = await GetDocumentAsync();
        var item = doc.Items.FirstOrDefault(t => t.Id == id);
        if (item != null)
        {
            item.Status = status;
            await SaveDocumentAsync(doc);
        }
    }

    public async Task ReorderAsync(List<Guid> orderedIds)
    {
        var doc = await GetDocumentAsync();
        var items = orderedIds
            .Select(id => doc.Items.FirstOrDefault(t => t.Id == id))
            .Where(t => t != null)
            .ToList();

        var orderSlots = items.Select(t => t!.Order).OrderBy(o => o).ToList();

        bool changed = false;
        for (int i = 0; i < items.Count && i < orderSlots.Count; i++)
        {
            if (items[i]!.Order != orderSlots[i])
            {
                items[i]!.Order = orderSlots[i];
                changed = true;
            }
        }

        if (changed)
            await SaveDocumentAsync(doc);
    }

    public async Task UpdateDescriptionAsync(Guid id, string description)
    {
        var doc = await GetDocumentAsync();
        var item = doc.Items.FirstOrDefault(t => t.Id == id);
        if (item != null)
        {
            item.Description = description.Trim();
            await SaveDocumentAsync(doc);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await GetDocumentAsync();
        doc.Items.RemoveAll(t => t.Id == id);
        await SaveDocumentAsync(doc);
    }
}
