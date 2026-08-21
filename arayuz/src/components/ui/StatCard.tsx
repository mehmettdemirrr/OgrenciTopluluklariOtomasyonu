import { Box, Paper, Stack, Typography, alpha, useTheme, type SvgIconProps } from '@mui/material'
import type { ComponentType } from 'react'

interface StatCardProps {
  label: string
  value: string | number
  icon?: ComponentType<SvgIconProps>
  color?: string
}

export function StatCard({ label, value, icon: Icon, color }: StatCardProps) {
  const theme = useTheme()
  const accent = color ?? theme.palette.primary.main

  return (
    <Paper variant="outlined" sx={{ p: 2.5, minWidth: 200, flex: '1 1 200px' }}>
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
        {Icon && (
          <Box
            sx={{
              width: 44,
              height: 44,
              borderRadius: 2,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: alpha(accent, 0.12),
              color: accent,
            }}
          >
            <Icon fontSize="small" />
          </Box>
        )}
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {value}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {label}
          </Typography>
        </Box>
      </Stack>
    </Paper>
  )
}
