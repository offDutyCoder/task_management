using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ILabelRequestRepository
{
    Task<IEnumerable<LabelRequest>> GetAllAsync(LabelRequestStatus? status = null);
    Task<LabelRequest?> GetByIdAsync(int id);
    Task<LabelRequest> CreateAsync(LabelRequest request);
    Task<LabelRequest> UpdateAsync(LabelRequest request);
    Task<LabelRequest> ApproveWithLabelAsync(LabelRequest request, Label label);
}
