using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _context;

    public TeamRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Team>> GetAllAsync()
    {
        return await _context.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Team?> GetByIdAsync(int id)
    {
        return await _context.Teams
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Team> CreateAsync(Team team, List<int> memberUserIds)
    {
        team.Members = memberUserIds.Select(uid => new TeamMember { UserId = uid }).ToList();
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        return await GetByIdAsync(team.Id) ?? team;
    }

    public async Task<Team> UpdateAsync(Team team, List<int> memberUserIds)
    {
        _context.Teams.Update(team);

        var existing = await _context.TeamMembers.Where(tm => tm.TeamId == team.Id).ToListAsync();
        _context.TeamMembers.RemoveRange(existing);

        foreach (var userId in memberUserIds)
        {
            _context.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = userId });
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(team.Id) ?? team;
    }

    public async Task DeleteAsync(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team != null)
        {
            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<int>> GetTeamIdsForUserAsync(int userId)
    {
        return await _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();
    }
}
