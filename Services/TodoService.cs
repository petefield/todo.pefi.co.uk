using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using TodoApp.Models;

namespace TodoApp.Services;

public class TodoService
{
    private readonly Container _container;

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
    }

    public async Task<List<TodoItem>> GetAllAsync()
    {
        var query = _container.GetItemLinqQueryable<TodoItem>()
            .OrderBy(t => t.Order)
            .ToFeedIterator();

        var items = new List<TodoItem>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            items.AddRange(response);
        }
        return items;
    }

    public async Task AddAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        var all = await GetAllAsync();
        var maxOrder = all.Count > 0 ? all.Max(t => t.Order) : 0;

        var item = new TodoItem
        {
            Title = title.Trim(),
            Order = maxOrder + 1
        };

        await _container.CreateItemAsync(item, new PartitionKey(item.Id.ToString()));
    }

    public async Task SetStatusAsync(Guid id, TodoStatus status)
    {
        var item = await GetItemAsync(id);
        if (item != null)
        {
            item.Status = status;
            await _container.UpsertItemAsync(item, new PartitionKey(id.ToString()));
        }
    }

    public async Task ReorderAsync(List<Guid> orderedIds)
    {
        var items = new List<TodoItem>();
        foreach (var id in orderedIds)
        {
            var item = await GetItemAsync(id);
            if (item != null) items.Add(item);
        }

        var orderSlots = items.Select(t => t.Order).OrderBy(o => o).ToList();

        for (int i = 0; i < items.Count && i < orderSlots.Count; i++)
        {
            if (items[i].Order != orderSlots[i])
            {
                items[i].Order = orderSlots[i];
                await _container.UpsertItemAsync(items[i], new PartitionKey(items[i].Id.ToString()));
            }
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _container.DeleteItemAsync<TodoItem>(id.ToString(), new PartitionKey(id.ToString()));
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound) { }
    }

    private async Task<TodoItem?> GetItemAsync(Guid id)
    {
        try
        {
            var response = await _container.ReadItemAsync<TodoItem>(id.ToString(), new PartitionKey(id.ToString()));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
