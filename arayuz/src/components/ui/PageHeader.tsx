import { Box, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { BackButton } from './BackButton'

interface PageHeaderProps {
  title: string
  description?: string
  action?: ReactNode
  /** Varsa ok ikonlu geri dönüş bu rotaya gider. */
  backTo?: string
}

export function PageHeader({ title, description, action, backTo }: PageHeaderProps) {
  return (
    <Stack sx={{ mb: 3.5, gap: 1.25 }}>
      {backTo && <BackButton to={backTo} />}
      <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
        <Box>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 800, fontSize: { xs: 26, md: 32 } }}>
            {title}
          </Typography>
          <Box
            sx={{
              width: 40,
              height: 3,
              borderRadius: 8,
              bgcolor: 'primary.main',
              mt: 1,
              mb: description ? 1.25 : 0,
            }}
          />
          {description && (
            <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 640, lineHeight: 1.6 }}>
              {description}
            </Typography>
          )}
        </Box>
        {action && <Box sx={{ flexShrink: 0 }}>{action}</Box>}
      </Stack>
    </Stack>
  )
}
