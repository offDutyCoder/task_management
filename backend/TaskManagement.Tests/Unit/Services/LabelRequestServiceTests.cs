using Moq;
using TaskManagement.Application.DTOs.Labels;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Tests.Unit.Services;

public class LabelRequestServiceTests
{
    private readonly Mock<ILabelRequestRepository> _mockRequestRepo;
    private readonly Mock<ILabelRepository> _mockLabelRepo;
    private readonly LabelRequestService _sut;

    public LabelRequestServiceTests()
    {
        _mockRequestRepo = new Mock<ILabelRequestRepository>();
        _mockLabelRepo = new Mock<ILabelRepository>();
        _sut = new LabelRequestService(_mockRequestRepo.Object, _mockLabelRepo.Object);
    }

    private static User MakeUser() => new()
    {
        Id = 2, DisplayName = "田中", LoginId = "tanaka",
        PasswordHash = "", IsActive = true,
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task ApproveAsync_WhenPending_ShouldCallApproveWithLabelAndReturnApprovedStatus()
    {
        // Given
        var request = new LabelRequest
        {
            Id = 1, RequestedByUserId = 2, RequestedName = "緊急",
            Status = LabelRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            RequestedBy = MakeUser(),
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _mockLabelRepo.Setup(r => r.ExistsNameAsync("緊急", It.IsAny<int?>())).ReturnsAsync(false);
        _mockRequestRepo
            .Setup(r => r.ApproveWithLabelAsync(It.IsAny<LabelRequest>(), It.IsAny<Label>()))
            .ReturnsAsync((LabelRequest req, Label _) => req);

        // When
        var result = await _sut.ApproveAsync(1, "#F44336");

        // Then
        Assert.Equal(LabelRequestStatus.Approved, result.Status);
        _mockRequestRepo.Verify(
            r => r.ApproveWithLabelAsync(
                It.Is<LabelRequest>(req => req.Status == LabelRequestStatus.Approved),
                It.Is<Label>(l => l.Name == "緊急" && l.Color == "#F44336")),
            Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WhenAlreadyApproved_ShouldThrowValidationException()
    {
        // Given
        var request = new LabelRequest
        {
            Id = 1, RequestedName = "緊急",
            Status = LabelRequestStatus.Approved,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            RequestedBy = MakeUser(),
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() => _sut.ApproveAsync(1, "#F44336"));
    }

    [Fact]
    public async Task ApproveAsync_WhenNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockRequestRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((LabelRequest?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ApproveAsync(99, "#F44336"));
    }

    [Fact]
    public async Task RejectAsync_WhenPending_ShouldReturnRejectedStatus()
    {
        // Given
        var request = new LabelRequest
        {
            Id = 1, RequestedName = "緊急",
            Status = LabelRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            RequestedBy = MakeUser(),
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _mockRequestRepo.Setup(r => r.UpdateAsync(It.IsAny<LabelRequest>())).ReturnsAsync((LabelRequest r) => r);

        // When
        var result = await _sut.RejectAsync(1);

        // Then
        Assert.Equal(LabelRequestStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task RejectAsync_WhenAlreadyRejected_ShouldThrowValidationException()
    {
        // Given
        var request = new LabelRequest
        {
            Id = 1, RequestedName = "緊急",
            Status = LabelRequestStatus.Rejected,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            RequestedBy = MakeUser(),
        };

        _mockRequestRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() => _sut.RejectAsync(1));
    }

    [Fact]
    public async Task RejectAsync_WhenNotFound_ShouldThrowNotFoundException()
    {
        // Given
        _mockRequestRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((LabelRequest?)null);

        // When / Then
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RejectAsync(99));
    }

    [Fact]
    public async Task CreateAsync_WhenLabelNameAlreadyExists_ShouldThrowValidationException()
    {
        // Given
        var dto = new CreateLabelRequestDto { RequestedName = "開発", Reason = "テスト" };
        _mockLabelRepo.Setup(r => r.ExistsNameAsync("開発", It.IsAny<int?>())).ReturnsAsync(true);

        // When / Then
        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(1, dto));
    }
}
