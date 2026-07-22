using Bogus;
using EforTakip.Domain.Customers;
using EforTakip.Domain.EmployeeLeaves;
using EforTakip.Domain.Employees;
using EforTakip.Domain.Projects;
using EforTakip.Domain.Settings;
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

        // Overview sekmesi için sabit stratejik hedef havuzu — gerçek bir strateji kataloğu
        // yerine, Clarity örneğindeki gibi kısa/anlamlı cümleler.
        var strategicGoalPool = new[]
        {
            "Dijital dönüşüm hedeflerine katkı",
            "Operasyonel verimliliği artırma",
            "Müşteri deneyimini iyileştirme",
            "Pazar payını büyütme",
            "Maliyet optimizasyonu",
        };

        // Her proje, bugünden 2-8 ay önce başlayıp 1-4 ay sonra biten bir aralığa yayılır —
        // Clarity PPM örneğindeki gibi "hâlâ devam eden" projeler çoğunlukta olsun diye.
        var projects = new Faker<Project>("tr")
            .CustomInstantiator(f =>
            {
                var startDate = todayDate.AddDays(-random.Next(60, 240));
                var endDate = todayDate.AddDays(random.Next(30, 120));
                var priority = (ProjectPriority)random.Next(1, 5);
                return Project.Create(
                    $"{f.Commerce.ProductName()} Projesi", f.Lorem.Sentence(), startDate, endDate,
                    sponsor: f.Name.FullName(),
                    priority: priority,
                    strategicGoal: strategicGoalPool[random.Next(strategicGoalPool.Length)]);
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

        // Proje yöneticisi — atanmış çalışanlardan biri (Overview sekmesi). Employee ataması
        // bittikten SONRA belirlenir ki gerçek bir proje üyesi olsun; Update tam-değiştirme
        // şeklinde olduğu için diğer alanlar kendi mevcut değerleriyle geri yazılıyor.
        foreach (var project in projects)
        {
            if (project.EmployeeIds.Count == 0)
                continue;

            var pmId = project.EmployeeIds.ElementAt(random.Next(project.EmployeeIds.Count));
            project.Update(
                project.Name, project.Description, project.StartDate, project.EndDate,
                project.Sponsor, pmId, project.Priority, project.StrategicGoal);
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

            var projectEmployeeIds = project.EmployeeIds.ToList();
            Guid? PickAssignee() => projectEmployeeIds.Count > 0 ? projectEmployeeIds[random.Next(projectEmployeeIds.Count)] : null;

            // Basit 2 seviyeli WBS (Schedule sekmesi): ilk görev "Faz 1", ortadaki görev
            // "Faz 2" başlığı olur (ParentTaskId=null); aradaki/sonraki görevler bu iki fazın
            // altına dağıtılır. Aynı faz içinde ardışık görevler birbirine DependsOnTaskId ile
            // (basit finish-to-start) zincirlenir — gerçek bir CPM hesaplaması yapılmaz.
            var phase2Index = names.Count / 2;
            ProjectTask? phase1 = null;
            ProjectTask? phase2 = null;
            var localTasks = new List<ProjectTask>();

            var cursor = projectStart;
            for (var idx = 0; idx < names.Count; idx++)
            {
                var name = names[idx];
                var taskStart = cursor;
                var taskEnd = idx == names.Count - 1 ? projectEnd : cursor.AddDays(Math.Max(1, slice));
                var estimatedHours = Math.Round((decimal)(random.NextDouble() * 30 + 10), 1);

                var isPhaseHeader = idx == 0 || idx == phase2Index;
                Guid? parentTaskId = null;
                if (!isPhaseHeader)
                    parentTaskId = idx < phase2Index ? phase1?.Id : phase2?.Id;
                Guid? dependsOnTaskId = !isPhaseHeader && localTasks.Count > 0 ? localTasks[^1].Id : null;

                var task = ProjectTask.Create(
                    project.Id, name, taskStart, taskEnd, estimatedHours, isMilestone: false,
                    parentTaskId: parentTaskId, dependsOnTaskId: dependsOnTaskId, assignedEmployeeId: PickAssignee());

                // Bitiş tarihi geçmişse çoğunlukla Bitti — ama bir kısmı kasıtlı olarak hâlâ
                // Devam Ediyor durumunda bırakılıyor (SPI'nin 1'in altına düşmesini sağlayan,
                // "planlanan bitmiş görünmesi gerekirken hâlâ sürüyor" senaryosu).
                task.SetStatus(
                    taskEnd <= todayDate
                        ? (random.NextDouble() > 0.2 ? ProjectTaskStatus.Done : ProjectTaskStatus.InProgress)
                        : taskStart <= todayDate
                            ? ProjectTaskStatus.InProgress
                            : ProjectTaskStatus.NotStarted);

                if (idx == 0) phase1 = task;
                if (idx == phase2Index) phase2 = task;

                localTasks.Add(task);
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

        // Risks/Issues sekmeleri için proje başına birkaç risk/sorun örneği.
        var riskTitlePool = new[]
        {
            "Anahtar personel kaybı riski", "Üçüncü parti entegrasyon gecikmesi", "Bütçe aşımı riski",
            "Kapsam genişlemesi (scope creep)", "Teknik borç birikimi", "Tedarikçi teslimat gecikmesi",
            "Güvenlik açığı riski", "Performans/ölçeklenebilirlik riski",
        };
        var issueTitlePool = new[]
        {
            "Test ortamı erişim sorunu", "Onay sürecinde gecikme", "Eksik gereksinim dokümantasyonu",
            "Entegrasyon test hatası", "Performans sorunu (yavaş yanıt süresi)", "Üçüncü parti API kesintisi",
            "Kaynak yetersizliği", "İletişim/koordinasyon eksikliği",
        };

        var projectRisks = new List<ProjectRisk>();
        var projectIssues = new List<ProjectIssue>();
        foreach (var project in projects)
        {
            var projectEmployeeIds = project.EmployeeIds.ToList();
            Guid? PickOwner() => projectEmployeeIds.Count > 0 ? projectEmployeeIds[random.Next(projectEmployeeIds.Count)] : null;

            var riskTitles = riskTitlePool.OrderBy(_ => random.Next()).Take(random.Next(2, 5)).ToList();
            for (var i = 0; i < riskTitles.Count; i++)
            {
                // En az biri yüksek olasılık×etki ile "kritik" bir demo senaryosu oluşturur.
                var probability = i == 0 ? random.Next(4, 6) : random.Next(1, 6);
                var impact = i == 0 ? random.Next(4, 6) : random.Next(1, 6);
                var risk = ProjectRisk.Create(
                    project.Id, riskTitles[i], bogus.Lorem.Sentence(8, 4), probability, impact,
                    random.NextDouble() > 0.4 ? bogus.Lorem.Sentence(6, 4) : null, PickOwner(),
                    todayDate.AddDays(-random.Next(5, 90)));

                if (i > 0 && random.NextDouble() < 0.3) risk.SetStatus(ProjectRiskStatus.Mitigating);
                else if (i > 1 && random.NextDouble() < 0.2) risk.SetStatus(ProjectRiskStatus.Closed);

                projectRisks.Add(risk);
            }

            var issueTitles = issueTitlePool.OrderBy(_ => random.Next()).Take(random.Next(2, 5)).ToList();
            for (var i = 0; i < issueTitles.Count; i++)
            {
                var priority = (ProjectIssuePriority)random.Next(1, 5);
                // En az biri süresi geçmiş (overdue) ve hâlâ açık — Issues sekmesindeki kırmızı
                // vurgu demo senaryosu için.
                var dueDate = i == 0 ? todayDate.AddDays(-random.Next(1, 15)) : todayDate.AddDays(random.Next(-10, 30));
                var issue = ProjectIssue.Create(project.Id, issueTitles[i], bogus.Lorem.Sentence(8, 4), priority, PickOwner(), dueDate);

                if (i == 0)
                {
                    issue.SetStatus(ProjectIssueStatus.Open);
                }
                else
                {
                    var roll = random.NextDouble();
                    issue.SetStatus(
                        roll < 0.4 ? ProjectIssueStatus.Open
                        : roll < 0.7 ? ProjectIssueStatus.InProgress
                        : roll < 0.9 ? ProjectIssueStatus.Resolved
                        : ProjectIssueStatus.Closed);
                }

                projectIssues.Add(issue);
            }
        }
        context.ProjectRisks.AddRange(projectRisks);
        context.ProjectIssues.AddRange(projectIssues);

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
                    var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                    // Hafta içi: ayın tamamı kapsansın diye yüksek olasılık (%90). Hafta sonu:
                    // çok daha düşük ama SIFIR DEĞİL bir olasılık (%12) — Planlama Doğruluğu'nun
                    // hafta sonu sütunlarının her zaman boş görünmesini önlemek için gerçekçi bir
                    // "ara sıra hafta sonu çalışma" senaryosu (hem Actual hem Planned tarafında).
                    var presenceProbability = isWeekend ? 0.12 : 0.9;
                    if (random.NextDouble() > presenceProbability)
                        continue;

                    // Hafta sonu çalışması genelde kısa/münferit olur (acil müdahale, yetişmeyen
                    // iş) — hafta içi ~3-8h yerine ~1-4h.
                    var dailyTotal = isWeekend
                        ? Math.Round((decimal)(random.NextDouble() * 3 + 1), 2)
                        : Math.Round((decimal)(random.NextDouble() * 5 + 3), 2);
                    foreach (var chunkHours in SplitIntoChunks(dailyTotal))
                    {
                        var project = assignedProjects[random.Next(assignedProjects.Count)];

                        var activityL1 = topLevelActivities[random.Next(topLevelActivities.Count)];
                        var candidatesL2 = subActivities.Where(a => a.ParentActivityId == activityL1.Id).ToList();
                        var activityL2 = candidatesL2[random.Next(candidatesL2.Count)];

                        result.Add(EmployeeWorkLog.Create(
                            employee.Id,
                            project.Id,
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

        // Güvenilirlik Skoru motorunu (bkz. lib/confidenceScore.ts) demo'da gösterebilmek için,
        // bilinçli olarak "dikkatsiz/tahmini girilmiş" birkaç Actual kayıt ekleniyor. Mevcut ayın
        // (monthStart..todayDate) dışında, 5 hafta öncesine denk gelen tamamen ayrı bir haftaya
        // yerleştiriliyor ki rastgele üretilen normal kayıtlarla asla çakışmasın (temiz, deterministik
        // bir demo senaryosu olsun).
        var badDataMonday = todayDate.AddDays(-(((int)todayDate.DayOfWeek + 6) % 7) - 35);
        var badDataEmployees = employeeProjects.Where(kv => kv.Value.Count > 0).Take(2).Select(kv => kv.Key).ToList();

        void AddBadLog(Guid employeeId, DateOnly date, decimal hours, string description)
        {
            var project = employeeProjects[employeeId][0];
            var activityL1 = topLevelActivities[0];
            var activityL2 = subActivities.First(a => a.ParentActivityId == activityL1.Id);
            workLogs.Add(EmployeeWorkLog.Create(
                employeeId, project.Id, activityL1.Id, activityL2.Id, date, hours, description, WorkLogEntryType.Actual));
        }

        if (badDataEmployees.Count >= 1)
        {
            var e1 = badDataEmployees[0];
            AddBadLog(e1, badDataMonday, 8m, "toplantı"); // jenerik + kısa + tam saat (tekil) + günlük toplam yuvarlak
            AddBadLog(e1, badDataMonday.AddDays(1), 8m, "toplantı"); // önceki günle birebir aynı açıklama (tekrar)
            AddBadLog(e1, badDataMonday.AddDays(2), 7m, "genel işler"); // + aşağıdakiyle birlikte günlük toplam 13h (şüpheli)
            AddBadLog(e1, badDataMonday.AddDays(2), 6m, "rutin işler");
            AddBadLog(e1, badDataMonday.AddDays(3), 5m, "iş"); // uzun süre + çok kısa açıklama orantısızlığı
            AddBadLog(e1, badDataMonday.AddDays(5), 4m, "email"); // hafta sonu (Cumartesi) + jenerik + kısa
        }

        if (badDataEmployees.Count >= 2)
        {
            var e2 = badDataEmployees[1];
            AddBadLog(e2, badDataMonday, 3m, "misc");
            AddBadLog(e2, badDataMonday.AddDays(1), 3m, "misc"); // tekrar
            AddBadLog(e2, badDataMonday.AddDays(6), 2m, "ofis işleri"); // hafta sonu (Pazar) + jenerik
        }

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

        // Güvenilirlik skoru ayarları — tek satırlık, varsayılan değerlerle (bkz.
        // ConfidenceScoreSettings.CreateDefault) sabit bir Id ile seed edilir; admin panelinden
        // güncellenebilir.
        context.ConfidenceScoreSettings.Add(ConfidenceScoreSettings.CreateDefault(ConfidenceScoreSettingsSeedData.SettingsId));

        await context.SaveChangesAsync();
    }
}
