using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Tasks;

public class AddAssigneeRequest
{
    [Required]
    public int UserId { get; set; }
}
