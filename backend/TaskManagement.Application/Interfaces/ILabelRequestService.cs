using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface ILabelRequestService
{
    Task<IEnumerable<LabelRequestResponse>> GetAllAsync(LabelRequestStatus? status);
    Task<LabelRequestResponse> CreateAsync(int userId, CreateLabelRequestDto request);
    Task<LabelRequestResponse> ApproveAsync(int id, string color);
    Task<LabelRequestResponse> RejectAsync(int id);
}
