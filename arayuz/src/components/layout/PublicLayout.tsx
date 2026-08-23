import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from '@mui/material'
import { Link as RouterLink, Outlet } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { AppFooter } from './AppFooter'
import logo from '../../assets/logo.png'

// docs/PLAN-V2.md §14.5: giriş yapmamış ziyaretçi kabuğu — sidebar yok, sade üst bar + footer.
export function PublicLayout() {
  const { isAuthenticated } = useAuth()

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' }}>
      <AppBar position="sticky" color="inherit" sx={{ bgcolor: 'background.paper' }}>
        <Toolbar>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexGrow: 1 }}>
            <Box component="img" src={logo} alt="" sx={{ width: 28, height: 28 }} />
            <Typography component={RouterLink} to="/" variant="subtitle1" sx={{ fontWeight: 800, color: 'inherit', textDecoration: 'none' }}>
              Öğrenci Toplulukları
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} sx={{ display: { xs: 'none', sm: 'flex' }, mr: 2 }}>
            <Button component={RouterLink} to="/kulupler" color="inherit">
              Kulüpler
            </Button>
            <Button component={RouterLink} to="/etkinlikler" color="inherit">
              Etkinlikler
            </Button>
          </Stack>

          <Stack direction="row" spacing={1}>
            {isAuthenticated ? (
              <Button component={RouterLink} to="/panel" variant="contained">
                Panele Git
              </Button>
            ) : (
              <>
                <Button component={RouterLink} to="/login">
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

      <Container maxWidth="lg" sx={{ py: 4, flex: 1 }}>
        <Outlet />
      </Container>

      <AppFooter />
    </Box>
  )
}
