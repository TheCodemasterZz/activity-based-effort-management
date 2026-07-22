using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Settings.Commands.UpdateConfidenceScoreSettings;

public sealed class UpdateConfidenceScoreSettingsCommandHandler(IApplicationDbContext db, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateConfidenceScoreSettingsCommand>
{
    public async Task Handle(UpdateConfidenceScoreSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.ConfidenceScoreSettings.FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(ConfidenceScoreSettings), Guid.Empty);

        settings.Update(
            request.WeightDescriptionLength, request.WeightSpecificity, request.WeightGenericPenalty,
            request.WeightDuplicateDetection, request.WeightRoundHoursSingle, request.WeightDurationDescriptionRatio,
            request.WeightDailyRoundTotal, request.WeightDailyTotalReasonableness, request.WeightBaselineDeviation,
            request.WeightWeekendHoliday, request.ThresholdVeryLow, request.ThresholdLow, request.ThresholdMedium,
            request.ThresholdHigh, request.BaselineLookbackDays, request.DuplicateLookbackDays,
            request.DuplicateSimilarityThreshold, request.ShortDescriptionCharThreshold,
            request.LongDescriptionCharThreshold, request.LongDurationHoursThreshold,
            request.ShortDurationHoursThreshold, request.DailyTotalSuspiciousHours, request.GenericPhrasesCsv);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
