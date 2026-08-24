import { Box, Container, Drawer } from '@mui/material'
import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { AppFooter } from './AppFooter'
import { SideNav, SIDENAV_WIDTH } from './SideNav'
import { TopBar } from './TopBar'

export function AppShell() {
  const [mobileOpen, setMobileOpen] = useState(false)

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Box component="nav" sx={{ width: { md: SIDENAV_WIDTH }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: SIDENAV_WIDTH, border: 'none' },
          }}
        >
          <SideNav onNavigate={() => setMobileOpen(false)} />
        </Drawer>

        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              width: SIDENAV_WIDTH,
              border: 'none',
              // docs/PLAN-V4.md §23.1: docked kağıt, sayfa içeriğiyle birlikte uzuyordu (uzun bir
              // panelde nav kutusu 1598px oluyor) ve menünün kaydırma alanı 400px'e sıkışıp son
              // öğeleri kırpıyordu. Menü yüksekliği sayfa boyundan bağımsız olmalı.
              position: 'fixed',
              top: 0,
              height: '100vh',
            },
          }}
          open
        >
          <SideNav />
        </Drawer>
      </Box>

      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <TopBar onMenuClick={() => setMobileOpen(true)} />
        <Container maxWidth="lg" sx={{ py: 4, flex: 1 }}>
          <Outlet />
        </Container>
        <AppFooter />
      </Box>
    </Box>
  )
}
