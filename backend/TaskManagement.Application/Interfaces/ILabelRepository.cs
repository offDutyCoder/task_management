using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ILabelRepository
{
    Task<IEnumerable<Label>> GetAllActiveAsync();
    Task<Label?> GetByIdAsync(int id);
    Task<bool> ExistsNameAsync(string name, int? excludeId = null);
    Task<Label> CreateAsync(Label label);
    Task<Label> UpdateAsync(Label label);
}
