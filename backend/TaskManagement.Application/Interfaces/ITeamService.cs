using TaskManagement.Application.DTOs.Teams;

namespace TaskManagement.Application.Interfaces;

public interface ITeamService
{
    Task<IEnumerable<TeamDto>> GetAllAsync();
    Task<TeamDto> GetByIdAsync(int id);
    Task<TeamDto> CreateAsync(CreateTeamRequest request, int createdByUserId);
    Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request);
    Task DeleteAsync(int id, int currentUserId);
}
