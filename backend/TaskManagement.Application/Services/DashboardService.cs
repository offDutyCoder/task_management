using TaskManagement.Application.DTOs.Dashboard;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITeamRepository _teamRepository;

    public DashboardService(ITaskRepository taskRepository, ITeamRepository teamRepository)
    {
        _taskRepository = taskRepository;
        _teamRepository = teamRepository;
    }

    public async Task<DashboardResponse> GetDashboardAsync(int currentUserId, bool includeRecurring = false)
    {
        bool? recurringFilter = includeRecurring ? null : false;

        var myTeamIds = await _teamRepository.GetTeamIdsForUserAsync(currentUserId);

        var allFilter = new TaskFilterQuery
        {
            IsRecurring = recurringFilter,
            PageSize = 200,
        };

        var (allItems, _) = await _taskRepository.GetListAsync(allFilter, currentUserId);
        var allTasks = allItems.ToList();

        var myTasks = allTasks
            .Where(t =>
                t.Assignees.Any(a => a.UserId == currentUserId) ||
                (t.AssigneeTeamId.HasValue && myTeamIds.Contains(t.AssigneeTeamId.Value)))
            .Select(MapToListItem)
            .ToList();

        var teamTaskGroups = allTasks
            .GroupBy(t =>
            {
                if (t.AssigneeTeamId.HasValue)
                    return $"team:{t.AssigneeTeamId.Value}";
                var firstAssignee = t.Assignees.FirstOrDefault();
                return firstAssignee != null ? $"user:{firstAssignee.UserId}" : "user:0";
            })
            .Where(g => g.Key != "user:0")
            .Select(g =>
            {
                var sample = g.First();
                if (sample.AssigneeTeamId.HasValue)
                {
                    return new AssigneeGroup
                    {
                        AssigneeType = "team",
                        AssigneeId = sample.AssigneeTeamId.Value,
                        AssigneeName = sample.AssigneeTeam?.Name ?? $"チームID:{sample.AssigneeTeamId.Value}",
                        Tasks = g.Select(MapToListItem).ToList(),
                    };
                }
                else
                {
                    var assignee = sample.Assignees.First();
                    return new AssigneeGroup
                    {
                        AssigneeType = "user",
                        AssigneeId = assignee.UserId,
                        AssigneeName = assignee.User?.DisplayName ?? string.Empty,
                        Tasks = g.Select(MapToListItem).ToList(),
                    };
                }
            })
            .OrderBy(g => g.AssigneeName)
            .ToList();

        return new DashboardResponse
        {
            MyTasks = myTasks,
            TeamTaskGroups = teamTaskGroups,
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
