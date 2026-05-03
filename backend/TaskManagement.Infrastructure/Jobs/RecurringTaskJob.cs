using Hangfire;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Jobs;

public class RecurringTaskJob
{
    private readonly IRecurringTaskService _recurringTaskService;
    private readonly ILogger<RecurringTaskJob> _logger;

    public RecurringTaskJob(IRecurringTaskService recurringTaskService, ILogger<RecurringTaskJob> logger)
    {
        _recurringTaskService = recurringTaskService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        // サーバーはJST（UTC+9）で動作することを前提とする
        // GenerationTime はJST基準で設定・比較する
        var now = DateTime.Now;
        _logger.LogInformation("定期タスク生成ジョブ開始: {Time}", now);

        await _recurringTaskService.GenerateTasksAsync(DateOnly.FromDateTime(now), now.TimeOfDay);

        _logger.LogInformation("定期タスク生成ジョブ完了: {Time}", now);
    }
}
