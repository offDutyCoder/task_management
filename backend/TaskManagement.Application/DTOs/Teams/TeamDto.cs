namespace TaskManagement.Application.DTOs.Teams;

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public List<TeamMemberDto> Members { get; set; } = [];
}

public class TeamMemberDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
