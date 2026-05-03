using TaskManagement.Application.DTOs.Statuses;

namespace TaskManagement.Application.Interfaces;

public interface IStatusService
{
    Task<IEnumerable<StatusResponse>> GetAllAsync();
    Task<StatusResponse> GetByIdAsync(int id);
    Task<StatusResponse> CreateAsync(CreateStatusRequest request);
    Task<StatusResponse> UpdateAsync(int id, UpdateStatusRequest request);
    Task DeleteAsync(int id);
}
