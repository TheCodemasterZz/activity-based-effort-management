using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Application.WorkLogs;

/// <summary>
/// LogWork ve UpdateWorkLog komutlarının ortak cross-aggregate doğrulamasını taşır
/// (proje/çalışan ataması, ActivityL1/L2 ilişkisi) — DRY.
/// </summary>
internal static class WorkLogValidationHelper
{
    public static async Task ValidateAsync(
        IProjectRepository projectRepository,
        IRepository<DomainActivity> activityRepository,
        Guid projectId,
        Guid employeeId,
        Guid activityL1Id,
        Guid activityL2Id,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), projectId);

        if (!project.EmployeeIds.Contains(employeeId))
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
