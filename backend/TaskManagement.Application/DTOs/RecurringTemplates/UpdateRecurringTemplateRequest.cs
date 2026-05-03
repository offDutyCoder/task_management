namespace TaskManagement.Application.DTOs.RecurringTemplates;

public class UpdateRecurringTemplateRequest : CreateRecurringTemplateRequest
{
    public bool IsActive { get; set; }
}
