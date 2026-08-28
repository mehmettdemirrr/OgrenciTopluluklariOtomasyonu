import { Box, Paper, Stack, Typography, alpha, type SvgIconProps } from '@mui/material'
import type { ComponentType, ReactNode } from 'react'

interface InfoTileProps {
  label: string
  value: ReactNode
  icon?: ComponentType<SvgIconProps>
}

export function InfoTile({ label, value, icon: Icon }: InfoTileProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2,
        height: '100%',
        borderRadius: 2.5,
        bgcolor: 'background.paper',
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        {Icon && (
          <Box
            sx={{
              width: 40,
              height: 40,
              borderRadius: 2,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: (theme) => alpha(theme.palette.primary.main, 0.12),
              color: 'primary.dark',
              flexShrink: 0,
            }}
          >
            <Icon fontSize="small" />
          </Box>
        )}
        <Box sx={{ minWidth: 0 }}>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontWeight: 700, letterSpacing: 0.4, textTransform: 'uppercase' }}
          >
            {label}
          </Typography>
          <Typography variant="body2" sx={{ fontWeight: 700, mt: 0.25, wordBreak: 'break-word' }}>
            {value}
          </Typography>
        </Box>
      </Stack>
    </Paper>
  )
}
