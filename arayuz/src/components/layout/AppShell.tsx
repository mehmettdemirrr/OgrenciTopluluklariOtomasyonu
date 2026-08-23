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
            '& .MuiDrawer-paper': { width: SIDENAV_WIDTH, border: 'none' },
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
