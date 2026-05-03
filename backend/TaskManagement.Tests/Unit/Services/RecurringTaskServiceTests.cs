using Moq;
using TaskManagement.Application.DTOs.RecurringTemplates;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class RecurringTaskServiceTests
{
    private readonly Mock<IRecurringTemplateRepository> _mockTemplateRepo;
    private readonly Mock<ITaskRepository> _mockTaskRepo;
    private readonly RecurringTaskService _sut;

    public RecurringTaskServiceTests()
    {
        _mockTemplateRepo = new Mock<IRecurringTemplateRepository>();
        _mockTaskRepo = new Mock<ITaskRepository>();
        _sut = new RecurringTaskService(_mockTemplateRepo.Object, _mockTaskRepo.Object);
    }

    private static RecurringTemplate CreateTemplate(
        int id = 1,
        RecurringFrequency frequency = RecurringFrequency.Daily,
        bool excludeWeekends = false,
        string? weekDays = null,
        int? dayOfMonth = null,
        string generationTime = "08:00") => new()
    {
        Id = id,
        Title = "テストテンプレート",
        Priority = TaskPriority.Medium,
        Frequency = frequency,
        WeekDays = weekDays,
        DayOfMonth = dayOfMonth,
        ExcludeWeekends = excludeWeekends,
        GenerationTime = TimeSpan.Parse(generationTime),
        DefaultStatusId = 1,
        IsActive = true,
        CreatedByUserId = 1,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        Assignees = [],
        Labels = [],
        Shares = [],
    };

    [Fact]
    public async Task GenerateTasksAsync_WhenDaily_ShouldGenerateTask()
    {
        // Given
        var today = new DateOnly(2026, 5, 5);
        var template = CreateTemplate(frequency: RecurringFrequency.Daily);
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);
        _mockTemplateRepo.Setup(r => r.HasGeneratedTodayAsync(1, today)).ReturnsAsync(false);
        _mockTaskRepo
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new TaskItem { Id = 100 });

        // When
        await _sut.GenerateTasksAsync(today, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.Is<TaskItem>(t => t.IsRecurring && t.RecurringTemplateId == 1),
            It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenExcludeWeekendsAndSaturday_ShouldSkip()
    {
        // Given: 土曜日
        var saturday = new DateOnly(2026, 5, 9);
        var template = CreateTemplate(excludeWeekends: true);
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);

        // When
        await _sut.GenerateTasksAsync(saturday, TimeSpan.Parse("08:00"));

        // Then: タスク生成されない
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenExcludeWeekendsAndWeekday_ShouldGenerate()
    {
        // Given: 月曜日（2026-05-11）
        var monday = new DateOnly(2026, 5, 11);
        var template = CreateTemplate(excludeWeekends: true);
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);
        _mockTemplateRepo.Setup(r => r.HasGeneratedTodayAsync(1, monday)).ReturnsAsync(false);
        _mockTaskRepo
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new TaskItem { Id = 101 });

        // When
        await _sut.GenerateTasksAsync(monday, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenAlreadyGeneratedToday_ShouldSkip()
    {
        // Given
        var today = new DateOnly(2026, 5, 5);
        var template = CreateTemplate();
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);
        _mockTemplateRepo.Setup(r => r.HasGeneratedTodayAsync(1, today)).ReturnsAsync(true);

        // When
        await _sut.GenerateTasksAsync(today, TimeSpan.Parse("08:00"));

        // Then: 重複生成しない
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenWeeklyAndMatchingDay_ShouldGenerate()
    {
        // Given: 月曜日（DayOfWeek = 1）
        var monday = new DateOnly(2026, 5, 11);
        var template = CreateTemplate(frequency: RecurringFrequency.Weekly, weekDays: "1");
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);
        _mockTemplateRepo.Setup(r => r.HasGeneratedTodayAsync(1, monday)).ReturnsAsync(false);
        _mockTaskRepo
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new TaskItem { Id = 102 });

        // When
        await _sut.GenerateTasksAsync(monday, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenWeeklyAndNonMatchingDay_ShouldSkip()
    {
        // Given: 火曜日（DayOfWeek = 2）、テンプレートは月曜のみ
        var tuesday = new DateOnly(2026, 5, 12);
        var template = CreateTemplate(frequency: RecurringFrequency.Weekly, weekDays: "1");
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);

        // When
        await _sut.GenerateTasksAsync(tuesday, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenMonthlyAndMatchingDay_ShouldGenerate()
    {
        // Given: 毎月5日、今日は5日
        var fifth = new DateOnly(2026, 5, 5);
        var template = CreateTemplate(frequency: RecurringFrequency.Monthly, dayOfMonth: 5);
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);
        _mockTemplateRepo.Setup(r => r.HasGeneratedTodayAsync(1, fifth)).ReturnsAsync(false);
        _mockTaskRepo
            .Setup(r => r.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new TaskItem { Id = 103 });

        // When
        await _sut.GenerateTasksAsync(fifth, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTasksAsync_WhenMonthlyAndNonMatchingDay_ShouldSkip()
    {
        // Given: 毎月5日、今日は6日
        var sixth = new DateOnly(2026, 5, 6);
        var template = CreateTemplate(frequency: RecurringFrequency.Monthly, dayOfMonth: 5);
        _mockTemplateRepo
            .Setup(r => r.GetActiveForGenerationAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync([template]);

        // When
        await _sut.GenerateTasksAsync(sixth, TimeSpan.Parse("08:00"));

        // Then
        _mockTaskRepo.Verify(r => r.CreateAsync(
            It.IsAny<TaskItem>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenTemplateNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockTemplateRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((RecurringTemplate?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeactivateAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WhenTitleExists_ShouldThrowValidationException()
    {
        // Given
        _mockTemplateRepo.Setup(r => r.ExistsTitleAsync("既存", It.IsAny<int?>())).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() =>
            _sut.CreateAsync(new CreateRecurringTemplateRequest { Title = "既存", GenerationTime = "08:00", DefaultStatusId = 1 }, 1));
    }
}
