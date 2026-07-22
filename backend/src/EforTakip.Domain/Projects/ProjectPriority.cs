namespace EforTakip.Domain.Projects;

/// <summary>Projenin portföy içindeki göreli önceliği — Overview sekmesinde gösterilir,
/// şu an başka bir hesaplamayı etkilemez (salt bilgi amaçlı, gelecekte sıralama/filtreleme
/// için kullanılabilir).</summary>
public enum ProjectPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
