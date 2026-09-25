using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoAna.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasMaxLength(36).IsRequired();
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ThemeId).IsRequired();
        builder.Property(r => r.Difficulty).IsRequired();
        builder.Property(r => r.Mode).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.TimeLimitSeconds).IsRequired();
        builder.Property(r => r.StartedAt);
        builder.Property(r => r.RequirePassword).IsRequired();
        builder.Property(r => r.JoinPasswordHash).HasMaxLength(512);

        builder.Property(r => r.CurrentTurnPlayerId);
        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(r => r.CurrentTurnPlayer)
            .WithMany()
            .HasForeignKey(r => r.CurrentTurnPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Theme)
            .WithMany(t => t.Rooms)
            .HasForeignKey(r => r.ThemeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Cards)
            .WithOne(c => c.Room)
            .HasForeignKey(c => c.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Players)
            .WithOne(p => p.Room)
            .HasForeignKey(p => p.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
