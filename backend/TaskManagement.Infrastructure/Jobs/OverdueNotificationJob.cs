using Hangfire;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Jobs;

public class OverdueNotificationJob
{
    private readonly INotificationService _notificationService;

    public OverdueNotificationJob(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        await _notificationService.GenerateOverdueNotificationsAsync();
    }
}
