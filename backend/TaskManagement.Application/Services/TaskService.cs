using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;


namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;

    public TaskService(ITaskRepository taskRepository, IUserRepository userRepository, INotificationService notificationService)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
    }

    public async Task<TaskListResponse> GetTasksAsync(TaskFilterQuery filter, int currentUserId)
    {
        var (items, totalCount) = await _taskRepository.GetListAsync(filter, currentUserId);

        return new TaskListResponse
        {
            Items = items.Select(MapToListItem).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
        };
    }

    public async Task<TaskDetailResponse> GetTaskByIdAsync(int id, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(id, includeSubTasks: true)
            ?? throw new NotFoundException("タスク", id);

        await EnsureReadAccessAsync(task, currentUserId);

        return MapToDetailResponse(task);
    }

    public async Task<TaskDetailResponse> CreateTaskAsync(CreateTaskRequest request, int currentUserId)
    {
        if (request.ParentTaskId.HasValue)
        {
            var parent = await _taskRepository.GetByIdAsync(request.ParentTaskId.Value)
                ?? throw new NotFoundException("親タスク", request.ParentTaskId.Value);

            await EnsureReadAccessAsync(parent, currentUserId);
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            StatusId = request.StatusId,
            Priority = request.Priority,
            DueDate = request.DueDate,
            ParentTaskId = request.ParentTaskId,
            IsRecurring = false,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        List<int> shareUserIds;
        if (request.ParentTaskId.HasValue)
        {
            // サブタスクは親の共有設定を継承するため、自身の TaskShare レコードは持たない
            shareUserIds = [];
        }
        else
        {
            shareUserIds = request.ShareUserIds.Count == 0
                ? [currentUserId]
                : request.ShareUserIds.Contains(currentUserId)
                    ? request.ShareUserIds
                    : [.. request.ShareUserIds, currentUserId];
        }

        var created = await _taskRepository.CreateAsync(task, request.AssigneeIds, request.LabelIds, shareUserIds);

        if (request.AssigneeIds.Count > 0)
            await _notificationService.NotifyAssignedAsync(created.Id, created.Title, request.AssigneeIds);

        var withSubTasks = await _taskRepository.GetByIdAsync(created.Id, includeSubTasks: true)
            ?? throw new InvalidOperationException("作成したタスクの取得に失敗しました");

        return MapToDetailResponse(withSubTasks);
    }

    public async Task<TaskDetailResponse> UpdateTaskAsync(int id, UpdateTaskRequest request, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("タスク", id);

        await EnsureReadAccessAsync(task, currentUserId);

        var previousAssigneeIds = task.Assignees.Select(a => a.UserId).ToHashSet();

        task.Title = request.Title;
        task.Description = request.Description;
        task.StatusId = request.StatusId;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        var shareUserIds = request.ShareUserIds.Count == 0
            ? [currentUserId]
            : request.ShareUserIds.Contains(currentUserId)
                ? request.ShareUserIds
                : [.. request.ShareUserIds, currentUserId];

        await _taskRepository.UpdateAsync(task, request.AssigneeIds, request.LabelIds, shareUserIds);

        var newAssigneeIds = request.AssigneeIds.Where(uid => !previousAssigneeIds.Contains(uid)).ToList();
        if (newAssigneeIds.Count > 0)
            await _notificationService.NotifyAssignedAsync(id, task.Title, newAssigneeIds);

        var updated = await _taskRepository.GetByIdAsync(id, includeSubTasks: true)
            ?? throw new InvalidOperationException("更新したタスクの取得に失敗しました");

        return MapToDetailResponse(updated);
    }

    public async Task DeleteTaskAsync(int id, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("タスク", id);

        if (task.CreatedByUserId != currentUserId)
            throw new ForbiddenException("このタスクを削除する権限がありません");

        await _taskRepository.DeleteAsync(id);
    }

    public async Task<AssigneeDto> AddAssigneeAsync(int taskId, int userId, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException("タスク", taskId);

        await EnsureReadAccessAsync(task, currentUserId);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("ユーザー", userId);

        if (!user.IsActive)
            throw new NotFoundException("ユーザー", userId);

        if (await _taskRepository.IsAssigneeAsync(taskId, userId))
            throw new ConflictException($"ユーザーID {userId} は既に担当者として割り当てられています");

        await _taskRepository.AddAssigneeAsync(taskId, userId);
        await _notificationService.NotifyAssignedAsync(taskId, task.Title, [userId]);

        return new AssigneeDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
        };
    }

    public async Task RemoveAssigneeAsync(int taskId, int userId, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException("タスク", taskId);

        await EnsureReadAccessAsync(task, currentUserId);

        if (!await _taskRepository.IsAssigneeAsync(taskId, userId))
            throw new NotFoundException("担当者", userId);

        await _taskRepository.RemoveAssigneeAsync(taskId, userId);
    }

    public async Task<AssigneeDto> AddShareUserAsync(int taskId, int userId, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException("タスク", taskId);

        await EnsureReadAccessAsync(task, currentUserId);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("ユーザー", userId);

        if (!user.IsActive)
            throw new NotFoundException("ユーザー", userId);

        if (await _taskRepository.IsShareUserAsync(taskId, userId))
            throw new ConflictException($"ユーザーID {userId} は既に共有ユーザーとして設定されています");

        await _taskRepository.AddShareUserAsync(taskId, userId);

        return new AssigneeDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
        };
    }

    public async Task RemoveShareUserAsync(int taskId, int userId, int currentUserId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId)
            ?? throw new NotFoundException("タスク", taskId);

        await EnsureReadAccessAsync(task, currentUserId);

        if (task.CreatedByUserId == userId)
            throw new ForbiddenException("タスクの作成者を共有ユーザーから削除することはできません");

        if (!await _taskRepository.IsShareUserAsync(taskId, userId))
            throw new NotFoundException("共有ユーザー", userId);

        await _taskRepository.RemoveShareUserAsync(taskId, userId);
    }

    private async Task EnsureReadAccessAsync(TaskItem task, int currentUserId)
    {
        if (task.CreatedByUserId == currentUserId)
            return;

        IEnumerable<TaskShare> shares;
        if (task.ParentTaskId.HasValue)
            shares = await _taskRepository.GetParentSharesAsync(task.ParentTaskId.Value);
        else
            shares = task.Shares;

        if (!shares.Any(s => s.UserId == currentUserId))
            throw new ForbiddenException("このタスクを閲覧する権限がありません");
    }

    private static TaskListItem MapToListItem(TaskItem task) => new()
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

    private static TaskDetailResponse MapToDetailResponse(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        Status = new StatusDto
        {
            Id = task.Status.Id,
            Name = task.Status.Name,
            Color = task.Status.Color,
        },
        Priority = task.Priority,
        DueDate = task.DueDate,
        ParentTask = task.ParentTask is not null ? MapToListItem(task.ParentTask) : null,
        SubTasks = task.SubTasks.Select(MapToListItem).ToList(),
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
        ShareUsers = task.Shares.Select(s => new AssigneeDto
        {
            Id = s.UserId,
            DisplayName = s.User?.DisplayName ?? string.Empty,
        }).ToList(),
        CreatedByUserId = task.CreatedByUserId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
    };
}
