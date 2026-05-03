using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class TaskLabelConfiguration : IEntityTypeConfiguration<TaskLabel>
{
    public void Configure(EntityTypeBuilder<TaskLabel> builder)
    {
        builder.ToTable("TaskLabels");

        builder.HasKey(tl => new { tl.TaskItemId, tl.LabelId });

        builder.HasOne(tl => tl.TaskItem)
            .WithMany(t => t.TaskLabels)
            .HasForeignKey(tl => tl.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tl => tl.Label)
            .WithMany()
            .HasForeignKey(tl => tl.LabelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
