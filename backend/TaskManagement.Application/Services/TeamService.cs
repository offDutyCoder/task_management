using TaskManagement.Application.DTOs.Teams;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;

    public TeamService(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<IEnumerable<TeamDto>> GetAllAsync()
    {
        var teams = await _teamRepository.GetAllAsync();
        return teams.Select(MapToDto);
    }

    public async Task<TeamDto> GetByIdAsync(int id)
    {
        var team = await _teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("チーム", id);
        return MapToDto(team);
    }

    public async Task<TeamDto> CreateAsync(CreateTeamRequest request, int createdByUserId)
    {
        var team = new Team
        {
            Name = request.Name,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await _teamRepository.CreateAsync(team, request.MemberUserIds);
        return MapToDto(created);
    }

    public async Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request)
    {
        var team = await _teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("チーム", id);

        team.Name = request.Name;
        var updated = await _teamRepository.UpdateAsync(team, request.MemberUserIds);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id, int currentUserId)
    {
        var team = await _teamRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("チーム", id);

        if (team.CreatedByUserId != currentUserId)
            throw new ForbiddenException("このチームを削除する権限がありません");

        await _teamRepository.DeleteAsync(team.Id);
    }

    private static TeamDto MapToDto(Team team) => new()
    {
        Id = team.Id,
        Name = team.Name,
        CreatedByUserId = team.CreatedByUserId,
        Members = team.Members.Select(m => new TeamMemberDto
        {
            UserId = m.UserId,
            DisplayName = m.User?.DisplayName ?? string.Empty,
        }).ToList(),
    };
}
