using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryUserAttributeConfiguration : IEntityTypeConfiguration<DirectoryUserAttribute>
{
    public void Configure(EntityTypeBuilder<DirectoryUserAttribute> builder)
    {
        builder.ToTable("DirectoryUserAttributes");
        builder.HasKey(a => a.Id);

        // Fotoğraf (thumbnailPhoto) tipindeki alanlar Base64 metin olarak burada saklanır —
        // sabit bir kısa metin sınırı yeterli değil, bu yüzden sınırsız (text) bırakılır.
        builder.Property(a => a.Value);

        builder.HasIndex(a => new { a.DirectoryUserId, a.AttributeMappingId }).IsUnique();

        builder.HasOne<DirectoryAttributeMapping>()
            .WithMany()
            .HasForeignKey(a => a.AttributeMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: DirectoryUserId FK'i zaten Cascade — aynı satırdan ikinci bir cascade yolu
        // (referans verilen kullanıcı silinince) çoklu cascade döngüsüne yol açar.
        builder.HasOne<DirectoryUser>()
            .WithMany()
            .HasForeignKey(a => a.ReferencedDirectoryUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
