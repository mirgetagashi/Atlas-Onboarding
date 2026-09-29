using Atlas.Onboarding.Infrastructure.Auditing;
using Atlas.Onboarding.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Onboarding.Infrastructure.Persistence;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasMaxLength(200);
        builder.Property(m => m.LastError).HasMaxLength(2000);
        builder.HasIndex(m => new { m.PublishedAt, m.OccurredAt });
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApplicationId).HasMaxLength(35).IsUnicode(false);
        builder.Property(a => a.ActorType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ActorId).HasMaxLength(100);
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Purpose).HasMaxLength(100);
        builder.HasIndex(a => a.ApplicationId);
    }
}
