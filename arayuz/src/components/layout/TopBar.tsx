import {
  AppBar,
  Avatar,
  Box,
  Divider,
  IconButton,
  ListItemIcon,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
} from '@mui/material'
import MenuOutlinedIcon from '@mui/icons-material/MenuOutlined'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'

export function TopBar({ onMenuClick, title }: { onMenuClick: () => void; title?: string }) {
  const { email, logout } = useAuth()
  const navigate = useNavigate()
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)

  const handleLogout = async () => {
    setAnchorEl(null)
    await logout()
    navigate('/login', { replace: true })
  }

  const initial = email ? email.charAt(0).toUpperCase() : '?'

  return (
    <AppBar position="sticky" color="inherit" sx={{ bgcolor: 'background.paper' }}>
      <Toolbar sx={{ gap: 1 }}>
        <IconButton edge="start" onClick={onMenuClick} sx={{ display: { md: 'none' } }}>
          <MenuOutlinedIcon />
        </IconButton>

        <Typography variant="subtitle1" sx={{ flexGrow: 1, fontWeight: 700 }}>
          {title}
        </Typography>

        <IconButton onClick={(event) => setAnchorEl(event.currentTarget)} size="small">
          <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.dark', fontSize: 14 }}>{initial}</Avatar>
        </IconButton>

        <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={() => setAnchorEl(null)} onClick={() => setAnchorEl(null)}>
          {email && (
            <Box sx={{ px: 2, py: 1 }}>
              <Typography variant="body2" noWrap sx={{ maxWidth: 220, fontWeight: 600 }}>
                {email}
              </Typography>
            </Box>
          )}
          <Divider />
          <MenuItem onClick={handleLogout}>
            <ListItemIcon>
              <LogoutOutlinedIcon fontSize="small" />
            </ListItemIcon>
            Çıkış Yap
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  )
}
