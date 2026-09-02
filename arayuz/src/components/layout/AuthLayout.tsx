import { Box, alpha } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { PreferenceControls } from '../ui/PreferenceControls'
import { AppFooter } from './AppFooter'

/**
 * docs/PLAN-V3.md §15.2: kimlik sayfaları (login/register/confirm-email/forgot/reset) ne `AppShell`
 * ne `PublicLayout` altında; footer'ı beşine tek tek eklemek yerine bu ince kabuk sarmalıyor.
 * Sayfalar kendi köklerinde `flex: 1` kullanır (100vh DEĞİL) — aksi hâlde 100vh + footer her
 * kimlik sayfasında gereksiz bir kaydırma çubuğu üretirdi.
 */
export function AuthLayout() {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        position: 'relative',
        display: 'flex',
        flexDirection: 'column',
        bgcolor: 'background.default',
        backgroundImage: (theme) =>
          `radial-gradient(ellipse at 0% 0%, ${alpha(theme.palette.primary.main, 0.1)} 0%, transparent 46%), radial-gradient(ellipse at 100% 100%, ${alpha(theme.palette.secondary.main, 0.08)} 0%, transparent 42%)`,
      }}
    >
      <Box sx={{ position: 'absolute', top: 16, right: 16, zIndex: 2 }}>
        <PreferenceControls />
      </Box>
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', pt: { xs: 6, md: 0 } }}>
        <Outlet />
      </Box>
      <AppFooter />
    </Box>
  )
}
