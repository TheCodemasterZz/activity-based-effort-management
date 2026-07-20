using EforTakip.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EforTakip.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.CreatedAtUtc).IsRequired();
        builder.Property(n => n.IsRead).IsRequired();

        builder.HasData(
            new
            {
                Id = Id(1),
                Message = "Temmuz ayı efor raporu hazır.",
                CreatedAtUtc = new DateTime(2026, 7, 8, 9, 0, 0, DateTimeKind.Utc),
                IsRead = false,
            },
            new
            {
                Id = Id(2),
                Message = "'Software Delivery' değer akışına yeni bir aşama eklendi.",
                CreatedAtUtc = new DateTime(2026, 7, 7, 14, 30, 0, DateTimeKind.Utc),
                IsRead = false,
            },
            new
            {
                Id = Id(3),
                Message = "Mesai takviminizde güncelleme yapıldı.",
                CreatedAtUtc = new DateTime(2026, 7, 6, 11, 15, 0, DateTimeKind.Utc),
                IsRead = false,
            },
            new
            {
                Id = Id(4),
                Message = "15 Temmuz Demokrasi ve Millî Birlik Günü tatil takvimine eklendi.",
                CreatedAtUtc = new DateTime(2026, 7, 3, 8, 0, 0, DateTimeKind.Utc),
                IsRead = true,
            },
            new
            {
                Id = Id(5),
                Message = "Sistem bakımı bu hafta sonu planlanmıştır.",
                CreatedAtUtc = new DateTime(2026, 7, 1, 17, 45, 0, DateTimeKind.Utc),
                IsRead = true,
            });
    }

    private static Guid Id(int index) => Guid.Parse($"00000000-0000-0000-0008-{index:D12}");
}
