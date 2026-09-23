using MemoAna.Domain.Game;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace MemoAna.Infrastructure.Persistence.Configurations;

/// <inheritdoc/>
public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder.ToTable("Themes");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
               .HasMaxLength(36)
               .IsRequired();
        builder.Property(t => t.Name)
               .HasMaxLength(100)
               .IsRequired();
        builder.HasIndex(t => t.Name)
               .IsUnique()
               .HasDatabaseName("IX_UQ_Themes_Name");
        builder.Property(t => t.ThumbnailId)
               .HasMaxLength(50)
               .IsRequired();
        builder.HasIndex(t => t.ThumbnailId)
               .IsUnique()
               .HasDatabaseName("IX_UQ_Themes_ThumbnailId");
        builder.Property(t => t.Description)
               .HasMaxLength(1024);
        builder.Ignore(t => t.CardsBasePath);
        builder.Ignore(t => t.ThemeBasePath);
        builder.Ignore(t => t.ThumbnailPath);
        builder.Property(p => p.Cards)
               .IsRequired()
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions)default!),
                   v => JsonSerializer.Deserialize<List<(string, string)>>(v, (JsonSerializerOptions)default!)
               )
               .Metadata
               .SetValueComparer(new ValueComparer<List<(string, string)>>(
                   (c1, c2) => c1.SequenceEqual(c2),
                   c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                   c => c.ToList()
               ));
    }
}