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
import { keyframes } from '@mui/material/styles'
import MenuOutlinedIcon from '@mui/icons-material/MenuOutlined'
import { useState } from 'react'
import { Link as RouterLink, NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { useLocale } from '../../i18n/LocaleContext'
import { brand } from '../../theme/tokens'
import { PreferenceControls } from '../ui/PreferenceControls'
import { AppFooter } from './AppFooter'
import { BrandMark } from './BrandMark'
import { CampusLocationMenu } from './CampusLocationMenu'

const brandBarSlide = keyframes`
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
`
// docs/PLAN-V2.md §14.5: giriş yapmamış ziyaretçi kabuğu — sidebar yok, sade üst bar + footer.
const publicLinks = [
  { to: '/kulupler', labelKey: 'nav.clubs' },
  { to: '/etkinlikler', labelKey: 'nav.events' },
]

export function PublicLayout() {
  const { isAuthenticated } = useAuth()
  const { t } = useLocale()
  const location = useLocation()
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' }}>
      <AppBar
        position="sticky"
        color="transparent"
        sx={{
          color: 'common.white',
          bgcolor: 'transparent',
          backgroundImage: `linear-gradient(90deg, ${brand.navy} 0%, ${brand.orange} 38%, ${brand.gold} 68%, ${brand.navy} 100%)`,
          backgroundSize: '260% 100%',
          animation: `${brandBarSlide} 14s ease-in-out infinite`,
          boxShadow: (theme) => `0 8px 24px ${alpha(theme.palette.common.black, 0.18)}`,
          '@media (prefers-reduced-motion: reduce)': {
            animation: 'none',
            backgroundPosition: '20% 50%',
          },
        }}
      >
        <Toolbar
          sx={{
            gap: 1,
            minHeight: 72,
            color: 'common.white',
            '& .MuiIconButton-root': { color: 'common.white' },
            '& .MuiToggleButtonGroup-root': {
              bgcolor: (theme) => alpha(theme.palette.common.white, 0.12),
            },
            '& .MuiToggleButton-root': {
              color: 'common.white',
              borderColor: (theme) => alpha(theme.palette.common.white, 0.28),
              '&.Mui-selected': {
                color: 'common.white',
                bgcolor: (theme) => alpha(theme.palette.common.white, 0.22),
              },
            },
          }}
        >
          <BrandMark to="/" showSubtitle light />
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
                    color: 'common.white',
                    bgcolor: active ? (theme) => alpha(theme.palette.common.white, 0.18) : 'transparent',
                  }}
                >
                  {t(link.labelKey)}
                </Button>
              )
            })}
          </Stack>

          <IconButton
            edge="end"
            onClick={(event) => setMenuAnchor(event.currentTarget)}
            sx={{ display: { xs: 'inline-flex', sm: 'none' } }}
            aria-label={t('common.menu')}
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
                {t(link.labelKey)}
              </MenuItem>
            ))}
            <Divider />
            <MenuItem
              component={RouterLink}
              to="/#kampusler"
              onClick={() => {
                setMenuAnchor(null)
                window.setTimeout(() => {
                  document.getElementById('kampusler')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
                }, 0)
              }}
            >
              {t('home.campuses')}
            </MenuItem>
            <Divider />
            {isAuthenticated ? (
              <MenuItem component={RouterLink} to="/panel" onClick={() => setMenuAnchor(null)}>
                {t('common.goPanel')}
              </MenuItem>
            ) : (
              [
                <MenuItem key="login" component={RouterLink} to="/login" onClick={() => setMenuAnchor(null)}>
                  {t('common.login')}
                </MenuItem>,
                <MenuItem key="register" component={RouterLink} to="/register" onClick={() => setMenuAnchor(null)}>
                  {t('common.register')}
                </MenuItem>,
              ]
            )}
          </Menu>

          <CampusLocationMenu />

          <PreferenceControls />

          <Stack direction="row" spacing={1} sx={{ display: { xs: 'none', sm: 'flex' } }}>
            {isAuthenticated ? (
              <Button
                component={RouterLink}
                to="/panel"
                variant="contained"
                sx={{ bgcolor: 'common.white', color: 'secondary.main', '&:hover': { bgcolor: 'common.white' } }}
              >
                {t('common.goPanel')}
              </Button>
            ) : (
              <>
                <Button component={RouterLink} to="/login" color="inherit" sx={{ fontWeight: 600, color: 'common.white' }}>
                  {t('common.login')}
                </Button>
                <Button
                  component={RouterLink}
                  to="/register"
                  variant="contained"
                  sx={{ bgcolor: 'common.white', color: 'secondary.main', '&:hover': { bgcolor: 'common.white' } }}
                >
                  {t('common.register')}
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
