using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class LabelRequestService : ILabelRequestService
{
    private readonly ILabelRequestRepository _requestRepository;
    private readonly ILabelRepository _labelRepository;

    public LabelRequestService(ILabelRequestRepository requestRepository, ILabelRepository labelRepository)
    {
        _requestRepository = requestRepository;
        _labelRepository = labelRepository;
    }

    public async Task<IEnumerable<LabelRequestResponse>> GetAllAsync(LabelRequestStatus? status)
    {
        var requests = await _requestRepository.GetAllAsync(status);
        return requests.Select(MapToResponse);
    }

    public async Task<LabelRequestResponse> CreateAsync(int userId, CreateLabelRequestDto dto)
    {
        if (await _labelRepository.ExistsNameAsync(dto.RequestedName))
            throw new ValidationException($"ラベル名 '{dto.RequestedName}' は既に存在します");

        var request = new LabelRequest
        {
            RequestedByUserId = userId,
            RequestedName = dto.RequestedName,
            Reason = dto.Reason,
            Status = LabelRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var created = await _requestRepository.CreateAsync(request);
        return MapToResponse(created);
    }

    public async Task<LabelRequestResponse> ApproveAsync(int id, string color)
    {
        var request = await _requestRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ラベルリクエスト", id);

        if (request.Status != LabelRequestStatus.Pending)
            throw new ValidationException("このリクエストは既に処理済みです");

        if (await _labelRepository.ExistsNameAsync(request.RequestedName))
            throw new ValidationException($"ラベル名 '{request.RequestedName}' は既に存在します");

        var label = new Label
        {
            Name = request.RequestedName,
            Color = color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        request.Status = LabelRequestStatus.Approved;
        request.UpdatedAt = DateTime.UtcNow;
        var updated = await _requestRepository.ApproveWithLabelAsync(request, label);
        return MapToResponse(updated);
    }

    public async Task<LabelRequestResponse> RejectAsync(int id)
    {
        var request = await _requestRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ラベルリクエスト", id);

        if (request.Status != LabelRequestStatus.Pending)
            throw new ValidationException("このリクエストは既に処理済みです");

        request.Status = LabelRequestStatus.Rejected;
        request.UpdatedAt = DateTime.UtcNow;
        var updated = await _requestRepository.UpdateAsync(request);
        return MapToResponse(updated);
    }

    private static LabelRequestResponse MapToResponse(LabelRequest r) => new()
    {
        Id = r.Id,
        RequestedName = r.RequestedName,
        Reason = r.Reason,
        Status = r.Status,
        RequestedByDisplayName = r.RequestedBy?.DisplayName ?? string.Empty,
        CreatedAt = r.CreatedAt,
    };
}
