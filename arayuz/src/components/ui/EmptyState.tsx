import { Box, Stack, Typography, alpha, useTheme, type SvgIconProps } from '@mui/material'
import type { ComponentType, ReactNode } from 'react'
import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined'

interface EmptyStateProps {
  title: string
  description?: string
  icon?: ComponentType<SvgIconProps>
  action?: ReactNode
}

export function EmptyState({ title, description, icon: Icon = InboxOutlinedIcon, action }: EmptyStateProps) {
  const theme = useTheme()

  return (
    <Stack spacing={1.5} sx={{ alignItems: 'center', justifyContent: 'center', py: 6, px: 2, textAlign: 'center' }}>
      <Box
        sx={{
          width: 56,
          height: 56,
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: alpha(theme.palette.primary.main, 0.1),
          color: theme.palette.primary.main,
        }}
      >
        <Icon fontSize="medium" />
      </Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
        {title}
      </Typography>
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 360 }}>
          {description}
        </Typography>
      )}
      {action}
    </Stack>
  )
}
