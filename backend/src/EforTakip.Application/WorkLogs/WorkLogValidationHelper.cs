using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Users;
using Microsoft.EntityFrameworkCore;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.WorkLogs;

/// <summary>
/// LogWork ve UpdateWorkLog komutlarının ortak cross-aggregate doğrulamasını taşır
/// (kullanıcı/takvim, proje ataması, ActivityL1/L2 ilişkisi) — DRY.
/// </summary>
internal static class WorkLogValidationHelper
{
    public static async Task ValidateAsync(
        IProjectRepository projectRepository,
        IRepository<DomainActivity> activityRepository,
        IApplicationDbContext db,
        Guid projectId,
        Guid userId,
        Guid activityL1Id,
        Guid activityL2Id,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        // Takvimsiz kullanıcı için beklenen günlük çalışma saati bilinemez; kapasite ve
        // planlama hesapları yanlış sonuç üretir. Takvim ataması senkronda otomatik
        // yapılmaz (bilinçli karar) — admin, Kullanıcılar ekranından atar.
        if (user.WorkCalendarId is null)
            throw new BusinessRuleValidationException(
                "Mesai takvimi atanmamış kullanıcılar efor/plan girişi yapamaz. " +
                "Lütfen yöneticinizden Kullanıcılar ekranından bir mesai takvimi atamasını isteyin.");

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), projectId);

        if (!project.UserIds.Contains(userId))
            throw new BusinessRuleValidationException("Seçilen çalışan bu projeye atanmamış.");

        var activityL1 = await activityRepository.GetByIdAsync(activityL1Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DomainActivity), activityL1Id);

        if (activityL1.ParentActivityId is not null)
            throw new BusinessRuleValidationException("Seçilen aktivite üst seviye (L1) olmalıdır.");

        var activityL2 = await activityRepository.GetByIdAsync(activityL2Id, cancellationToken)
            ?? throw new NotFoundException(nameof(DomainActivity), activityL2Id);

        if (activityL2.ParentActivityId != activityL1Id)
            throw new BusinessRuleValidationException(
                "Seçilen alt aktivite, seçilen üst aktivitenin alt aktivitesi değil.");
    }
}
