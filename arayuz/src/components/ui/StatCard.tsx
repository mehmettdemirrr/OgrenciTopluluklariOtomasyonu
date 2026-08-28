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
    <Paper
      variant="outlined"
      sx={{
        p: 2.5,
        minWidth: 200,
        flex: '1 1 200px',
        borderRadius: 3,
        position: 'relative',
        overflow: 'hidden',
        transition: 'transform 180ms ease, box-shadow 180ms ease, border-color 180ms ease',
        '&:hover': {
          transform: 'translateY(-3px)',
          borderColor: alpha(accent, 0.45),
          boxShadow: `0 12px 28px ${alpha(theme.palette.secondary.main, 0.1)}`,
        },
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          background: `linear-gradient(135deg, ${alpha(accent, 0.08)} 0%, transparent 62%)`,
          pointerEvents: 'none',
        }}
      />
      <Stack direction="row" spacing={2} sx={{ alignItems: 'center', position: 'relative' }}>
        {Icon && (
          <Box
            sx={{
              width: 48,
              height: 48,
              borderRadius: 2.5,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: alpha(accent, 0.14),
              color: accent,
            }}
          >
            <Icon />
          </Box>
        )}
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 800, lineHeight: 1.1 }}>
            {value}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            {label}
          </Typography>
        </Box>
      </Stack>
    </Paper>
  )
}
