using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Directories;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Holidays;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Projects;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkCalendars;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;
using DomainActivity = EforTakip.Domain.Activities.Activity;

namespace EforTakip.Persistence;

public sealed class EforTakipDbContext(DbContextOptions<EforTakipDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectCustomerAssignment> ProjectCustomerAssignments => Set<ProjectCustomerAssignment>();

    public DbSet<ProjectEmployeeAssignment> ProjectEmployeeAssignments => Set<ProjectEmployeeAssignment>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    public DbSet<ValueStream> ValueStreams => Set<ValueStream>();

    public DbSet<ValueStreamStage> ValueStreamStages => Set<ValueStreamStage>();

    public DbSet<DomainActivity> Activities => Set<DomainActivity>();

    public DbSet<StageActivityAssignment> StageActivityAssignments => Set<StageActivityAssignment>();

    public DbSet<EmployeeWorkLog> EmployeeWorkLogs => Set<EmployeeWorkLog>();

    public DbSet<Holiday> Holidays => Set<Holiday>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<WorkCalendar> WorkCalendars => Set<WorkCalendar>();

    public DbSet<WorkCalendarDay> WorkCalendarDays => Set<WorkCalendarDay>();

    public DbSet<WorkLogApproval> WorkLogApprovals => Set<WorkLogApproval>();

    public DbSet<EmployeeLeave> EmployeeLeaves => Set<EmployeeLeave>();

    public DbSet<Domain.Directories.Directory> Directories => Set<Domain.Directories.Directory>();

    public DbSet<DirectoryUser> DirectoryUsers => Set<DirectoryUser>();

    public DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings => Set<DirectoryAttributeMapping>();

    public DbSet<DirectoryUserAttribute> DirectoryUserAttributes => Set<DirectoryUserAttribute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EforTakipDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
