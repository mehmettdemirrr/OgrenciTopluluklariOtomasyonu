import { Box, Button, Card, CardContent, CardMedia, Divider, Grid, IconButton, Stack, Typography } from '@mui/material'
import CalendarMonthOutlinedIcon from '@mui/icons-material/CalendarMonthOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import NearMeOutlinedIcon from '@mui/icons-material/NearMeOutlined'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import ScheduleRoundedIcon from '@mui/icons-material/ScheduleRounded'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { EventTimeline } from '../ui/EventTimeline'
import { RichTextContent } from '../richtext/RichTextContent'
import { useLocale } from '../../i18n/LocaleContext'
import { downloadEventIcs } from '../../utils/calendar'
import { mapsDirectionsUrl, mapsEmbedUrl } from '../../utils/maps'

export interface EventDetailLayoutProps {
  event: {
    id: number
    clubId: number
    clubName: string
    clubLogoFileId: number | null
    title: string
    description: string | null
    descriptionJson: string | null
    location: string | null
    startDateUtc: string
    endDateUtc: string
    capacity: number | null
    participantCount: number | null
    viewCount: number
    posterFileId: number | null
  }
  /** Kulüp sayfasının adresi — vitrinde /kulupler/:id, panelde /clubs/:id. */
  clubHref: string
  /** Birincil eylem alanı: anonimde "Giriş Yap", panelde katıl/iptal (A-76). */
  primaryAction?: ReactNode
  /** Kitle rozeti gibi eylemin altına giren küçük not. */
  actionNote?: ReactNode
  /** Panele özgü ek bölümler (katılımcı listesi, yönetim kartları). */
  children?: ReactNode
}

/** docs/MIMARI.md · A-76: etkinlik detay düzeni tek bileşendir; anonim ve panel sayfaları onu giydirir. */
export function EventDetailLayout({ event, clubHref, primaryAction, actionNote, children }: EventDetailLayoutProps) {
  const { t } = useLocale()

  return (
    <Grid container spacing={3}>
      <Grid size={{ xs: 12, md: 8 }}>
        {event.posterFileId && (
          <Card variant="outlined" sx={{ borderRadius: 3, mb: 3, bgcolor: 'common.black' }}>
            <CardMedia
              component="img"
              image={`/api/files/${event.posterFileId}`}
              alt=""
              sx={{ maxHeight: 420, objectFit: 'contain' }}
            />
          </Card>
        )}

        <Card variant="outlined" sx={{ borderRadius: 3 }}>
          <CardContent sx={{ p: { xs: 2.5, md: 3.5 } }}>
            <Typography variant="h6" sx={{ fontWeight: 800, mb: 2 }}>
              {t('event.details')}
            </Typography>
            <Divider sx={{ mb: 2 }} />
            <RichTextContent json={event.descriptionJson} fallbackText={event.description ?? ''} />
          </CardContent>
        </Card>

        {children}
      </Grid>

      <Grid size={{ xs: 12, md: 4 }}>
        <Stack spacing={2} sx={{ position: { md: 'sticky' }, top: { md: 88 } }}>
          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent
              component={RouterLink}
              to={clubHref}
              sx={{ display: 'flex', alignItems: 'center', gap: 1.5, textDecoration: 'none', color: 'inherit' }}
            >
              <Box
                component={event.clubLogoFileId ? 'img' : 'div'}
                src={event.clubLogoFileId ? `/api/files/${event.clubLogoFileId}` : undefined}
                alt=""
                sx={{ width: 44, height: 44, borderRadius: '50%', objectFit: 'cover', bgcolor: 'action.hover' }}
              />
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: 0.4 }}>
                  {t('event.organizer')}
                </Typography>
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }} noWrap>
                  {event.clubName}
                </Typography>
              </Box>
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <ScheduleRoundedIcon fontSize="small" color="action" />
                  <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                    {t('event.time')}
                  </Typography>
                </Stack>
                <IconButton
                  size="small"
                  aria-label={t('event.addToCalendar')}
                  title={t('event.addToCalendar')}
                  onClick={() =>
                    downloadEventIcs({
                      id: event.id,
                      title: event.title,
                      startIso: event.startDateUtc,
                      endIso: event.endDateUtc,
                      location: event.location,
                      description: event.description,
                    })
                  }
                >
                  <CalendarMonthOutlinedIcon fontSize="small" />
                </IconButton>
              </Stack>
              {/* Y-79: geri sayım yalnızca bilgilendirir. */}
              <EventTimeline startIso={event.startDateUtc} endIso={event.endDateUtc} />
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
                <PlaceOutlinedIcon fontSize="small" color="action" />
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                  {t('event.location')}
                </Typography>
              </Stack>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
                {event.location || t('event.locationMissing')}
              </Typography>
              {event.location && (
                <>
                  <Box
                    component="iframe"
                    title={t('event.location')}
                    src={mapsEmbedUrl(event.location)}
                    sx={{ width: '100%', height: 180, border: 0, borderRadius: 2, mb: 1.5 }}
                    loading="lazy"
                    referrerPolicy="no-referrer-when-downgrade"
                  />
                  <Button
                    fullWidth
                    variant="outlined"
                    color="error"
                    startIcon={<NearMeOutlinedIcon />}
                    component="a"
                    href={mapsDirectionsUrl(event.location)}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {t('event.directions')}
                  </Button>
                </>
              )}
            </CardContent>
          </Card>

          <Card variant="outlined" sx={{ borderRadius: 3 }}>
            <CardContent>
              <Grid container spacing={1} sx={{ textAlign: 'center' }}>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.views')}
                  </Typography>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', justifyContent: 'center' }}>
                    <VisibilityOutlinedIcon fontSize="small" color="action" />
                    <Typography sx={{ fontWeight: 800 }}>{event.viewCount}</Typography>
                  </Stack>
                </Grid>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.participants')}
                  </Typography>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', justifyContent: 'center' }}>
                    <GroupsOutlinedIcon fontSize="small" color="action" />
                    <Typography sx={{ fontWeight: 800 }}>{event.participantCount ?? '—'}</Typography>
                  </Stack>
                </Grid>
                <Grid size={4}>
                  <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
                    {t('event.capacityLabel')}
                  </Typography>
                  <Typography sx={{ fontWeight: 800 }}>{event.capacity ?? t('event.unlimited')}</Typography>
                </Grid>
              </Grid>
            </CardContent>
          </Card>

          {primaryAction}
          {actionNote}
        </Stack>
      </Grid>
    </Grid>
  )
}
