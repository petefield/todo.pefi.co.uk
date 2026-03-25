using TodoApp.Models;

namespace TodoApp.Services;

public class DailyNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyNotificationService> _logger;
    private readonly TimeOnly _notificationTime;
    private readonly TimeZoneInfo _timeZone;

    public DailyNotificationService(
        IServiceProvider serviceProvider,
        ILogger<DailyNotificationService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var timeString = configuration.GetValue<string>("Notifications:DailyTime") ?? "09:00";
        _notificationTime = TimeOnly.Parse(timeString);

        var timeZoneId = configuration.GetValue<string>("Notifications:TimeZone") ?? TimeZoneInfo.Local.Id;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextNotification();
            _logger.LogInformation("Next daily notification scheduled in {Delay}", delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested) break;

            await SendDailyNotificationAsync();
        }
    }

    private TimeSpan CalculateDelayUntilNextNotification()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
        var todayNotification = now.Date.Add(_notificationTime.ToTimeSpan());

        var next = todayNotification > now
            ? todayNotification
            : todayNotification.AddDays(1);

        return next - now;
    }

    private async Task SendDailyNotificationAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var todoService = scope.ServiceProvider.GetRequiredService<TodoService>();
            var pushService = scope.ServiceProvider.GetRequiredService<PushNotificationService>();

            var items = await todoService.GetAllAsync();
            var remaining = items.Count(i =>
                i.Status == TodoStatus.Pending || i.Status == TodoStatus.InProgress);

            if (remaining == 0) return;

            var body = remaining == 1
                ? "You have 1 to do remaining."
                : $"You have {remaining} to dos remaining.";

            await pushService.SendNotificationToAllAsync("📝 To Do's", body);
            _logger.LogInformation("Daily notification sent: {Body}", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send daily notification");
        }
    }
}
