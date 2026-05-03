using TaskManagement.Application.DTOs.RecurringTemplates;
using TaskManagement.Application.DTOs.Tasks;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

public class RecurringTaskService : IRecurringTaskService
{
    private readonly IRecurringTemplateRepository _templateRepository;
    private readonly ITaskRepository _taskRepository;

    public RecurringTaskService(IRecurringTemplateRepository templateRepository, ITaskRepository taskRepository)
    {
        _templateRepository = templateRepository;
        _taskRepository = taskRepository;
    }

    public async Task<IEnumerable<RecurringTemplateResponse>> GetAllAsync()
    {
        var templates = await _templateRepository.GetAllAsync();
        return templates.Select(MapToResponse);
    }

    public async Task<RecurringTemplateResponse> GetByIdAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("定期タスクテンプレート", id);
        return MapToResponse(template);
    }

    public async Task<RecurringTemplateResponse> CreateAsync(CreateRecurringTemplateRequest request, int createdByUserId)
    {
        if (await _templateRepository.ExistsTitleAsync(request.Title))
            throw new ValidationException($"テンプレート名 '{request.Title}' は既に使用されています");

        var now = DateTime.UtcNow;
        var template = new RecurringTemplate
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Frequency = request.Frequency,
            WeekDays = SerializeWeekDays(request.WeekDays),
            DayOfMonth = request.DayOfMonth,
            ExcludeWeekends = request.ExcludeWeekends,
            ExcludeHolidays = false,
            GenerationTime = ParseGenerationTime(request.GenerationTime),
            DefaultStatusId = request.DefaultStatusId,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now,
            Assignees = request.AssigneeIds.Select(uid => new RecurringTemplateAssignee { UserId = uid }).ToList(),
            Labels = request.LabelIds.Select(lid => new RecurringTemplateLabel { LabelId = lid }).ToList(),
            Shares = request.ShareUserIds.Select(uid => new RecurringTemplateShare { UserId = uid }).ToList(),
        };

        var created = await _templateRepository.CreateAsync(template);
        var result = await _templateRepository.GetByIdAsync(created.Id)
            ?? throw new InvalidOperationException("作成したテンプレートが見つかりません");
        return MapToResponse(result);
    }

    public async Task<RecurringTemplateResponse> UpdateAsync(int id, UpdateRecurringTemplateRequest request)
    {
        var template = await _templateRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("定期タスクテンプレート", id);

        if (await _templateRepository.ExistsTitleAsync(request.Title, excludeId: id))
            throw new ValidationException($"テンプレート名 '{request.Title}' は既に使用されています");

        template.Title = request.Title;
        template.Description = request.Description;
        template.Priority = request.Priority;
        template.Frequency = request.Frequency;
        template.WeekDays = SerializeWeekDays(request.WeekDays);
        template.DayOfMonth = request.DayOfMonth;
        template.ExcludeWeekends = request.ExcludeWeekends;
        template.GenerationTime = ParseGenerationTime(request.GenerationTime);
        template.DefaultStatusId = request.DefaultStatusId;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        template.Assignees.Clear();
        foreach (var uid in request.AssigneeIds)
            template.Assignees.Add(new RecurringTemplateAssignee { RecurringTemplateId = id, UserId = uid });

        template.Labels.Clear();
        foreach (var lid in request.LabelIds)
            template.Labels.Add(new RecurringTemplateLabel { RecurringTemplateId = id, LabelId = lid });

        template.Shares.Clear();
        foreach (var uid in request.ShareUserIds)
            template.Shares.Add(new RecurringTemplateShare { RecurringTemplateId = id, UserId = uid });

        await _templateRepository.UpdateAsync(template);
        var result = await _templateRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("更新したテンプレートが見つかりません");
        return MapToResponse(result);
    }

    public async Task DeactivateAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("定期タスクテンプレート", id);

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepository.UpdateAsync(template);
    }

    public async Task GenerateTasksAsync(DateOnly today, TimeSpan currentTime)
    {
        var templates = await _templateRepository.GetActiveForGenerationAsync(currentTime);

        foreach (var template in templates)
        {
            if (template.ExcludeWeekends && IsWeekend(today))
                continue;

            if (!ShouldGenerateToday(template, today))
                continue;

            if (await _templateRepository.HasGeneratedTodayAsync(template.Id, today))
                continue;

            var now = DateTime.UtcNow;
            var taskItem = new TaskItem
            {
                Title = template.Title,
                Description = template.Description,
                StatusId = template.DefaultStatusId,
                Priority = template.Priority,
                DueDate = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                IsRecurring = true,
                RecurringTemplateId = template.Id,
                CreatedByUserId = template.CreatedByUserId,
                CreatedAt = now,
                UpdatedAt = now,
            };

            await _taskRepository.CreateAsync(
                taskItem,
                template.Assignees.Select(a => a.UserId).ToList(),
                template.Labels.Select(l => l.LabelId).ToList(),
                template.Shares.Select(s => s.UserId).ToList());
        }
    }

    private static bool IsWeekend(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    private static bool ShouldGenerateToday(RecurringTemplate template, DateOnly today)
    {
        return template.Frequency switch
        {
            RecurringFrequency.Daily => true,
            RecurringFrequency.Weekly => ShouldGenerateWeekly(template, today),
            RecurringFrequency.Monthly => template.DayOfMonth == today.Day,
            _ => false,
        };
    }

    private static bool ShouldGenerateWeekly(RecurringTemplate template, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(template.WeekDays))
            return false;

        var weekDays = ParseWeekDays(template.WeekDays);
        return weekDays.Contains((int)today.DayOfWeek);
    }

    private static string? SerializeWeekDays(int[]? weekDays)
    {
        if (weekDays == null || weekDays.Length == 0)
            return null;
        return string.Join(",", weekDays);
    }

    private static int[] ParseWeekDays(string weekDays)
    {
        return weekDays.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();
    }

    private static TimeSpan ParseGenerationTime(string generationTime)
    {
        if (!TimeSpan.TryParseExact(generationTime, @"hh\:mm", null, out var ts))
            throw new ValidationException($"生成時刻の形式が不正です: '{generationTime}'。HH:mm 形式で入力してください");
        return ts;
    }

    private static RecurringTemplateResponse MapToResponse(RecurringTemplate template) => new()
    {
        Id = template.Id,
        Title = template.Title,
        Description = template.Description,
        Priority = template.Priority.ToString(),
        Frequency = template.Frequency.ToString(),
        WeekDays = string.IsNullOrWhiteSpace(template.WeekDays)
            ? null
            : ParseWeekDays(template.WeekDays),
        DayOfMonth = template.DayOfMonth,
        ExcludeWeekends = template.ExcludeWeekends,
        GenerationTime = template.GenerationTime.ToString(@"hh\:mm"),
        DefaultStatusId = template.DefaultStatusId,
        IsActive = template.IsActive,
        Assignees = template.Assignees
            .Select(a => new AssigneeDto { Id = a.UserId, DisplayName = a.User?.DisplayName ?? "" })
            .ToArray(),
        Labels = template.Labels
            .Select(l => new LabelDto { Id = l.LabelId, Name = l.Label?.Name ?? "", Color = l.Label?.Color ?? "" })
            .ToArray(),
        ShareUsers = template.Shares
            .Select(s => new AssigneeDto { Id = s.UserId, DisplayName = s.User?.DisplayName ?? "" })
            .ToArray(),
        CreatedAt = template.CreatedAt,
    };
}
