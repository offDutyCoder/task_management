using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class LabelRepository : ILabelRepository
{
    private readonly AppDbContext _context;

    public LabelRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Label>> GetAllActiveAsync()
    {
        return await _context.Labels
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<Label?> GetByIdAsync(int id)
    {
        return await _context.Labels.FindAsync(id);
    }

    public async Task<bool> ExistsNameAsync(string name, int? excludeId = null)
    {
        var query = _context.Labels.Where(l => l.Name == name);
        if (excludeId.HasValue)
            query = query.Where(l => l.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<Label> CreateAsync(Label label)
    {
        _context.Labels.Add(label);
        await _context.SaveChangesAsync();
        return label;
    }

    public async Task<Label> UpdateAsync(Label label)
    {
        _context.Labels.Update(label);
        await _context.SaveChangesAsync();
        return label;
    }
}
