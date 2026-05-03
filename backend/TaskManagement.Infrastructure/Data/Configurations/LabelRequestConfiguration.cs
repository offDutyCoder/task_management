using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class LabelRequestConfiguration : IEntityTypeConfiguration<LabelRequest>
{
    public void Configure(EntityTypeBuilder<LabelRequest> builder)
    {
        builder.ToTable("LabelRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RequestedName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Reason)
            .HasMaxLength(200);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        builder.HasOne(r => r.RequestedBy)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Status);
    }
}
