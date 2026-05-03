using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("TaskAssignees");

        builder.HasKey(ta => new { ta.TaskItemId, ta.UserId });

        builder.Property(ta => ta.AssignedAt)
            .IsRequired();

        builder.HasOne(ta => ta.TaskItem)
            .WithMany(t => t.Assignees)
            .HasForeignKey(ta => ta.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ta => ta.User)
            .WithMany()
            .HasForeignKey(ta => ta.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ta => ta.UserId);
    }
}
