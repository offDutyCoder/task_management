using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepository;

    public LabelService(ILabelRepository labelRepository)
    {
        _labelRepository = labelRepository;
    }

    public async Task<IEnumerable<LabelResponse>> GetAllAsync()
    {
        var labels = await _labelRepository.GetAllActiveAsync();
        return labels.Select(MapToResponse);
    }

    public async Task<LabelResponse> CreateAsync(CreateLabelRequest request)
    {
        if (await _labelRepository.ExistsNameAsync(request.Name))
            throw new ValidationException($"ラベル名 '{request.Name}' は既に使用されています");

        var label = new Label
        {
            Name = request.Name,
            Color = request.Color,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var created = await _labelRepository.CreateAsync(label);
        return MapToResponse(created);
    }

    public async Task<LabelResponse> UpdateAsync(int id, UpdateLabelRequest request)
    {
        var label = await _labelRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("ラベル", id);

        if (await _labelRepository.ExistsNameAsync(request.Name, excludeId: id))
            throw new ValidationException($"ラベル名 '{request.Name}' は既に使用されています");

        label.Name = request.Name;
        label.Color = request.Color;
        label.IsActive = request.IsActive;
        label.UpdatedAt = DateTime.UtcNow;

        var updated = await _labelRepository.UpdateAsync(label);
        return MapToResponse(updated);
    }

    private static LabelResponse MapToResponse(Label label) => new()
    {
        Id = label.Id,
        Name = label.Name,
        Color = label.Color,
        IsActive = label.IsActive,
    };
}
