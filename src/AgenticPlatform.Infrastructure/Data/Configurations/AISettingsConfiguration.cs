using AgenticPlatform.Core.Entities;
using AgenticPlatform.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class AISettingsConfiguration : IEntityTypeConfiguration<AISettings>
{
    public static readonly Guid GlobalSettingsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<AISettings> builder)
    {
        builder.ToTable("AISettings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(settings => settings.Model)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(settings => settings.Temperature)
            .IsRequired();

        builder.Property(settings => settings.MaxTokens)
            .IsRequired();

        builder.Property(settings => settings.TopP)
            .IsRequired();

        builder.Property(settings => settings.SystemPrompt)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(settings => settings.ApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.BaseUrl)
            .HasMaxLength(500);

        builder.HasData(new AISettings
        {
            Id = GlobalSettingsId,
            Provider = AIProvider.Ollama,
            Model = "llama3.1",
            Temperature = 0.2,
            MaxTokens = 2048,
            TopP = 0.9,
            SystemPrompt = "You are a helpful AI agent.",
            BaseUrl = "http://localhost:11434",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
