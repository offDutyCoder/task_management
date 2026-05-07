using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Teams;

public class UpdateTeamRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public List<int> MemberUserIds { get; set; } = [];
}
