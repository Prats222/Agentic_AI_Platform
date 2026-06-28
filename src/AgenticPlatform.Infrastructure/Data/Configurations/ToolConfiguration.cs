using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ToolConfiguration : IEntityTypeConfiguration<Tool>
{
    public void Configure(EntityTypeBuilder<Tool> builder)
    {
        builder.ToTable("Tools");

        builder.HasKey(tool => tool.Id);

        builder.Property(tool => tool.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(tool => tool.Description)
            .HasMaxLength(1000);

        builder.Property(tool => tool.Category)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tool => tool.InputSchemaJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(tool => tool.EndpointUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.HasIndex(tool => tool.Name)
            .IsUnique();
    }
}
