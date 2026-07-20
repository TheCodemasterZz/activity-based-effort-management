using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.ValueStreams.Dtos;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.ValueStreams;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.ValueStreams.Queries.GetValueStreamById;

public sealed class GetValueStreamByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetValueStreamByIdQuery, ValueStreamDetailDto>
{
    public async Task<ValueStreamDetailDto> Handle(GetValueStreamByIdQuery request, CancellationToken cancellationToken)
    {
        var valueStream = await db.ValueStreams
            .AsNoTracking()
            .Include(v => v.Stages)
            .FirstOrDefaultAsync(v => v.Id == request.ValueStreamId, cancellationToken)
            ?? throw new NotFoundException(nameof(ValueStream), request.ValueStreamId);

        return valueStream.Adapt<ValueStreamDetailDto>();
    }
}
