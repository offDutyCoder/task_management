using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(int id, bool includeSubTasks = false)
    {
        var query = _context.TaskItems
            .Include(t => t.Status)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Shares).ThenInclude(s => s.User)
            .Include(t => t.ParentTask).ThenInclude(p => p != null ? p.Status : null!)
            .Include(t => t.ParentTask).ThenInclude(p => p != null ? p.Assignees : null!)
            .Include(t => t.ParentTask).ThenInclude(p => p != null ? p.TaskLabels : null!)
            .Include(t => t.ParentTask).ThenInclude(p => p != null ? p.SubTasks : null!);

        if (includeSubTasks)
        {
            query = query
                .Include(t => t.SubTasks).ThenInclude(s => s.Status)
                .Include(t => t.SubTasks).ThenInclude(s => s.Assignees).ThenInclude(a => a.User)
                .Include(t => t.SubTasks).ThenInclude(s => s.TaskLabels).ThenInclude(tl => tl.Label)
                .Include(t => t.SubTasks).ThenInclude(s => s.SubTasks);
        }

        return await query.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetListAsync(TaskFilterQuery filter, int currentUserId)
    {
        var query = _context.TaskItems
            .Include(t => t.Status)
            .Include(t => t.Assignees).ThenInclude(a => a.User)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Shares)
            .Include(t => t.SubTasks)
            .Where(t => t.CreatedByUserId == currentUserId
                || (t.ParentTaskId == null && t.Shares.Any(s => s.UserId == currentUserId))
                || (t.ParentTaskId != null && _context.TaskShares
                    .Any(ps => ps.TaskItemId == t.ParentTaskId && ps.UserId == currentUserId)));

        if (filter.StatusId.HasValue)
            query = query.Where(t => t.StatusId == filter.StatusId.Value);

        if (filter.AssigneeId.HasValue)
            query = query.Where(t => t.Assignees.Any(a => a.UserId == filter.AssigneeId.Value));

        if (filter.LabelId.HasValue)
            query = query.Where(t => t.TaskLabels.Any(tl => tl.LabelId == filter.LabelId.Value));

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.IsRecurring.HasValue)
            query = query.Where(t => t.IsRecurring == filter.IsRecurring.Value);

        if (filter.DueAfter.HasValue)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= filter.DueAfter.Value);

        if (filter.DueBefore.HasValue)
            query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value <= filter.DueBefore.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(t => t.Title.Contains(search)
                || (t.Description != null && t.Description.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.UpdatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<TaskItem> CreateAsync(TaskItem task, List<int> assigneeIds, List<int> labelIds, List<int> shareUserIds)
    {
        task.Assignees = assigneeIds.Select(userId => new TaskAssignee { UserId = userId, AssignedAt = DateTime.UtcNow }).ToList();
        task.TaskLabels = labelIds.Select(labelId => new TaskLabel { LabelId = labelId }).ToList();
        task.Shares = shareUserIds.Select(userId => new TaskShare { UserId = userId }).ToList();

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task, List<int> assigneeIds, List<int> labelIds, List<int> shareUserIds)
    {
        _context.TaskItems.Update(task);

        var existingAssignees = await _context.TaskAssignees
            .Where(a => a.TaskItemId == task.Id)
            .ToListAsync();
        _context.TaskAssignees.RemoveRange(existingAssignees);

        var existingLabels = await _context.TaskLabels
            .Where(tl => tl.TaskItemId == task.Id)
            .ToListAsync();
        _context.TaskLabels.RemoveRange(existingLabels);

        var existingShares = await _context.TaskShares
            .Where(ts => ts.TaskItemId == task.Id)
            .ToListAsync();
        _context.TaskShares.RemoveRange(existingShares);

        foreach (var userId in assigneeIds)
        {
            _context.TaskAssignees.Add(new TaskAssignee
            {
                TaskItemId = task.Id,
                UserId = userId,
                AssignedAt = DateTime.UtcNow,
            });
        }

        foreach (var labelId in labelIds)
        {
            _context.TaskLabels.Add(new TaskLabel
            {
                TaskItemId = task.Id,
                LabelId = labelId,
            });
        }

        foreach (var userId in shareUserIds)
        {
            _context.TaskShares.Add(new TaskShare
            {
                TaskItemId = task.Id,
                UserId = userId,
            });
        }

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _context.TaskItems.FindAsync(id);
        if (task is not null)
        {
            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<TaskShare>> GetParentSharesAsync(int parentTaskId)
    {
        return await _context.TaskShares
            .Where(ts => ts.TaskItemId == parentTaskId)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TaskItems.AnyAsync(t => t.Id == id);
    }

    public async Task<bool> IsAssigneeAsync(int taskId, int userId)
    {
        return await _context.TaskAssignees
            .AnyAsync(a => a.TaskItemId == taskId && a.UserId == userId);
    }

    public async Task AddAssigneeAsync(int taskId, int userId)
    {
        _context.TaskAssignees.Add(new TaskAssignee
        {
            TaskItemId = taskId,
            UserId = userId,
            AssignedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAssigneeAsync(int taskId, int userId)
    {
        var assignee = await _context.TaskAssignees
            .FirstOrDefaultAsync(a => a.TaskItemId == taskId && a.UserId == userId);
        if (assignee is not null)
        {
            _context.TaskAssignees.Remove(assignee);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsShareUserAsync(int taskId, int userId)
    {
        return await _context.TaskShares
            .AnyAsync(s => s.TaskItemId == taskId && s.UserId == userId);
    }

    public async Task AddShareUserAsync(int taskId, int userId)
    {
        _context.TaskShares.Add(new TaskShare
        {
            TaskItemId = taskId,
            UserId = userId,
        });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveShareUserAsync(int taskId, int userId)
    {
        var share = await _context.TaskShares
            .FirstOrDefaultAsync(s => s.TaskItemId == taskId && s.UserId == userId);
        if (share is not null)
        {
            _context.TaskShares.Remove(share);
            await _context.SaveChangesAsync();
        }
    }
}
