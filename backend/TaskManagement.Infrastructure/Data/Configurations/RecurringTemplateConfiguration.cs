using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class RecurringTemplateConfiguration : IEntityTypeConfiguration<RecurringTemplate>
{
    public void Configure(EntityTypeBuilder<RecurringTemplate> builder)
    {
        builder.ToTable("RecurringTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(4000);

        builder.Property(t => t.Priority)
            .IsRequired();

        builder.Property(t => t.Frequency)
            .IsRequired();

        builder.Property(t => t.WeekDays)
            .HasMaxLength(50);

        builder.Property(t => t.GenerationTime)
            .IsRequired();

        builder.Property(t => t.DefaultStatusId)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedByUserId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .IsRequired();

        builder.HasOne(t => t.DefaultStatus)
            .WithMany()
            .HasForeignKey(t => t.DefaultStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.GenerationTime);
    }
}
