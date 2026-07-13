using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.ToTable("Tools");

        builder.HasKey(tool => tool.Id);

        builder.Property(tool => tool.RealmId)
            .HasDefaultValue(ApplicationRealms.UserRealmId)
            .IsRequired();

        builder.HasOne(tool => tool.Realm)
            .WithMany(realm => realm.Tools)
            .HasForeignKey(tool => tool.RealmId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(tool => tool.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(tool => tool.Description)
            .HasMaxLength(1000);

        builder.Property(tool => tool.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tool => tool.InputSchemaJson)
            .IsRequired();

        builder.Property(tool => tool.EndpointUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(tool => tool.SecretJson)
            .HasDefaultValue("{}")
            .IsRequired();

        builder.Property(tool => tool.CreatedByDisplayName)
            .HasMaxLength(150);

        builder.HasIndex(tool => new { tool.RealmId, tool.Name })
            .IsUnique();
    }
}
