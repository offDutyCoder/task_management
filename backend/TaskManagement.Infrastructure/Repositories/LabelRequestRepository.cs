using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class LabelRequestRepository : ILabelRequestRepository
{
    private readonly AppDbContext _context;

    public LabelRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LabelRequest>> GetAllAsync(LabelRequestStatus? status = null)
    {
        var query = _context.LabelRequests
            .Include(r => r.RequestedBy)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    public async Task<LabelRequest?> GetByIdAsync(int id)
    {
        return await _context.LabelRequests
            .Include(r => r.RequestedBy)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<LabelRequest> CreateAsync(LabelRequest request)
    {
        _context.LabelRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<LabelRequest> UpdateAsync(LabelRequest request)
    {
        _context.LabelRequests.Update(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<LabelRequest> ApproveWithLabelAsync(LabelRequest request, Label label)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Labels.Add(label);
        _context.LabelRequests.Update(request);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return request;
    }
}
