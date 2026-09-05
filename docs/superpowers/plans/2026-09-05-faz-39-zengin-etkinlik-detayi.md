# Faz 39 — Zengin Etkinlik Detayı Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Etkinlik detay sayfası; biçimlendirilmiş açıklama (kalın, renk, liste, hizalama), konum haritası + yol tarifi ve zaman çizelgesi (başlangıç/bitiş + kalan süre) taşısın. Afiş zaten var; bu faz onun etrafını doldurur.

**Architecture:** Açıklama, Faz 38'in altyapısını **aynen** kullanır: `Event.DescriptionJson` alanı düğüm ağacını tutar, sunucu aynı `RichTextDocumentValidator` ile kapalı izin listesini uygular, düz metin aynası mevcut `Description` alanına yazılır (arama ve e-posta oradan okur), arayüz aynı `RichTextEditor`/`RichTextContent` bileşenlerini kullanır. İkinci bir zengin metin yolu açılmaz.

Harita, ana sayfada zaten kullanılan Google Maps embed yardımcılarını (`arayuz/src/data/campuses.ts:23-29`) ortak modüle taşıyıp yeniden kullanır — harita kütüphanesi veya API anahtarı projeye girmez. Geri sayım, istemci saatiyle hesaplanan **yalnızca görsel** bir göstergedir; katılım açık mı, etkinlik başladı mı gibi kararlar backend'de kalır.

**Tech Stack:** .NET 8, EF Core 8, React 18 + MUI, `date-fns` (mevcut), TipTap (Faz 38'de kurulur).

**Spec:** `docs/MIMARI.md` (v6.5 → v6.6 bu fazda). **Bağımlılık: Faz 38 önce tamamlanmalıdır** — `RichTextDocumentValidator`, `RichTextPlainTextExtractor`, `RichTextEditor` ve `RichTextContent` oradan gelir.

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- A-71/A-72/Y-78 (Faz 38): zengin metin yapısal JSON'dur, renk anlamsal token'dır, izin listesi kapalıdır ve kapı sunucudadır. Bu faz o kuralların **kullanıcısıdır**, ikinci bir yol açmaz.
- İstemci saati güvenilmez: geri sayım süslemedir, yetki/karar üretmez (Y-79).
- Y-34: her test kendi verisini tohumlar.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-43, A-73, Y-79.

> **Numara notu:** Faz 38 iki karar ama tek kural ürettiği için (A-71, A-72, Y-78) bu fazın kararı **A-73**, kuralı **Y-79**'dur. Faz 40 (A-74/Y-80) ve Faz 41 (A-75/Y-81) planları da bu diziye göre yazılmıştır.

- [ ] **Step 1: Sürüm ve sayaçlar**

v6.6 kaydını ekle; sayaçları **73 karar / 79 kural / 39 faz** yap.

- [ ] **Step 2: K-43**

```markdown
- **K-43 — Zengin etkinlik detayı:** Etkinlik detay sayfası afiş, biçimlendirilmiş açıklama,
  konum haritası ve yol tarifi bağlantısı ile başlangıç/bitiş ve kalan süreyi gösteren bir zaman
  çizelgesi taşır. Konum boşsa harita bölümü hiç çizilmez.
```

- [ ] **Step 3: A-73**

```markdown
- **A-73 — Harita gömülü iframe'dir, kütüphane değildir.** Konum, Google Maps'in `output=embed`
  ucuna gömülür ve yol tarifi bağlantısı aynı sorgu metniyle kurulur; harita SDK'sı veya API
  anahtarı projeye girmez. Yardımcılar `arayuz/src/utils/maps.ts` altında tek yerde durur
  (ana sayfanın kampüs haritası da oradan beslenir).
```

- [ ] **Step 4: Y-79**

```markdown
- **Y-79 — Geri sayım karar vermez.** Kalan süre göstergesi istemci saatinden hesaplanır ve
  yalnızca bilgilendirir. Katılım açıklığı, etkinlik durumu ve kontenjan kararları backend'in
  verdiği alanlardan okunur; "tarayıcıda tarih geçmiş" diye bir düğme açılıp kapanamaz.
```

---

### Task 2: Etkinlik açıklamasına zengin içerik alanı

**Files:**
- Modify: `src/Entities/Event.cs`
- Modify: `src/DataAccess/Concrete/EntityFramework/Contexts/ApplicationDbContext.cs`
- Create: `src/DataAccess/Migrations/<timestamp>_20260905_Faz39_EtkinlikAciklamasi.cs` (EF üretir)
- Modify: `src/Business/DTOs/Events/CreateEventRequestDto.cs`
- Modify: `src/Business/DTOs/Events/UpdateEventRequestDto.cs`
- Modify: `src/Business/DTOs/Events/EventListItemDto.cs`
- Modify: `src/Business/Concrete/EventManager.cs` (`CreateAsync`, `UpdateAsync`, `MapToDto`)
- Modify: `src/Business/Concrete/EventParticipationManager.cs` (`GetMineAsync` — ikinci DTO kurulum yeri)
- Test: `tests/Business.Tests/EventManagerTests.cs`

**Interfaces:**
- Consumes: `RichTextDocumentValidator`, `RichTextPlainTextExtractor` (Faz 38, Task 3).
- Produces: `Event.DescriptionJson` (`string?`), `EventListItemDto.DescriptionJson`, istek DTO'larında `DescriptionJson`.

- [ ] **Step 1: Başarısız testleri yaz**

```csharp
[Fact]
public async Task CreateAsync_StoresDescriptionJson_AndDerivesPlainText()
{
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.EventsManage);

    var result = await sut.CreateAsync(1, new CreateEventRequestDto
    {
        Title = "Tanışma Toplantısı",
        DescriptionJson = ValidDoc,   // "Kayıtlar 15 Ekim"
        StartDateUtc = Now.AddDays(3),
        EndDateUtc = Now.AddDays(3).AddHours(2),
        Audience = EventAudience.Public,
    });

    Assert.True(result.IsSuccess);
    var @event = await context.Events.SingleAsync();
    Assert.NotNull(@event.DescriptionJson);
    Assert.Equal("Kayıtlar 15 Ekim", @event.Description);
}

[Fact]
public async Task CreateAsync_Rejects_WhenDescriptionJsonHasDisallowedNode()
{
    SeedMembershipWithCapabilities(clubId: 1, ClubCapability.EventsManage);

    var result = await sut.CreateAsync(1, BuildRequestWithDescriptionJson("""{"type":"doc","content":[{"type":"iframe"}]}"""));

    Assert.False(result.IsSuccess);
    Assert.False(await context.Events.AnyAsync());
}

[Fact]
public async Task GetByIdAsync_CarriesDescriptionJson()
{
    var eventId = await SeedEventWithDescriptionJsonAsync();

    var result = await sut.GetByIdAsync(eventId);

    Assert.NotNull(result.Data.DescriptionJson);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~EventManagerTests"
```

- [ ] **Step 3: Entity alanı ve migration**

`src/Entities/Event.cs`, `Description`'ın hemen altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-43/A-71: biçimlendirilmiş açıklamanın düğüm ağacı (JSON).
    /// Null = düz metin açıklama. Description bu alanın düz metin aynasıdır.
    /// </summary>
    public string? DescriptionJson { get; set; }
```

`ApplicationDbContext`'teki `Event` yapılandırmasına `entity.Property(e => e.DescriptionJson).HasColumnType("nvarchar(max)");` ekle.

```bash
dotnet ef migrations add 20260905_Faz39_EtkinlikAciklamasi --project src/DataAccess --startup-project src/WebAPI
dotnet ef database update --project src/DataAccess --startup-project src/WebAPI
```

- [ ] **Step 4: DTO alanlarını ekle**

`CreateEventRequestDto` ve `UpdateEventRequestDto`:

```csharp
    /// <summary>docs/MIMARI.md · K-43/A-71: biçimlendirilmiş açıklama ağacı. Null = düz metin.</summary>
    public string? DescriptionJson { get; set; }
```

`EventListItemDto`:

```csharp
    /// <summary>docs/MIMARI.md · A-71: null ise Description düz metin olarak gösterilir.</summary>
    public string? DescriptionJson { get; set; }
```

- [ ] **Step 5: `CreateAsync`/`UpdateAsync`'te doğrula, düz metni türet**

Faz 38'deki `AnnouncementManager` deseninin aynısı; yetki kapısından **sonra**:

```csharp
        // Y-78: izin listesi kapalıdır; reddedilen içerik sessizce temizlenmez, hata döner.
        var description = request.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(request.DescriptionJson))
        {
            var validation = RichTextDocumentValidator.Validate(request.DescriptionJson);
            if (!validation.IsValid)
            {
                return DataResult<int>.ValidationError(validation.Error ?? Messages.UnsupportedRichTextContent);
            }

            description = RichTextPlainTextExtractor.Extract(request.DescriptionJson);
        }
```

ve varlığa `Description = description, DescriptionJson = request.DescriptionJson`.

- [ ] **Step 6: HER İKİ DTO kurulum yerini de doldur**

```bash
grep -rn "new EventListItemDto" src/Business
```
`EventManager.MapToDto` ve `EventParticipationManager.GetMineAsync` — ikisine de `DescriptionJson = e.DescriptionJson,` ekle. (Faz 35'te `PosterFileId` bu ikinci noktada atlanmıştı; aynı tuzak.)

- [ ] **Step 7: Testleri çalıştır**

```bash
dotnet test tests/Business.Tests --filter "FullyQualifiedName~EventManagerTests"
```
Beklenen: PASS.

---

### Task 3: Harita yardımcılarını ortak modüle taşı

**Files:**
- Create: `arayuz/src/utils/maps.ts`
- Modify: `arayuz/src/data/campuses.ts:23-29`
- Modify: `arayuz/src/pages/public/HomePage.tsx:20` (import yolu)

**Interfaces:**
- Produces: `mapsEmbedUrl(query)`, `mapsSearchUrl(query)`, `mapsDirectionsUrl(query)` (sonuncusu yeni).

- [ ] **Step 1: Modülü oluştur**

```ts
/** docs/MIMARI.md · A-73: harita gömülü iframe'dir; SDK veya API anahtarı yoktur. */
export function mapsSearchUrl(query: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`
}

export function mapsEmbedUrl(query: string) {
  return `https://maps.google.com/maps?q=${encodeURIComponent(query)}&z=16&output=embed`
}

export function mapsDirectionsUrl(query: string) {
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(query)}`
}
```

- [ ] **Step 2: `campuses.ts`'ten kopyaları kaldır**

`mapsSearchUrl` ve `mapsEmbedUrl` tanımlarını sil. Yeniden dışa aktarma köprüsü **bırakma** — `HomePage.tsx`'in importunu doğrudan `../../utils/maps` yap; iki kaynak bırakmak A-73'ün "tek yer" amacını bozar.

- [ ] **Step 3: Derle**

```bash
cd arayuz && npm run build
```
Beklenen: hatasız.

---

### Task 4: Zaman çizelgesi bileşeni

**Files:**
- Create: `arayuz/src/components/ui/EventTimeline.tsx`
- Modify: `arayuz/src/i18n/messages.ts`

**Interfaces:**
- Produces: `<EventTimeline startIso={string} endIso={string} />`

- [ ] **Step 1: Bileşeni yaz**

Başlangıç → Bitiş satırları (tarih + saat, `toLocaleString(dateLocale)`) ve üstte kalan süre özeti:

```tsx
/**
 * docs/MIMARI.md · Y-79: buradaki kalan süre YALNIZCA bilgilendirir.
 * Katılım/durum kararları backend alanlarından okunur, bu hesaptan değil.
 */
function remainingLabel(startIso: string, endIso: string, now: Date, t: Translate) {
  const start = new Date(startIso)
  const end = new Date(endIso)
  if (now >= end) return t('event.finished')
  if (now >= start) return t('event.ongoing')

  const days = differenceInCalendarDays(start, now)
  if (days > 0) return t('event.daysLeft', { count: days })

  const hours = differenceInHours(start, now)
  return hours > 0 ? t('event.hoursLeft', { count: hours }) : t('event.startingSoon')
}
```

`now`, `useState(() => new Date())` ile tutulur ve 60 saniyelik `setInterval` ile tazelenir; `useEffect` temizliğinde interval kapatılır.

- [ ] **Step 2: i18n anahtarlarını ekle**

`arayuz/src/i18n/messages.ts` içindeki **hem TR hem EN** sözlüğüne: `event.finished`, `event.ongoing`, `event.daysLeft` ("{count} gün kaldı"), `event.hoursLeft`, `event.startingSoon`, `event.start`, `event.end`, `event.location`, `event.directions`.

**Dikkat:** İki sözlükten birini atlamak, dil değiştirince ham anahtarın ekrana basılması demektir.

- [ ] **Step 3: Derle**

```bash
cd arayuz && npm run build
```

---

### Task 5: Etkinlik detay ve düzenleme ekranlarını bağla

**Files:**
- Modify: `arayuz/src/pages/EventDetailPage.tsx`
- Modify: `arayuz/src/pages/ClubDetailEventsTab.tsx` (etkinlik oluştur/düzenle formu)
- Modify: `arayuz/src/api/types.ts`

**Interfaces:**
- Consumes: `RichTextEditor`, `RichTextContent` (Faz 38), `mapsEmbedUrl`/`mapsDirectionsUrl` (Task 3), `EventTimeline` (Task 4), `descriptionJson` (Task 2).

- [ ] **Step 1: Tipi güncelle**

`EventListItemDto` arayüzüne `descriptionJson: string | null`.

- [ ] **Step 2: Açıklamayı zengin render'a çevir**

`EventDetailPage.tsx`'te açıklama bugün düz `Typography` ile basılıyor:

```tsx
{(event.descriptionJson || event.description) && (
  <RichTextContent json={event.descriptionJson} fallbackText={event.description ?? ''} />
)}
```

- [ ] **Step 3: Formda editörü kullan**

`ClubDetailEventsTab.tsx`'teki etkinlik formunda açıklama alanını `RichTextEditor` ile değiştir; kaydederken `descriptionJson` gönder.

- [ ] **Step 4: Zaman çizelgesini yerleştir**

Afiş/başlık altındaki bilgi kartına `<EventTimeline startIso={event.startDateUtc} endIso={event.endDateUtc} />` ekle.

- [ ] **Step 5: Harita bölümünü ekle**

```tsx
{event.location && (
  <SectionCard title={t('event.location')}>
    <Typography variant="body2" sx={{ mb: 1.5 }}>{event.location}</Typography>
    <Box sx={{ borderRadius: 3, overflow: 'hidden', border: '1px solid', borderColor: 'divider' }}>
      <Box
        component="iframe"
        title={event.location}
        src={mapsEmbedUrl(event.location)}
        loading="lazy"
        referrerPolicy="no-referrer-when-downgrade"
        sx={{ display: 'block', width: '100%', height: { xs: 220, md: 300 }, border: 0 }}
      />
    </Box>
    <Button
      href={mapsDirectionsUrl(event.location)}
      target="_blank"
      rel="noopener noreferrer"
      variant="outlined"
      endIcon={<OpenInNewOutlinedIcon />}
      sx={{ mt: 1.5 }}
    >
      {t('event.directions')}
    </Button>
  </SectionCard>
)}
```

- [ ] **Step 6: Mobilde kontrol et**

Tarayıcıyı 390 px genişliğe getir; afiş, harita, zaman çizelgesi ve biçimli metin taşmadan dizilmeli, yatay kaydırma olmamalı.

- [ ] **Step 7: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 6: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
```
Beklenen: tamamı PASS.

- [ ] **Step 2: Elle doğrula**

- Biçimli (kalın + renkli + listeli) bir etkinlik açıklaması yaz, detay sayfasında doğru göründüğünü ve **koyu modda okunabildiğini** gör.
- Konumu olan bir etkinlikte harita ve "Yol tarifi" bağlantısını gör; konumu **boş** olanda harita bölümünün hiç çizilmediğini doğrula.
- Başlangıcı geçmiş, bitişi gelecekte olan etkinlikte "Devam ediyor", bitmişte "Sona erdi" yazdığını gör.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Faz 39: etkinlik detayinda harita, zaman cizelgesi ve bicimli aciklama"
```
