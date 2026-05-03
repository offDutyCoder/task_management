using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IRecurringTemplateRepository
{
    Task<IEnumerable<RecurringTemplate>> GetAllAsync();
    Task<RecurringTemplate?> GetByIdAsync(int id);
    Task<IEnumerable<RecurringTemplate>> GetActiveForGenerationAsync(TimeSpan currentTime);
    Task<RecurringTemplate> CreateAsync(RecurringTemplate template);
    Task<RecurringTemplate> UpdateAsync(RecurringTemplate template);
    Task<bool> ExistsTitleAsync(string title, int? excludeId = null);
    Task<bool> HasGeneratedTodayAsync(int templateId, DateOnly today);
}
