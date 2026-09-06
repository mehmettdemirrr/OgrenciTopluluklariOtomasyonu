# Faz 43 — Etkinlik ve Duyuru Formları Ayrı Sayfada Uygulama Planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Etkinlik ve duyuru oluşturma/düzenleme, dar bir modal yerine kendi adresine sahip tam sayfada yapılsın; aynı form iki farklı yerde ikinci kez kurulmasın.

**Architecture:** Her varlık için **tek bir form sayfası** vardır (`EventFormPage`, `AnnouncementFormPage`) ve bu sayfa hem oluşturma hem düzenleme kipinde çalışır — kip, rotadaki `:id`'nin varlığından anlaşılır. Sayfayı hangi ekranın açtığı `returnTo` sorgu parametresinde taşınır; kaydettikten sonra kullanıcı geldiği yere döner. Kulüp bağlamı `clubId` sorgu parametresiyle gelir; yoksa sayfa kendi içinde kulüp seçtirir (yöneticinin `/events` üzerinden gelen akışı). Bugün beş ayrı modalda tekrarlanan form alanları bu iki sayfaya taşınır ve modallar **silinir**.

**Tech Stack:** React 18 + MUI 9, react-hook-form + zod, TanStack Query, .NET 8 (yalnızca mimari test).

**Spec:** `docs/MIMARI.md` (v6.9 → v6.10 bu fazda). İlgili kararlar: K-37 (kuruluş başvurusu zaten diyalog değil tam sayfa — bu fazın precedent'i), A-71 (zengin metin), Y-35 (arayüzde gizlemek yetki değildir).

## Global Constraints

- Belge önce, kod sonra (Task 1).
- Çalışma dalı `master`; faz başına **tek commit**; commit mesajında araç imzası/süreç anlatımı yok.
- **Backend'e tek bir ekleme yapılır:** `GET /api/announcements/{id}` (Task 6). Kod tarandı — tekil duyuru okuyan bir uç **yok** (yalnızca liste, POST, PUT, DELETE). Düzenleme artık bir *sayfa* olduğu için adresi doğrudan açılabilmeli ve yenilemeye dayanmalı; veriyi `location.state` ile taşımak F5'te formu boş bırakırdı. Bunun dışında hiçbir uç, yetki kapısı veya doğrulayıcı değişmez.
- Y-35: form sayfasını gizlemek/göstermek yetki değildir — uçlar kendi 403'lerini döndürmeye devam eder. Rota koruması yalnızca kolaylıktır.
- **Onay diyalogları modal kalır.** "Etkinliği İptal Et" (gerekçe soran), "Sil" onayları ve `ConfirmDialog` kullanan her yer bu fazın kapsamı dışındadır — bunlar form değil, karar sorusudur.
- MUI 9: `Stack`/`Typography` üzerinde `alignItems`, `gap`, `flexWrap`, `fontWeight` doğrudan prop olarak verilmez, `sx` içine yazılır.
- Test komutları: `dotnet test`; `cd arayuz && npm run build && npm run lint`.

### Bu planın kabul ettiği varsayım (kullanıcı onayı alınamadı, oturum "soru sorma" modundaydı)

**Düzenleme formları da taşınır.** İstek "oluşturma formu" diyordu; ancak oluşturma sayfaya taşınıp düzenleme modalda kalırsa aynı alan kümesi (başlık, zengin metin, tarih, kontenjan, kitle) iki ayrı yerde tanımlı kalır — bu, projede daha önce iki kez canımızı yakan "ikinci yapım noktası" tuzağının aynısıdır. Tek sayfa iki kipte çalışınca toplam kod da azalır. Yalnızca oluşturma istenirse Task 4 Step 3 ve Task 5 Step 3'teki düzenleme kipi düşülür, sayfalar oluşturmaya özel kalır.

---

### Task 1: MIMARI'ye kapsam, karar ve kuralı yaz (belge önce)

**Files:**
- Modify: `docs/MIMARI.md`

**Interfaces:**
- Produces: K-47, A-78, Y-83.

- [ ] **Step 1: Sürüm ve sayaçlar**

`**Sürüm:** v6.9` → `v6.10`; kronolojik listenin sonuna kalın olarak:

```markdown
· **v6.10: 6 Eylül 2026 (K-47, A-78, Y-83, Faz 43 — form sayfaları)**
```

Giriş paragrafının sonuna: `**v6.10** etkinlik ve duyuru formlarını modaldan tam sayfaya taşıyarak 1 kapsam maddesi (K-47), 1 karar (A-78) ve 1 kural (Y-83) ekledi.`
Sayaçlar: `Karar | 78 (… + 1 v6.10)`, `Yasak kural | 83 (… + 1 v6.10)`, `Uygulama fazı | 43 (… + 1 v6.10)`.
İçindekiler: `Y-01 … Y-83`, `K-01 … K-47`, `A-01 … A-78`.

- [ ] **Step 2: K-47**

```markdown
| **K-47** | **Form sayfaları** | Etkinlik ve duyuru oluşturma/düzenleme kendi adresinde tam sayfadır (`/events/new`, `/events/{id}/edit`, `/announcements/new`, `/announcements/{id}/edit`); modal yalnızca onay sorularına kalır. Form, geldiği ekrana geri döner ve kaydedilmemiş değişiklikle çıkarken uyarır | Tek `EventFormPage`/`AnnouncementFormPage` iki kipte (oluştur/düzenle), `clubId` ve `returnTo` sorgu parametreleri (A-78, Y-83) |
```

- [ ] **Step 3: A-78**

```markdown
| **A-78** | Form **bir sayfadır**, kip rotadan gelir | A | Her varlığın formu tek bileşendedir; oluşturma ile düzenleme aynı sayfanın iki kipidir (`:id` varsa düzenleme). Çağıran ekran formu parametreyle bağlar: `clubId` bağlamı verir (yoksa sayfa kulüp seçtirir), `returnTo` kaydettikten sonra nereye dönüleceğini söyler. Gerekçe: K-37'de kuruluş başvurusu için verilen "evrak yüklemeli form diyaloga sığmaz" kararının genellenmesi — zengin metin editörü, tarih alanları ve görsel yükleme `maxWidth="sm"` bir diyalogda sıkışıyordu. Aynı alanların oluşturma ve düzenleme için iki kez yazılması, projede iki kez yaşanan "ikinci yapım noktası" hatasının arayüz karşılığıdır (K-47, Y-83) |
```

- [ ] **Step 4: Y-83**

```markdown
| **Y-83** | Etkinlik/duyuru form alanlarını bir `Dialog` içinde (yeniden) kurmak — zengin metin editörünü modalda açmak | Form alanları yalnızca form sayfası bileşenlerinde tanımlanır; modal, onay sorularına (`ConfirmDialog`, "İptal gerekçesi") ayrılmıştır. Gerekçe: aynı formun iki kopyası er ya da geç ayrışır — biri `descriptionJson`'a geçerken öbürü düz metinde kalır. İhlali `Architecture.Tests` kaynak taramasıyla yakalar: `arayuz/src/pages` altında hem `<Dialog` hem `RichTextEditor` geçen bir dosya olamaz (K-47, A-78) |
```

---

### Task 2: Ortak form iskeleti ve "kaydedilmemiş değişiklik" koruması

**Files:**
- Create: `arayuz/src/components/ui/FormPageShell.tsx`
- Create: `arayuz/src/hooks/useReturnTo.ts`
- Modify: `arayuz/src/i18n/messages.ts`

**Interfaces:**
- Produces: `<FormPageShell title backTo onCancel onSubmit submitLabel isSubmitting isDirty>`, `useReturnTo(fallback: string)` → `{ returnTo, goBack }`.

- [ ] **Step 1: `useReturnTo` kancasını yaz**

```ts
import { useCallback } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'

/**
 * docs/MIMARI.md · A-78: formu hangi ekranın açtığı `returnTo` ile taşınır.
 * Açık yönlendirme (open redirect) olmasın diye YALNIZCA uygulama içi, "/" ile başlayan
 * ve "//" ile başlamayan adresler kabul edilir; aksi hâlde fallback kullanılır.
 */
export function useReturnTo(fallback: string) {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()

  const raw = searchParams.get('returnTo')
  const returnTo = raw && raw.startsWith('/') && !raw.startsWith('//') ? raw : fallback

  const goBack = useCallback(() => navigate(returnTo, { replace: true }), [navigate, returnTo])

  return { returnTo, goBack }
}
```

- [ ] **Step 2: `FormPageShell` bileşenini yaz**

```tsx
import { Box, Button, Card, CardContent, Stack } from '@mui/material'
import { useEffect, useState, type ReactNode } from 'react'
import { ConfirmDialog } from './ConfirmDialog'
import { PageHeader } from './PageHeader'
import { useLocale } from '../../i18n/LocaleContext'

interface FormPageShellProps {
  title: string
  description?: string
  backTo: string
  isDirty: boolean
  isSubmitting: boolean
  submitLabel: string
  onSubmit: () => void
  onCancel: () => void
  children: ReactNode
}

/** docs/MIMARI.md · K-47: form sayfalarının ortak kabuğu — başlık, kart, kaydet/vazgeç ve çıkış uyarısı. */
export function FormPageShell({
  title, description, backTo, isDirty, isSubmitting, submitLabel, onSubmit, onCancel, children,
}: FormPageShellProps) {
  const { t } = useLocale()
  const [confirmOpen, setConfirmOpen] = useState(false)

  // Sekme kapatma/yenileme için tarayıcı uyarısı; uygulama içi çıkış aşağıdaki onayla sorulur.
  useEffect(() => {
    if (!isDirty) {
      return
    }
    const handler = (event: BeforeUnloadEvent) => {
      event.preventDefault()
      event.returnValue = ''
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [isDirty])

  return (
    <>
      <PageHeader title={title} description={description} backTo={backTo} />

      <Card variant="outlined" sx={{ borderRadius: 3 }}>
        <CardContent sx={{ p: { xs: 2, md: 3 } }}>
          <Box sx={{ maxWidth: 760 }}>{children}</Box>
        </CardContent>
      </Card>

      <Stack direction="row" spacing={1} sx={{ mt: 3, justifyContent: 'flex-end' }}>
        <Button onClick={() => (isDirty ? setConfirmOpen(true) : onCancel())}>{t('common.cancel')}</Button>
        <Button variant="contained" size="large" onClick={onSubmit} disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </Stack>

      {/* İmza doğrulandı: ConfirmDialog({ open, title, description?, confirmLabel?, cancelLabel?,
          destructive?, loading?, onConfirm, onCancel }) — kapatma prop'u `onCancel`, `onClose` DEĞİL. */}
      <ConfirmDialog
        open={confirmOpen}
        title={t('form.discardTitle')}
        description={t('form.discardBody')}
        confirmLabel={t('form.discardConfirm')}
        destructive
        onCancel={() => setConfirmOpen(false)}
        onConfirm={() => {
          setConfirmOpen(false)
          onCancel()
        }}
      />
    </>
  )
}
```

**Doğrulanmış imzalar** (plan yazılırken kodda kontrol edildi): `ConfirmDialog({ open, title, description?, confirmLabel?, cancelLabel?, destructive?, loading?, onConfirm, onCancel })`; `PageHeader({ title, description?, backTo? })` — `ClubDetailPage`'deki kullanımın aynısı.

- [ ] **Step 3: i18n anahtarları**

`messages.ts`'e tr+en ekle: `form.discardTitle` ("Kaydedilmemiş değişiklikler" / "Unsaved changes"), `form.discardBody` ("Bu sayfadan çıkarsanız girdikleriniz kaybolur. Çıkmak istiyor musunuz?" / "Leaving this page discards your changes. Continue?"), `form.discardConfirm` ("Çık" / "Discard"), `form.saveEvent` ("Etkinliği Kaydet"), `form.createEvent` ("Etkinlik Oluştur"), `form.saveAnnouncement` ("Duyuruyu Kaydet"), `form.createAnnouncement` ("Duyuru Yayınla"), `form.selectClub` ("Topluluk seçin").

- [ ] **Step 4: Derle**

```bash
cd arayuz && npm run build
```

---

### Task 3: Etkinlik form sayfası

**Files:**
- Create: `arayuz/src/pages/forms/EventFormPage.tsx`
- Modify: `arayuz/src/App.tsx`
- Modify: `arayuz/src/schemas/eventForm.ts`

**Interfaces:**
- Consumes: `FormPageShell`, `useReturnTo` (Task 2), `eventFormSchema`/`toEventPayload`/`plainTextToDoc` (mevcut).
- Produces: `/events/new`, `/events/:id/edit` rotaları.

- [ ] **Step 1: Sayfayı yaz**

```tsx
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, FormControl, FormControlLabel, FormLabel, Radio, RadioGroup, Skeleton, TextField } from '@mui/material'
import { useEffect } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { useNotifier } from '../../notifications/NotifierProvider'
import { FormPageShell } from '../../components/ui/FormPageShell'
import { RemoteSelect } from '../../components/ui/RemoteSelect'
import { RichTextEditor } from '../../components/richtext/RichTextEditor'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useReturnTo } from '../../hooks/useReturnTo'
import { useLocale } from '../../i18n/LocaleContext'
import { emptyEventFormValues, eventFormSchema, plainTextToDoc, toEventPayload, toLocalInputValue, type EventFormValues } from '../../schemas/eventForm'
import type { ClubListItemDto, EventListItemDto, PagedResult } from '../../api/types'

export function EventFormPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const notify = useNotifier()
  const queryClient = useQueryClient()

  const eventId = id ? Number(id) : null
  const isEdit = eventId !== null
  const clubIdParam = searchParams.get('clubId')
  const presetClubId = clubIdParam ? Number(clubIdParam) : null
  const { returnTo, goBack } = useReturnTo(isEdit ? `/events/${eventId}` : '/events')

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { isDirty, isSubmitting },
  } = useForm<EventFormValues>({
    resolver: zodResolver(eventFormSchema),
    defaultValues: emptyEventFormValues,
  })

  // Düzenleme kipinde mevcut kaydı forma yükle.
  const eventQuery = useQuery({
    queryKey: ['events', eventId],
    queryFn: async () => (await apiClient.get<EventListItemDto>(`/events/${eventId}`)).data,
    enabled: isEdit,
  })

  useDocumentTitle(isEdit ? eventQuery.data?.title : t('form.createEvent'))

  useEffect(() => {
    if (!isEdit || !eventQuery.data) {
      return
    }
    const event = eventQuery.data
    reset({
      title: event.title,
      // A-71: eski düz metin kayıt düzenlemeye açılınca kaybolmasın diye tek paragraflık belgeye sarılır.
      descriptionJson: event.descriptionJson ?? (event.description ? plainTextToDoc(event.description) : ''),
      location: event.location ?? '',
      startDateTime: toLocalInputValue(event.startDateUtc),
      endDateTime: toLocalInputValue(event.endDateUtc),
      capacity: event.capacity ? String(event.capacity) : '',
      audience: event.audience,
    })
  }, [isEdit, eventQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: EventFormValues) => {
      const payload = toEventPayload(values)
      if (isEdit) {
        await apiClient.put(`/events/${eventId}`, payload)
        return eventId
      }
      const clubId = presetClubId ?? values.clubId
      const response = await apiClient.post<number>(`/clubs/${clubId}/events`, payload)
      return response.data
    },
    onSuccess: (savedId) => {
      notify({ message: isEdit ? 'Etkinlik güncellendi.' : 'Etkinlik oluşturuldu (taslak).', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['events'] })
      queryClient.invalidateQueries({ queryKey: ['club-events'] })
      queryClient.invalidateQueries({ queryKey: ['events', savedId] })
      goBack()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Etkinlik kaydedilemedi.'), severity: 'error' }),
  })

  if (isEdit && eventQuery.isLoading) {
    return <Skeleton variant="rounded" height={420} />
  }

  return (
    <FormPageShell
      title={isEdit ? 'Etkinliği Düzenle' : t('form.createEvent')}
      backTo={returnTo}
      isDirty={isDirty}
      isSubmitting={isSubmitting || saveMutation.isPending}
      submitLabel={isEdit ? t('form.saveEvent') : t('form.createEvent')}
      onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
      onCancel={goBack}
    >
      {!isEdit && presetClubId === null && (
        <Controller
          name="clubId"
          control={control}
          render={({ field, fieldState }) => (
            <RemoteSelect<ClubListItemDto>
              label={t('form.selectClub')}
              value={field.value || null}
              onChange={(value) => field.onChange(value ?? 0)}
              queryKey={['clubs', 'event-form']}
              fetchOptions={async (term) =>
                (await apiClient.get<PagedResult<ClubListItemDto>>('/clubs', {
                  params: { pageIndex: 0, pageSize: 20, search: term || undefined, isActive: true },
                })).data.items
              }
              getOptionId={(club) => club.id}
              getOptionLabel={(club) => club.name}
              error={!!fieldState.error}
              helperText={fieldState.error?.message}
            />
          )}
        />
      )}

      <Controller
        name="title"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="normal" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="descriptionJson"
        control={control}
        render={({ field }) => (
          <Box sx={{ mt: 2, mb: 2 }}>
            <RichTextEditor value={field.value || null} onChange={field.onChange} />
          </Box>
        )}
      />
      <Controller name="location" control={control} render={({ field }) => <TextField {...field} fullWidth margin="normal" label="Yer" />} />
      <Controller
        name="startDateTime"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} fullWidth margin="normal" label="Başlangıç" type="datetime-local" slotProps={{ inputLabel: { shrink: true } }} error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="endDateTime"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} fullWidth margin="normal" label="Bitiş" type="datetime-local" slotProps={{ inputLabel: { shrink: true } }} error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="capacity"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} fullWidth margin="normal" label="Kontenjan (boş = sınırsız)" type="number" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="audience"
        control={control}
        render={({ field }) => (
          <FormControl margin="normal">
            <FormLabel>Kimler katılabilir?</FormLabel>
            <RadioGroup {...field} row>
              <FormControlLabel value="Public" control={<Radio />} label="Herkese açık" />
              <FormControlLabel value="ClubMembers" control={<Radio />} label="Sadece topluluk üyeleri" />
            </RadioGroup>
          </FormControl>
        )}
      />
      {watch('audience') === 'ClubMembers' && (
        <Box sx={{ mt: 1 }}>
          {/* K-38/Y-72: kitle kararı sunucuda da uygulanır; buradaki not yalnızca bilgilendirir. */}
        </Box>
      )}
    </FormPageShell>
  )
}
```

- [ ] **Step 2: Şemaya `clubId` ve tarih yardımcısını ekle**

`arayuz/src/schemas/eventForm.ts`:

```ts
export const eventFormSchema = z
  .object({
    // A-78: yalnızca kulüp bağlamı olmadan açılan formda doldurulur; 0 = seçilmedi.
    clubId: z.number().int(),
    title: z.string().min(1, 'Başlık gerekli.'),
    // … mevcut alanlar aynen kalır
  })
  // … mevcut refine'lar aynen kalır
  .refine((values) => values.clubId > 0, { message: 'Topluluk seçin.', path: ['clubId'] })

export const emptyEventFormValues: EventFormValues = {
  clubId: 0,
  // … mevcut varsayılanlar
}

/** `datetime-local` alanı yerel saat bekler; sunucudan gelen ISO/UTC değeri dönüştürülür. */
export function toLocalInputValue(iso: string): string {
  const date = new Date(iso)
  const offset = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}
```

**Uyarı:** `clubId` alanı `toEventPayload`'a **girmez** — kulüp adres satırında (`/clubs/{clubId}/events`) taşınır. `toEventPayload` gövdesine dokunma.
**Uyarı 2:** `clubId` zorunlu refine'ı, kulüp bağlamıyla açılan formda tetiklenmemeli. Bunu `EventFormPage`'de `presetClubId` varken `reset({ ...emptyEventFormValues, clubId: presetClubId })` diyerek sağla (Step 1'deki `useEffect`'e ekle).

- [ ] **Step 3: Rotaları ekle**

`arayuz/src/App.tsx`, korumalı blokta `"/events/:id"` rotasının **üstüne** (yoksa `new` bir id sanılır):

```tsx
                  <Route
                    path="/events/new"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsWrite}>
                        <EventFormPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/events/:id/edit"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsWrite}>
                        <EventFormPage />
                      </ProtectedRoute>
                    }
                  />
```

- [ ] **Step 4: Derle**

```bash
cd arayuz && npm run build
```

---

### Task 4: Etkinlik modallarını kaldır ve çağrıları sayfaya bağla

**Files:**
- Modify: `arayuz/src/pages/EventsPage.tsx`
- Modify: `arayuz/src/pages/ClubDetailEventsTab.tsx`
- Modify: `arayuz/src/pages/EventDetailPage.tsx`

- [ ] **Step 1: `/events` sayfasındaki "Etkinlik Oluştur" düğmesini bağla**

`EventsPage.tsx` içindeki create `Dialog`'unu, `useFormDialog`/`useForm` kurulumunu, `createEventMutation`'ı ve artık kullanılmayan importları **sil**; düğmeyi bağla:

```tsx
        <Button variant="contained" component={RouterLink} to="/events/new?returnTo=/events">
          Etkinlik Oluştur
        </Button>
```

- [ ] **Step 2: Kulüp sekmesindeki düğmeyi bağla**

`ClubDetailEventsTab.tsx` içindeki create `Dialog`'unu, form kurulumunu ve `createEventMutation`'ı sil; düğmeyi bağla (kulüp bağlamı ve dönüş adresi parametrede):

```tsx
          <Button variant="contained" component={RouterLink} to={`/events/new?clubId=${clubId}&returnTo=/clubs/${clubId}`}>
            Etkinlik Oluştur
          </Button>
```

`canManage` koşulu **aynen kalır** (A-75).

- [ ] **Step 3: Detay sayfasındaki "Düzenle" düğmesini bağla**

`EventDetailPage.tsx` içindeki düzenleme `Dialog`'unu, `editDialog`, `useForm`, `updateMutation` (yalnızca formu besleyen kısmı) ve ilgili importları sil; düğmeyi bağla:

```tsx
              <Button variant="outlined" component={RouterLink} to={`/events/${eventId}/edit?returnTo=/events/${eventId}`}>
                Düzenle
              </Button>
```

**"Etkinliği İptal Et" diyaloğuna ve `ConfirmDialog`'lara dokunma** — onlar onay sorusudur (Global Constraints).

- [ ] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```
Beklenen: kullanılmayan import/değişken hatası kalmamalı; hata verirse artık kullanılmayan importları temizle.

---

### Task 5: Tekil duyuru okuma ucu (düzenleme sayfasının veri kaynağı)

**Files:**
- Modify: `src/Business/Abstract/IAnnouncementService.cs`
- Modify: `src/Business/Concrete/AnnouncementManager.cs`
- Modify: `src/WebAPI/Controllers/AnnouncementsController.cs`
- Test: `tests/WebAPI.IntegrationTests/AnnouncementFlowTests.cs`

**Interfaces:**
- Produces: `GET /api/announcements/{id}` → `AnnouncementListItemDto`.

- [ ] **Step 1: Başarısız entegrasyon testini yaz**

`tests/WebAPI.IntegrationTests/AnnouncementFlowTests.cs` içine (dosyanın kendi `SeedScenarioAsync`/`SendWithBearerAsync` yardımcılarını kullanır):

```csharp
[Fact(DisplayName = "K-47: duyuru düzenleme sayfası için tekil okuma ucu duyuruyu döndürür")]
public async Task GetById_ReturnsAnnouncement_ForManager()
{
    var scenario = await SeedScenarioAsync("ann-getbyid");
    var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);
    var createResponse = await SendWithBearerAsync(
        HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", advisorToken,
        new { Title = "Okunacak Duyuru", Content = "İçerik", Visibility = "Members" });
    var announcementId = await createResponse.Content.ReadFromJsonAsync<int>();

    var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/announcements/{announcementId}", advisorToken);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.Equal("Okunacak Duyuru", body.GetProperty("title").GetString());
}

[Fact(DisplayName = "Y-35: kulüple ilgisi olmayan kullanıcı tekil duyuru ucundan Forbidden alır")]
public async Task GetById_ReturnsForbidden_ForUnrelatedUser()
{
    var scenario = await SeedScenarioAsync("ann-getbyid-forbidden");
    var advisorToken = await LoginAndGetAccessTokenAsync(scenario.AdvisorEmail, scenario.AdvisorPassword);
    var createResponse = await SendWithBearerAsync(
        HttpMethod.Post, $"/api/clubs/{scenario.ClubId}/announcements", advisorToken,
        new { Title = "Gizli", Content = "İçerik", Visibility = "Members" });
    var announcementId = await createResponse.Content.ReadFromJsonAsync<int>();

    const string password = "Officer!Test123456";
    const string email = "ann-getbyid-outsider@test.local";
    using (var scope = _factory.Services.CreateScope())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await userManager.CreateAsync(user, password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "ClubOfficer");
    }

    var outsiderToken = await LoginAndGetAccessTokenAsync(email, password);
    var response = await SendWithBearerAsync(HttpMethod.Get, $"/api/announcements/{announcementId}", outsiderToken);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

- [ ] **Step 2: Çalıştır, başarısız olduklarını gör**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~GetById_Returns"
```
Beklenen: ikisi de FAIL (404 — uç yok).

- [ ] **Step 3: Servis sözleşmesine ekle**

`IAnnouncementService`'e, `DeleteAsync`'in altına:

```csharp
    /// <summary>
    /// docs/MIMARI.md · K-47: düzenleme SAYFASININ veri kaynağı — adres doğrudan açılabildiği için
    /// tekil okuma gerekir. Kapı yazma kapısının aynısıdır (EnsureCanManageAsync): düzenleyebilen okur.
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.AnnouncementsWrite)]
    Task<IDataResult<AnnouncementListItemDto>> GetByIdAsync(int announcementId, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: `AnnouncementManager`'a yaz**

Mevcut `EnsureCanManageAsync` **yeniden kullanılır** — ikinci bir kapı yazma:

```csharp
    public async Task<IDataResult<AnnouncementListItemDto>> GetByIdAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.GetAsync(a => a.Id == announcementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null)
        {
            return DataResult<AnnouncementListItemDto>.NotFound(Messages.AnnouncementNotFound);
        }

        var accessResult = await EnsureCanManageAsync(announcementId, cancellationToken).ConfigureAwait(false);
        if (!accessResult.IsSuccess)
        {
            return DataResult<AnnouncementListItemDto>.Forbidden(accessResult.Message);
        }

        var club = announcement.ClubId is { } clubId
            ? await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false)
            : null;

        return DataResult<AnnouncementListItemDto>.Success(new AnnouncementListItemDto
        {
            Id = announcement.Id,
            ClubId = announcement.ClubId,
            ClubName = club?.Name,
            Title = announcement.Title,
            Content = announcement.Content,
            ContentJson = announcement.ContentJson,
            ImageFileId = announcement.ImageFileId,
            Visibility = announcement.Visibility,
            PublishedAtUtc = announcement.PublishedAtUtc,
        });
    }
```

**Not:** `AnnouncementListItemDto` bu dosyada zaten iki yerde kuruluyor (`MapWithClubNamesAsync` ve `PublicContentManager`); burası **üçüncü** yapım noktasıdır. Alan listesini `MapWithClubNamesAsync`'ten birebir kopyala — eksik alan bırakırsan düzenleme formu o alanı sessizce boşaltır.
**Not 2:** `DataResult<T>.Forbidden` yardımcısı yoksa `Core/Utilities/Results` altındaki mevcut sözleşmeye bak ve oradaki karşılığını kullan (`AnnouncementManager` içindeki diğer dönüşler nasıl yapıyorsa aynısı).

- [ ] **Step 5: Controller ucunu ekle**

```csharp
    [HttpGet("announcements/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await announcementService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }
```

- [ ] **Step 6: Testleri çalıştır**

```bash
dotnet test tests/WebAPI.IntegrationTests --filter "FullyQualifiedName~AnnouncementFlowTests"
```
Beklenen: PASS.

---

### Task 6: Duyuru form sayfası ve modalların kaldırılması

**Files:**
- Create: `arayuz/src/pages/forms/AnnouncementFormPage.tsx`
- Modify: `arayuz/src/App.tsx`
- Modify: `arayuz/src/pages/AnnouncementsPage.tsx`
- Modify: `arayuz/src/pages/ClubDetailAnnouncementsTab.tsx`

**Interfaces:**
- Consumes: `FormPageShell`, `useReturnTo`, `announcementFormSchema`.
- Produces: `/announcements/new`, `/announcements/:id/edit` rotaları.

- [ ] **Step 1: Sayfayı yaz**

Kip kuralları: `:id` varsa düzenleme (`PUT /announcements/{id}`); yoksa `clubId` parametresi varsa kulüp duyurusu (`POST /clubs/{clubId}/announcements`), o da yoksa sistem duyurusu (`POST /announcements`, `announcements.global` ister).

```tsx
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Box, Button, MenuItem, Skeleton, Stack, TextField, Typography } from '@mui/material'
import ImageOutlinedIcon from '@mui/icons-material/ImageOutlined'
import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useParams, useSearchParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import { extractErrorMessage } from '../../api/errors'
import { useNotifier } from '../../notifications/NotifierProvider'
import { FormPageShell } from '../../components/ui/FormPageShell'
import { RichTextEditor } from '../../components/richtext/RichTextEditor'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useReturnTo } from '../../hooks/useReturnTo'
import { useLocale } from '../../i18n/LocaleContext'
import { announcementFormSchema, type AnnouncementFormValues } from '../../schemas/announcementForm'
import type { AnnouncementListItemDto } from '../../api/types'

const emptyAnnouncementFormValues: AnnouncementFormValues = { title: '', contentJson: '', visibility: 'Members' }

export function AnnouncementFormPage() {
  const { t } = useLocale()
  const { id } = useParams<{ id: string }>()
  const [searchParams] = useSearchParams()
  const notify = useNotifier()
  const queryClient = useQueryClient()
  const imageInputRef = useRef<HTMLInputElement>(null)
  const [pendingImage, setPendingImage] = useState<File | null>(null)

  const announcementId = id ? Number(id) : null
  const isEdit = announcementId !== null
  const clubIdParam = searchParams.get('clubId')
  const clubId = clubIdParam ? Number(clubIdParam) : null
  const { returnTo, goBack } = useReturnTo(clubId ? `/clubs/${clubId}` : '/announcements')

  const {
    control,
    handleSubmit,
    reset,
    formState: { isDirty, isSubmitting },
  } = useForm<AnnouncementFormValues>({
    resolver: zodResolver(announcementFormSchema),
    defaultValues: emptyAnnouncementFormValues,
  })

  const announcementQuery = useQuery({
    queryKey: ['announcement', announcementId],
    queryFn: async () => (await apiClient.get<AnnouncementListItemDto>(`/announcements/${announcementId}`)).data,
    enabled: isEdit,
  })

  useDocumentTitle(isEdit ? announcementQuery.data?.title : t('form.createAnnouncement'))

  useEffect(() => {
    if (!isEdit || !announcementQuery.data) {
      return
    }
    const announcement = announcementQuery.data
    reset({
      title: announcement.title,
      contentJson: announcement.contentJson ?? plainTextToDoc(announcement.content),
      visibility: announcement.visibility,
    })
  }, [isEdit, announcementQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: AnnouncementFormValues) => {
      let savedId = announcementId
      if (isEdit) {
        await apiClient.put(`/announcements/${announcementId}`, values)
      } else if (clubId) {
        savedId = (await apiClient.post<number>(`/clubs/${clubId}/announcements`, values)).data
      } else {
        savedId = (await apiClient.post<number>('/announcements', values)).data
      }

      // K-42: görsel, duyuru kaydedildikten SONRA kendi ucuna yüklenir.
      if (pendingImage && savedId) {
        const formData = new FormData()
        formData.append('file', pendingImage)
        await apiClient.post(`/announcements/${savedId}/image`, formData)
      }
      return savedId
    },
    onSuccess: () => {
      notify({ message: isEdit ? 'Duyuru güncellendi.' : 'Duyuru yayınlandı.', severity: 'success' })
      queryClient.invalidateQueries({ queryKey: ['announcements'] })
      queryClient.invalidateQueries({ queryKey: ['club-announcements'] })
      goBack()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Duyuru kaydedilemedi.'), severity: 'error' }),
  })

  const handleImageChange = (event: ChangeEvent<HTMLInputElement>) => {
    setPendingImage(event.target.files?.[0] ?? null)
    event.target.value = ''
  }

  if (isEdit && announcementQuery.isLoading) {
    return <Skeleton variant="rounded" height={420} />
  }

  return (
    <FormPageShell
      title={isEdit ? 'Duyuruyu Düzenle' : clubId ? 'Yeni Duyuru' : 'Sistem Duyurusu'}
      backTo={returnTo}
      isDirty={isDirty || pendingImage !== null}
      isSubmitting={isSubmitting || saveMutation.isPending}
      submitLabel={isEdit ? t('form.saveAnnouncement') : t('form.createAnnouncement')}
      onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
      onCancel={goBack}
    >
      <Controller
        name="title"
        control={control}
        render={({ field, fieldState }) => (
          <TextField {...field} autoFocus fullWidth margin="normal" label="Başlık" error={!!fieldState.error} helperText={fieldState.error?.message} />
        )}
      />
      <Controller
        name="contentJson"
        control={control}
        render={({ field, fieldState }) => (
          <Box sx={{ mt: 2, mb: 1 }}>
            <RichTextEditor value={field.value || null} onChange={field.onChange} />
            {fieldState.error && (
              <Typography variant="caption" color="error">
                {fieldState.error.message}
              </Typography>
            )}
          </Box>
        )}
      />
      <Controller
        name="visibility"
        control={control}
        render={({ field }) => (
          <TextField {...field} select fullWidth margin="normal" label="Görünürlük">
            <MenuItem value="Members">Üyelere özel</MenuItem>
            <MenuItem value="Public">Herkese açık</MenuItem>
          </TextField>
        )}
      />

      <input ref={imageInputRef} type="file" accept="image/*" hidden onChange={handleImageChange} />
      <Stack direction="row" spacing={1} sx={{ mt: 2, alignItems: 'center' }}>
        <Button variant="outlined" startIcon={<ImageOutlinedIcon />} onClick={() => imageInputRef.current?.click()}>
          Kapak Görseli Seç
        </Button>
        <Typography variant="caption" color="text.secondary">
          {pendingImage?.name ?? 'Seçilmedi'}
        </Typography>
      </Stack>
    </FormPageShell>
  )
}
```

**Dikkat:** `plainTextToDoc` bugün **üç** yerde ayrı ayrı tanımlı (`AnnouncementsPage.tsx:36`, `ClubDetailAnnouncementsTab.tsx:32`, `schemas/eventForm.ts:53`). İlk ikisini sil ve `arayuz/src/schemas/announcementForm.ts` içine tek kopya olarak taşı (`eventForm.ts`'teki etkinlik kopyası yerinde kalır, o başka bir şemanın parçası):

```ts
// docs/MIMARI.md · A-71: eski düz metin duyuru düzenlemeye açılınca kaybolmasın diye
// editöre tek paragraflık bir belge olarak yüklenir.
export function plainTextToDoc(text: string): string {
  return JSON.stringify({
    type: 'doc',
    content: [{ type: 'paragraph', content: text ? [{ type: 'text', text }] : [] }],
  })
}
```

**Dikkat 2:** `GET /api/announcements/{id}` ucu **Task 5'te eklenir**; bu sayfa onu kullanır (`location.state` ile taşımak F5'te formu boşaltırdı).

- [ ] **Step 2: Rotaları ekle**

```tsx
                  <Route
                    path="/announcements/new"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.AnnouncementsWrite}>
                        <AnnouncementFormPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/announcements/:id/edit"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.AnnouncementsWrite}>
                        <AnnouncementFormPage />
                      </ProtectedRoute>
                    }
                  />
```

- [ ] **Step 3: İki duyuru ekranındaki modalları kaldır**

`AnnouncementsPage.tsx` ve `ClubDetailAnnouncementsTab.tsx` içindeki **dört** form `Dialog`'unu, `createForm`/`editForm` kurulumlarını, `createMutation`/`updateMutation`/`createGlobalMutation`/`updateGlobalMutation`'ları ve görsel yükleme yardımcılarını sil; düğmeleri bağla:

```tsx
// AnnouncementsPage (sistem duyurusu)
<Button variant="contained" component={RouterLink} to="/announcements/new?returnTo=/announcements">Sistem Duyurusu</Button>
// satır eylemi:
<Button size="small" component={RouterLink} to={`/announcements/${row.id}/edit?returnTo=/announcements`}>Düzenle</Button>

// ClubDetailAnnouncementsTab (kulüp duyurusu)
<Button variant="contained" component={RouterLink} to={`/announcements/new?clubId=${clubId}&returnTo=/clubs/${clubId}`}>Duyuru Ekle</Button>
<Button size="small" component={RouterLink} to={`/announcements/${announcement.id}/edit?clubId=${clubId}&returnTo=/clubs/${clubId}`}>Düzenle</Button>
```

Silme (`deleteMutation` + `ConfirmDialog`) **kalır**.

- [ ] **Step 4: Derle ve lint'le**

```bash
cd arayuz && npm run build && npm run lint
```

---

### Task 7: Y-83 mimari testi

**Files:**
- Create: `tests/Architecture.Tests/FormPageArchitectureTests.cs`

**Interfaces:**
- Consumes: `SolutionPaths.FindRepositoryRoot()` (Faz 38'de eklendi).

- [ ] **Step 1: Testi yaz**

```csharp
using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-83: form alanları modalda kurulamaz — zengin metin editörü diyaloga girmez.</summary>
public class FormPageArchitectureTests
{
    [Fact(DisplayName = "Y-83: sayfa bileşenlerinde <Dialog> ile RichTextEditor aynı dosyada bulunamaz")]
    public void Pages_DoNotOpenRichTextEditorInsideDialog()
    {
        var pagesDirectory = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src", "pages");
        Assert.True(Directory.Exists(pagesDirectory), $"Sayfa dizini bulunamadı: {pagesDirectory}");

        var offenders = Directory
            .EnumerateFiles(pagesDirectory, "*.tsx", SearchOption.AllDirectories)
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("<Dialog", StringComparison.Ordinal)
                    && source.Contains("RichTextEditor", StringComparison.Ordinal);
            })
            .Select(path => Path.GetFileName(path))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Y-83 ihlali — form alanları modalda kuruluyor: {string.Join(", ", offenders)}. " +
            "Form sayfası kullan (A-78); modal yalnızca onay sorularına aittir.");
    }
}
```

- [ ] **Step 2: Çalıştır**

```bash
dotnet test tests/Architecture.Tests --filter "FullyQualifiedName~FormPageArchitectureTests"
```
Beklenen: PASS. FAIL veriyorsa Task 4/5'te bir modal kalmıştır — testi gevşetme, modalı kaldır.

---

### Task 8: Tam doğrulama ve tek commit

- [ ] **Step 1: Tüm testler**

```bash
dotnet test
cd arayuz && npm run build && npm run lint
```
Beklenen: iki bilinen `/api/public/stats` hatası dışında tamamı PASS.

- [ ] **Step 2: Elle doğrula**

- `/events` → "Etkinlik Oluştur" → sayfa açılmalı, kulüp seçtirmeli, kaydedince `/events`'e dönmeli.
- Kulüp sayfası → Etkinlikler sekmesi → "Etkinlik Oluştur" → kulüp seçimi **görünmemeli** (bağlam parametreden geldi), kaydedince kulüp sayfasına dönmeli.
- Etkinlik detayı → "Düzenle" → alanlar dolu gelmeli; bir alanı değiştirip "Vazgeç" → uyarı çıkmalı.
- Duyuru: sistem duyurusu ve kulüp duyurusu ayrı ayrı oluşturulmalı, kapak görseli yüklenmeli, düzenleme eski içeriği kaybetmemeli.

- [ ] **Step 3: Commit**

```bash
git add -A -- docs/MIMARI.md docs/superpowers/plans/2026-09-06-faz-43-form-sayfalari.md src tests arayuz/src
git commit -m "$(cat <<'EOF'
Faz 43: etkinlik ve duyuru formlari ayri sayfaya tasindi

Docs: docs/MIMARI.md v6.10 (K-47, A-78, Y-83).
EOF
)"
```
