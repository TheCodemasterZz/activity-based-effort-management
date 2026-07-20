using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.Projects.Commands.AssignCustomerToProject;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Exceptions;
using EforTakip.Domain.Projects;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace EforTakip.Application.Tests.Projects.Commands;

public class AssignCustomerToProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IRepository<Customer> _customerRepository = Substitute.For<IRepository<Customer>>();
    private readonly IApplicationDbContext _db = Substitute.For<IApplicationDbContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public AssignCustomerToProjectCommandHandlerTests()
    {
        _db.ProjectCustomerAssignments.Returns(Substitute.For<DbSet<ProjectCustomerAssignment>>());
    }

    [Fact]
    public async Task Handle_WithExistingProjectAndCustomer_AssignsCustomer()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        var customer = Customer.Create("Acme A.Ş.");
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _customerRepository.GetByIdAsync(customer.Id, Arg.Any<CancellationToken>()).Returns(customer);

        var handler = new AssignCustomerToProjectCommandHandler(_projectRepository, _customerRepository, _db, _unitOfWork);
        var command = new AssignCustomerToProjectCommand(project.Id, customer.Id);

        await handler.Handle(command, CancellationToken.None);

        project.CustomerIds.Should().Contain(customer.Id);
        _db.ProjectCustomerAssignments.Received(1).Add(Arg.Any<ProjectCustomerAssignment>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistingCustomer_ThrowsNotFoundException()
    {
        var project = Project.Create("Efor Takip Platformu", null);
        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);
        _customerRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Customer?)null);

        var handler = new AssignCustomerToProjectCommandHandler(_projectRepository, _customerRepository, _db, _unitOfWork);
        var command = new AssignCustomerToProjectCommand(project.Id, Guid.NewGuid());

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
