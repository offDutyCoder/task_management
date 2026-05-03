using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id, bool includeSubTasks = false);
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetListAsync(TaskFilterQuery filter, int currentUserId);
    Task<TaskItem> CreateAsync(TaskItem task, List<int> assigneeIds, List<int> labelIds, List<int> shareUserIds);
    Task<TaskItem> UpdateAsync(TaskItem task, List<int> assigneeIds, List<int> labelIds, List<int> shareUserIds);
    Task DeleteAsync(int id);
    Task<IEnumerable<TaskShare>> GetParentSharesAsync(int parentTaskId);
    Task<bool> ExistsAsync(int id);
    Task<bool> IsAssigneeAsync(int taskId, int userId);
    Task AddAssigneeAsync(int taskId, int userId);
    Task RemoveAssigneeAsync(int taskId, int userId);
    Task<bool> IsShareUserAsync(int taskId, int userId);
    Task AddShareUserAsync(int taskId, int userId);
    Task RemoveShareUserAsync(int taskId, int userId);
}
