using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ContextDocumentConfiguration : IEntityTypeConfiguration<ContextDocument>
{
    public void Configure(EntityTypeBuilder<ContextDocument> builder)
    {
        builder.ToTable("ContextDocuments");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.HasOne(document => document.Realm)
            .WithMany(realm => realm.ContextDocuments)
            .HasForeignKey(document => document.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(document => document.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(document => document.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(document => document.FileExtension)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(document => document.ExtractedText)
            .IsRequired();

        builder.Property(document => document.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();
    }
}
