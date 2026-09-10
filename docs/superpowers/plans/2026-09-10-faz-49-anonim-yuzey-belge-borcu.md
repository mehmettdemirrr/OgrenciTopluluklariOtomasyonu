# Faz 49 — Anonim Yüzeyin Belge Borcu ve Eksik Testleri Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** `2b28f39` commit'inin belgelemeden eklediği anonim uçlar (anasayfa takvimi, duyuru detayı) MIMARI'ye yazılsın, Y-72'nin artık kodla çelişen ifadesi tadil edilsin ve bu uçlar anonim yüzey testlerinin kapsamına alınsın.

**Architecture:** Kod değişmez, **belge ve test** değişir. Tek istisna: yeni anonim uçların `PublicEndpointAuthorizationTests` taban listesine ve `PublicSurfaceLeakTests` sızıntı taramasına eklenmesi. Takvimin iki varyantı (anonim = kilitli dilim, giriş yapmış = üyelik kapsamına göre açık) kararlaştırılmış davranıştır; bu faz onu yazıya döker ve testle kilitler.

**Tech Stack:** .NET 8, xUnit, WebApplicationFactory tabanlı entegrasyon testleri.

**Spec:** `docs/MIMARI.md` (v6.15 → v6.16 bu fazda). İlgili kararlar: A-42 (anonim yüzey tek dosyada, denetlenebilir), Y-58 (vitrin PII taşımaz), **Y-72** (kitle filtresi — bu fazda tadil edilir), K-38, A-77/A-82 (sayaç deseni).

## Neden bu faz gerekli

`4f36aa8` ve `2b28f39` commit'lerinin **hiçbiri** `docs/MIMARI.md`'ye dokunmadı. Bunun iki somut sonucu var:

1. **Y-72 artık kodla çelişiyor.** Kural şöyle diyor: *"anonim uç yalnızca `Audience == Public` döner"* (`docs/MIMARI.md` Y-72) ve K-38 *"ikincisi anonim vitrinde görünmez"* diyor. Oysa `PublicContentManager.GetCalendarEventsAsync` üyelere özel etkinlikleri `Locked = true` olarak **döndürüyor** — başlık/kulüp/id yok ama varlığı ve saat dilimi anonim ziyaretçiye görünüyor. Bu bilinçli bir tasarım (DTO yorumunda yazıyor) ama kural güncellenmediği için belge yanlış bir garanti veriyor.
2. **Yeni anonim uçlar testlerin dışında.** `PublicEndpointAuthorizationTests` `/api/public/*` uçlarının anonim erişilebilirliğini tek tek sayıyor; `calendar-events` ve `announcements/{id}` listede yok. `PublicSurfaceLeakTests` her anonim ucu tarıyor; takvim ucu taranmıyor.

Bu faz kod davranışını **değiştirmez** — belgeyi koda, testleri de belgeye yetiştirir.

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Davranış değişmez:** bu fazda hiçbir manager/controller mantığı düzenlenmez. Test bir davranışı kırmızı gösterirse dur ve bildir — düzeltmeyi bu fazın içine alma.
- Y-34: her test kendi verisini tohumlar; `PublicSurfaceLeakTests` içindeki `SeedScenarioAsync` deseni kullanılır.
- Takvim ucu tarih aralığı ister: parametresiz çağrı **içinde bulunulan ay**, parametreli çağrı en fazla 62 gün (`CalendarEvents.MaxRangeDays`). Testler aralığı **açıkça** verir; "bu ay" varsayımına yaslanan test ay sonunda kırılır.
- Test komutları: `dotnet test`.

---

### Task 1: MIMARI — kapsam, karar ve Y-72 tadili (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-53, A-84; Y-72 ve K-38 tadilleri.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.15` → `v6.16`; kronolojik listeye kalın olarak:

```markdown
· **v6.16: 10 Eylül 2026 (K-53, A-84, Faz 49 — anasayfa takvimi ve duyuru vitrini; Y-72 ile K-38 tadil edildi)**
```

Giriş paragrafına: `**v6.16** anasayfa takvimini ve duyuru vitrinini belgeleyerek 1 kapsam maddesi (K-53) ve 1 karar (A-84) ekledi ve **Y-72 ile K-38'i tadil etti**.`

Sayaçlar: `Karar | 84 (… + 1 v6.16)`, `Uygulama fazı | 49 (… + 1 v6.16)`. **`Yasak kural` sayacı 88'de kalır** — bu faz yeni yasak eklemiyor, mevcut Y-72'yi tadil ediyor.
İçindekiler: `K-01 … K-53`, `A-01 … A-84`. (`Y-01 … Y-88` değişmez.)

- [x] **Step 2: Y-72'yi tadil et (satırı değiştir, silme)**

Mevcut Y-72 satırını şununla değiştir:

```markdown
| **Y-72** *(v6.16'da tadil edildi)* | Kitle alanı olmadan etkinlik kaydetmek; anonim ucun `ClubMembers` kitleli etkinliğin **kimlik alanlarını** (id, başlık, kulüp, konum, afiş) döndürmesi; kayıt anında üyelik kontrolünü atlamak | `Event.Audience` yazma anında zorunlu; `RegisterAsync` `ClubMembers` etkinlikte **güncel dönem** üyeliği arar (A-65). Anonim **liste ve detay** uçları yalnızca `Audience == Public` döner. **v6.16 istisnası — yalnızca takvim:** anasayfa takvimi (`GET /api/public/calendar-events`) üyelere özel etkinliği `Locked = true` ile, **sadece başlangıç/bitiş saatiyle** gösterir; kimlik alanlarının tamamı boştur (A-84). Gerekçe: takvimin işe yaraması için o saat diliminin dolu olduğunun görünmesi gerekir, ama ne yapıldığının görünmesi gerekmez. **v6.0-v6.15'teki hâli:** anonim uçların hiçbiri `ClubMembers` etkinliği hiçbir biçimde döndürmezdi (K-38, A-84, Y-57) |
```

- [x] **Step 3: K-38'e tadil notu düş**

K-38 satırının "İçerik" sütunundaki `ikincisi anonim vitrinde görünmez` ifadesini şununla değiştir:

```markdown
ikincisi anonim vitrin listelerinde ve detayında görünmez; anasayfa takviminde yalnızca kilitli saat dilimi olarak görünür (Y-72 v6.16 tadili)
```

- [x] **Step 4: K-53'ü K-52'nin altına ekle**

```markdown
| **K-53** | **Anasayfa takvimi ve duyuru vitrini** | Anasayfada aylık etkinlik takvimi vardır: herkese açık etkinlikler başlık/kulüp/konumla, üyelere özel olanlar kilitli saat dilimi olarak görünür; giriş yapmış kullanıcıda kendi kulüplerinin üyelere özel etkinlikleri de açılır. Duyurular ayrı bir vitrin listesi ve detay sayfası taşır | `PublicCalendarEventDto`, `GET /api/public/calendar-events`, `GET /api/events/calendar`, `GET /api/public/announcements/{id}`, `HomeEventsCalendar` (A-84, Y-72) |
```

- [x] **Step 5: A-84'ü A-83'ün altına ekle**

```markdown
| **A-84** | Takvimin **iki varyantı** vardır; kilit kararı sunucuda, kimlikten verilir | A | Aynı takvim iki uçtan servis edilir. **Anonim uç** (`PublicContentManager.GetCalendarEventsAsync`) kimlik tanımaz: `Audience == ClubMembers` olan her etkinlik `Locked` döner, bu yüzden sonuç herkes için aynıdır ve `[CacheAspect]` ile önbelleklenir. **Giriş yapmış uç** (`EventManager.GetCalendarAsync`) kilidi yalnızca çağıranın **danışmanı olduğu** ve **güncel dönemde üyesi olduğu** kulüpler için açar; `clubs.manage.all` taşıyan yönetici için hepsini açar. Kapsam token'daki kimlikten hesaplanır, istemciden gelen hiçbir parametre onu genişletemez (Y-22). Bu uç kullanıcıya göre değiştiği için **`[CacheAspect]` taşımaz** — önbelleklenseydi bir kullanıcının açılmış etkinlikleri başka bir kullanıcıya servis edilirdi; iki varyantın ayrılma sebebi tam olarak budur. Kilitli DTO'da `Id`/`ClubId`/`ClubName`/`Title`/`Location`/`PosterFileId` **null**'dur; sızıntı testi bunu tarar (K-53, Y-72, Y-58) |
```

- [x] **Step 6: Sayaç bütünlüğünü doğrula**

```bash
grep -oE '^\| \*\*[AYK]-[0-9]+\*\*' docs/MIMARI.md | sort | uniq -c | awk '$1>1'
```
Beklenen: yalnızca `2 | **K-13**`.

---

### Task 2: Yeni anonim uçları taban testine ekle

**Files:**
- Modify: `tests/WebAPI.IntegrationTests/PublicEndpointAuthorizationTests.cs:48-53`

**Interfaces:**
- Consumes: mevcut `PublicEndpoint_WithoutToken_ReturnsOk(string path)` teorisi.

- [x] **Step 1: İki satırı ekle**

Mevcut `[InlineData("/api/public/club-categories")]` satırının altına:

```csharp
    [InlineData("/api/public/calendar-events")]
```

`announcements/{id}` bu teoriye **eklenmez**: teori sabit bir yol listesi üzerinden gidiyor ve kimlik gerektiren bir id'ye ihtiyaç duyar; onun anonim erişimi Task 3'teki sızıntı testinde zaten doğrulanıyor (mevcut `GetPublicAnnouncementById_Anonymous_LeaksNothing` testi ucu anonim `_client` ile çağırıyor).

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicEndpointAuthorizationTests"
```
Beklenen: `/api/public/calendar-events` satırı PASS. **Bilinen istisna:** `/api/public/stats` satırı bu depoda öteden beri `500` döndüğü için FAIL kalır — bu fazın kapsamı dışıdır, düzeltme.

---

### Task 3: Takvimin kilidini sızıntı testiyle kilitle

**Files:**
- Modify: `tests/WebAPI.IntegrationTests/PublicSurfaceLeakTests.cs`

**Interfaces:**
- Consumes: `SeedScenarioAsync(string suffix)` → `Scenario` (mevcut; `ActiveClubName` ve `PublishedEventTitle` alanlarını taşır).

- [x] **Step 1: Testi yaz**

Dosyadaki son `[Fact]`'in ardına ekle:

```csharp
    [Fact(DisplayName = "Y-72/A-84: anonim takvim üyelere özel etkinliği yalnızca kilitli dilim olarak gösterir")]
    public async Task GetPublicCalendar_Anonymous_LocksMembersOnlyEvents()
    {
        var suffix = $"cal{Guid.NewGuid():N}"[..11];
        var scenario = await SeedScenarioAsync(suffix);
        var membersOnlyTitle = $"pub-leak-calendar-members-{suffix}";
        var start = DateTime.UtcNow.AddDays(3);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var club = await db.Clubs.SingleAsync(c => c.Name == scenario.ActiveClubName);

            db.Events.Add(new Event
            {
                ClubId = club.Id,
                Title = membersOnlyTitle,
                StartDateUtc = start,
                EndDateUtc = start.AddHours(2),
                Status = EventStatus.Published,
                Audience = EventAudience.ClubMembers,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // Aralık AÇIKÇA verilir: parametresiz çağrı "içinde bulunulan ay"a bakar ve ay sonunda
        // seed edilen etkinlik aralığın dışında kalırdı (CalendarEvents.TryResolveRange).
        var from = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var to = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var response = await _client.GetAsync($"/api/public/calendar-events?fromUtc={from}&toUtc={to}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        // Herkese açık etkinlik başlığıyla görünür — filtre "her şeyi ele" hâline gelirse bu satır kırılır.
        Assert.Contains(scenario.PublishedEventTitle, body, StringComparison.Ordinal);
        // Üyelere özel etkinliğin başlığı ve kulübü sızmaz; yalnızca saat dilimi görünür (A-84).
        Assert.DoesNotContain(membersOnlyTitle, body, StringComparison.Ordinal);
        AssertNoPii(body, scenario);

        var locked = JsonDocument.Parse(body).RootElement.EnumerateArray()
            .Where(item => item.GetProperty("locked").GetBoolean())
            .ToList();
        Assert.NotEmpty(locked);
        Assert.All(locked, item =>
        {
            Assert.Equal(JsonValueKind.Null, item.GetProperty("id").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("title").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("clubName").ValueKind);
            Assert.Equal(JsonValueKind.Null, item.GetProperty("posterFileId").ValueKind);
        });
    }
```

Dosyanın başında `using System.Text.Json;` yoksa ekle.

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~GetPublicCalendar_Anonymous_LocksMembersOnlyEvents"
```
Beklenen: PASS. FAIL ise kod davranışı belgeyle çelişiyordur — **düzeltmeyi bu faza alma**, bulguyu bildir.

---

### Task 4: Giriş yapmış takvimin kapsamını birim testiyle kilitle

**Files:**
- Modify: `tests/Business.Tests/EventManagerTests.cs`

**Interfaces:**
- Consumes: `EventManager.GetCalendarAsync(DateTime?, DateTime?, CancellationToken)` → `IDataResult<IReadOnlyList<PublicCalendarEventDto>>`.

- [x] **Step 1: Testi yaz**

`EventManagerTests` sınıfının sonuna ekle (`using Business.DTOs.Public;` gerekiyorsa dosya başına ekle). Kurucudaki mevcut mock alanlarını kullan; sınıf hangi repository mock'larını taşıyorsa (`_eventRepository`, `_clubRepository`, `_academicStaffRepository`, `_studentRepository`, `_academicTermRepository`, `_clubMembershipRepository`, `_currentUser`, `_clock`) onları kur:

```csharp
    [Fact(DisplayName = "A-84: takvim yalnızca çağıranın üyesi olduğu kulübün üyelere özel etkinliğini açar")]
    public async Task GetCalendarAsync_UnlocksOnlyOwnClubs()
    {
        var now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
        _clock.Setup(c => c.UtcNow).Returns(now);
        _currentUser.Setup(c => c.UserId).Returns(500);
        _currentUser.Setup(c => c.Permissions).Returns([]);

        var mine = new Event
        {
            Id = 1, ClubId = 10, Title = "Üyesi Olduğum", Audience = EventAudience.ClubMembers, Status = EventStatus.Published,
            StartDateUtc = now.AddDays(1), EndDateUtc = now.AddDays(1).AddHours(2), CreatedAtUtc = now,
        };
        var foreign = new Event
        {
            Id = 2, ClubId = 20, Title = "Yabanci Kulup", Audience = EventAudience.ClubMembers, Status = EventStatus.Published,
            StartDateUtc = now.AddDays(2), EndDateUtc = now.AddDays(2).AddHours(2), CreatedAtUtc = now,
        };
        _eventRepository
            .Setup(r => r.GetListPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<Expression<Func<Event, DateTime>>>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Event>([mine, foreign], 2, 0, 100));

        _academicStaffRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicStaff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AcademicStaff?)null);
        _studentRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Student { Id = 7, ApplicationUserId = 500, StudentNumber = "S1", DepartmentId = 1, EnrollmentYear = 2026 });
        _academicTermRepository
            .Setup(r => r.GetAsync(It.IsAny<Expression<Func<AcademicTerm, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AcademicTerm { Id = 3, Name = "2026-Güz", StartDateUtc = now.AddMonths(-1), EndDateUtc = now.AddMonths(3), IsCurrent = true });
        _clubMembershipRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubMembership, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubMembership { ClubId = 10, StudentId = 7, AcademicTermId = 3, ClubRole = ClubRole.Member, JoinedAtUtc = now }]);
        _clubRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Club { Id = 10, Name = "Benim Kulübüm", AdvisorId = 1, IsActive = true, CreatedAtUtc = now }]);

        var result = await _sut.GetCalendarAsync(now.AddDays(-1), now.AddDays(10));

        Assert.True(result.IsSuccess);
        var own = Assert.Single(result.Data!, item => item.Locked == false);
        Assert.Equal("Üyesi Olduğum", own.Title);
        var locked = Assert.Single(result.Data!, item => item.Locked);
        Assert.Null(locked.Title);
        Assert.Null(locked.ClubId);
    }
```

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~GetCalendarAsync_UnlocksOnlyOwnClubs"
```
Beklenen: PASS. Mock alan adları sınıfta farklıysa yalnızca adları uyarla; kurgu aynı kalsın.

---

### Task 5: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Belgeyi gözden geçir**

- Y-72'nin yeni hâli koda uyuyor mu: anonim **liste ve detay** hâlâ yalnızca `Public` döndürüyor, **yalnızca takvim** kilitli dilim gösteriyor.
- A-84'te yazan "giriş yapmış uç `[CacheAspect]` taşımaz" iddiası `IEventService.GetCalendarAsync` üzerinde doğrulanmalı — attribute eklenmişse belge değil **kod** yanlıştır, bildir.

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-10-faz-49-anonim-yuzey-belge-borcu.md tests
git commit -m "$(cat <<'EOF'
Faz 49: anasayfa takvimi ve duyuru vitrini belgelendi, anonim yuzey testleri genisletildi

Docs: docs/MIMARI.md v6.16 (K-53, A-84; Y-72 ve K-38 tadil edildi).
EOF
)"
```
