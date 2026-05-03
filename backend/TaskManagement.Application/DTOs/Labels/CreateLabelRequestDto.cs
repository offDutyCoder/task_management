using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Labels;

public class CreateLabelRequestDto
{
    [Required(ErrorMessage = "ラベル名は必須です")]
    [MaxLength(50, ErrorMessage = "ラベル名は50文字以内で入力してください")]
    public string RequestedName { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "理由は200文字以内で入力してください")]
    public string? Reason { get; set; }
}
