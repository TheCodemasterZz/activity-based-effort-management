namespace EforTakip.Application.Directories.Dtos;

public sealed class OrgChartNodeDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public Guid? ManagerId { get; init; }
    public string? PhotoBase64 { get; init; }
}

/// <summary>
/// "Kullanıcı" tipinde bir alan eşlemesi (ör. Yönetici) tanımlı değilse organizasyon şeması
/// çıkarılamaz — <see cref="HasManagerMapping"/> bu durumu frontend'e bildirir.
/// </summary>
public sealed class OrgChartResultDto
{
    public bool HasManagerMapping { get; init; }
    public IReadOnlyCollection<OrgChartNodeDto> Nodes { get; init; } = [];
}
