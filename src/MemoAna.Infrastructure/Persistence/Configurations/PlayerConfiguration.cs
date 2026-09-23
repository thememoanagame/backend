using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoAna.Infrastructure.Persistence.Configurations;

/// <inheritdoc/>
public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
               .HasMaxLength(36)
               .IsRequired();
        builder.Property(p => p.PeerIdentifier) // MQTT clientId ou UserId real
               .IsRequired();
        builder.Property(p => p.Name)
               .HasMaxLength(50)
               .IsRequired();
        builder.Property(p => p.Score)
               .HasDefaultValue(0);
    }
}