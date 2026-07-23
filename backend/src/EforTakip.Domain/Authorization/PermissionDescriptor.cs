namespace EforTakip.Domain.Authorization;

/// <summary>Bir izin anahtarının UI'da gösterilecek insan-okur karşılığı.</summary>
public sealed record PermissionDescriptor(string Key, string ModuleLabel, string Label);
