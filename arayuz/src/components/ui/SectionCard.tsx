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
    <Paper variant="outlined" sx={{ p: 3, ...sx }}>
      {(title || action) && (
        <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
          {title && (
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {title}
            </Typography>
          )}
          {action}
        </Stack>
      )}
      <Box>{children}</Box>
    </Paper>
  )
}
