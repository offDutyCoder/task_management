using TaskStatus = TaskManagement.Domain.Entities.TaskStatus;

namespace TaskManagement.Application.Interfaces;

public interface IStatusRepository
{
    Task<IEnumerable<TaskStatus>> GetAllAsync();
    Task<TaskStatus?> GetByIdAsync(int id);
    Task<TaskStatus> CreateAsync(TaskStatus status);
    Task<TaskStatus> UpdateAsync(TaskStatus status);
    Task<bool> ExistsUsedByTaskAsync(int statusId);
    Task<bool> ExistsNameAsync(string name, int? excludeId = null);
}
