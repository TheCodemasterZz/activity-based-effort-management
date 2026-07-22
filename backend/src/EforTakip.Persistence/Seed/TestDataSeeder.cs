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
        var todayDate = DateOnly.FromDateTime(DateTime.Today);

        var customers = new Faker<Customer>("tr")
            .CustomInstantiator(f => Customer.Create(f.Company.CompanyName(1)))
            .Generate(5);
        context.Customers.AddRange(customers);

        // Faker<T>.CustomInstantiator index vermediğinden, çalışanları 2 sabit mesai
        // takviminden (bkz. WorkCalendarSeedData) sırayla atayabilmek için düz bir Faker
        // örneği + Enumerable.Range kullanılıyor.
        var employeeCalendarIds = new[] { WorkCalendarSeedData.StandardCalendarId, WorkCalendarSeedData.FlexCalendarId };
        var employees = Enumerable.Range(0, 25)
            .Select(i => Employee.Create(bogus.Name.FullName(), bogus.Internet.Email(), employeeCalendarIds[i % 2]))
            .ToList();
        context.Employees.AddRange(employees);

        // Rastgele efor kayıtları (work log) için Activity L1/L2 kaynağı: "Software Delivery"
        // değer akışının HasData ile önceden (EnsureCreatedAsync üzerinden) yüklenmiş gerçek
        // kataloğu — ayrı, anlamsız bir sahte aktivite listesi tutulmuyor.
        var topLevelActivities = await context.Activities.Where(a => a.ParentActivityId == null).ToListAsync();
        var subActivities = await context.Activities.Where(a => a.ParentActivityId != null).ToListAsync();

        // Her proje, bugünden 2-8 ay önce başlayıp 1-4 ay sonra biten bir aralığa yayılır —
        // Clarity PPM örneğindeki gibi "hâlâ devam eden" projeler çoğunlukta olsun diye.
        var projects = new Faker<Project>("tr")
            .CustomInstantiator(f =>
            {
                var startDate = todayDate.AddDays(-random.Next(60, 240));
                var endDate = todayDate.AddDays(random.Next(30, 120));
                return Project.Create($"{f.Commerce.ProductName()} Projesi", f.Lorem.Sentence(), startDate, endDate);
            })
            .Generate(4);

        // Sağlık rozeti (ON TRACK/AT RISK/NEEDS HELP) — Clarity örneğindeki gibi çoğunlukla
        // yeşil, birkaçı dikkat çeken renklerde.
        foreach (var project in projects)
        {
            var roll = random.NextDouble();
            project.SetHealthStatus(roll < 0.6 ? ProjectHealthStatus.OnTrack : roll < 0.85 ? ProjectHealthStatus.AtRisk : ProjectHealthStatus.NeedsHelp);
        }

        foreach (var project in projects)
        {
            foreach (var customer in customers.OrderBy(_ => random.Next()).Take(random.Next(1, 3)))
                project.AssignCustomer(customer.Id);
        }

        // Her çalışan en az bir projeye atanır — 25 kişinin hepsi için PlanWork/LogWork'te
        // Temmuz ayının tamamını kapsayan mock veri üretebilmek adına kimse boşta kalmasın.
        // Bir kişi, çeşitlilik olsun diye 1-2 rastgele projeye atanabilir.
        foreach (var employee in employees)
        {
            foreach (var project in projects.OrderBy(_ => random.Next()).Take(random.Next(1, 3)))
                project.AssignEmployee(employee.Id);
        }
        context.Projects.AddRange(projects);

        // Görevler (+ birer kilometre taşı) — Clarity kartındaki elmas-şeritli zaman çizelgesi ve
        // SPI/EVM hesaplaması (bkz. ProjectTask.BaselineEffortHours/BaselineEndDate) için.
        var taskNamePool = new[]
        {
            "Gereksinim Analizi", "Mimari Tasarım", "Ekran Tasarımı", "Backend Geliştirme",
            "Servis Geliştirme", "Veritabanı Geliştirme", "Entegrasyon", "Birim Test",
            "Kullanıcı Kabul Testi", "Devreye Alma", "Kullanıcı Eğitimi", "Kapanış",
        };

        var projectTasks = new List<ProjectTask>();
        foreach (var project in projects)
        {
            var taskCount = random.Next(5, 9);
            var names = taskNamePool.OrderBy(_ => random.Next()).Take(taskCount).ToList();
            var projectStart = project.StartDate!.Value;
            var projectEnd = project.EndDate!.Value;
            var totalDays = Math.Max(taskCount, projectEnd.DayNumber - projectStart.DayNumber);
            var slice = totalDays / taskCount;

            var cursor = projectStart;
            foreach (var name in names)
            {
                var taskStart = cursor;
                var taskEnd = name == names[^1] ? projectEnd : cursor.AddDays(Math.Max(1, slice));
                var estimatedHours = Math.Round((decimal)(random.NextDouble() * 30 + 10), 1);

                var task = ProjectTask.Create(project.Id, name, taskStart, taskEnd, estimatedHours);

                // Bitiş tarihi geçmişse çoğunlukla Bitti — ama bir kısmı kasıtlı olarak hâlâ
                // Devam Ediyor durumunda bırakılıyor (SPI'nin 1'in altına düşmesini sağlayan,
                // "planlanan bitmiş görünmesi gerekirken hâlâ sürüyor" senaryosu).
                task.SetStatus(
                    taskEnd <= todayDate
                        ? (random.NextDouble() > 0.2 ? ProjectTaskStatus.Done : ProjectTaskStatus.InProgress)
                        : taskStart <= todayDate
                            ? ProjectTaskStatus.InProgress
                            : ProjectTaskStatus.NotStarted);

                projectTasks.Add(task);
                cursor = taskEnd;
            }

            var milestoneDate = projectStart.AddDays(totalDays / 2);
            var milestone = ProjectTask.Create(
                project.Id, $"{project.Name} — Ara Değerlendirme", milestoneDate, milestoneDate, 0, isMilestone: true);
            milestone.SetStatus(milestoneDate <= todayDate ? ProjectTaskStatus.Done : ProjectTaskStatus.NotStarted);
            projectTasks.Add(milestone);
        }
        context.ProjectTasks.AddRange(projectTasks);

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

        // İçinde bulunulan ayın tamamı (ör. Temmuz 2026) — her 25 çalışan için hem gerçekleşen
        // (Actual, sadece bugüne kadar) hem planlanan (Planned, bugünden sonrası) kayıtlarla
        // baştan sona doldurulur. Bugünün kendisi Actual tarafında sayılır.
        var monthStart = new DateOnly(todayDate.Year, todayDate.Month, 1);
        var monthEnd = new DateOnly(todayDate.Year, todayDate.Month, DateTime.DaysInMonth(todayDate.Year, todayDate.Month));

        var employeeProjects = projects
            .SelectMany(p => p.EmployeeIds.Select(employeeId => (employeeId, project: p)))
            .GroupBy(x => x.employeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.project).ToList());

        // Bir günlük toplam saati (ör. 8h), 15dk (0.25h) ile 2h arasında değişen, gerçek bir
        // efor günlüğüne benzeyen birden fazla küçük parçaya böler (ör. 8h -> 5 kayıt).
        List<decimal> SplitIntoChunks(decimal totalHours)
        {
            var chunks = new List<decimal>();
            var remaining = totalHours;
            while (remaining > 0.01m)
            {
                var maxChunk = Math.Min(remaining, 2.0m);
                if (maxChunk <= 0.25m)
                {
                    chunks.Add(remaining);
                    break;
                }

                var raw = 0.25m + (decimal)random.NextDouble() * (maxChunk - 0.25m);
                var chunk = Math.Round(raw * 4, MidpointRounding.AwayFromZero) / 4; // en yakın 15dk'ya yuvarla
                if (chunk < 0.25m)
                    chunk = 0.25m;
                if (chunk > remaining)
                    chunk = remaining;

                chunks.Add(chunk);
                remaining -= chunk;
            }
            return chunks;
        }

        // Actual ve Planned için ayrı ayrı çağrılır: Actual sadece bugüne kadar (gelecek tarihli
        // gerçekleşen kayıt olamaz — domain kuralı), Planned ise ayın TAMAMI için (geçmiş günler
        // dahil — "plan" geriye dönük olarak da anlamlı, ör. o gün için ne planlanmıştı sorusu).
        // Aynı gün için hem Actual hem Planned kaydı oluşabilir; bu Kapasite Yönetimi'ndeki
        // "karma" modun (günlük bazda Actual varsa onu, yoksa Planned'ı sayan, asla ikisini
        // toplamayan) tam olarak neden var olduğu senaryo.
        List<EmployeeWorkLog> GenerateWorkLogs(WorkLogEntryType entryType, DateOnly rangeStart, DateOnly rangeEnd)
        {
            var result = new List<EmployeeWorkLog>();
            foreach (var employee in employees)
            {
                if (!employeeProjects.TryGetValue(employee.Id, out var assignedProjects) || assignedProjects.Count == 0)
                    continue;

                for (var date = rangeStart; date <= rangeEnd; date = date.AddDays(1))
                {
                    if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                        continue;

                    // Ayın tamamı kapsansın istendiği için yüksek olasılık (%90) kullanılıyor —
                    // yine de her gün deterministik olmasın diye tam %100 değil.
                    if (random.NextDouble() > 0.9)
                        continue;

                    var dailyTotal = Math.Round((decimal)(random.NextDouble() * 5 + 3), 2); // ~3-8h/gün
                    foreach (var chunkHours in SplitIntoChunks(dailyTotal))
                    {
                        var project = assignedProjects[random.Next(assignedProjects.Count)];
                        var assignedCustomerIds = project.CustomerIds.ToList();
                        if (assignedCustomerIds.Count == 0)
                            continue;

                        var activityL1 = topLevelActivities[random.Next(topLevelActivities.Count)];
                        var candidatesL2 = subActivities.Where(a => a.ParentActivityId == activityL1.Id).ToList();
                        var activityL2 = candidatesL2[random.Next(candidatesL2.Count)];

                        result.Add(EmployeeWorkLog.Create(
                            employee.Id,
                            project.Id,
                            assignedCustomerIds[random.Next(assignedCustomerIds.Count)],
                            activityL1.Id,
                            activityL2.Id,
                            date,
                            chunkHours,
                            bogus.Lorem.Sentence(6, 4),
                            entryType));
                    }
                }
            }
            return result;
        }

        var workLogs = new List<EmployeeWorkLog>();
        workLogs.AddRange(GenerateWorkLogs(WorkLogEntryType.Actual, monthStart, todayDate));
        workLogs.AddRange(GenerateWorkLogs(WorkLogEntryType.Planned, monthStart, monthEnd));
        context.EmployeeWorkLogs.AddRange(workLogs);

        var actualWorkLogs = workLogs.Where(l => l.EntryType == WorkLogEntryType.Actual).ToList();
        var plannedWorkLogs = workLogs.Where(l => l.EntryType == WorkLogEntryType.Planned).ToList();

        // İzin (leave) örnek verisi — 25 çalışanın bir kısmına tam günlük, bir kısmına saatlik
        // (kısmi) izin atanır. Tarihler ayın tamamına yayılır.
        var assignedEmployeeIdsPool = employeeProjects.Keys.ToList();
        var leaveCandidates = assignedEmployeeIdsPool.OrderBy(_ => random.Next()).Take(10).ToList();
        var leaves = new List<EmployeeLeave>();

        for (var i = 0; i < leaveCandidates.Count; i++)
        {
            var employeeId = leaveCandidates[i];
            var isFullDay = i % 2 == 0;
            var dayOffset = random.Next(0, monthEnd.DayNumber - monthStart.DayNumber);
            var date = monthStart.AddDays(dayOffset);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                date = date.AddDays(2);
            if (date > monthEnd)
                date = monthEnd;

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

        // Onaylı hafta örnekleri — Log Work tarafında tamamen geçmişte kalmış (Pazartesi–Pazar)
        // bir hafta, Plan Work tarafında ise tamamen gelecekteki bir hafta, daha geniş bir
        // çalışan grubu için önceden onaylanmış olarak seed edilir; böylece onay renklendirmesi
        // ("Onaylı"/"Onaylanan Efor Süresi") demo'da elle onaylamaya gerek kalmadan görülebilir.
        var daysSinceMonday = ((int)todayDate.DayOfWeek + 6) % 7;
        var thisWeekMonday = todayDate.AddDays(-daysSinceMonday);
        var approvedWeekStart = thisWeekMonday.AddDays(-7);
        var approvedWeekEnd = approvedWeekStart.AddDays(6);

        var approvalCandidates = assignedEmployeeIdsPool.OrderBy(_ => random.Next()).Take(8).ToList();
        var approvals = new List<WorkLogApproval>();
        foreach (var employeeId in approvalCandidates)
        {
            var approval = WorkLogApproval.Create(
                employeeId, ApprovalPeriodType.Weekly, approvedWeekStart, approvedWeekEnd, "Demo: haftalık onay örneği");

            foreach (var log in actualWorkLogs.Where(l =>
                l.EmployeeId == employeeId && l.WorkDate >= approvedWeekStart && l.WorkDate <= approvedWeekEnd))
                log.MarkApproved(approval.Id);

            approvals.Add(approval);
        }
        context.WorkLogApprovals.AddRange(approvals);

        // Plan Work için de, gelecek haftalardan birinin planı bir grup çalışan için önceden
        // onaylı olarak seed edilir — Actual onaylarından tamamen bağımsız (ayrı EntryType).
        var plannedApprovedWeekStart = thisWeekMonday.AddDays(7);
        var plannedApprovedWeekEnd = plannedApprovedWeekStart.AddDays(6);

        var plannedApprovalCandidates = assignedEmployeeIdsPool.OrderBy(_ => random.Next()).Take(8).ToList();
        var plannedApprovals = new List<WorkLogApproval>();
        foreach (var employeeId in plannedApprovalCandidates)
        {
            var approval = WorkLogApproval.Create(
                employeeId, ApprovalPeriodType.Weekly, plannedApprovedWeekStart, plannedApprovedWeekEnd,
                "Demo: haftalık plan onayı örneği", WorkLogEntryType.Planned);

            foreach (var log in plannedWorkLogs.Where(l =>
                l.EmployeeId == employeeId && l.WorkDate >= plannedApprovedWeekStart && l.WorkDate <= plannedApprovedWeekEnd))
                log.MarkApproved(approval.Id);

            plannedApprovals.Add(approval);
        }
        context.WorkLogApprovals.AddRange(plannedApprovals);

        // "Software Delivery" value stream'i (aşama + L1/L2 aktiviteleri) ve Türkiye resmi
        // tatil günleri artık SoftwareDeliverySeedData üzerinden EF Core HasData ile modele
        // gömülü — Program.cs'teki EnsureCreatedAsync() çağrısı bunları Test Mode'da da
        // (InMemory sağlayıcısında) otomatik olarak uygular, gerçek DB'ye migration
        // uygulandığında da aynı satırlar eklenir. Burada tekrar eklemeye gerek yok.

        await context.SaveChangesAsync();
    }
}
