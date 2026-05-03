using TaskManagement.Application.DTOs.Statuses;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskStatus = TaskManagement.Domain.Entities.TaskStatus;

namespace TaskManagement.Application.Services;

public class StatusService : IStatusService
{
    private readonly IStatusRepository _statusRepository;

    public StatusService(IStatusRepository statusRepository)
    {
        _statusRepository = statusRepository;
    }

    public async Task<IEnumerable<StatusResponse>> GetAllAsync()
    {
        var statuses = await _statusRepository.GetAllAsync();
        return statuses.Select(MapToResponse);
    }

    public async Task<StatusResponse> GetByIdAsync(int id)
    {
        var status = await _statusRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ステータス", id);
        return MapToResponse(status);
    }

    public async Task<StatusResponse> CreateAsync(CreateStatusRequest request)
    {
        if (await _statusRepository.ExistsNameAsync(request.Name))
            throw new ValidationException($"ステータス名 '{request.Name}' は既に使用されています");

        var now = DateTime.UtcNow;
        var status = new TaskStatus
        {
            Name = request.Name,
            Color = request.Color,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _statusRepository.CreateAsync(status);
        return MapToResponse(created);
    }

    public async Task<StatusResponse> UpdateAsync(int id, UpdateStatusRequest request)
    {
        var status = await _statusRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ステータス", id);

        if (await _statusRepository.ExistsNameAsync(request.Name, excludeId: id))
            throw new ValidationException($"ステータス名 '{request.Name}' は既に使用されています");

        status.Name = request.Name;
        status.Color = request.Color;
        status.DisplayOrder = request.DisplayOrder;
        status.IsActive = request.IsActive;
        status.UpdatedAt = DateTime.UtcNow;

        var updated = await _statusRepository.UpdateAsync(status);
        return MapToResponse(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var status = await _statusRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ステータス", id);

        if (await _statusRepository.ExistsUsedByTaskAsync(id))
            throw new ConflictException("このステータスは使用中のタスクがあるため削除できません");

        status.IsActive = false;
        status.UpdatedAt = DateTime.UtcNow;
        await _statusRepository.UpdateAsync(status);
    }

    private static StatusResponse MapToResponse(TaskStatus status) => new()
    {
        Id = status.Id,
        Name = status.Name,
        Color = status.Color,
        DisplayOrder = status.DisplayOrder,
        IsActive = status.IsActive,
        CreatedAt = status.CreatedAt,
    };
}
