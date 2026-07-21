using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Domain.Exceptions;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Application.Directories.Queries.GetDirectoryById;

public sealed class GetDirectoryByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetDirectoryByIdQuery, DirectoryDto>
{
    public async Task<DirectoryDto> Handle(GetDirectoryByIdQuery request, CancellationToken cancellationToken)
    {
        var directory = await db.Directories
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == request.DirectoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Directory), request.DirectoryId);

        return directory.Adapt<DirectoryDto>();
    }
}
