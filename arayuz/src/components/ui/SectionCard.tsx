import { Box, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

interface SectionCardProps {
  title?: string
  action?: ReactNode
  children: ReactNode
  sx?: object
}

export function SectionCard({ title, action, children, sx }: SectionCardProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        p: { xs: 2.5, md: 3 },
        borderRadius: 3,
        ...sx,
      }}
    >
      {(title || action) && (
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 2.5, gap: 2 }}>
          {title && (
            <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center', minWidth: 0 }}>
              <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: 'primary.main', flexShrink: 0 }} />
              <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
                {title}
              </Typography>
            </Stack>
          )}
          {action}
        </Stack>
      )}
      <Box>{children}</Box>
    </Paper>
  )
}
