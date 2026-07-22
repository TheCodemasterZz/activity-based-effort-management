using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Projects;
using EforTakip.Application.ValueStreams;
using EforTakip.Application.WorkCalendars;
using EforTakip.Domain.Customers;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Holidays;
using EforTakip.Domain.Notifications;
using EforTakip.Domain.Projects;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkLogs;
using EforTakip.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DomainActivity = EforTakip.Domain.Activities.Activity;
using DirectoryEntity = EforTakip.Domain.Directories.Directory;
using EforTakip.Domain.Directories;

namespace EforTakip.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var useTestMode = configuration.GetValue<bool>("UseTestMode");

        services.AddDbContext<EforTakipDbContext>(options =>
        {
            if (useTestMode)
            {
                // Test Mode: gerçek bir veritabanına bağlanmadan, uygulama başında bir kez
                // seed edilen gerçekçi sahte veriyle in-memory çalışır (bkz. Seed/TestDataSeeder).
                options.UseInMemoryDatabase("EforTakipTestDb");
            }
            else
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsql => npgsql.MigrationsAssembly(typeof(EforTakipDbContext).Assembly.FullName));
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<EforTakipDbContext>());

        services.AddScoped<IRepository<Customer>, RepositoryBase<Customer>>();
        services.AddScoped<IRepository<Employee>, RepositoryBase<Employee>>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IRepository<ProjectTask>, RepositoryBase<ProjectTask>>();
        services.AddScoped<IRepository<ProjectRisk>, RepositoryBase<ProjectRisk>>();
        services.AddScoped<IRepository<ProjectIssue>, RepositoryBase<ProjectIssue>>();
        services.AddScoped<IValueStreamRepository, ValueStreamRepository>();
        services.AddScoped<IRepository<DomainActivity>, RepositoryBase<DomainActivity>>();
        services.AddScoped<IRepository<EmployeeWorkLog>, RepositoryBase<EmployeeWorkLog>>();
        services.AddScoped<IRepository<Holiday>, RepositoryBase<Holiday>>();
        services.AddScoped<IRepository<Notification>, RepositoryBase<Notification>>();
        services.AddScoped<IWorkCalendarRepository, WorkCalendarRepository>();
        services.AddScoped<IRepository<EmployeeLeave>, RepositoryBase<EmployeeLeave>>();
        services.AddScoped<IRepository<DirectoryEntity>, RepositoryBase<DirectoryEntity>>();
        services.AddScoped<IRepository<DirectoryUser>, RepositoryBase<DirectoryUser>>();
        services.AddScoped<IRepository<DirectoryAttributeMapping>, RepositoryBase<DirectoryAttributeMapping>>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
