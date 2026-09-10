import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import { Box, CardMedia, Chip, Skeleton, Stack, Typography, alpha } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiClient } from '../../api/client'
import type { PublicAnnouncementListItemDto } from '../../api/types'
import { BackButton } from '../../components/ui/BackButton'
import { EmptyState } from '../../components/ui/EmptyState'
import { RichTextContent } from '../../components/richtext/RichTextContent'
import { useDocumentTitle } from '../../hooks/useDocumentTitle'
import { useLocale } from '../../i18n/LocaleContext'
import { formatAnnouncementDateTime } from '../../utils/announcementFormat'

export function PublicAnnouncementDetailPage() {
  const { t, dateLocale } = useLocale()
  const { id } = useParams<{ id: string }>()
  const announcementId = Number(id)

  const announcementQuery = useQuery({
    queryKey: ['public-announcement', announcementId],
    queryFn: async () => (await apiClient.get<PublicAnnouncementListItemDto>(`/public/announcements/${announcementId}`)).data,
    enabled: Number.isInteger(announcementId) && announcementId > 0,
  })

  useDocumentTitle(announcementQuery.data?.title)

  if (announcementQuery.isLoading) {
    return (
      <Stack spacing={2}>
        <BackButton to="/duyurular" />
        <Skeleton variant="rounded" height={280} />
      </Stack>
    )
  }

  if (announcementQuery.isError || !announcementQuery.data) {
    return (
      <Stack spacing={2}>
        <BackButton to="/duyurular" />
        <EmptyState
          icon={CampaignOutlinedIcon}
          title={t('public.announcementMissing')}
          description={t('public.announcementMissingLead')}
        />
      </Stack>
    )
  }

  const announcement = announcementQuery.data

  return (
    <Stack spacing={3}>
      <BackButton to="/duyurular" />

      <Box
        sx={{
          borderRadius: 3,
          overflow: 'hidden',
          border: '1px solid',
          borderColor: (theme) => alpha(theme.palette.secondary.main, 0.08),
          bgcolor: 'background.paper',
        }}
      >
        {announcement.imageFileId ? (
          <CardMedia
            component="img"
            image={`/api/files/${announcement.imageFileId}`}
            alt=""
            sx={{ height: { xs: 220, md: 320 }, objectFit: 'cover' }}
          />
        ) : (
          <Box
            sx={{
              height: { xs: 160, md: 200 },
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              color: 'primary.main',
              background: (theme) =>
                `linear-gradient(135deg, ${alpha(theme.palette.primary.main, 0.12)} 0%, ${alpha(theme.palette.secondary.main, 0.18)} 100%)`,
            }}
          >
            <CampaignOutlinedIcon sx={{ fontSize: 56 }} />
          </Box>
        )}

        <Box sx={{ p: { xs: 2.5, md: 4 } }}>
          <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 1.5 }}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700, letterSpacing: '0.04em' }}>
              {formatAnnouncementDateTime(announcement.publishedAtUtc, dateLocale)}
            </Typography>
            {announcement.clubName && announcement.clubId && (
              <Chip
                size="small"
                label={announcement.clubName}
                component={RouterLink}
                to={`/kulupler/${announcement.clubId}`}
                clickable
                variant="outlined"
              />
            )}
          </Stack>

          <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mb: 2.5, color: 'text.primary' }}>
            {announcement.title}
          </Typography>

          <RichTextContent json={announcement.contentJson} fallbackText={announcement.content} />
        </Box>
      </Box>
    </Stack>
  )
}
