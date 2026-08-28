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
    <Stack
      spacing={1.5}
      sx={{
        alignItems: 'center',
        justifyContent: 'center',
        py: 7,
        px: 2,
        textAlign: 'center',
        borderRadius: 3,
        border: '1px dashed',
        borderColor: 'divider',
        bgcolor: (t) => alpha(t.palette.primary.main, 0.03),
      }}
    >
      <Box
        sx={{
          width: 64,
          height: 64,
          borderRadius: '50%',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          bgcolor: alpha(theme.palette.primary.main, 0.12),
          color: theme.palette.primary.dark,
        }}
      >
        <Icon />
      </Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 800 }}>
        {title}
      </Typography>
      {description && (
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 380, lineHeight: 1.6 }}>
          {description}
        </Typography>
      )}
      {action}
    </Stack>
  )
}
