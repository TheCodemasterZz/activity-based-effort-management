using Bogus;
using EforTakip.Domain.Customers;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Projects;
using EforTakip.Domain.ValueStreams;
using EforTakip.Domain.WorkLogApprovals;
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

        // İzin (leave) örnek verisi — projelere atanmış (yani tabloda görünen) çalışanlar
        // arasından bir kısmına tam günlük, bir kısmına saatlik (kısmi) izin atanır. Tarihler
        // work log'ların kapsadığı son 14 günlük aralığa ve önümüzdeki haftaya yayılır.
        var assignedEmployeeIdsPool = projects.SelectMany(p => p.EmployeeIds).Distinct().ToList();
        var leaveCandidates = assignedEmployeeIdsPool.OrderBy(_ => random.Next()).Take(8).ToList();
        var leaves = new List<EmployeeLeave>();

        for (var i = 0; i < leaveCandidates.Count; i++)
        {
            var employeeId = leaveCandidates[i];
            var isFullDay = i % 2 == 0;
            var dayOffset = random.Next(-13, 7);
            var date = DateOnly.FromDateTime(DateTime.Today.AddDays(dayOffset));
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                date = date.AddDays(2);

            if (isFullDay)
            {
                var spanDays = random.Next(0, 3); // tek gün ya da 2-3 günlük tam izin
                leaves.Add(EmployeeLeave.Create(
                    employeeId, date, date.AddDays(spanDays), isFullDay: true, startTime: null, endTime: null,
                    description: "Yıllık izin"));
            }
            else
            {
                var startHour = random.Next(9, 15);
                var duration = random.Next(1, 4);
                leaves.Add(EmployeeLeave.Create(
                    employeeId, date, date, isFullDay: false,
                    startTime: new TimeOnly(startHour, 0), endTime: new TimeOnly(Math.Min(startHour + duration, 18), 0),
                    description: "Kısmi mazeret izni"));
            }
        }
        context.EmployeeLeaves.AddRange(leaves);

        // Onaylı hafta örnekleri — geçen haftanın tamamı (Pazartesi–Pazar) birkaç çalışan için
        // önceden onaylanmış olarak seed edilir; böylece onay renklendirmesi ("Onaylı"/"Onaylanan
        // Efor Süresi") demo'da elle onaylamaya gerek kalmadan doğrudan görülebilir.
        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        var daysSinceMonday = ((int)todayDate.DayOfWeek + 6) % 7;
        var thisWeekMonday = todayDate.AddDays(-daysSinceMonday);
        var approvedWeekStart = thisWeekMonday.AddDays(-7);
        var approvedWeekEnd = approvedWeekStart.AddDays(6);

        var approvalCandidates = assignedEmployeeIdsPool.OrderBy(_ => random.Next()).Take(3).ToList();
        var approvals = new List<WorkLogApproval>();
        foreach (var employeeId in approvalCandidates)
        {
            var approval = WorkLogApproval.Create(
                employeeId, ApprovalPeriodType.Weekly, approvedWeekStart, approvedWeekEnd, "Demo: haftalık onay örneği");

            foreach (var log in workLogs.Where(l =>
                l.EmployeeId == employeeId && l.WorkDate >= approvedWeekStart && l.WorkDate <= approvedWeekEnd))
                log.MarkApproved(approval.Id);

            approvals.Add(approval);
        }
        context.WorkLogApprovals.AddRange(approvals);

        // "Software Delivery" value stream'i (aşama + L1/L2 aktiviteleri) ve Türkiye resmi
        // tatil günleri artık SoftwareDeliverySeedData üzerinden EF Core HasData ile modele
        // gömülü — Program.cs'teki EnsureCreatedAsync() çağrısı bunları Test Mode'da da
        // (InMemory sağlayıcısında) otomatik olarak uygular, gerçek DB'ye migration
        // uygulandığında da aynı satırlar eklenir. Burada tekrar eklemeye gerek yok.

        await context.SaveChangesAsync();
    }
}
