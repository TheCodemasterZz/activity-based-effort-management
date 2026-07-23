using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Activities;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Directories;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Holidays;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Roles;
using EforTakip.Domain.Settings;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkCalendars;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Tests.Directories.Commands;

/// <summary>
/// Senkronizasyon testleri için gerçek EF Core InMemory context'i. NSubstitute ile mock'lanan
/// DbSet'ler async LINQ (AnyAsync/ToListAsync) desteklemediğinden gerçek context kullanılır.
/// </summary>
public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectCustomerAssignment> ProjectCustomerAssignments => Set<ProjectCustomerAssignment>();
    public DbSet<ProjectEmployeeAssignment> ProjectEmployeeAssignments => Set<ProjectEmployeeAssignment>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ProjectRisk> ProjectRisks => Set<ProjectRisk>();
    public DbSet<ProjectIssue> ProjectIssues => Set<ProjectIssue>();
    public DbSet<ConfidenceScoreSettings> ConfidenceScoreSettings => Set<ConfidenceScoreSettings>();
    public DbSet<ValueStream> ValueStreams => Set<ValueStream>();
    public DbSet<ValueStreamStage> ValueStreamStages => Set<ValueStreamStage>();
    public DbSet<Activity> Activities => Set<Activity>();
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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<DirectoryUserRole> DirectoryUserRoles => Set<DirectoryUserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DirectoryUser>()
            .HasMany(u => u.Attributes)
            .WithOne()
            .HasForeignKey(a => a.DirectoryUserId);

        modelBuilder.Entity<DirectoryUser>()
            .Metadata
            .FindNavigation(nameof(DirectoryUser.Attributes))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<DirectoryUser>()
            .HasMany(u => u.Roles)
            .WithOne()
            .HasForeignKey(r => r.DirectoryUserId);

        modelBuilder.Entity<DirectoryUser>()
            .Metadata
            .FindNavigation(nameof(DirectoryUser.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Permissions)
            .WithOne()
            .HasForeignKey(p => p.RoleId);

        modelBuilder.Entity<Role>()
            .Metadata
            .FindNavigation(nameof(Role.Permissions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        base.OnModelCreating(modelBuilder);
    }
}
