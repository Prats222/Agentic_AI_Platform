using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Role).HasMaxLength(20).IsRequired();
        builder.Property(item => item.Content).HasMaxLength(8000).IsRequired();
        builder.HasIndex(item => new { item.ConversationId, item.CreatedAt });
    }
}
