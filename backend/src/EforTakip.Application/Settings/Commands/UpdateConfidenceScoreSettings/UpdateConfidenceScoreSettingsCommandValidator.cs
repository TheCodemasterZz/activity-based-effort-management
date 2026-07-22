using FluentValidation;

namespace EforTakip.Application.Settings.Commands.UpdateConfidenceScoreSettings;

public sealed class UpdateConfidenceScoreSettingsCommandValidator : AbstractValidator<UpdateConfidenceScoreSettingsCommand>
{
    public UpdateConfidenceScoreSettingsCommandValidator()
    {
        RuleFor(x => x.WeightDescriptionLength).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightSpecificity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightGenericPenalty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightDuplicateDetection).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightRoundHoursSingle).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightDurationDescriptionRatio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightDailyRoundTotal).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightDailyTotalReasonableness).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightBaselineDeviation).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WeightWeekendHoliday).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ThresholdVeryLow).InclusiveBetween(0, 100);
        RuleFor(x => x.ThresholdLow).InclusiveBetween(0, 100);
        RuleFor(x => x.ThresholdMedium).InclusiveBetween(0, 100);
        RuleFor(x => x.ThresholdHigh).InclusiveBetween(0, 100);

        RuleFor(x => x.DuplicateSimilarityThreshold).InclusiveBetween(0, 1);
        RuleFor(x => x.BaselineLookbackDays).GreaterThan(0);
        RuleFor(x => x.DuplicateLookbackDays).GreaterThan(0);
        RuleFor(x => x.GenericPhrasesCsv).NotNull();
    }
}
