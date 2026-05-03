using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskStatus = TaskManagement.Domain.Entities.TaskStatus;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class TaskStatusConfiguration : IEntityTypeConfiguration<TaskStatus>
{
    public void Configure(EntityTypeBuilder<TaskStatus> builder)
    {
        builder.ToTable("TaskStatuses");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.Name)
            .IsUnique();

        builder.Property(s => s.Color)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(s => s.DisplayOrder)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new TaskStatus { Id = 1, Name = "未着手", Color = "#9E9E9E", DisplayOrder = 1, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TaskStatus { Id = 2, Name = "進行中", Color = "#2196F3", DisplayOrder = 2, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TaskStatus { Id = 3, Name = "レビュー中", Color = "#FF9800", DisplayOrder = 3, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TaskStatus { Id = 4, Name = "保留", Color = "#F44336", DisplayOrder = 4, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new TaskStatus { Id = 5, Name = "完了", Color = "#4CAF50", DisplayOrder = 5, IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
    }
}
