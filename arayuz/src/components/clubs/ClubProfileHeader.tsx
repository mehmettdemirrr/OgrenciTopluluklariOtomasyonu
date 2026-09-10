import { Box, Card, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'

export interface ClubProfileHeaderProps {
  name: string
  logoFileId: number | null
  /** Kuruluş yılı, üye/görüntülenme/etkinlik sayısı, kategori rozetleri — sayfa kendi setini verir. */
  badges?: ReactNode
  /** Panelde "Düzenle"/"Pasife Al"; vitrinde yok. */
  actions?: ReactNode
}

/** docs/MIMARI.md · K-54/A-85: kulüp künyesi — panel ve vitrin bu tek bileşeni sarar. */
export function ClubProfileHeader({ name, logoFileId, badges, actions }: ClubProfileHeaderProps) {
  return (
    <Card variant="outlined" sx={{ borderRadius: 3, textAlign: 'center', pt: 4, pb: 3, px: 2 }}>
      <Box
        component={logoFileId ? 'img' : 'div'}
        src={logoFileId ? `/api/files/${logoFileId}` : undefined}
        alt=""
        sx={{
          width: 108, height: 108, borderRadius: '50%', objectFit: 'contain',
          bgcolor: 'background.paper', border: '4px solid', borderColor: 'background.paper',
          boxShadow: 3, mx: 'auto', display: 'block', p: 1,
        }}
      />
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800, mt: 2 }}>
        {name}
      </Typography>
      {badges && (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2 }}>
          {badges}
        </Stack>
      )}
      {actions && (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'center', mt: 2.5 }}>
          {actions}
        </Stack>
      )}
    </Card>
  )
}
