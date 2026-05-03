using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class RecurringTemplateShareConfiguration : IEntityTypeConfiguration<RecurringTemplateShare>
{
    public void Configure(EntityTypeBuilder<RecurringTemplateShare> builder)
    {
        builder.ToTable("RecurringTemplateShares");

        builder.HasKey(s => new { s.RecurringTemplateId, s.UserId });

        builder.HasOne(s => s.Template)
            .WithMany(t => t.Shares)
            .HasForeignKey(s => s.RecurringTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);
    }
}
