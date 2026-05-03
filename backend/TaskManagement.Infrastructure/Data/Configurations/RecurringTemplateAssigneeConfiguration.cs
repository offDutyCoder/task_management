using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class RecurringTemplateAssigneeConfiguration : IEntityTypeConfiguration<RecurringTemplateAssignee>
{
    public void Configure(EntityTypeBuilder<RecurringTemplateAssignee> builder)
    {
        builder.ToTable("RecurringTemplateAssignees");

        builder.HasKey(a => new { a.RecurringTemplateId, a.UserId });

        builder.HasOne(a => a.Template)
            .WithMany(t => t.Assignees)
            .HasForeignKey(a => a.RecurringTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.UserId);
    }
}
