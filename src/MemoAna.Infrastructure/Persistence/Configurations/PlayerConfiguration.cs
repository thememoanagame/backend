using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoAna.Infrastructure.Persistence.Configurations;

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasMaxLength(36).IsRequired();
        builder.Property(p => p.PeerIdentifier).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Score).HasDefaultValue(0);
        builder.Property(p => p.MqttUsername).HasMaxLength(128).IsRequired();
        builder.Property(p => p.MqttPasswordHash).HasMaxLength(512).IsRequired();

        builder.HasIndex(p => p.MqttUsername)
            .IsUnique()
            .HasDatabaseName("IX_UQ_Players_MqttUsername");
    }
}
