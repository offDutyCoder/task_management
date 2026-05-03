using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITaskRepository _taskRepository;

    public DashboardService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int currentUserId, bool includeRecurring = false)
    {
        bool? recurringFilter = includeRecurring ? null : false;

        var myTasksFilter = new TaskFilterQuery
        {
            AssigneeId = currentUserId,
            IsRecurring = recurringFilter,
            PageSize = 100,
        };

        var teamTasksFilter = new TaskFilterQuery
        {
            IsRecurring = recurringFilter,
            PageSize = 100,
        };

        var (myTaskItems, _) = await _taskRepository.GetListAsync(myTasksFilter, currentUserId);
        var (teamTaskItems, _) = await _taskRepository.GetListAsync(teamTasksFilter, currentUserId);

        return new DashboardResponse
        {
            MyTasks = myTaskItems.Select(MapToListItem).ToList(),
            TeamTasks = teamTaskItems.Select(MapToListItem).ToList(),
        };
    }

    private static TaskListItem MapToListItem(Domain.Entities.TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Status = new StatusDto
        {
            Id = task.Status.Id,
            Name = task.Status.Name,
            Color = task.Status.Color,
        },
        Priority = task.Priority,
        DueDate = task.DueDate,
        Assignees = task.Assignees.Select(a => new AssigneeDto
        {
            Id = a.UserId,
            DisplayName = a.User?.DisplayName ?? string.Empty,
        }).ToList(),
        Labels = task.TaskLabels.Select(tl => new LabelDto
        {
            Id = tl.LabelId,
            Name = tl.Label?.Name ?? string.Empty,
            Color = tl.Label?.Color ?? string.Empty,
        }).ToList(),
        SubTaskCount = task.SubTasks.Count,
        IsRecurring = task.IsRecurring,
    };
}
