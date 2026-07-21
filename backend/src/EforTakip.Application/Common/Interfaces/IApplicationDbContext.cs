using EforTakip.Domain.Activities;
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

namespace EforTakip.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }

    DbSet<Employee> Employees { get; }

    DbSet<Project> Projects { get; }

    DbSet<ProjectCustomerAssignment> ProjectCustomerAssignments { get; }

    DbSet<ProjectEmployeeAssignment> ProjectEmployeeAssignments { get; }

    DbSet<ValueStream> ValueStreams { get; }

    DbSet<ValueStreamStage> ValueStreamStages { get; }

    DbSet<Activity> Activities { get; }

    DbSet<StageActivityAssignment> StageActivityAssignments { get; }

    DbSet<EmployeeWorkLog> EmployeeWorkLogs { get; }

    DbSet<Holiday> Holidays { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<WorkCalendar> WorkCalendars { get; }

    DbSet<WorkCalendarDay> WorkCalendarDays { get; }

    DbSet<WorkLogApproval> WorkLogApprovals { get; }

    DbSet<EmployeeLeave> EmployeeLeaves { get; }

    DbSet<Domain.Directories.Directory> Directories { get; }

    DbSet<DirectoryUser> DirectoryUsers { get; }

    DbSet<DirectoryAttributeMapping> DirectoryAttributeMappings { get; }

    DbSet<DirectoryUserAttribute> DirectoryUserAttributes { get; }
}
