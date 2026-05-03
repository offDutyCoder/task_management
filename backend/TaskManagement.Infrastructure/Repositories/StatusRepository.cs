using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Data;
using TaskStatus = TaskManagement.Domain.Entities.TaskStatus;

namespace TaskManagement.Infrastructure.Repositories;

public class StatusRepository : IStatusRepository
{
    private readonly AppDbContext _context;

    public StatusRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskStatus>> GetAllAsync()
    {
        return await _context.TaskStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
    }

    public async Task<TaskStatus?> GetByIdAsync(int id)
    {
        return await _context.TaskStatuses.FindAsync(id);
    }

    public async Task<TaskStatus> CreateAsync(TaskStatus status)
    {
        _context.TaskStatuses.Add(status);
        await _context.SaveChangesAsync();
        return status;
    }

    public async Task<TaskStatus> UpdateAsync(TaskStatus status)
    {
        _context.TaskStatuses.Update(status);
        await _context.SaveChangesAsync();
        return status;
    }

    public async Task<bool> ExistsUsedByTaskAsync(int statusId)
    {
        // TaskItem エンティティ実装時に実際の参照チェックを追加する
        await Task.CompletedTask;
        return false;
    }

    public async Task<bool> ExistsNameAsync(string name, int? excludeId = null)
    {
        var query = _context.TaskStatuses.Where(s => s.Name == name);
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync();
    }
}
