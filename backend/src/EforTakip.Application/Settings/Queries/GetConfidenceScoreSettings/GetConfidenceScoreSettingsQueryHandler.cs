using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Settings.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Settings;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Settings.Queries.GetConfidenceScoreSettings;

public sealed class GetConfidenceScoreSettingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetConfidenceScoreSettingsQuery, ConfidenceScoreSettingsDto>
{
    public async Task<ConfidenceScoreSettingsDto> Handle(GetConfidenceScoreSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await db.ConfidenceScoreSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(ConfidenceScoreSettings), Guid.Empty);

        return settings.Adapt<ConfidenceScoreSettingsDto>();
    }
}
