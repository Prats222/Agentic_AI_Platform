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

        builder.Property(settings => settings.GeminiApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.OpenRouterApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.GroqApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.CerebrasApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.DeepSeekApiKey)
            .HasMaxLength(4000);

        builder.Property(settings => settings.BaseUrl)
            .HasMaxLength(500);

        builder.HasData(new AISettings
        {
            Id = GlobalSettingsId,
            Provider = AIProvider.Gemini,
            Model = "gemini-2.5-flash",
            Temperature = 0.2,
            MaxTokens = 2048,
            TopP = 0.9,
            SystemPrompt = "You are a helpful AI agent.",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
            CreatedAt = new DateTimeOffset(2026, 6, 28, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
