using Atlas.Markets;
using Atlas.Onboarding.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Onboarding.Infrastructure.Persistence;

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<Application>
{
    public void Configure(EntityTypeBuilder<Application> builder)
    {
        builder.ToTable("Applications");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => ApplicationId.Parse(value))
            .HasMaxLength(35)
            .IsUnicode(false);

        builder.Property(a => a.Market)
            .HasConversion(market => market.Value, value => MarketCode.Parse(value))
            .HasMaxLength(2)
            .IsUnicode(false);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.RejectionReason).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.ApplicantTokenHash).HasMaxLength(64).IsUnicode(false);
        builder.Property(a => a.TermsVersion).HasMaxLength(20);
        builder.Property(a => a.IdentityVerificationReference).HasMaxLength(100);
        builder.Property(a => a.ScreeningReference).HasMaxLength(100);
        builder.Property(a => a.ReviewedBy).HasMaxLength(100);
        builder.Property(a => a.ReviewReason).HasMaxLength(1000);
        builder.Property(a => a.ActivatedBy).HasMaxLength(100);
        builder.Property(a => a.ActivationBranchId).HasMaxLength(50);

        // Optimistic concurrency: with several replicas, two writers on the same row cannot both win.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(a => a.Status);

        builder.OwnsOne(a => a.Details, details =>
        {
            details.Property(d => d.FirstName).HasColumnName("FirstName").HasMaxLength(100);
            details.Property(d => d.LastName).HasColumnName("LastName").HasMaxLength(100);
            details.Property(d => d.DateOfBirth).HasColumnName("DateOfBirth");
            details.Property(d => d.Email).HasColumnName("Email").HasMaxLength(254);
            details.Property(d => d.Phone).HasColumnName("Phone").HasMaxLength(20);
        });

        builder.OwnsOne(a => a.Identifier, identifier =>
        {
            identifier.Property(i => i.Type).HasColumnName("IdentifierType").HasConversion<string>().HasMaxLength(20);
            identifier.Property(i => i.Value).HasColumnName("IdentifierValue").HasMaxLength(32);
        });

        builder.OwnsMany(a => a.Documents, documents =>
        {
            documents.ToTable("ApplicationDocuments");
            documents.WithOwner().HasForeignKey("ApplicationId");
            documents.Property<ApplicationId>("ApplicationId")
                .HasConversion(id => id.Value, value => ApplicationId.Parse(value))
                .HasMaxLength(35)
                .IsUnicode(false);
            documents.HasKey("ApplicationId", nameof(ApplicationDocument.Type)); // one document per type
            documents.Property(d => d.Type).HasConversion<string>().HasMaxLength(30);
            documents.Property(d => d.StorageKey).HasMaxLength(200);
            documents.Property(d => d.ContentType).HasMaxLength(100);
        });

        builder.Navigation(a => a.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(a => a.DomainEvents);
        builder.Ignore(a => a.IsEditable);
    }
}
