using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoAna.Infrastructure.Persistence.Configurations;

/// <inheritdoc/>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
               .IsRequired();
        builder.Property(r => r.ThemeId)
               .IsRequired();
        builder.Property(r => r.CurrentTurnPlayerId);// Opcional, but at begging can be null 
        // Enum GameStatus as string
        builder.Property(r => r.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();
        // Relationshp with Theme (Muitas Salas para Um Tema)
        builder.HasOne(r => r.Theme)
               .WithMany(t => t.Rooms)
               .HasForeignKey(r => r.ThemeId)
               .OnDelete(DeleteBehavior.Restrict); // Do not allow delete a theme if there are rooms associated with it
        // Relationshp with Cards (Also delets the cards if the room is destroyed)
        builder.HasMany(r => r.Cards)
               .WithOne(c => c.Room)
               .HasForeignKey(c => c.RoomId)
               .OnDelete(DeleteBehavior.Cascade);
        // Relationshp with Players
        builder.HasMany(r => r.Players)
               .WithOne(p => p.Room)
               .HasForeignKey(p => p.RoomId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}