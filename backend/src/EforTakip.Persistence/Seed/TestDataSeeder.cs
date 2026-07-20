using Bogus;
using EforTakip.Domain.Customers;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Projects;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkLogs;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Persistence.Seed;

/// <summary>
/// Test Mode'da (appsettings "UseTestMode": true) gerçek bir veritabanı bağlantısı
/// kurulmadan çalışabilmek için uygulama başlangıcında bir kez gerçekçi sahte veri üretir.
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedAsync(EforTakipDbContext context)
    {
        if (await context.Customers.AnyAsync())
            return;

        var random = new Random(1234);
        var bogus = new Faker("tr");

        var customers = new Faker<Customer>("tr")
            .CustomInstantiator(f => Customer.Create(f.Company.CompanyName(1)))
            .Generate(5);
        context.Customers.AddRange(customers);

        // Faker<T>.CustomInstantiator index vermediğinden, çalışanları 2 sabit mesai
        // takviminden (bkz. WorkCalendarSeedData) sırayla atayabilmek için düz bir Faker
        // örneği + Enumerable.Range kullanılıyor.
        var employeeCalendarIds = new[] { WorkCalendarSeedData.StandardCalendarId, WorkCalendarSeedData.FlexCalendarId };
        var employees = Enumerable.Range(0, 100)
            .Select(i => Employee.Create(bogus.Name.FullName(), bogus.Internet.Email(), employeeCalendarIds[i % 2]))
            .ToList();
        context.Employees.AddRange(employees);

        // Rastgele efor kayıtları (work log) için Activity L1/L2 kaynağı: "Software Delivery"
        // değer akışının HasData ile önceden (EnsureCreatedAsync üzerinden) yüklenmiş gerçek
        // kataloğu — ayrı, anlamsız bir sahte aktivite listesi tutulmuyor.
        var topLevelActivities = await context.Activities.Where(a => a.ParentActivityId == null).ToListAsync();
        var subActivities = await context.Activities.Where(a => a.ParentActivityId != null).ToListAsync();

        var projects = new Faker<Project>("tr")
            .CustomInstantiator(f => Project.Create($"{f.Commerce.ProductName()} Projesi", f.Lorem.Sentence()))
            .Generate(4);

        foreach (var project in projects)
        {
            foreach (var customer in customers.OrderBy(_ => random.Next()).Take(random.Next(1, 3)))
                project.AssignCustomer(customer.Id);

            foreach (var employee in employees.OrderBy(_ => random.Next()).Take(random.Next(2, 5)))
                project.AssignEmployee(employee.Id);
        }
        context.Projects.AddRange(projects);

        var valueStreams = new[] { "Ürün Geliştirme Süreci", "Müşteri Talep Süreci" }
            .Select(name => ValueStream.Create(name, null))
            .ToList();
        foreach (var valueStream in valueStreams)
        {
            valueStream.AddStage("Talep Alma", 1);
            valueStream.AddStage("Analiz", 2);
            valueStream.AddStage("Geliştirme", 3);
            valueStream.AddStage("Test", 4);
            valueStream.AddStage("Devreye Alma", 5);
        }
        context.ValueStreams.AddRange(valueStreams);

        var workLogs = new List<EmployeeWorkLog>();
        foreach (var project in projects)
        {
            var assignedCustomerIds = project.CustomerIds.ToList();
            var assignedEmployeeIds = project.EmployeeIds.ToList();

            for (var dayOffset = 0; dayOffset < 14; dayOffset++)
            {
                var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-dayOffset));
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                foreach (var employeeId in assignedEmployeeIds)
                {
                    if (random.NextDouble() > 0.6)
                        continue;

                    var activityL1 = topLevelActivities[random.Next(topLevelActivities.Count)];
                    var candidatesL2 = subActivities.Where(a => a.ParentActivityId == activityL1.Id).ToList();
                    var activityL2 = candidatesL2[random.Next(candidatesL2.Count)];

                    workLogs.Add(EmployeeWorkLog.Create(
                        employeeId,
                        project.Id,
                        assignedCustomerIds[random.Next(assignedCustomerIds.Count)],
                        activityL1.Id,
                        activityL2.Id,
                        date,
                        Math.Round((decimal)(random.NextDouble() * 6 + 1), 1),
                        bogus.Lorem.Sentence(6, 4)));
                }
            }
        }
        context.EmployeeWorkLogs.AddRange(workLogs);

        // "Software Delivery" value stream'i (aşama + L1/L2 aktiviteleri) ve Türkiye resmi
        // tatil günleri artık SoftwareDeliverySeedData üzerinden EF Core HasData ile modele
        // gömülü — Program.cs'teki EnsureCreatedAsync() çağrısı bunları Test Mode'da da
        // (InMemory sağlayıcısında) otomatik olarak uygular, gerçek DB'ye migration
        // uygulandığında da aynı satırlar eklenir. Burada tekrar eklemeye gerek yok.

        await context.SaveChangesAsync();
    }
}
