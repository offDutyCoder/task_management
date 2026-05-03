using TaskManagement.Application.DTOs.Labels;

namespace TaskManagement.Application.Interfaces;

public interface ILabelService
{
    Task<IEnumerable<LabelResponse>> GetAllAsync();
    Task<LabelResponse> CreateAsync(CreateLabelRequest request);
    Task<LabelResponse> UpdateAsync(int id, UpdateLabelRequest request);
}
