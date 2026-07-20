using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.WorkLogApprovals;

/// <summary>Onaylanmış bir döneme denk gelen tarih aralığına yeni efor kaydı eklenmesini veya
/// mevcut bir kaydın o aralığa taşınmasını engeller — onay "kapanmış dönem" anlamına gelir.</summary>
public static class WorkLogApprovalGuard
{
    public static async Task EnsureRangeNotApprovedAsync(
        IApplicationDbContext db, Guid employeeId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var hasOverlap = await db.WorkLogApprovals.AnyAsync(
            a => a.EmployeeId == employeeId && a.PeriodStart <= end && a.PeriodEnd >= start,
            cancellationToken);

        if (hasOverlap)
            throw new BusinessRuleValidationException(
                "Bu tarih aralığının bir kısmı onaylanmış bir döneme denk geliyor, kayıt eklenemez/taşınamaz.");
    }
}
