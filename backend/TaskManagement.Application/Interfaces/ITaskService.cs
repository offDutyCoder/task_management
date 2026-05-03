using TaskManagement.Application.DTOs.Tasks;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskListResponse> GetTasksAsync(TaskFilterQuery filter, int currentUserId);
    Task<TaskDetailResponse> GetTaskByIdAsync(int id, int currentUserId);
    Task<TaskDetailResponse> CreateTaskAsync(CreateTaskRequest request, int currentUserId);
    Task<TaskDetailResponse> UpdateTaskAsync(int id, UpdateTaskRequest request, int currentUserId);
    Task DeleteTaskAsync(int id, int currentUserId);
    Task<AssigneeDto> AddAssigneeAsync(int taskId, int userId, int currentUserId);
    Task RemoveAssigneeAsync(int taskId, int userId, int currentUserId);
    Task<AssigneeDto> AddShareUserAsync(int taskId, int userId, int currentUserId);
    Task RemoveShareUserAsync(int taskId, int userId, int currentUserId);
}
