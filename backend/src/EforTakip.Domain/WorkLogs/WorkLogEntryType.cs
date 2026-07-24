namespace EforTakip.Domain.WorkLogs;

/// <summary>Bir WorkLog/WorkLogApproval kaydının gerçekleşmiş (Log Work) mi yoksa
/// planlanmış (Plan Work) bir efor/onay mı olduğunu ayırt eder. İki sayfa da aynı tabloları
/// paylaşır — birbirini etkilememesi için tüm sorgular ve onay akışı bu alana göre filtrelenir.</summary>
public enum WorkLogEntryType
{
    Actual = 0,
    Planned = 1
}
