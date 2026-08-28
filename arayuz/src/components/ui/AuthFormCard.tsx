import { Paper } from '@mui/material'
import type { ReactNode } from 'react'

export function AuthFormCard({ children }: { children: ReactNode }) {
  return (
    <Paper
      variant="outlined"
      sx={{
        width: '100%',
        maxWidth: 440,
        p: { xs: 3, sm: 4.5 },
        borderRadius: 3,
        '&:hover': {
          transform: 'none',
          boxShadow: 'none',
        },
      }}
    >
      {children}
    </Paper>
  )
}
