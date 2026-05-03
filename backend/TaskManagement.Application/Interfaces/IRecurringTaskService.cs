using TaskManagement.Application.DTOs.RecurringTemplates;

namespace TaskManagement.Application.Interfaces;

public interface IRecurringTaskService
{
    Task<IEnumerable<RecurringTemplateResponse>> GetAllAsync();
    Task<RecurringTemplateResponse> GetByIdAsync(int id);
    Task<RecurringTemplateResponse> CreateAsync(CreateRecurringTemplateRequest request, int createdByUserId);
    Task<RecurringTemplateResponse> UpdateAsync(int id, UpdateRecurringTemplateRequest request);
    Task DeactivateAsync(int id);
    Task GenerateTasksAsync(DateOnly today, TimeSpan currentTime);
}
