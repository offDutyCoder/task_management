using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ITeamRepository
{
    Task<IEnumerable<Team>> GetAllAsync();
    Task<Team?> GetByIdAsync(int id);
    Task<Team> CreateAsync(Team team, List<int> memberUserIds);
    Task<Team> UpdateAsync(Team team, List<int> memberUserIds);
    Task DeleteAsync(int id);
    Task<List<int>> GetTeamIdsForUserAsync(int userId);
}
