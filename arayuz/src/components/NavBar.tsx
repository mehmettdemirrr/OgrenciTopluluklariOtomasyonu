import { AppBar, Box, Button, Toolbar, Typography } from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { Permissions } from '../auth/permissions'
import { getSession } from '../auth/tokenStore'

export function NavBar() {
  const { logout, hasPermission } = useAuth()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  // K-14: Hangfire paneli sessionStorage'da olmayan (bellek-içi) access token'ı taşımaz —
  // JwtBearerEvents.OnMessageReceived (Program.cs) yalnızca /hangfire yolunda bu köprüyü kabul eder.
  const hangfireUrl = `${import.meta.env.VITE_BACKEND_URL}/hangfire?access_token=${getSession().accessToken ?? ''}`

  return (
    <AppBar position="static">
      <Toolbar sx={{ gap: 2 }}>
        <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
          Öğrenci Toplulukları Otomasyonu
        </Typography>

        <Button color="inherit" component={RouterLink} to="/clubs">
          Kulüpler
        </Button>

        {hasPermission(Permissions.MembershipsWrite) && (
          <Button color="inherit" component={RouterLink} to="/review">
            Başvuru İncele
          </Button>
        )}

        {hasPermission(Permissions.ReportsRead) && (
          <Button color="inherit" component={RouterLink} to="/reports">
            Raporlarım
          </Button>
        )}

        {hasPermission(Permissions.HangfireDashboard) && (
          <Button color="inherit" component="a" href={hangfireUrl} target="_blank" rel="noreferrer">
            Hangfire Paneli
          </Button>
        )}

        <Box>
          <Button color="inherit" onClick={handleLogout}>
            Çıkış Yap
          </Button>
        </Box>
      </Toolbar>
    </AppBar>
  )
}
