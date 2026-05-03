using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Application.DTOs.Statuses;

public class CreateStatusRequest
{
    [Required(ErrorMessage = "名前は必須です")]
    [MaxLength(50, ErrorMessage = "名前は50文字以内で入力してください")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "色は必須です")]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "#から始まる6桁のHEXコードを入力してください")]
    public string Color { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "表示順は1以上の値を入力してください")]
    public int DisplayOrder { get; set; }
}
