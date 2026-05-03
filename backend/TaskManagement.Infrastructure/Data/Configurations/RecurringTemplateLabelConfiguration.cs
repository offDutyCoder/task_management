using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class RecurringTemplateLabelConfiguration : IEntityTypeConfiguration<RecurringTemplateLabel>
{
    public void Configure(EntityTypeBuilder<RecurringTemplateLabel> builder)
    {
        builder.ToTable("RecurringTemplateLabels");

        builder.HasKey(l => new { l.RecurringTemplateId, l.LabelId });

        builder.HasOne(l => l.Template)
            .WithMany(t => t.Labels)
            .HasForeignKey(l => l.RecurringTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Label)
            .WithMany()
            .HasForeignKey(l => l.LabelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.LabelId);
    }
}
