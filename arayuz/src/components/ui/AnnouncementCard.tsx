import { Box, CardMedia, Paper, Stack, Typography, alpha } from '@mui/material'
import type { ReactNode } from 'react'
import { DateBadge } from './DateBadge'
import { RichTextContent } from '../richtext/RichTextContent'

interface AnnouncementCardProps {
  title: string
  content: string
  contentJson?: string | null
  imageFileId?: number | null
  publishedAtUtc: string
  meta?: string
  chip?: ReactNode
  action?: ReactNode
}

export function AnnouncementCard({ title, content, contentJson, imageFileId, publishedAtUtc, meta, chip, action }: AnnouncementCardProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        overflow: 'hidden',
        transition: 'border-color 180ms ease, box-shadow 180ms ease',
        '&:hover': {
          borderColor: (theme) => alpha(theme.palette.primary.main, 0.42),
          boxShadow: (theme) => `0 12px 28px ${alpha(theme.palette.secondary.main, 0.08)}`,
        },
      }}
    >
      {imageFileId && (
        <CardMedia component="img" height={160} image={`/api/files/${imageFileId}`} alt="" sx={{ objectFit: 'cover' }} />
      )}
      <Box sx={{ p: { xs: 2, md: 2.5 } }}>
        <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start' }}>
          <DateBadge iso={publishedAtUtc} />
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{ alignItems: { sm: 'flex-start' }, justifyContent: 'space-between', mb: 0.75 }}
            >
              <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap', minWidth: 0 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
                  {title}
                </Typography>
                {chip}
              </Stack>
              {action}
            </Stack>
            {meta && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                {meta}
              </Typography>
            )}
            <RichTextContent json={contentJson ?? null} fallbackText={content} />
          </Box>
        </Stack>
      </Box>
    </Paper>
  )
}
