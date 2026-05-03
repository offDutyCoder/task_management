using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TaskManagement.Infrastructure.Jobs;

public class HangfireJobRegistrationService : IHostedService
{
    private readonly ILogger<HangfireJobRegistrationService> _logger;

    public HangfireJobRegistrationService(ILogger<HangfireJobRegistrationService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RecurringJob.AddOrUpdate<OverdueNotificationJob>(
                "overdue-notifications",
                job => job.ExecuteAsync(),
                "5 0 * * *");  // 毎日 00:05

            RecurringJob.AddOrUpdate<RecurringTaskJob>(
                "recurring-task-generation",
                job => job.ExecuteAsync(),
                Cron.Minutely);  // 毎分
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hangfire の RecurringJob 登録に失敗しました。Hangfire ストレージが利用できない可能性があります。");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
