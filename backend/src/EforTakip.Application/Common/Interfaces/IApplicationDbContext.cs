using EforTakip.Domain.Activities;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Directories;
using EforTakip.Domain.Leaves;
using EforTakip.Domain.Holidays;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Roles;
using EforTakip.Domain.Settings;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkCalendars;
using EforTakip.Domain.Users;
using EforTakip.Domain.WorkLogApprovals;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }


    DbSet<Project> Projects { get; }

    DbSet<ProjectCustomerAssignment> ProjectCustomerAssignments { get; }

    DbSet<ProjectUserAssignment> ProjectUserAssignments { get; }

    DbSet<ProjectTask> ProjectTasks { get; }

    DbSet<ProjectRisk> ProjectRisks { get; }

    DbSet<ProjectIssue> ProjectIssues { get; }

    DbSet<ValueStream> ValueStreams { get; }

    DbSet<ValueStreamStage> ValueStreamStages { get; }

    DbSet<Activity> Activities { get; }

    DbSet<StageActivityAssignment> StageActivityAssignments { get; }

    DbSet<WorkLog> WorkLogs { get; }

    DbSet<Holiday> Holidays { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<WorkCalendar> WorkCalendars { get; }

    DbSet<WorkCalendarDay> WorkCalendarDays { get; }

    DbSet<WorkLogApproval> WorkLogApprovals { get; }

    DbSet<Leave> Leaves { get; }

    DbSet<Domain.Directories.Directory> Directories { get; }

    DbSet<User> Users { get; }

    DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings { get; }

    DbSet<UserAttribute> UserAttributes { get; }

    DbSet<ConfidenceScoreSettings> ConfidenceScoreSettings { get; }

    DbSet<Role> Roles { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<UserRole> UserRoles { get; }
}
