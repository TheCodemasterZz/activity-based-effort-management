using Asp.Versioning;
using EforTakip.Api.Contracts.Settings;
using EforTakip.Application.Settings.Commands.UpdateConfidenceScoreSettings;
using EforTakip.Application.Settings.Dtos;
using EforTakip.Application.Settings.Queries.GetConfidenceScoreSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

/// <summary>Güvenilirlik skoru motorunun tüm ağırlık/eşik/parametre ayarları — tek satırlık
/// (singleton) yapılandırma, admin panelindeki "Güvenilirlik Skoru Ayarları" bölümünden yönetilir.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ConfidenceScoreSettingsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ConfidenceScoreSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfidenceScoreSettingsDto>> Get(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetConfidenceScoreSettingsQuery(), cancellationToken));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(UpdateConfidenceScoreSettingsRequestBody body, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new UpdateConfidenceScoreSettingsCommand(
                body.WeightDescriptionLength, body.WeightSpecificity, body.WeightGenericPenalty,
                body.WeightDuplicateDetection, body.WeightRoundHoursSingle, body.WeightDurationDescriptionRatio,
                body.WeightDailyRoundTotal, body.WeightDailyTotalReasonableness, body.WeightBaselineDeviation,
                body.WeightWeekendHoliday, body.ThresholdVeryLow, body.ThresholdLow, body.ThresholdMedium,
                body.ThresholdHigh, body.BaselineLookbackDays, body.DuplicateLookbackDays,
                body.DuplicateSimilarityThreshold, body.ShortDescriptionCharThreshold,
                body.LongDescriptionCharThreshold, body.LongDurationHoursThreshold,
                body.ShortDurationHoursThreshold, body.DailyTotalSuspiciousHours, body.GenericPhrasesCsv),
            cancellationToken);
        return NoContent();
    }
}
