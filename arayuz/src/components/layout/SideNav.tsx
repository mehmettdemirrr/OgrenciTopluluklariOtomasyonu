import {
  Box,
  Divider,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  alpha,
} from '@mui/material'
import SpaceDashboardOutlinedIcon from '@mui/icons-material/SpaceDashboardOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import Groups2OutlinedIcon from '@mui/icons-material/Groups2Outlined'
import EventOutlinedIcon from '@mui/icons-material/EventOutlined'
import EventAvailableOutlinedIcon from '@mui/icons-material/EventAvailableOutlined'
import CampaignOutlinedIcon from '@mui/icons-material/CampaignOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import BarChartOutlinedIcon from '@mui/icons-material/BarChartOutlined'
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import type { ComponentType } from 'react'
import type { SvgIconProps } from '@mui/material'
import { useAuth } from '../../auth/AuthContext'
import { Permissions } from '../../auth/permissions'
import { getSession } from '../../auth/tokenStore'

interface NavItem {
  label: string
  to: string
  icon: ComponentType<SvgIconProps>
  permission?: string
  external?: boolean
}

const genelItems: NavItem[] = [
  { label: 'Panel', to: '/panel', icon: SpaceDashboardOutlinedIcon },
  { label: 'Kulüpler', to: '/clubs', icon: GroupsOutlinedIcon },
  { label: 'Kulüplerim', to: '/my-clubs', icon: Groups2OutlinedIcon },
  { label: 'Etkinlikler', to: '/events', icon: EventOutlinedIcon, permission: Permissions.EventsRead },
  { label: 'Etkinliklerim', to: '/my-events', icon: EventAvailableOutlinedIcon, permission: Permissions.EventsRead },
  { label: 'Duyurular', to: '/announcements', icon: CampaignOutlinedIcon, permission: Permissions.ClubsRead },
  { label: 'Başvuru İncele', to: '/review', icon: FactCheckOutlinedIcon, permission: Permissions.MembershipsWrite },
  { label: 'Raporlarım', to: '/reports', icon: BarChartOutlinedIcon, permission: Permissions.ReportsRead },
]

const yonetimItems: NavItem[] = [
  { label: 'Yetki Matrisi', to: '/authorization', icon: AdminPanelSettingsOutlinedIcon, permission: Permissions.RolesManage },
  { label: 'Referans Verisi', to: '/reference', icon: CategoryOutlinedIcon, permission: Permissions.ReferenceManage },
]

export const SIDENAV_WIDTH = 248

export function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const { hasPermission } = useAuth()
  const location = useLocation()

  const hangfireUrl = `${import.meta.env.VITE_BACKEND_URL}/hangfire?access_token=${getSession().accessToken ?? ''}`

  const renderItems = (items: NavItem[]) =>
    items
      .filter((item) => !item.permission || hasPermission(item.permission))
      .map((item) => {
        const active = location.pathname.startsWith(item.to)
        return (
          <ListItemButton
            key={item.to}
            component={RouterLink}
            to={item.to}
            onClick={onNavigate}
            selected={active}
            sx={{
              borderRadius: 2,
              mx: 1,
              mb: 0.5,
              color: active ? 'primary.light' : alpha('#ffffff', 0.85),
              '&.Mui-selected': {
                bgcolor: alpha('#ffffff', 0.08),
              },
              '&:hover': {
                bgcolor: alpha('#ffffff', 0.06),
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 36, color: 'inherit' }}>
              <item.icon fontSize="small" />
            </ListItemIcon>
            <ListItemText slotProps={{ primary: { sx: { fontSize: 14, fontWeight: active ? 700 : 500 } } }}>{item.label}</ListItemText>
          </ListItemButton>
        )
      })

  return (
    <Box sx={{ height: '100%', bgcolor: 'secondary.main', color: 'common.white', display: 'flex', flexDirection: 'column' }}>
      <Toolbar sx={{ px: 2.5 }}>
        <Typography variant="subtitle1" noWrap sx={{ fontWeight: 800, color: 'common.white' }}>
          Öğrenci Toplulukları
        </Typography>
      </Toolbar>

      <Box sx={{ flex: 1, overflowY: 'auto', py: 1 }}>
        <List dense subheader={<NavGroupLabel text="Genel" />}>
          {renderItems(genelItems)}
        </List>

        {(hasPermission(Permissions.RolesManage) || hasPermission(Permissions.ReferenceManage)) && (
          <>
            <Divider sx={{ borderColor: alpha('#ffffff', 0.12), my: 1 }} />
            <List dense subheader={<NavGroupLabel text="Yönetim" />}>
              {renderItems(yonetimItems)}
            </List>
          </>
        )}
      </Box>

      {hasPermission(Permissions.HangfireDashboard) && (
        <>
          <Divider sx={{ borderColor: alpha('#ffffff', 0.12) }} />
          <ListItemButton component="a" href={hangfireUrl} target="_blank" rel="noreferrer" sx={{ py: 1.5, px: 2.5 }}>
            <ListItemIcon sx={{ minWidth: 36, color: alpha('#ffffff', 0.85) }}>
              <OpenInNewOutlinedIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText slotProps={{ primary: { sx: { fontSize: 13, color: alpha('#ffffff', 0.85) } } }}>Hangfire Paneli</ListItemText>
          </ListItemButton>
        </>
      )}
    </Box>
  )
}

function NavGroupLabel({ text }: { text: string }) {
  return (
    <Typography
      variant="overline"
      sx={{ px: 3, color: alpha('#ffffff', 0.5), fontSize: 11, letterSpacing: 1 }}
    >
      {text}
    </Typography>
  )
}
