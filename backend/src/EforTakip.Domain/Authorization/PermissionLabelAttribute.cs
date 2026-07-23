namespace EforTakip.Domain.Authorization;

/// <summary>
/// Bir izin sabitinin veya modül sınıfının kullanıcıya gösterilecek Türkçe başlığını taşır.
/// Ham anahtar (ör. "directory:manage") hiçbir zaman doğrudan ekrana basılmaz — Permissions
/// kataloğu bu attribute'u reflection ile okuyup PermissionDescriptor listesine çevirir.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
public sealed class PermissionLabelAttribute(string label) : Attribute
{
    public string Label { get; } = label;
}
