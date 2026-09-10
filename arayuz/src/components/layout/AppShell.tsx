import { Box, Container, Drawer, useMediaQuery, useTheme } from '@mui/material'
import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { AppFooter } from './AppFooter'
import { SideNav, SIDENAV_WIDTH } from './SideNav'
import { TopBar } from './TopBar'

const SIDENAV_OPEN_KEY = 'app-sidenav-open'

const drawerPaperSx = {
  width: SIDENAV_WIDTH,
  border: 'none',
  // MuiPaper kökü 16px yuvarlatır; docked menüde sayfa arka planı köşelerde beyaz üçgen bırakır.
  borderRadius: 0,
  overflow: 'hidden',
  bgcolor: 'secondary.main',
} as const

function readDesktopNavOpen() {
  return localStorage.getItem(SIDENAV_OPEN_KEY) !== 'false'
}

export function AppShell() {
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const [mobileOpen, setMobileOpen] = useState(false)
  const [desktopOpen, setDesktopOpen] = useState(readDesktopNavOpen)

  useEffect(() => {
    localStorage.setItem(SIDENAV_OPEN_KEY, String(desktopOpen))
  }, [desktopOpen])

  const handleMenuClick = () => {
    if (isDesktop) {
      setDesktopOpen((open) => !open)
      return
    }
    setMobileOpen(true)
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <Box
        component="nav"
        sx={{
          width: { md: desktopOpen ? SIDENAV_WIDTH : 0 },
          flexShrink: { md: 0 },
          overflow: 'hidden',
          transition: theme.transitions.create('width', {
            easing: theme.transitions.easing.sharp,
            duration: desktopOpen
              ? theme.transitions.duration.enteringScreen
              : theme.transitions.duration.leavingScreen,
          }),
        }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': drawerPaperSx,
          }}
        >
          <SideNav onNavigate={() => setMobileOpen(false)} />
        </Drawer>

        <Drawer
          variant="persistent"
          open={desktopOpen}
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              ...drawerPaperSx,
              // docs/PLAN-V4.md §23.1: docked kağıt, sayfa içeriğiyle birlikte uzuyordu (uzun bir
              // panelde nav kutusu 1598px oluyor) ve menünün kaydırma alanı 400px'e sıkışıp son
              // öğeleri kırpıyordu. Menü yüksekliği sayfa boyundan bağımsız olmalı.
              position: 'fixed',
              top: 0,
              height: '100vh',
            },
          }}
        >
          <SideNav onCollapse={() => setDesktopOpen(false)} />
        </Drawer>
      </Box>

      <Box sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <TopBar
          onMenuClick={handleMenuClick}
          navOpen={isDesktop && desktopOpen}
          showBrand={!isDesktop || !desktopOpen}
        />
        <Container id="main-content" component="main" tabIndex={-1} maxWidth="lg" sx={{ py: { xs: 3, md: 4 }, flex: 1 }}>
          <Outlet />
        </Container>
        <AppFooter />
      </Box>
    </Box>
  )
}
