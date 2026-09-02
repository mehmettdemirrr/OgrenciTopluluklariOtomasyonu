import {
  AppBar,
  Avatar,
  Box,
  Chip,
  Divider,
  IconButton,
  ListItemIcon,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
  alpha,
} from '@mui/material'
import MenuOutlinedIcon from '@mui/icons-material/MenuOutlined'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { useLocale } from '../../i18n/LocaleContext'
import { PreferenceControls } from '../ui/PreferenceControls'

export function TopBar({ onMenuClick, title }: { onMenuClick: () => void; title?: string }) {
  const { email, logout } = useAuth()
  const { t } = useLocale()
  const navigate = useNavigate()
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null)

  const handleLogout = async () => {
    setAnchorEl(null)
    await logout()
    navigate('/login', { replace: true })
  }

  const initial = email ? email.charAt(0).toUpperCase() : '?'

  return (
    <AppBar
      position="sticky"
      color="inherit"
      sx={{
        bgcolor: (theme) => alpha(theme.palette.background.paper, 0.88),
        backdropFilter: 'blur(14px)',
      }}
    >
      <Toolbar sx={{ gap: 1, minHeight: 72 }}>
        <IconButton edge="start" onClick={onMenuClick} sx={{ display: { md: 'none' } }} aria-label={t('common.menu')}>
          <MenuOutlinedIcon />
        </IconButton>

        <Typography variant="subtitle1" sx={{ flexGrow: 1, fontWeight: 700 }}>
          {title}
        </Typography>

        <PreferenceControls />

        <Chip
          onClick={(event) => setAnchorEl(event.currentTarget)}
          avatar={<Avatar sx={{ bgcolor: 'primary.dark', fontSize: 13 }}>{initial}</Avatar>}
          label={
            <Typography variant="body2" noWrap sx={{ maxWidth: { xs: 88, sm: 180 }, fontWeight: 600 }}>
              {email ?? t('common.account')}
            </Typography>
          }
          variant="outlined"
          sx={{
            height: 40,
            pl: 0.25,
            cursor: 'pointer',
            bgcolor: 'background.paper',
            '& .MuiChip-avatar': { width: 28, height: 28, ml: '4px' },
          }}
        />

        <Menu
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={() => setAnchorEl(null)}
          onClick={() => setAnchorEl(null)}
          slotProps={{ paper: { sx: { minWidth: 220, mt: 1 } } }}
        >
          {email && (
            <Box sx={{ px: 2, py: 1.25 }}>
              <Typography variant="caption" color="text.secondary">
                {t('common.session')}
              </Typography>
              <Typography variant="body2" noWrap sx={{ maxWidth: 220, fontWeight: 700 }}>
                {email}
              </Typography>
            </Box>
          )}
          <Divider />
          <MenuItem onClick={() => { setAnchorEl(null); navigate('/profile') }}>
            <ListItemIcon>
              <PersonOutlineOutlinedIcon fontSize="small" />
            </ListItemIcon>
            {t('common.profile')}
          </MenuItem>
          <MenuItem onClick={handleLogout}>
            <ListItemIcon>
              <LogoutOutlinedIcon fontSize="small" />
            </ListItemIcon>
            {t('common.logout')}
          </MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>
  )
}
