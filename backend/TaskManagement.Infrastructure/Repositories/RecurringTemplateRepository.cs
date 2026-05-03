using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class RecurringTemplateRepository : IRecurringTemplateRepository
{
    private readonly AppDbContext _context;

    public RecurringTemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecurringTemplate>> GetAllAsync()
    {
        return await _context.RecurringTemplates
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.Labels).ThenInclude(l => l.Label)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<RecurringTemplate?> GetByIdAsync(int id)
    {
        return await _context.RecurringTemplates
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.Labels).ThenInclude(l => l.Label)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<RecurringTemplate>> GetActiveForGenerationAsync(TimeSpan currentTime)
    {
        var lowerBound = currentTime - TimeSpan.FromMinutes(1);
        var upperBound = currentTime + TimeSpan.FromMinutes(1);

        return await _context.RecurringTemplates
            .Include(t => t.Assignees)
            .Include(t => t.Labels)
            .Include(t => t.Shares)
            .Where(t => t.IsActive && t.GenerationTime >= lowerBound && t.GenerationTime <= upperBound)
            .ToListAsync();
    }

    public async Task<RecurringTemplate> CreateAsync(RecurringTemplate template)
    {
        _context.RecurringTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<RecurringTemplate> UpdateAsync(RecurringTemplate template)
    {
        _context.RecurringTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<bool> ExistsTitleAsync(string title, int? excludeId = null)
    {
        var query = _context.RecurringTemplates.Where(t => t.Title == title);
        if (excludeId.HasValue)
            query = query.Where(t => t.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> HasGeneratedTodayAsync(int templateId, DateOnly today)
    {
        var todayStart = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await _context.TaskItems
            .AnyAsync(t => t.RecurringTemplateId == templateId
                && t.CreatedAt >= todayStart
                && t.CreatedAt <= todayEnd);
    }
}
