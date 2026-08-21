import { AppBar, Box, Button, Container, Stack, Toolbar, Typography } from '@mui/material'
import { Link as RouterLink, Outlet } from 'react-router-dom'

// docs/PLAN-V2.md §8.2 · Faz 14'te dolacak giriş yapmamış ziyaretçi kabuğu — iskelet burada
// kuruluyor ki tema tek seferde oturtulsun.
export function PublicLayout() {
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', bgcolor: 'background.default' }}>
      <AppBar position="sticky" color="inherit" sx={{ bgcolor: 'background.paper' }}>
        <Toolbar>
          <Typography variant="subtitle1" sx={{ flexGrow: 1, fontWeight: 800 }}>
            Öğrenci Toplulukları
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button component={RouterLink} to="/login">
              Giriş Yap
            </Button>
          </Stack>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 4, flex: 1 }}>
        <Outlet />
      </Container>

      <Box component="footer" sx={{ py: 3, textAlign: 'center', color: 'text.secondary' }}>
        <Typography variant="caption">Öğrenci Toplulukları Otomasyonu</Typography>
      </Box>
    </Box>
  )
}
