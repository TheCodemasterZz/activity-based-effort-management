using EforTakip.Domain.Directories;
using EforTakip.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class UserAttributeConfiguration : IEntityTypeConfiguration<UserAttribute>
{
    public void Configure(EntityTypeBuilder<UserAttribute> builder)
    {
        builder.ToTable("UserAttributes");
        builder.HasKey(a => a.Id);

        // Fotoğraf (thumbnailPhoto) tipindeki alanlar Base64 metin olarak burada saklanır —
        // sabit bir kısa metin sınırı yeterli değil, bu yüzden sınırsız (text) bırakılır.
        builder.Property(a => a.Value);

        builder.HasIndex(a => new { a.UserId, a.AttributeMappingId }).IsUnique();

        builder.HasOne<DirectoryAttributeMapping>()
            .WithMany()
            .HasForeignKey(a => a.AttributeMappingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict: UserId FK'i zaten Cascade — aynı satırdan ikinci bir cascade yolu
        // (referans verilen kullanıcı silinince) çoklu cascade döngüsüne yol açar.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ReferencedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
