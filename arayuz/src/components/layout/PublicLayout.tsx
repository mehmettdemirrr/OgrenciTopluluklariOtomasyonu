import {
  AppBar,
  Box,
  Button,
  Container,
  Divider,
  IconButton,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  alpha,
} from '@mui/material'
import MenuOutlinedIcon from '@mui/icons-material/MenuOutlined'
import { useState } from 'react'
import { Link as RouterLink, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { AppFooter } from './AppFooter'
import { BrandMark } from './BrandMark'

// docs/PLAN-V2.md §14.5: giriş yapmamış ziyaretçi kabuğu — sidebar yok, sade üst bar + footer.
const publicLinks = [
  { to: '/kulupler', label: 'Kulüpler' },
  { to: '/etkinlikler', label: 'Etkinlikler' },
]

export function PublicLayout() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' }}>
      <AppBar
        position="sticky"
        color="inherit"
        sx={{
          bgcolor: (theme) => alpha(theme.palette.background.paper, 0.86),
          backdropFilter: 'blur(16px)',
        }}
      >
        <Toolbar sx={{ gap: 1, minHeight: 72 }}>
          <BrandMark to="/" showSubtitle />

          <Box sx={{ flexGrow: 1 }} />

          <Stack direction="row" spacing={0.5} sx={{ display: { xs: 'none', sm: 'flex' }, mr: 1 }}>
            {publicLinks.map((link) => {
              const active = location.pathname.startsWith(link.to)
              return (
                <Button
                  key={link.to}
                  component={NavLink}
                  to={link.to}
                  color="inherit"
                  sx={{
                    fontWeight: active ? 800 : 600,
                    color: active ? 'primary.dark' : 'text.primary',
                    bgcolor: active ? (theme) => alpha(theme.palette.primary.main, 0.1) : 'transparent',
                  }}
                >
                  {link.label}
                </Button>
              )
            })}
          </Stack>

          <IconButton
            edge="end"
            onClick={(event) => setMenuAnchor(event.currentTarget)}
            sx={{ display: { xs: 'inline-flex', sm: 'none' } }}
            aria-label="Menü"
          >
            <MenuOutlinedIcon />
          </IconButton>

          <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={() => setMenuAnchor(null)}>
            {publicLinks.map((link) => (
              <MenuItem
                key={link.to}
                component={RouterLink}
                to={link.to}
                onClick={() => setMenuAnchor(null)}
                selected={location.pathname.startsWith(link.to)}
              >
                {link.label}
              </MenuItem>
            ))}
            <Divider />
            {isAuthenticated ? (
              <MenuItem component={RouterLink} to="/panel" onClick={() => setMenuAnchor(null)}>
                Panele Git
              </MenuItem>
            ) : (
              [
                <MenuItem key="login" component={RouterLink} to="/login" onClick={() => setMenuAnchor(null)}>
                  Giriş Yap
                </MenuItem>,
                <MenuItem key="register" component={RouterLink} to="/register" onClick={() => setMenuAnchor(null)}>
                  Kayıt Ol
                </MenuItem>,
              ]
            )}
          </Menu>

          <Stack direction="row" spacing={1} sx={{ display: { xs: 'none', sm: 'flex' } }}>
            {isAuthenticated ? (
              <Button component={RouterLink} to="/panel" variant="contained">
                Panele Git
              </Button>
            ) : (
              <>
                <Button component={RouterLink} to="/login" color="inherit" sx={{ fontWeight: 600 }}>
                  Giriş Yap
                </Button>
                <Button component={RouterLink} to="/register" variant="contained">
                  Kayıt Ol
                </Button>
              </>
            )}
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: { xs: 3, md: 5 }, flex: 1 }}>
        <Outlet />
      </Container>

      <AppFooter />
    </Box>
  )
}
