using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class TaskShareConfiguration : IEntityTypeConfiguration<TaskShare>
{
    public void Configure(EntityTypeBuilder<TaskShare> builder)
    {
        builder.ToTable("TaskShares");

        builder.HasKey(ts => new { ts.TaskItemId, ts.UserId });

        builder.HasOne(ts => ts.TaskItem)
            .WithMany(t => t.Shares)
            .HasForeignKey(ts => ts.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.User)
            .WithMany()
            .HasForeignKey(ts => ts.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ts => ts.UserId);
    }
}
