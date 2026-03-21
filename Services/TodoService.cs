using Microsoft.Azure.Cosmos;
using TodoApp.Models;

namespace TodoApp.Services;

public class TodoService
{
    private readonly Container _container;
    private const string DocumentId = "todo-list";
    private const string PartitionKey = "todo-list";

    public TodoService(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "TodoApp";
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? "Todos";
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public static async Task InitializeAsync(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "TodoApp";
        var containerName = configuration.GetValue<string>("CosmosDb:ContainerName") ?? "Todos";
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

        // Ensure the single document exists
        var container = cosmosClient.GetContainer(databaseName, containerName);
        try
        {
            await container.ReadItemAsync<TodoList>(DocumentId, new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await container.CreateItemAsync(new TodoList(), new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
        }
    }

    private async Task<TodoList> GetDocumentAsync()
    {
        var response = await _container.ReadItemAsync<TodoList>(DocumentId, new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
        return response.Resource;
    }

    private async Task SaveDocumentAsync(TodoList doc)
    {
        await _container.UpsertItemAsync(doc, new Microsoft.Azure.Cosmos.PartitionKey(PartitionKey));
    }

    public async Task<List<TodoItem>> GetAllAsync()
    {
        var doc = await GetDocumentAsync();
        return doc.Items.OrderBy(t => t.Order).ToList();
    }

    public async Task AddAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var doc = await GetDocumentAsync();
        var maxOrder = doc.Items.Count > 0 ? doc.Items.Max(t => t.Order) : 0;

        doc.Items.Add(new TodoItem
        {
            Title = title.Trim(),
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

    public async Task DeleteAsync(Guid id)
    {
        var doc = await GetDocumentAsync();
        doc.Items.RemoveAll(t => t.Id == id);
        await SaveDocumentAsync(doc);
    }
}
