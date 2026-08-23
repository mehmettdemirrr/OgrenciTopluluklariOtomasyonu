import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { AppFooter } from './AppFooter'

/**
 * docs/PLAN-V3.md §15.2: kimlik sayfaları (login/register/confirm-email/forgot/reset) ne `AppShell`
 * ne `PublicLayout` altında; footer'ı beşine tek tek eklemek yerine bu ince kabuk sarmalıyor.
 * Sayfalar kendi köklerinde `flex: 1` kullanır (100vh DEĞİL) — aksi hâlde 100vh + footer her
 * kimlik sayfasında gereksiz bir kaydırma çubuğu üretirdi.
 */
export function AuthLayout() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Outlet />
      <AppFooter />
    </Box>
  )
}
