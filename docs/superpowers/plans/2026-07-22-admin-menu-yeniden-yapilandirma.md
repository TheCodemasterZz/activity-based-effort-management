# Admin Menü Yeniden Yapılandırma Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `AdminPage.tsx`'in "KULLANICI YÖNETİMİ" sol menüsünü iki gruba ayırmak (KULLANICI
YÖNETİMİ / ACTIVE DIRECTORY), "Alan Eşlemeleri"ni ("AD Attributes" adıyla) dizine özel hale
getirip menüden kaldırıp dizin detayına taşımak, ve dizine-özel "Kullanıcılar" ekranını tüm
dizinleri kapsayan tek bir bağımsız "Kullanıcılar" menü öğesiyle değiştirmek.

**Architecture:** Backend'de `DirectoryAttributeMapping` entity'sine `DirectoryId` eklenir (domain
+ persistence migration + CQRS + nested API route). Frontend'de: (1) `AdminPage.tsx`'in
`ADMIN_TABS` tanımı iki `AdminGroup`'a bölünür, (2) `AttributeMappingsSection.tsx` dizine özel
hale gelip `UserDirectorySection.tsx`'in bir alt-görünümü olur, `DirectoryList.tsx`'e "AD
Attributes" linki eklenir, (3) yeni `UsersSection.tsx`, mevcut `DirectoryUserList.tsx`'in
mantığını dizin bağımsız hale getirip dizin filtresi + "Dizin" sütunu ekler, `DirectoryUserList.tsx`
silinir.

**Tech Stack:** ASP.NET Core (.NET), EF Core + PostgreSQL (Npgsql), MediatR (CQRS), Mapster,
FluentValidation, React 19, TypeScript, Tailwind 4, TanStack Query 5.

## Global Constraints

- Sistemde şu an hiç `DirectoryAttributeMapping` kaydı yok — migration'da backfill/veri taşıma
  adımı gerekmez.
- Aynı kişinin birden fazla AD dizininde tanımlı olması durumunda birleştirme (deduplication)
  yapılmaz; her `DirectoryUser` kaydı bağımsız bir satır olarak kalır, "Dizin" sütunuyla ayırt
  edilir. Bu davranışı değiştirecek hiçbir kod yazılmaz.
- Kullanıcıya gösterilen tüm metinlerde "Alan Eşlemesi/Eşlemeleri" ifadesi **"AD Attribute"**
  olarak değiştirilir (İngilizce, kullanıcı talebi).
- Proje genelinde component-level otomatik frontend testi altyapısı yok; frontend doğrulaması
  `npx tsc --noEmit` (tip kontrolü) ve tarayıcıda manuel uçtan uca yapılır.
- Backend'de her adımdan sonra ilgili test projesi (`dotnet test`) yeşil kalmalı.
- Var olan Tailwind sınıf/renk konvansiyonlarına uyulur (`slate`/`indigo` paleti, `rounded-md`,
  `text-sm`).

---

### Task 1: `DirectoryAttributeMapping` domain entity'sine `DirectoryId` ekle

**Files:**
- Modify: `backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs` (tamamı)
- Modify: `backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs` (tamamı)

**Interfaces:**
- Produces: `DirectoryAttributeMapping.Create(Guid directoryId, string adAttributeName, string
  systemFieldName, string fieldType, bool isSynced, int sortOrder)` — Task 2 (persistence config)
  ve Task 3 (CQRS) bu yeni imzayı kullanacak. `DirectoryAttributeMapping.DirectoryId` (Guid,
  public get) — Task 2 EF konfigürasyonunda FK olarak kullanılacak.

- [ ] **Step 1: Testleri yeni imzaya göre güncelle**

`backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs` dosyasının
tamamını şununla değiştir:

```csharp
using EforTakip.Domain.Directories;
using EforTakip.Domain.Exceptions;
using FluentAssertions;

namespace EforTakip.Domain.Tests.Directories;

public class DirectoryAttributeMappingTests
{
    private static readonly Guid DirectoryId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_CreatesMapping()
    {
        var mapping = DirectoryAttributeMapping.Create(DirectoryId, "company", "Kurum", "text", true, 0);

        mapping.DirectoryId.Should().Be(DirectoryId);
        mapping.AdAttributeName.Should().Be("company");
        mapping.SystemFieldName.Should().Be("Kurum");
        mapping.FieldType.Should().Be("text");
        mapping.IsSynced.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyDirectoryId_Throws()
    {
        var act = () => DirectoryAttributeMapping.Create(Guid.Empty, "company", "Kurum", "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Theory]
    [InlineData("", "Kurum")]
    [InlineData("company", "")]
    [InlineData("   ", "Kurum")]
    public void Create_WithEmptyNames_Throws(string adName, string systemName)
    {
        var act = () => DirectoryAttributeMapping.Create(DirectoryId, adName, systemName, "text", true, 0);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var mapping = DirectoryAttributeMapping.Create(DirectoryId, "company", "Kurum", "text", true, 0);

        mapping.Update("department", "Departman", "text", false, 1);

        mapping.AdAttributeName.Should().Be("department");
        mapping.SystemFieldName.Should().Be("Departman");
        mapping.IsSynced.Should().BeFalse();
        mapping.SortOrder.Should().Be(1);
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryAttributeMappingTests`
Expected: FAIL — `DirectoryAttributeMapping.Create` metodu `Guid directoryId` parametresi almıyor, derleme hatası.

- [ ] **Step 3: Entity'yi güncelle**

`backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs` dosyasının tamamını
şununla değiştir:

```csharp
using EforTakip.Domain.Common;
using EforTakip.Domain.Exceptions;

namespace EforTakip.Domain.Directories;

public sealed class DirectoryAttributeMapping : Entity, IAggregateRoot
{
    /// <summary>
    /// AD'de DN (Distinguished Name) formatında gelen alanlar için (ör. manager). Senkronizasyon
    /// sırasında bu tip, değerin aynı taramadaki başka bir kullanıcıya referans olarak
    /// çözümlenmeye çalışılmasını tetikler.
    /// </summary>
    public const string UserReferenceFieldType = "user";

    /// <summary>
    /// AD'de ikili (binary) gelen alanlar için (ör. thumbnailPhoto). Senkronizasyon sırasında
    /// metin gibi UTF-8 çözülmeye çalışılmaz; ham baytlar Base64'e çevrilip Value alanında saklanır.
    /// </summary>
    public const string PhotoFieldType = "photo";

    public Guid DirectoryId { get; private set; }
    public string AdAttributeName { get; private set; } = default!;
    public string SystemFieldName { get; private set; } = default!;
    public string FieldType { get; private set; } = default!;
    public bool IsSynced { get; private set; }
    public int SortOrder { get; private set; }

    private DirectoryAttributeMapping()
    {
        // EF Core
    }

    public static DirectoryAttributeMapping Create(
        Guid directoryId, string adAttributeName, string systemFieldName, string fieldType,
        bool isSynced, int sortOrder)
    {
        if (directoryId == Guid.Empty)
            throw new BusinessRuleValidationException("AD Attribute bir dizine bağlı olmalıdır.");

        Validate(adAttributeName, systemFieldName, fieldType);

        return new DirectoryAttributeMapping
        {
            DirectoryId = directoryId,
            AdAttributeName = adAttributeName.Trim(),
            SystemFieldName = systemFieldName.Trim(),
            FieldType = fieldType.Trim(),
            IsSynced = isSynced,
            SortOrder = sortOrder
        };
    }

    public void Update(
        string adAttributeName, string systemFieldName, string fieldType, bool isSynced, int sortOrder)
    {
        Validate(adAttributeName, systemFieldName, fieldType);

        AdAttributeName = adAttributeName.Trim();
        SystemFieldName = systemFieldName.Trim();
        FieldType = fieldType.Trim();
        IsSynced = isSynced;
        SortOrder = sortOrder;
    }

    private static void Validate(string adAttributeName, string systemFieldName, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(adAttributeName))
            throw new BusinessRuleValidationException("AD alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(systemFieldName))
            throw new BusinessRuleValidationException("Sistem alan adı boş olamaz.");
        if (string.IsNullOrWhiteSpace(fieldType))
            throw new BusinessRuleValidationException("Alan tipi boş olamaz.");
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Domain.Tests/EforTakip.Domain.Tests.csproj --filter DirectoryAttributeMappingTests`
Expected: PASS (5 test).

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Domain/Directories/DirectoryAttributeMapping.cs backend/tests/EforTakip.Domain.Tests/Directories/DirectoryAttributeMappingTests.cs
git commit -m "feat: scope DirectoryAttributeMapping to a single directory"
```

---

### Task 2: Persistence konfigürasyonu ve migration

**Files:**
- Modify: `backend/src/EforTakip.Persistence/Configurations/DirectoryAttributeMappingConfiguration.cs` (tamamı)
- Create: migration dosyaları (`dotnet ef migrations add` ile üretilecek)

**Interfaces:**
- Consumes: `DirectoryAttributeMapping.DirectoryId` (Task 1), `Directory`
  (`backend/src/EforTakip.Domain/Directories/Directory.cs`, değişmedi).
- Produces: `DirectoryAttributeMappings` tablosunda `DirectoryId` sütunu + `Directories` FK'ı +
  `(DirectoryId, AdAttributeName)` unique index — Task 3'teki sorgu/komutlar bu şemaya güvenecek.

- [ ] **Step 1: EF konfigürasyonunu güncelle**

`backend/src/EforTakip.Persistence/Configurations/DirectoryAttributeMappingConfiguration.cs`
dosyasının tamamını şununla değiştir:

```csharp
using EforTakip.Domain.Directories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Directory = EforTakip.Domain.Directories.Directory;

namespace EforTakip.Persistence.Configurations;

public sealed class DirectoryAttributeMappingConfiguration : IEntityTypeConfiguration<DirectoryAttributeMapping>
{
    public void Configure(EntityTypeBuilder<DirectoryAttributeMapping> builder)
    {
        builder.ToTable("DirectoryAttributeMappings");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.AdAttributeName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.SystemFieldName).IsRequired().HasMaxLength(150);
        builder.Property(m => m.FieldType).IsRequired().HasMaxLength(50);

        builder.HasIndex(m => new { m.DirectoryId, m.AdAttributeName }).IsUnique();

        builder.HasOne<Directory>()
            .WithMany()
            .HasForeignKey(m => m.DirectoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 2: Migration oluştur**

Run:
```bash
dotnet ef migrations add AddDirectoryIdToAttributeMappings \
  --project backend/src/EforTakip.Persistence \
  --startup-project backend/src/EforTakip.Api
```
Expected: `Migrations\<timestamp>_AddDirectoryIdToAttributeMappings.cs` ve `.Designer.cs`
dosyaları oluşur, `EforTakipDbContextModelSnapshot.cs` güncellenir. Üretilen migration dosyasını
aç ve `Up` metodunda `AddColumn<Guid>("DirectoryId", "DirectoryAttributeMappings", ...)`,
`CreateIndex` (unique, `DirectoryId`+`AdAttributeName`) ve `AddForeignKey` (→ `Directories`,
`ON DELETE CASCADE`) adımlarının bulunduğunu doğrula. Tabloda hiç kayıt olmadığı için
`DirectoryId` sütunu `nullable: false` olarak eklenebilir (var olan satır olmadığından bir
varsayılan değer sorunu çıkmaz).

- [ ] **Step 3: Migration'ı uygula**

Run: `cd backend/src/EforTakip.Api && dotnet ef database update --project ../EforTakip.Persistence`
Expected: `Applying migration '..._AddDirectoryIdToAttributeMappings'.` ve hatasız tamamlanma.

- [ ] **Step 4: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 5: Commit**

```bash
git add backend/src/EforTakip.Persistence/Configurations/DirectoryAttributeMappingConfiguration.cs backend/src/EforTakip.Persistence/Migrations/
git commit -m "feat: add DirectoryId FK and unique index to DirectoryAttributeMappings"
```

---

### Task 3: CQRS katmanını dizine özel hale getir

**Files:**
- Modify: `backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings/GetAttributeMappingsQuery.cs`
- Modify: `backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings/GetAttributeMappingsQueryHandler.cs`
- Modify: `backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping/CreateAttributeMappingCommand.cs`
- Modify: `backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping/CreateAttributeMappingCommandHandler.cs`
- Modify: `backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `DirectoryAttributeMapping.Create(Guid directoryId, ...)` (Task 1).
- Produces: `GetAttributeMappingsQuery(Guid DirectoryId)`, `CreateAttributeMappingCommand(Guid
  DirectoryId, string AdAttributeName, string SystemFieldName, string FieldType, bool IsSynced,
  int SortOrder)` — Task 4 (API controller) bu imzaları kullanacak.
  `UpdateAttributeMappingCommand` ve `DeleteAttributeMappingCommand` **değişmiyor** (spesifikasyon
  kararı: bir eşleme başka dizine taşınamaz, güncelleme/silme zaten `Id` ile çalışıyor).

- [ ] **Step 1: `CreateAttributeMappingCommandHandlerTests`'i güncelle**

`backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs`
dosyasının tamamını şununla değiştir:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Domain.Directories;
using FluentAssertions;
using NSubstitute;

namespace EforTakip.Application.Tests.Directories.Commands;

public class CreateAttributeMappingCommandHandlerTests
{
    private readonly IRepository<DirectoryAttributeMapping> _repository =
        Substitute.For<IRepository<DirectoryAttributeMapping>>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_CreatesMapping()
    {
        var directoryId = Guid.NewGuid();
        var handler = new CreateAttributeMappingCommandHandler(_repository, _unitOfWork);
        var command = new CreateAttributeMappingCommand(directoryId, "company", "Kurum", "text", true, 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(
            Arg.Is<DirectoryAttributeMapping>(m =>
                m.DirectoryId == directoryId && m.AdAttributeName == "company" && m.SystemFieldName == "Kurum"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Testi çalıştır, başarısız olduğunu doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateAttributeMappingCommandHandlerTests`
Expected: FAIL — `CreateAttributeMappingCommand` 6 parametre almıyor, derleme hatası.

- [ ] **Step 3: `CreateAttributeMappingCommand` ve handler'ı güncelle**

`backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping/CreateAttributeMappingCommand.cs`:

```csharp
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed record CreateAttributeMappingCommand(
    Guid DirectoryId,
    string AdAttributeName,
    string SystemFieldName,
    string FieldType,
    bool IsSynced,
    int SortOrder) : IRequest<Guid>;
```

`backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping/CreateAttributeMappingCommandHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Domain.Directories;
using MediatR;

namespace EforTakip.Application.Directories.Commands.CreateAttributeMapping;

public sealed class CreateAttributeMappingCommandHandler(
    IRepository<DirectoryAttributeMapping> repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAttributeMappingCommand, Guid>
{
    public async Task<Guid> Handle(CreateAttributeMappingCommand request, CancellationToken cancellationToken)
    {
        var mapping = DirectoryAttributeMapping.Create(
            request.DirectoryId, request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);

        await repository.AddAsync(mapping, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapping.Id;
    }
}
```

- [ ] **Step 4: Testi çalıştır, geçtiğini doğrula**

Run: `dotnet test backend/tests/EforTakip.Application.Tests/EforTakip.Application.Tests.csproj --filter CreateAttributeMappingCommandHandlerTests`
Expected: PASS.

- [ ] **Step 5: `GetAttributeMappingsQuery` ve handler'ı güncelle**

`backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings/GetAttributeMappingsQuery.cs`:

```csharp
using EforTakip.Application.Directories.Dtos;
using MediatR;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed record GetAttributeMappingsQuery(Guid DirectoryId)
    : IRequest<IReadOnlyCollection<DirectoryAttributeMappingDto>>;
```

`backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings/GetAttributeMappingsQueryHandler.cs`:

```csharp
using EforTakip.Application.Common.Interfaces;
using EforTakip.Application.Directories.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EforTakip.Application.Directories.Queries.GetAttributeMappings;

public sealed class GetAttributeMappingsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAttributeMappingsQuery, IReadOnlyCollection<DirectoryAttributeMappingDto>>
{
    public async Task<IReadOnlyCollection<DirectoryAttributeMappingDto>> Handle(
        GetAttributeMappingsQuery request, CancellationToken cancellationToken)
    {
        return await db.DirectoryAttributeMappings
            .AsNoTracking()
            .Where(m => m.DirectoryId == request.DirectoryId)
            .OrderBy(m => m.SortOrder)
            .ProjectToType<DirectoryAttributeMappingDto>()
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Backend derlemesini ve tüm testleri doğrula**

Run: `dotnet build backend/EforTakip.sln && dotnet test backend/EforTakip.sln`
Expected: Derleme başarılı, tüm testler PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/EforTakip.Application/Directories/Queries/GetAttributeMappings backend/src/EforTakip.Application/Directories/Commands/CreateAttributeMapping backend/tests/EforTakip.Application.Tests/Directories/Commands/CreateAttributeMappingCommandHandlerTests.cs
git commit -m "feat: scope attribute mapping query/create command to a directory"
```

---

### Task 4: API route'unu dizin altına nest et

**Files:**
- Modify: `backend/src/EforTakip.Api/Controllers/v1/DirectoryAttributeMappingsController.cs` (tamamı)

**Interfaces:**
- Consumes: `GetAttributeMappingsQuery(Guid DirectoryId)`, `CreateAttributeMappingCommand(Guid
  DirectoryId, ...)` (Task 3), `UpdateAttributeMappingCommand`, `DeleteAttributeMappingCommand`
  (değişmedi).
- Produces: `api/v1/directories/{directoryId:guid}/attribute-mappings` route grubu — Task 5
  (frontend api client) bu route'ları çağıracak.

- [ ] **Step 1: Controller'ı nested route'a taşı**

`backend/src/EforTakip.Api/Controllers/v1/DirectoryAttributeMappingsController.cs` dosyasının
tamamını şununla değiştir:

```csharp
using Asp.Versioning;
using EforTakip.Application.Directories.Commands.CreateAttributeMapping;
using EforTakip.Application.Directories.Commands.DeleteAttributeMapping;
using EforTakip.Application.Directories.Commands.UpdateAttributeMapping;
using EforTakip.Application.Directories.Dtos;
using EforTakip.Application.Directories.Queries.GetAttributeMappings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EforTakip.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/directories/{directoryId:guid}/attribute-mappings")]
public sealed class DirectoryAttributeMappingsController(ISender mediator) : ControllerBase
{
    public sealed record CreateAttributeMappingRequest(
        string AdAttributeName, string SystemFieldName, string FieldType, bool IsSynced, int SortOrder);

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DirectoryAttributeMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<DirectoryAttributeMappingDto>>> GetAll(
        Guid directoryId, CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetAttributeMappingsQuery(directoryId), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        Guid directoryId, CreateAttributeMappingRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAttributeMappingCommand(
            directoryId, request.AdAttributeName, request.SystemFieldName, request.FieldType,
            request.IsSynced, request.SortOrder);
        var id = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { version = "1.0", directoryId }, new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(
        Guid directoryId, Guid id, UpdateAttributeMappingCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route ve gövde kimlikleri eşleşmiyor.");
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid directoryId, Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAttributeMappingCommand(id), cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 2: Backend derlemesini doğrula**

Run: `dotnet build backend/EforTakip.sln`
Expected: Derleme başarılı, hata yok.

- [ ] **Step 3: Commit**

```bash
git add backend/src/EforTakip.Api/Controllers/v1/DirectoryAttributeMappingsController.cs
git commit -m "feat: nest attribute mappings API under /directories/{directoryId}"
```

---

### Task 5: Frontend API client ve hook'u dizine özel hale getir

**Files:**
- Modify: `frontend/src/api/directoryAttributeMappings.ts` (tamamı)
- Modify: `frontend/src/hooks/useAttributeMappings.ts` (tamamı)

**Interfaces:**
- Consumes: nested API route'ları (Task 4).
- Produces: `useAttributeMappings(directoryId: string)`,
  `useCreateAttributeMappingMutation(directoryId: string)`,
  `useUpdateAttributeMappingMutation(directoryId: string)`,
  `useDeleteAttributeMappingMutation(directoryId: string)` — Task 6
  (`AttributeMappingsSection.tsx`) bu hook'ları kullanacak.

- [ ] **Step 1: API client fonksiyonlarını güncelle**

`frontend/src/api/directoryAttributeMappings.ts` dosyasının tamamını şununla değiştir:

```ts
import { apiClient } from './client';
import type { DirectoryAttributeMappingDto } from './types';

export interface SaveAttributeMappingPayload {
  adAttributeName: string;
  systemFieldName: string;
  fieldType: string;
  isSynced: boolean;
  sortOrder: number;
}

export function getAttributeMappings(directoryId: string) {
  return apiClient.get<DirectoryAttributeMappingDto[]>(
    `/api/v1/directories/${directoryId}/attribute-mappings`,
  );
}

export function createAttributeMapping(directoryId: string, payload: SaveAttributeMappingPayload) {
  return apiClient.post<{ id: string }>(
    `/api/v1/directories/${directoryId}/attribute-mappings`,
    payload,
  );
}

export function updateAttributeMapping(
  directoryId: string,
  id: string,
  payload: SaveAttributeMappingPayload,
) {
  return apiClient.put<void>(
    `/api/v1/directories/${directoryId}/attribute-mappings/${id}`,
    { ...payload, id },
  );
}

export function deleteAttributeMapping(directoryId: string, id: string) {
  return apiClient.delete<void>(`/api/v1/directories/${directoryId}/attribute-mappings/${id}`);
}
```

- [ ] **Step 2: Hook'ları güncelle**

`frontend/src/hooks/useAttributeMappings.ts` dosyasının tamamını şununla değiştir:

```ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createAttributeMapping,
  deleteAttributeMapping,
  getAttributeMappings,
  updateAttributeMapping,
  type SaveAttributeMappingPayload,
} from '../api/directoryAttributeMappings';

const queryKey = (directoryId: string) => ['directoryAttributeMappings', directoryId];

export function useAttributeMappings(directoryId: string) {
  return useQuery({
    queryKey: queryKey(directoryId),
    queryFn: () => getAttributeMappings(directoryId),
  });
}

export function useCreateAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: SaveAttributeMappingPayload) => createAttributeMapping(directoryId, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}

export function useUpdateAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SaveAttributeMappingPayload }) =>
      updateAttributeMapping(directoryId, id, payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}

export function useDeleteAttributeMappingMutation(directoryId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteAttributeMapping(directoryId, id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(directoryId) }),
  });
}
```

- [ ] **Step 3: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: `AttributeMappingsSection.tsx`'te hook çağrılarının artık `directoryId` beklediğine dair
hatalar görülecek (Task 6'da düzeltilecek) — bu adımda bunun dışında hata olmamalı.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/api/directoryAttributeMappings.ts frontend/src/hooks/useAttributeMappings.ts
git commit -m "feat: scope attribute mapping frontend API/hooks to a directory"
```

---

### Task 6: `AttributeMappingsSection.tsx`'i dizine özel ve "AD Attributes" adıyla güncelle

**Files:**
- Modify: `frontend/src/components/admin/directory/AttributeMappingsSection.tsx` (tamamı)

**Interfaces:**
- Consumes: `useAttributeMappings(directoryId)`, `useCreateAttributeMappingMutation(directoryId)`,
  `useUpdateAttributeMappingMutation(directoryId)`, `useDeleteAttributeMappingMutation(directoryId)`
  (Task 5), `DirectoryDto` (`frontend/src/api/types.ts`, değişmedi).
- Produces: `AttributeMappingsSection` bileşeni, prop'lar: `{ directory: DirectoryDto; onBack: ()
  => void }` (eski prop almayan haliyle karşılaştırıldığında artık dizin context'i alıyor) —
  Task 7 (`UserDirectorySection.tsx`) bunu kullanacak.

- [ ] **Step 1: Dosyanın tamamını yeni implementasyonla değiştir**

`frontend/src/components/admin/directory/AttributeMappingsSection.tsx` dosyasının tamamını
şununla değiştir:

```tsx
import { useState, type FormEvent } from 'react';
import { ApiError } from '../../../api/client';
import {
  useAttributeMappings,
  useCreateAttributeMappingMutation,
  useDeleteAttributeMappingMutation,
  useUpdateAttributeMappingMutation,
} from '../../../hooks/useAttributeMappings';
import type { DirectoryAttributeMappingDto, DirectoryDto } from '../../../api/types';

const inputClass =
  'w-full rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500';

interface AttributeMappingsSectionProps {
  directory: DirectoryDto;
  onBack: () => void;
}

export function AttributeMappingsSection({ directory, onBack }: AttributeMappingsSectionProps) {
  const mappings = useAttributeMappings(directory.id);
  const createMutation = useCreateAttributeMappingMutation(directory.id);
  const updateMutation = useUpdateAttributeMappingMutation(directory.id);
  const deleteMutation = useDeleteAttributeMappingMutation(directory.id);

  const [adAttributeName, setAdAttributeName] = useState('');
  const [systemFieldName, setSystemFieldName] = useState('');
  const [fieldType, setFieldType] = useState('text');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const items = mappings.data ?? [];

  const handleCreate = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage(null);

    try {
      await createMutation.mutateAsync({
        adAttributeName: adAttributeName.trim(),
        systemFieldName: systemFieldName.trim(),
        fieldType,
        isSynced: true,
        sortOrder: items.length,
      });
      setAdAttributeName('');
      setSystemFieldName('');
      setFieldType('text');
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute eklenemedi.');
    }
  };

  const handleToggleSynced = async (mapping: DirectoryAttributeMappingDto) => {
    setErrorMessage(null);
    try {
      await updateMutation.mutateAsync({
        id: mapping.id,
        payload: {
          adAttributeName: mapping.adAttributeName,
          systemFieldName: mapping.systemFieldName,
          fieldType: mapping.fieldType,
          isSynced: !mapping.isSynced,
          sortOrder: mapping.sortOrder,
        },
      });
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute güncellenemedi.');
    }
  };

  const handleDelete = async (mapping: DirectoryAttributeMappingDto) => {
    if (!window.confirm(`"${mapping.systemFieldName}" AD Attribute'unu silmek istediğinize emin misiniz?`))
      return;

    setErrorMessage(null);
    try {
      await deleteMutation.mutateAsync(mapping.id);
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : 'AD Attribute silinemedi.');
    }
  };

  const canCreate = adAttributeName.trim().length > 0 && systemFieldName.trim().length > 0;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-base font-semibold text-slate-800">{directory.name} — AD Attributes</h2>
        <button
          type="button"
          onClick={onBack}
          className="text-sm text-slate-500 hover:text-slate-700"
        >
          ← Listeye dön
        </button>
      </div>

      <form onSubmit={handleCreate} className="mb-5 flex flex-wrap items-end gap-2">
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Dizindeki Alan</span>
          <input
            value={adAttributeName}
            onChange={(e) => setAdAttributeName(e.target.value)}
            placeholder="company"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Sistemdeki Ad</span>
          <input
            value={systemFieldName}
            onChange={(e) => setSystemFieldName(e.target.value)}
            placeholder="Kurum"
            className={inputClass}
          />
        </label>
        <label className="block">
          <span className="mb-1 block text-xs font-medium text-slate-600">Tip</span>
          <select
            value={fieldType}
            onChange={(e) => setFieldType(e.target.value)}
            className={inputClass}
          >
            <option value="text">Metin</option>
            <option value="user">Kullanıcı</option>
            <option value="photo">Fotoğraf</option>
          </select>
        </label>
        <button
          type="submit"
          disabled={!canCreate || createMutation.isPending}
          className="rounded-lg bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:bg-slate-300"
        >
          {createMutation.isPending ? 'Ekleniyor…' : 'Ekle'}
        </button>
      </form>

      {errorMessage && (
        <p role="alert" className="mb-4 rounded-md bg-rose-50 px-3 py-2 text-sm text-rose-700">
          {errorMessage}
        </p>
      )}

      {mappings.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          Henüz AD Attribute tanımlanmamış.
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Dizindeki Alan</th>
              <th className="py-2 pr-4 font-medium">Sistemdeki Ad</th>
              <th className="py-2 pr-4 font-medium">Tip</th>
              <th className="py-2 pr-4 font-medium">Senkronize</th>
              <th className="py-2 font-medium">İşlem</th>
            </tr>
          </thead>
          <tbody>
            {items.map((mapping) => (
              <tr key={mapping.id} className="border-b border-slate-50 last:border-0">
                <td className="py-2 pr-4 font-mono text-xs text-slate-600">
                  {mapping.adAttributeName}
                </td>
                <td className="py-2 pr-4 text-slate-700">{mapping.systemFieldName}</td>
                <td className="py-2 pr-4 text-slate-500">
                  {mapping.fieldType === 'user'
                    ? 'Kullanıcı'
                    : mapping.fieldType === 'photo'
                      ? 'Fotoğraf'
                      : 'Metin'}
                </td>
                <td className="py-2 pr-4">
                  <input
                    type="checkbox"
                    checked={mapping.isSynced}
                    onChange={() => handleToggleSynced(mapping)}
                    className="h-4 w-4"
                  />
                </td>
                <td className="py-2">
                  <button
                    type="button"
                    onClick={() => handleDelete(mapping)}
                    className="text-xs text-rose-600 hover:underline"
                  >
                    Sil
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: `AttributeMappingsSection.tsx` için hata yok. `AdminPage.tsx` ve
`UserDirectorySection.tsx`'te bu bileşenin eski (prop'suz) kullanımına dair hatalar görülecek —
bunlar Task 7 ve Task 10'da düzeltilecek.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/admin/directory/AttributeMappingsSection.tsx
git commit -m "feat: make AttributeMappingsSection directory-scoped and rename to AD Attributes"
```

---

### Task 7: AD Attributes'ı dizin detayına bağla, `DirectoryUserCard` metnini güncelle

**Files:**
- Modify: `frontend/src/components/admin/directory/UserDirectorySection.tsx` (tamamı)
- Modify: `frontend/src/components/admin/directory/DirectoryList.tsx:22-26,143-151` (props + JSX)
- Modify: `frontend/src/components/admin/directory/DirectoryUserCard.tsx:207-211` (metin)

**Interfaces:**
- Consumes: `AttributeMappingsSection` (Task 6, `{ directory, onBack }`).
- Produces: `UserDirectorySection` artık dizine özel kullanıcı listesi (`{ kind: 'users' }` /
  `{ kind: 'userDetail' }`) görünümlerini içermiyor — bunlar Task 9'da bağımsız `UsersSection`'a
  taşınacak.

- [ ] **Step 1: `UserDirectorySection.tsx`'i güncelle**

`frontend/src/components/admin/directory/UserDirectorySection.tsx` dosyasının tamamını şununla
değiştir:

```tsx
import { useState } from 'react';
import { AttributeMappingsSection } from './AttributeMappingsSection';
import { DirectoryForm } from './DirectoryForm';
import { DirectoryList } from './DirectoryList';
import type { DirectoryDto } from '../../../api/types';

type View =
  | { kind: 'list' }
  | { kind: 'form'; directory: DirectoryDto | null }
  | { kind: 'attributeMappings'; directory: DirectoryDto };

/** Uygulamada router yok; bu bölümün alt ekranları yerel görünüm durumuyla yönetilir. */
export function UserDirectorySection() {
  const [view, setView] = useState<View>({ kind: 'list' });

  if (view.kind === 'form') {
    return <DirectoryForm directory={view.directory} onClose={() => setView({ kind: 'list' })} />;
  }

  if (view.kind === 'attributeMappings') {
    return (
      <AttributeMappingsSection directory={view.directory} onBack={() => setView({ kind: 'list' })} />
    );
  }

  return (
    <DirectoryList
      onAdd={() => setView({ kind: 'form', directory: null })}
      onEdit={(directory) => setView({ kind: 'form', directory })}
      onViewAttributeMappings={(directory) => setView({ kind: 'attributeMappings', directory })}
    />
  );
}
```

- [ ] **Step 2: `DirectoryList.tsx`'teki "Kullanıcılar" linkini "AD Attributes" ile değiştir**

`frontend/src/components/admin/directory/DirectoryList.tsx` dosyasında satır 22-26'yı bul:

```tsx
interface DirectoryListProps {
  onAdd: () => void;
  onEdit: (directory: DirectoryDto) => void;
  onViewUsers: (directory: DirectoryDto) => void;
}
```

Şununla değiştir:

```tsx
interface DirectoryListProps {
  onAdd: () => void;
  onEdit: (directory: DirectoryDto) => void;
  onViewAttributeMappings: (directory: DirectoryDto) => void;
}
```

Aynı dosyada fonksiyon imzasını bul:

```tsx
export function DirectoryList({ onAdd, onEdit, onViewUsers }: DirectoryListProps) {
```

Şununla değiştir:

```tsx
export function DirectoryList({ onAdd, onEdit, onViewAttributeMappings }: DirectoryListProps) {
```

Aynı dosyada "İşlemler" sütunundaki (satır 143-151 civarı) şunu bul:

```tsx
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    <button
                      type="button"
                      onClick={() => onViewUsers(directory)}
                      className="text-indigo-600 hover:underline"
                    >
                      Kullanıcılar
                    </button>
                    {directory.source === 1 && (
```

Şununla değiştir:

```tsx
                <td className="py-2">
                  <div className="flex gap-2 text-xs">
                    {directory.source === 1 && (
                      <button
                        type="button"
                        onClick={() => onViewAttributeMappings(directory)}
                        className="text-indigo-600 hover:underline"
                      >
                        AD Attributes
                      </button>
                    )}
                    {directory.source === 1 && (
```

- [ ] **Step 3: `DirectoryUserCard.tsx`'teki metni güncelle**

`frontend/src/components/admin/directory/DirectoryUserCard.tsx` dosyasında satır 207-211'i bul:

```tsx
              <p className="py-2 text-sm text-slate-400">
                Senkronize edilmiş alan yok. Alan Eşlemeleri bölümünden alan tanımlayıp dizini
                yeniden senkronize edin.
              </p>
```

Şununla değiştir:

```tsx
              <p className="py-2 text-sm text-slate-400">
                Senkronize edilmiş alan yok. AD Attributes bölümünden alan tanımlayıp dizini
                yeniden senkronize edin.
              </p>
```

- [ ] **Step 4: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: `UserDirectorySection.tsx` ve `DirectoryList.tsx` için hata yok. `AdminPage.tsx`'teki
eski `'attributeMappings'` kind kullanımına dair hata devam edecek — Task 10'da düzeltilecek.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/components/admin/directory/UserDirectorySection.tsx frontend/src/components/admin/directory/DirectoryList.tsx frontend/src/components/admin/directory/DirectoryUserCard.tsx
git commit -m "feat: surface AD Attributes from the directory detail view"
```

---

### Task 8: Yeni `UsersSection.tsx` — tüm dizinler için tek kullanıcı listesi

**Files:**
- Create: `frontend/src/components/admin/directory/UsersSection.tsx`
- Delete: `frontend/src/components/admin/directory/DirectoryUserList.tsx`

**Interfaces:**
- Consumes: `useDirectories()` (`frontend/src/hooks/useDirectories.ts`, değişmedi),
  `useDirectoryUsers({ directoryId？, searchTerm?, pageNumber?, pageSize? })`
  (`frontend/src/hooks/useDirectoryUsers.ts`, değişmedi — `directoryId` zaten opsiyonel),
  `DirectoryUserCard` (`frontend/src/components/admin/directory/DirectoryUserCard.tsx`, Task 7
  sonrası metni güncellendi, prop imzası değişmedi: `{ userId, onBack?, onSelectUser? }`).
- Produces: `UsersSection` bileşeni, prop almaz — Task 10 (`AdminPage.tsx`) bunu render edecek.

- [ ] **Step 1: `DirectoryUserList.tsx`'i sil**

```bash
git rm frontend/src/components/admin/directory/DirectoryUserList.tsx
```

- [ ] **Step 2: `UsersSection.tsx`'i oluştur**

`frontend/src/components/admin/directory/UsersSection.tsx`:

```tsx
import { useState } from 'react';
import { useDirectories } from '../../../hooks/useDirectories';
import { useDirectoryUsers } from '../../../hooks/useDirectoryUsers';
import { DirectoryUserCard } from './DirectoryUserCard';

const PAGE_SIZE_OPTIONS = [25, 50, 100];

type View = { kind: 'list' } | { kind: 'detail'; userId: string };

export function UsersSection() {
  const [view, setView] = useState<View>({ kind: 'list' });
  const directories = useDirectories();
  const [selectedDirectoryId, setSelectedDirectoryId] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(PAGE_SIZE_OPTIONS[0]);

  const users = useDirectoryUsers({
    directoryId: selectedDirectoryId || undefined,
    searchTerm,
    pageNumber,
    pageSize,
  });
  const items = users.data?.items ?? [];
  const totalCount = users.data?.totalCount ?? 0;
  const totalPages = users.data?.totalPages ?? 1;

  if (view.kind === 'detail') {
    return (
      <DirectoryUserCard
        userId={view.userId}
        onBack={() => setView({ kind: 'list' })}
        onSelectUser={(userId) => setView({ kind: 'detail', userId })}
      />
    );
  }

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder="Kullanıcı adı, görünen ad veya e-posta ara"
          className="w-full max-w-sm rounded-md border border-slate-300 px-3 py-2 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
        />

        <label className="flex items-center gap-2 text-sm text-slate-500">
          Dizin
          <select
            value={selectedDirectoryId}
            onChange={(e) => {
              setSelectedDirectoryId(e.target.value);
              setPageNumber(1);
            }}
            className="rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
          >
            <option value="">Tüm Dizinler</option>
            {(directories.data?.items ?? []).map((directory) => (
              <option key={directory.id} value={directory.id}>
                {directory.name}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2 text-sm text-slate-500">
          Sayfa başına
          <select
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPageNumber(1);
            }}
            className="rounded-md border border-slate-300 px-2 py-1.5 text-sm outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
          >
            {PAGE_SIZE_OPTIONS.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>

        {totalCount > 0 && <span className="text-sm text-slate-400">{totalCount} kullanıcı</span>}
      </div>

      {users.isLoading ? (
        <div className="py-8 text-center text-sm text-slate-400">Yükleniyor…</div>
      ) : items.length === 0 ? (
        <div className="rounded-xl border border-dashed border-slate-200 py-12 text-center text-sm text-slate-500">
          {searchTerm ? 'Aramayla eşleşen kullanıcı yok.' : 'Sistemde henüz kullanıcı yok.'}
        </div>
      ) : (
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-xs uppercase tracking-wide text-slate-400">
              <th className="py-2 pr-4 font-medium">Kullanıcı Adı</th>
              <th className="py-2 pr-4 font-medium">Dizin</th>
              <th className="py-2 pr-4 font-medium">Görünen Ad</th>
              <th className="py-2 pr-4 font-medium">E-posta</th>
              <th className="py-2 font-medium">Durum</th>
            </tr>
          </thead>
          <tbody>
            {items.map((user) => (
              <tr
                key={user.id}
                onClick={() => setView({ kind: 'detail', userId: user.id })}
                className="cursor-pointer border-b border-slate-50 last:border-0 hover:bg-slate-50"
              >
                <td className="py-2 pr-4 text-indigo-600">{user.username}</td>
                <td className="py-2 pr-4 text-slate-500">{user.directoryName}</td>
                <td className="py-2 pr-4 text-slate-700">{user.displayName ?? '—'}</td>
                <td className="py-2 pr-4 text-slate-500">{user.email ?? '—'}</td>
                <td className="py-2">
                  <span
                    className={
                      'rounded-full px-2 py-0.5 text-xs font-medium ' +
                      (user.isActive
                        ? 'bg-emerald-50 text-emerald-700'
                        : 'bg-slate-100 text-slate-500')
                    }
                  >
                    {user.isActive ? 'Aktif' : 'Pasif'}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <span className="text-sm text-slate-400">
            Sayfa {pageNumber} / {totalPages}
          </span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              disabled={pageNumber <= 1}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Önceki
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1)
              .filter(
                (page) =>
                  page === 1 ||
                  page === totalPages ||
                  Math.abs(page - pageNumber) <= 1,
              )
              .map((page, index, pages) => (
                <span key={page} className="flex items-center">
                  {index > 0 && pages[index - 1] !== page - 1 && (
                    <span className="px-1 text-slate-300">…</span>
                  )}
                  <button
                    type="button"
                    onClick={() => setPageNumber(page)}
                    className={
                      'min-w-[2rem] rounded-md px-2 py-1.5 text-sm ' +
                      (page === pageNumber
                        ? 'bg-indigo-600 text-white'
                        : 'text-slate-600 hover:bg-slate-50')
                    }
                  >
                    {page}
                  </button>
                </span>
              ))}
            <button
              type="button"
              onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
              disabled={pageNumber >= totalPages}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Sonraki
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 3: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: `UsersSection.tsx` için hata yok. `AdminPage.tsx`'te `DirectoryUserList`'e ait eski
importun kırılmasına dair hata görülecek — Task 10'da düzeltilecek (`AdminPage.tsx` zaten
`DirectoryUserList`'i doğrudan import etmiyordu, bu yüzden hata beklenmez; eğer varsa Task 10'da
giderilecek).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/components/admin/directory/UsersSection.tsx
git commit -m "feat: add cross-directory UsersSection with directory filter and column"
```

---

### Task 9: `AdminPage.tsx` sol menüsünü yeniden grupla

**Files:**
- Modify: `frontend/src/pages/AdminPage.tsx:1-90` (import + `SectionKind` + `ADMIN_TABS`),
  `frontend/src/pages/AdminPage.tsx:260-283` (`SectionContent`)

**Interfaces:**
- Consumes: `UsersSection` (Task 8, prop almaz), `UserDirectorySection` (Task 7, değişmedi
  dışarıdan bakıldığında — hâlâ prop almıyor), `OrgChartSection` (değişmedi).

- [ ] **Step 1: Import bloğunu güncelle**

`frontend/src/pages/AdminPage.tsx` dosyasının en üstünde şunu bul:

```tsx
import { UserDirectorySection } from '../components/admin/directory/UserDirectorySection';
import { AttributeMappingsSection } from '../components/admin/directory/AttributeMappingsSection';
import { OrgChartSection } from '../components/admin/directory/OrgChartSection';
```

Şununla değiştir:

```tsx
import { UserDirectorySection } from '../components/admin/directory/UserDirectorySection';
import { UsersSection } from '../components/admin/directory/UsersSection';
import { OrgChartSection } from '../components/admin/directory/OrgChartSection';
```

- [ ] **Step 2: `SectionKind` union'ını güncelle**

Şunu bul:

```ts
type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'attributeMappings'
  | 'orgChart'
  | 'placeholder';
```

Şununla değiştir:

```ts
type SectionKind =
  | 'employees'
  | 'notifications'
  | 'valueStreams'
  | 'activities'
  | 'holidays'
  | 'workCalendars'
  | 'userDirectory'
  | 'users'
  | 'orgChart'
  | 'placeholder';
```

- [ ] **Step 3: `users` tab'ının gruplarını ikiye ayır**

Şunu bul:

```ts
  {
    key: 'users',
    label: 'Kullanıcı Yönetimi',
    groups: [
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
          { key: 'attributeMappings', label: 'Alan Eşlemeleri', kind: 'attributeMappings' },
          { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
    ],
  },
```

Şununla değiştir:

```ts
  {
    key: 'users',
    label: 'Kullanıcı Yönetimi',
    groups: [
      {
        header: 'KULLANICI YÖNETİMİ',
        sections: [
          { key: 'employees', label: 'Çalışanlar', kind: 'employees' },
          { key: 'users', label: 'Kullanıcılar', kind: 'users' },
          { key: 'orgChart', label: 'Organizasyon Şeması', kind: 'orgChart' },
          { key: 'roles', label: 'Roller ve İzinler', kind: 'placeholder' },
        ],
      },
      {
        header: 'ACTIVE DIRECTORY',
        sections: [
          { key: 'userDirectory', label: 'Kullanıcı Klasörü', kind: 'userDirectory' },
        ],
      },
    ],
  },
```

- [ ] **Step 4: `SectionContent`'i güncelle**

Şunu bul:

```tsx
    case 'attributeMappings':
      return <AttributeMappingsSection />;
    case 'orgChart':
      return <OrgChartSection />;
```

Şununla değiştir:

```tsx
    case 'users':
      return <UsersSection />;
    case 'orgChart':
      return <OrgChartSection />;
```

- [ ] **Step 5: Tip kontrolü**

Run: `cd frontend && npx tsc --noEmit`
Expected: Hata yok.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/AdminPage.tsx
git commit -m "feat: regroup admin sidebar into KULLANICI YÖNETİMİ / ACTIVE DIRECTORY"
```

---

### Task 10: Uçtan uca doğrulama

**Files:** Yok (yalnızca doğrulama).

- [ ] **Step 1: Backend'i başlat**

Run: `cd backend/src/EforTakip.Api && dotnet run`
Expected: `Now listening on: http://localhost:5298`

- [ ] **Step 2: Frontend'i başlat**

Run: `cd frontend && npm run dev`
Expected: `Local: http://localhost:5173/`

- [ ] **Step 3: Tarayıcıda admin` / `Admin123!` ile giriş yap, Ayarlar (⚙️) → Kullanıcı Yönetimi'ne git**

Expected: Sol menüde iki grup görünür — "KULLANICI YÖNETİMİ" (Çalışanlar, Kullanıcılar,
Organizasyon Şeması, Roller ve İzinler) ve "ACTIVE DIRECTORY" (Kullanıcı Klasörü).

- [ ] **Step 4: "Kullanıcılar"a tıkla**

Expected: Arama kutusu, "Dizin" filtresi ("Tüm Dizinler" dahil), sayfa başına seçici ve tüm
dizinlerdeki kullanıcıları listeleyen bir tablo görünür; tabloda "Dizin" sütunu her satırda
kullanıcının geldiği dizin adını gösterir. Bir dizin seçilince liste o dizinle filtrelenir.
Bir satıra tıklanınca kullanıcı kartı açılır, "← Kullanıcılara dön" ile listeye dönülür.

- [ ] **Step 5: "Kullanıcı Klasörü"ne git, bir Active Directory dizininin satırındaki
  "İşlemler"i incele**

Expected: "Kullanıcılar" linki artık yok. `source === 1` olan dizinlerde "AD Attributes" linki
var; Internal Users dizininde bu link görünmüyor.

- [ ] **Step 6: "AD Attributes"e tıkla, bir alan ekle**

Expected: Başlıkta "{Dizin Adı} — AD Attributes" görünür. Yeni bir eşleme eklenip tablo
güncellenir. "← Listeye dön" ile dizin listesine dönülür.

- [ ] **Step 7: Birden fazla AD dizini varsa, her birinde ayrı ayrı AD Attributes tanımlayıp
  birbirini etkilemediğini doğrula**

Expected: Bir dizine eklenen AD Attribute, diğer dizinin AD Attributes listesinde görünmez
(dizine özel olduğu doğrulanır).

- [ ] **Step 8: "Organizasyon Şeması"na git**

Expected: Hâlâ "KULLANICI YÖNETİMİ" grubunun altında, dizin seçici ve ağaç görünümüyle çalışmaya
devam ediyor (bu çalışmada değiştirilmedi).

- [ ] **Step 9: Backend ve frontend süreçlerini durdur**

Run (PowerShell): `Get-Process -Name 'EforTakip.Api' -ErrorAction SilentlyContinue | Stop-Process -Force`

Frontend dev server'ı çalıştırdığın terminalde Ctrl+C ile durdur.
