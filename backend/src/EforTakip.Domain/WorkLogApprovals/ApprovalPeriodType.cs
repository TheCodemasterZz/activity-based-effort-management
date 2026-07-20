namespace EforTakip.Domain.WorkLogApprovals;

/// <summary>Efor onayı yalnızca tam hafta (Pazartesi–Pazar) bazında verilebilir — bkz.
/// WorkLogApproval.Create.</summary>
public enum ApprovalPeriodType
{
    Weekly = 1
}
