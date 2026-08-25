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
import HowToRegOutlinedIcon from '@mui/icons-material/HowToRegOutlined'
import PlaylistAddCheckOutlinedIcon from '@mui/icons-material/PlaylistAddCheckOutlined'
import BarChartOutlinedIcon from '@mui/icons-material/BarChartOutlined'
import AdminPanelSettingsOutlinedIcon from '@mui/icons-material/AdminPanelSettingsOutlined'
import ManageAccountsOutlinedIcon from '@mui/icons-material/ManageAccountsOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import type { ComponentType } from 'react'
import type { SvgIconProps } from '@mui/material'
import { useAuth } from '../../auth/AuthContext'
import { Permissions } from '../../auth/permissions'
import { getSession } from '../../auth/tokenStore'
import logo from '../../assets/logo.png'

interface NavItem {
  label: string
  to: string
  icon: ComponentType<SvgIconProps>
  permission?: string
  external?: boolean
}

interface NavGroup {
  /** null = başlıksız (yalnızca Panel). */
  label: string | null
  items: NavItem[]
}

// docs/PLAN-V4.md §23.1 (A-52): 13 öğe tek "Genel" grubunda 900px'e sığmıyordu ve grup üç ayrı
// kavramı (keşif / kişisel / inceleme) karıştırıyordu. Beş gruba bölündü, her biri 3-4 öğe.
const navGroups: NavGroup[] = [
  {
    label: null,
    items: [{ label: 'Panel', to: '/panel', icon: SpaceDashboardOutlinedIcon }],
  },
  {
    label: 'Keşfet',
    items: [
      { label: 'Kulüpler', to: '/clubs', icon: GroupsOutlinedIcon },
      { label: 'Etkinlikler', to: '/events', icon: EventOutlinedIcon, permission: Permissions.EventsRead },
      { label: 'Duyurular', to: '/announcements', icon: CampaignOutlinedIcon, permission: Permissions.ClubsRead },
    ],
  },
  {
    label: 'Benim',
    items: [
      { label: 'Kulüplerim', to: '/my-clubs', icon: Groups2OutlinedIcon },
      { label: 'Etkinliklerim', to: '/my-events', icon: EventAvailableOutlinedIcon, permission: Permissions.EventsRead },
      { label: 'Başvurularım', to: '/my-applications', icon: FactCheckOutlinedIcon },
    ],
  },
  {
    label: 'İnceleme',
    items: [
      { label: 'Başvuru İncele', to: '/review', icon: HowToRegOutlinedIcon, permission: Permissions.MembershipsWrite },
      { label: 'Topluluk Kurma', to: '/club-applications', icon: PlaylistAddCheckOutlinedIcon, permission: Permissions.ClubsWrite },
      { label: 'Raporlarım', to: '/reports', icon: BarChartOutlinedIcon, permission: Permissions.ReportsRead },
    ],
  },
  {
    label: 'Yönetim',
    items: [
      { label: 'Roller ve İzinler', to: '/authorization/roles', icon: AdminPanelSettingsOutlinedIcon, permission: Permissions.RolesManage },
      { label: 'Kullanıcılar', to: '/authorization/users', icon: ManageAccountsOutlinedIcon, permission: Permissions.RolesManage },
      { label: 'Referans Verisi', to: '/reference', icon: CategoryOutlinedIcon, permission: Permissions.ReferenceManage },
      { label: 'Denetim İzi', to: '/audit', icon: HistoryOutlinedIcon, permission: Permissions.AuditRead },
    ],
  },
]

export const SIDENAV_WIDTH = 248

export function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const { hasPermission } = useAuth()
  const location = useLocation()

  const hangfireUrl = `${import.meta.env.VITE_BACKEND_URL}/hangfire?access_token=${getSession().accessToken ?? ''}`

  const visibleGroups = navGroups
    .map((group) => ({ ...group, items: group.items.filter((item) => !item.permission || hasPermission(item.permission)) }))
    .filter((group) => group.items.length > 0)

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
      <Toolbar sx={{ px: 2.5, gap: 1.25 }}>
        <Box component="img" src={logo} alt="" sx={{ width: 32, height: 32 }} />
        <Typography variant="subtitle1" noWrap sx={{ fontWeight: 800, color: 'common.white' }}>
          Öğrenci Toplulukları
        </Typography>
      </Toolbar>

      <Box sx={{ flex: 1, overflowY: 'auto', py: 1 }}>
        {/* Bir grubun tüm öğeleri izin filtresine takılırsa başlığı da çizilmez. */}
        {visibleGroups.map((group) => (
          <List
            key={group.label ?? 'root'}
            dense
            disablePadding
            subheader={group.label ? <NavGroupLabel text={group.label} /> : undefined}
            sx={{ mb: 1 }}
          >
            {renderItems(group.items)}
          </List>
        ))}
      </Box>

      {hasPermission(Permissions.HangfireDashboard) && (
        <>
          <Divider sx={{ borderColor: alpha('#ffffff', 0.12) }} />
          {/*
            §23.1'in asıl sorunu: MuiListItemButton `flex-grow: 1` taşır (satır içinde kullanılmak
            üzere tasarlandığı için). Sütun yönlü flex kabında doğrudan çocuk olunca DİKEY büyüyüp
            435px kaplıyor ve menünün kaydırma alanını 400px'e sıkıştırıyordu — son iki grup bu
            yüzden kırpılıyordu. "14 öğe sığmıyor" teşhisi yanlıştı.
          */}
          <ListItemButton
            component="a"
            href={hangfireUrl}
            target="_blank"
            rel="noreferrer"
            sx={{ py: 1.5, px: 2.5, flex: '0 0 auto' }}
          >
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
    // §23.1: 13 öğe + 4 başlık 900px'e ancak sığıyor — başlık satırı sıkı tutulur, ayırıcı yok.
    <Typography
      component="div"
      variant="overline"
      sx={{ px: 3, pt: 1, display: 'block', lineHeight: 1.6, color: alpha('#ffffff', 0.5), fontSize: 11, letterSpacing: 1 }}
    >
      {text}
    </Typography>
  )
}
