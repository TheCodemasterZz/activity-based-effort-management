using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using MediatR;

namespace EforTakip.Application.Projects.Commands.AssignCustomerToProject;

public sealed class AssignCustomerToProjectCommandHandler(
    IProjectRepository projectRepository,
    IRepository<Customer> customerRepository,
    IApplicationDbContext db,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignCustomerToProjectCommand>
{
    public async Task Handle(AssignCustomerToProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        _ = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var assignment = project.AssignCustomer(request.CustomerId);

        // EF Core, client-side üretilen Guid anahtarlı yeni koleksiyon öğelerini
        // fixup ile "Added" yerine "Modified" işaretleyebiliyor — açıkça ekleyerek garanti ediyoruz.
        db.ProjectCustomerAssignments.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
