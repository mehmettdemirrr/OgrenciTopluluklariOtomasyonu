import { Paper, alpha } from '@mui/material'
import type { ReactNode } from 'react'

export function AuthFormCard({ children }: { children: ReactNode }) {
  return (
    <Paper
      elevation={0}
      sx={{
        width: '100%',
        maxWidth: 440,
        position: 'relative',
        overflow: 'hidden',
        p: { xs: 3, sm: 4.5 },
        borderRadius: 4,
        border: '1px solid',
        borderColor: 'divider',
        boxShadow: (theme) => `0 24px 56px ${alpha(theme.palette.secondary.main, 0.12)}`,
        '&::before': {
          content: '""',
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: 4,
          background: (theme) =>
            `linear-gradient(90deg, ${theme.palette.secondary.main}, ${theme.palette.warning.main}, ${theme.palette.accent.main})`,
        },
      }}
    >
      {children}
    </Paper>
  )
}
