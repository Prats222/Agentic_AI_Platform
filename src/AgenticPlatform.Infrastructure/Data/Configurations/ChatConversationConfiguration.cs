using AgenticPlatform.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AgenticPlatform.Infrastructure.Data.Configurations;

public sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("ChatConversations");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(120).IsRequired();
        builder.Property(item => item.Provider).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Model).HasMaxLength(150).IsRequired();
        builder.HasIndex(item => new { item.UserId, item.UpdatedAt });
        builder.HasMany(item => item.Messages)
            .WithOne(item => item.Conversation)
            .HasForeignKey(item => item.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
