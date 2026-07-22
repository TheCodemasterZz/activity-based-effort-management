using EforTakip.Application.Settings.Dtos;
using MediatR;

namespace EforTakip.Application.Settings.Queries.GetConfidenceScoreSettings;

public sealed record GetConfidenceScoreSettingsQuery : IRequest<ConfidenceScoreSettingsDto>;
