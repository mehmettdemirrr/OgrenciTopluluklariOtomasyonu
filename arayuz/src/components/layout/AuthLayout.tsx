import { AppBar, Box, Toolbar } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { PreferenceControls } from '../ui/PreferenceControls'
import { AppFooter } from './AppFooter'
import { BrandMark } from './BrandMark'
import { CampusLocationMenu } from './CampusLocationMenu'
import { brandSlideBarSx, brandSlideToolbarSx } from './brandSlideBar'

/**
 * docs/PLAN-V3.md §15.2: kimlik sayfaları (login/register/confirm-email/forgot/reset) ne `AppShell`
 * ne `PublicLayout` altında; footer'ı beşine tek tek eklemek yerine bu ince kabuk sarmalıyor.
 * Sayfalar kendi köklerinde `flex: 1` kullanır (100vh DEĞİL) — aksi hâlde 100vh + footer her
 * kimlik sayfasında gereksiz bir kaydırma çubuğu üretirdi.
 */
export function AuthLayout() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' }}>
      <AppBar position="sticky" color="transparent" sx={brandSlideBarSx}>
        <Toolbar sx={brandSlideToolbarSx}>
          <BrandMark to="/" showSubtitle light />
          <Box sx={{ flexGrow: 1 }} />
          <CampusLocationMenu />
          <PreferenceControls />
        </Toolbar>
      </AppBar>
      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <Outlet />
      </Box>
      <AppFooter />
    </Box>
  )
}
