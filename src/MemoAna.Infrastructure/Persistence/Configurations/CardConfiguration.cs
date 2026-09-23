using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoAna.Infrastructure.Persistence.Configurations;
///<inheritdoc/>
public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    ///<inheritdoc/>
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
               .HasMaxLength(36)
               .IsRequired();
        builder.Property(c => c.LiteDbImageId)
               .HasMaxLength(50) // ObjectId do LiteDB salvo como string
               .IsRequired();
        // Valores booleanos
        builder.Property(c => c.IsFlipped)
               .HasDefaultValue(false);
        builder.Property(c => c.IsMatched)
               .HasDefaultValue(false);
        // Índice de Performance Crítico para a lógica autoritativa
        // Garante busca super rápida (O(1) ou O(log n)) quando o jogador clica na carta e o Worker consulta
        // Também impede de existir, por acidente, duas cartas na mesma posição na mesma sala.
        builder.HasIndex(c => new { c.RoomId, c.Position })
               .IsUnique()
               .HasDatabaseName("IX_UQ_Cards_Room_Position");
    }
}