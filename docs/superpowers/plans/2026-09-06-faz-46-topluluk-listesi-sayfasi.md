# Faz 46 — Topluluk Listesi Sayfası Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Anonim vitrin topluluk listesi referans tasarımdaki kart düzenine geçsin: logo, kategori rozetleri, **üye ve etkinlik sayısı**, iki satırlık açıklama ve iki eylem — "Giriş Yap ve Katıl" (giriş yapmışta "Katıl") ile "İncele".

**Architecture:** Kart sayıları vitrin DTO'suna eklenir ve **sayfa başına tek toplu sorgudan** gelir (`IClubStatsDal`); satır başına sayım N+1 üretirdi. "Üye sayısı" güncel dönemin üyelikleri, "etkinlik sayısı" ise anonim yüzeyde görünen (yayınlanmış + herkese açık) etkinliklerdir — vitrinde gösterilen her sayı, vitrinde görülebilen veriyle tutarlı olmalıdır. Kart üzerindeki "Katıl" düğmesi hiçbir uygunluk kararı vermez; kararı üyelik başvurusu ucu verir ve mesajı kullanıcıya o döndürür.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI 9, TanStack Query.

**Spec:** `docs/MIMARI.md` (v6.12 → v6.13 bu fazda). İlgili kararlar: A-42/Y-58 (anonim yüzey), Y-72 (kitle), Y-42 (sayım SQL'de), Y-35 (arayüz gizlemek yetki değildir), §22.3 (üyelik dönemseldir).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **"Soru Sor" özelliği YOKTUR.** Referans tasarımdaki üçüncü düğme bilinçli olarak dışarıda bırakıldı (kullanıcı isteği). Kulübe ulaşma yolu Faz 40'ta gelen iletişim/sosyal bilgileridir.
- **Y-42:** sayımlar SQL'de ve sayfa başına tek sorguda yapılır; `GetListAsync().Count` yasak.
- **Y-58:** vitrin DTO'su isim/öğrenci no taşımaz — yalnızca sayı.
- **Y-35:** "Katıl" düğmesi arayüzde koşula bağlanıp gizlenmez; uygunluğu (öğrenci mi, kulüp aktif mi, dönem var mı, zaten üye mi) sunucu söyler.
- MUI 9: `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight` doğrudan prop verilmez — `sx` içine yazılır.
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bağımlılıklar

- **Faz 45 (çoklu kategori)** yapılmışsa kartta birden çok rozet çizilir (`clubCategoryNames`). Yapılmadıysa Task 4 **Step 2**'deki kart bileşeninin rozet bloğu tek değerle yazılır — `{club.clubCategoryName && <Chip … label={club.clubCategoryName} />}` — ve tip tanımı (`clubCategoryNames: string[]`) eklenmez; başka hiçbir şey değişmez.
- Filtre çubuğu (kategori + arama + "Sorgula" + harf satırı) **zaten var** (`ClubBrowseFilters.tsx`, Faz 24 civarı) ve referans tasarımla örtüşüyor; bu faz ona dokunmaz.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-50, A-81, Y-86.

- [x] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.12` → `v6.13`; kronolojik listeye kalın olarak:

```markdown
· **v6.13: 6 Eylül 2026 (K-50, A-81, Y-86, Faz 46 — topluluk listesi sayfası)**
```

Giriş paragrafına: `**v6.13** vitrin topluluk listesine üye/etkinlik sayısı ve katılım eylemi ekleyerek 1 kapsam maddesi (K-50), 1 karar (A-81) ve 1 kural (Y-86) ekledi.`
Sayaçlar: `Karar | 81 (… + 1 v6.13)`, `Yasak kural | 86 (… + 1 v6.13)`, `Uygulama fazı | 46 (… + 1 v6.13)`.
İçindekiler: `Y-01 … Y-86`, `K-01 … K-50`, `A-01 … A-81`.

- [x] **Step 2: K-50**

```markdown
| **K-50** | **Topluluk listesi kartı** | Vitrin kartı logoyu, kategori rozetlerini, **üye ve etkinlik sayısını**, kısa açıklamayı ve iki eylemi taşır: giriş yapmamışa "Giriş Yap ve Katıl", giriş yapmışa "Katıl" ve her ikisinde "İncele". Kulübe soru sorma özelliği kapsam dışıdır | `PublicClubListItemDto.MemberCount`/`EventCount`, `IClubStatsDal` (tek toplu sorgu), `POST /api/clubs/{id}/membership-applications` (mevcut uç) (A-81, Y-86) |
```

- [x] **Step 3: A-81**

```markdown
| **A-81** | Vitrindeki sayılar **vitrinde görülebilen veriyle** tutarlıdır ve tek sorgudan gelir | A | "Üye sayısı" **güncel dönemin** `ClubMembership` satırlarıdır (§22.3: üyelik dönemseldir; geçmiş dönemleri toplamak, bugün 12 üyesi olan kulübü 300 üyeli göstermek demektir). "Etkinlik sayısı" anonim yüzeyde görünen etkinliklerdir: `Status == Published && Audience == Public` (Y-58/Y-72) — vitrinde göremeyeceği bir etkinliği sayan bir rozet, kullanıcıya bulamayacağı bir şeyi vaat eder. İkisi de `IClubStatsDal.GetCountsAsync(clubIds)` ile **sayfa başına tek** `GROUP BY` sorgusundan okunur; satır başına sayım 12 kartlık sayfada 24 sorgu açardı (Y-42, Y-10) (K-50) |
```

- [x] **Step 4: Y-86**

```markdown
| **Y-86** | Vitrin kartındaki "Katıl" düğmesini arayüzde uygunluk kararına bağlamak — öğrenci mi, kulüp aktif mi, dönem açık mı, zaten üye mi diye bakıp düğmeyi gizlemek/pasifleştirmek | Düğme yalnızca üyelik başvurusu ucunu çağırır; kararı ve mesajı sunucu döndürür (`NotAStudent`, `ClubNotActive`, `AlreadyClubMember`, `DuplicatePendingApplication`, `NoCurrentAcademicTerm`). Gerekçe: Y-35'in kart karşılığı — arayüzde verilen "uygun değilsin" kararı, kuralın ikinci bir kopyasıdır ve sunucudaki kural değişince sessizce yanlışa döner; ayrıca kullanıcı **neden** olmadığını öğrenemez (K-50, A-81) |
```

---

### Task 2: Kart sayıları — DAL ve DTO

**Files:**
- Create: `src/DataAccess/Repositories/IClubStatsDal.cs`
- Create: `src/DataAccess/Repositories/EfClubStatsDal.cs`
- Modify: `src/DataAccess/DependencyResolvers/DataAccessAutofacModule.cs`
- Modify: `src/Business/DTOs/Public/PublicClubListItemDto.cs`
- Modify: `src/Business/Concrete/PublicContentManager.cs`
- Test: `tests/Business.Tests/PublicContentManagerTests.cs`

**Interfaces:**
- Produces: `IClubStatsDal.GetCountsAsync(IReadOnlyCollection<int> clubIds, ct)` → `IReadOnlyDictionary<int, ClubCardStats>`, `ClubCardStats(int MemberCount, int EventCount)`.

- [x] **Step 1: Başarısız testi yaz**

`tests/Business.Tests/PublicContentManagerTests.cs` (sınıf başına `private readonly Mock<IClubStatsDal> _clubStatsDal = new();` ekle, kurucuya geçir ve kurucuda varsayılan boş sözlük döndür):

```csharp
[Fact(DisplayName = "K-50: vitrin kartı üye ve etkinlik sayısını taşır")]
public async Task GetClubsAsync_CarriesMemberAndEventCounts()
{
    var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
    SetupPagedFilter<Club, string>(_clubRepository, [club]);
    _clubStatsDal
        .Setup(d => d.GetCountsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Dictionary<int, ClubCardStats> { [1] = new(388, 2) });

    var result = await _sut.GetClubsAsync(0, 20);

    var item = Assert.Single(result.Data!.Items);
    Assert.Equal(388, item.MemberCount);
    Assert.Equal(2, item.EventCount);
}
```

- [x] **Step 2: Çalıştır, başarısız olduğunu gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~CarriesMemberAndEventCounts"
```
Beklenen: derleme hatası (`IClubStatsDal` yok).

- [x] **Step 3: DAL arayüzünü yaz**

```csharp
namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-81/Y-42: vitrin kartı sayıları — sayfa başına TEK GROUP BY sorgusu.</summary>
public interface IClubStatsDal
{
    Task<IReadOnlyDictionary<int, ClubCardStats>> GetCountsAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default);
}

/// <summary>A-81: üye = GÜNCEL dönemin üyelikleri; etkinlik = vitrinde görünen (Published + Public) etkinlikler.</summary>
public sealed record ClubCardStats(int MemberCount, int EventCount);
```

- [x] **Step 4: DAL uygulamasını yaz**

```csharp
using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

/// <summary>docs/MIMARI.md · A-81: iki GROUP BY sorgusu (üyelik + etkinlik), satır başına sayım yok.</summary>
public sealed class EfClubStatsDal(AppDbContext context) : IClubStatsDal
{
    public async Task<IReadOnlyDictionary<int, ClubCardStats>> GetCountsAsync(
        IReadOnlyCollection<int> clubIds, CancellationToken cancellationToken = default)
    {
        if (clubIds.Count == 0)
        {
            return new Dictionary<int, ClubCardStats>();
        }

        // §22.3: üyelik dönemseldir — güncel dönem yoksa üye sayısı 0'dır, geçmiş dönem toplanmaz.
        var currentTermId = await context.AcademicTerms.AsNoTracking()
            .Where(t => t.IsCurrent)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var memberCounts = currentTermId is { } termId
            ? await context.ClubMemberships.AsNoTracking()
                .Where(m => clubIds.Contains(m.ClubId) && m.AcademicTermId == termId)
                .GroupBy(m => m.ClubId)
                .Select(g => new { ClubId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClubId, x => x.Count, cancellationToken)
                .ConfigureAwait(false)
            : [];

        // Y-58/Y-72: vitrinde görünmeyen etkinlik sayılmaz.
        var eventCounts = await context.Events.AsNoTracking()
            .Where(e => clubIds.Contains(e.ClubId) && e.Status == EventStatus.Published && e.Audience == EventAudience.Public)
            .GroupBy(e => e.ClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClubId, x => x.Count, cancellationToken)
            .ConfigureAwait(false);

        return clubIds.ToDictionary(
            id => id,
            id => new ClubCardStats(memberCounts.GetValueOrDefault(id), eventCounts.GetValueOrDefault(id)));
    }
}
```

**Not:** `Events` üzerindeki soft delete query filter (Y-16) otomatik uygulanır — silinmiş etkinlik sayılmaz, ek koşul yazma.

- [x] **Step 5: Autofac kaydı**

```csharp
        builder.RegisterType<EfClubStatsDal>()
            .As<IClubStatsDal>()
            .InstancePerLifetimeScope();
```

- [x] **Step 6: DTO ve manager**

`PublicClubListItemDto`'ya:

```csharp
    /// <summary>docs/MIMARI.md · A-81: güncel dönemin üye sayısı. İsim/öğrenci no taşınmaz (Y-58).</summary>
    public int MemberCount { get; set; }

    /// <summary>docs/MIMARI.md · A-81: vitrinde görünen (yayınlanmış + herkese açık) etkinlik sayısı.</summary>
    public int EventCount { get; set; }
```

`PublicContentManager` kurucusuna `IClubStatsDal clubStatsDal` ekle; `GetClubsAsync` içinde sayfa öğeleri kurulmadan önce:

```csharp
        var stats = await clubStatsDal
            .GetCountsAsync(paged.Items.Select(c => c.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);
```
ve kart kurulumunda `MemberCount = stats.GetValueOrDefault(c.Id)?.MemberCount ?? 0` yerine record olduğu için:

```csharp
                MemberCount = stats.TryGetValue(c.Id, out var s) ? s.MemberCount : 0,
                EventCount = stats.TryGetValue(c.Id, out var e) ? e.EventCount : 0,
```

- [x] **Step 7: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests
```
Beklenen: PASS.

---

### Task 3: Sayıların anonim uçtan gerçekten geldiğini doğrula (entegrasyon)

**Files:**
- Test: `tests/WebAPI.IntegrationTests/PublicClubCardTests.cs`

- [x] **Step 1: Testi yaz**

Tohumlama desenini `tests/WebAPI.IntegrationTests/ClubDetailVisibilityTests.cs`'ten kopyala (fakülte → bölüm → güncel dönem → danışman → kulüp), sonra kulübe bir öğrenci üyeliği ve iki etkinlik ekle (biri `Published+Public`, biri `Draft`):

```csharp
[Fact(DisplayName = "A-81: vitrin kartı güncel dönem üye sayısını ve yalnızca yayınlanmış herkese açık etkinlikleri sayar")]
public async Task PublicClubs_CarryCurrentTermMemberCountAndVisibleEventCount()
{
    var clubId = await SeedClubWithOneMemberAndTwoEventsAsync("clubcard");

    var response = await _client.GetAsync($"/api/public/clubs?pageIndex=0&pageSize=50");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var page = await response.Content.ReadFromJsonAsync<JsonElement>();
    var card = page.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == clubId);

    Assert.Equal(1, card.GetProperty("memberCount").GetInt32());
    Assert.Equal(1, card.GetProperty("eventCount").GetInt32());   // taslak sayılmaz
    Assert.False(card.TryGetProperty("members", out _));           // Y-58: liste sızmaz
}
```

- [x] **Step 2: Çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~PublicClubCardTests"
```
Beklenen: PASS.

---

### Task 4: Kart tasarımı

**Files:**
- Create: `arayuz/src/components/clubs/ClubBrowseCard.tsx`
- Modify: `arayuz/src/pages/public/PublicClubsPage.tsx`
- Modify: `arayuz/src/api/types.ts`
- Modify: `arayuz/src/i18n/messages.ts`

**Interfaces:**
- Consumes: `PublicClubListItemDto.memberCount/eventCount` (Task 2).
- Produces: `<ClubBrowseCard club onJoin isAuthenticated joining />`.

- [x] **Step 1: Tipleri ve metinleri ekle**

`types.ts` → `PublicClubListItemDto`'ya `memberCount: number`, `eventCount: number`.

`messages.ts`'e tr+en: `public.memberCount` ("{count} Üye" / "{count} Members"), `public.eventCount` ("{count} Etkinlik" / "{count} Events"), `public.loginAndJoin` ("Giriş Yap ve Katıl" / "Sign In and Join"), `public.join` ("Katıl" / "Join"), `public.inspect` ("İncele" / "View"), `public.shareClub` ("Paylaş" / "Share").

- [x] **Step 2: Kart bileşenini yaz**

```tsx
import { Box, Button, Card, CardContent, Chip, IconButton, Stack, Tooltip, Typography, alpha } from '@mui/material'
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import ShareOutlinedIcon from '@mui/icons-material/ShareOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { useLocale } from '../../i18n/LocaleContext'
import type { PublicClubListItemDto } from '../../api/types'

interface ClubBrowseCardProps {
  club: PublicClubListItemDto
  isAuthenticated: boolean
  joining: boolean
  onJoin: (clubId: number) => void
  onShare: (club: PublicClubListItemDto) => void
}

/** docs/MIMARI.md · K-50: vitrin kartı. Y-86: "Katıl" uygunluk kararı vermez, ucu çağırır. */
export function ClubBrowseCard({ club, isAuthenticated, joining, onJoin, onShare }: ClubBrowseCardProps) {
  const { t } = useLocale()

  return (
    <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column', borderRadius: 3, position: 'relative' }}>
      <Tooltip title={t('public.shareClub')}>
        <IconButton
          size="small"
          aria-label={t('public.shareClub')}
          onClick={() => onShare(club)}
          sx={{ position: 'absolute', top: 8, left: 8, zIndex: 1 }}
        >
          <ShareOutlinedIcon fontSize="small" />
        </IconButton>
      </Tooltip>

      <Stack
        direction="row"
        spacing={0.5}
        useFlexGap
        sx={{ position: 'absolute', top: 8, right: 8, zIndex: 1, flexWrap: 'wrap', justifyContent: 'flex-end', maxWidth: '65%' }}
      >
        {/* Faz 45 yapıldıysa çoklu rozet; yapılmadıysa tek ad ile aynı blok kullanılır. */}
        {club.clubCategoryNames.map((name) => (
          <Chip key={name} size="small" label={name} sx={{ bgcolor: 'text.primary', color: 'background.paper', fontWeight: 700 }} />
        ))}
      </Stack>

      <Box
        sx={{
          height: 170,
          mt: 4,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 3,
          background: (theme) => (club.logoFileId ? 'transparent' : `linear-gradient(135deg, ${alpha(theme.palette.secondary.main, 0.08)} 0%, ${alpha(theme.palette.primary.main, 0.12)} 100%)`),
        }}
      >
        {club.logoFileId ? (
          <Box component="img" src={`/api/files/${club.logoFileId}`} alt="" sx={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }} />
        ) : (
          <GroupsOutlinedIcon sx={{ fontSize: 56, color: 'primary.dark' }} />
        )}
      </Box>

      <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 800, textTransform: 'uppercase', mb: 1 }}>
          {club.name}
        </Typography>

        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', mb: 1.5 }}>
          <Chip size="small" icon={<GroupsOutlinedIcon />} label={t('public.memberCount', { count: club.memberCount })} />
          <Chip size="small" icon={<EventAvailableOutlinedIcon />} label={t('public.eventCount', { count: club.eventCount })} />
        </Stack>

        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', mb: 2, minHeight: 40 }}
        >
          {club.description || t('common.noDescription')}
        </Typography>

        <Stack spacing={1} sx={{ mt: 'auto' }}>
          {isAuthenticated ? (
            <Button variant="contained" color="success" disabled={joining} onClick={() => onJoin(club.id)}>
              {t('public.join')}
            </Button>
          ) : (
            <Button variant="contained" color="success" startIcon={<LoginOutlinedIcon />} component={RouterLink} to="/login">
              {t('public.loginAndJoin')}
            </Button>
          )}
          <Button variant="outlined" component={RouterLink} to={`/kulupler/${club.id}`}>
            {t('public.inspect')}
          </Button>
        </Stack>
      </CardContent>
    </Card>
  )
}
```

- [x] **Step 3: Sayfayı karta bağla**

`PublicClubsPage.tsx` içindeki `<Card …>` bloğunu **tamamen** `ClubBrowseCard` ile değiştir ve katılım/paylaşım davranışlarını sayfaya ekle:

```tsx
  const { isAuthenticated } = useAuth()
  const notify = useNotifier()

  // Y-86: uygunluk kararı sunucudadır; buradaki tek iş isteği göndermek ve dönen mesajı göstermek.
  const joinMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.post(`/clubs/${clubId}/membership-applications`)
    },
    onSuccess: () => notify({ message: 'Başvurunuz alındı, danışman onayı bekleniyor.', severity: 'success' }),
    onError: (error) => notify({ message: extractErrorMessage(error, 'Başvuru gönderilemedi.'), severity: 'error' }),
  })

  const handleShare = async (club: PublicClubListItemDto) => {
    const url = `${window.location.origin}/kulupler/${club.id}`
    if (navigator.share) {
      await navigator.share({ title: club.name, url }).catch(() => undefined)
      return
    }
    await navigator.clipboard.writeText(url).catch(() => undefined)
    notify({ message: 'Bağlantı kopyalandı.', severity: 'success' })
  }
```

ve grid içinde:

```tsx
            <Grid key={club.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <ClubBrowseCard
                club={club}
                isAuthenticated={isAuthenticated}
                joining={joinMutation.isPending}
                onJoin={(clubId) => joinMutation.mutate(clubId)}
                onShare={handleShare}
              />
            </Grid>
```

`useAuth`, `useMutation`, `useNotifier`, `extractErrorMessage` importlarını ekle; kartın kendisi artık `RouterLink` sarmalayıcısı olmadığı için eski `component={RouterLink}` kullanımını **kaldır** (düğmeler içeride kendi bağlantılarını taşıyor — iç içe `<a>` geçersiz HTML üretirdi).

- [x] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 5: Tam doğrulama ve tek commit

- [x] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [x] **Step 2: Elle doğrula**

- Anonim pencerede `/kulupler`: kartlarda logo, rozetler, "N Üye"/"N Etkinlik" ve iki düğme görünmeli; "Giriş Yap ve Katıl" giriş sayfasına gitmeli, "İncele" detaya.
- Giriş yapmış öğrenciyle aynı sayfada "Katıl" → başarı mesajı; aynı kulübe ikinci kez "Katıl" → sunucunun "zaten bekleyen başvurunuz var" mesajı görünmeli (Y-86: arayüz düğmeyi gizlemiyor, sebebi sunucu söylüyor).
- Öğrenci profili olmayan bir kullanıcıyla "Katıl" → sunucunun `NotAStudent` mesajı görünmeli.
- Üye sayısı: kulübe güncel dönemde bir üye ekle → kart 1 göstermeli; geçmiş döneme ait üyelik sayıyı **artırmamalı**.
- Paylaş düğmesi: bağlantı kopyalanmalı (veya mobilde paylaşım sayfası açılmalı).

- [x] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-46-topluluk-listesi-sayfasi.md src tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 46: topluluk listesi kart tasarimi ve vitrin sayilari

Docs: docs/MIMARI.md v6.13 (K-50, A-81, Y-86).
EOF
)"
```
