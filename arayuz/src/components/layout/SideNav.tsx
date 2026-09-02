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
import { useLocale } from '../../i18n/LocaleContext'
import { BrandMark } from './BrandMark'

interface NavItem {
  labelKey: string
  to: string
  icon: ComponentType<SvgIconProps>
  permission?: string
  external?: boolean
}

interface NavGroup {
  /** null = başlıksız (yalnızca Panel). */
  labelKey: string | null
  items: NavItem[]
}

// docs/PLAN-V4.md §23.1 (A-52): 13 öğe tek "Genel" grubunda 900px'e sığmıyordu ve grup üç ayrı
// kavramı (keşif / kişisel / inceleme) karıştırıyordu. Beş gruba bölündü, her biri 3-4 öğe.
const navGroups: NavGroup[] = [
  {
    labelKey: null,
    items: [{ labelKey: 'nav.panel', to: '/panel', icon: SpaceDashboardOutlinedIcon }],
  },
  {
    labelKey: 'nav.explore',
    items: [
      { labelKey: 'nav.clubs', to: '/clubs', icon: GroupsOutlinedIcon },
      { labelKey: 'nav.events', to: '/events', icon: EventOutlinedIcon, permission: Permissions.EventsRead },
      { labelKey: 'nav.announcements', to: '/announcements', icon: CampaignOutlinedIcon, permission: Permissions.ClubsRead },
    ],
  },
  {
    labelKey: 'nav.mine',
    items: [
      { labelKey: 'nav.myClubs', to: '/my-clubs', icon: Groups2OutlinedIcon },
      { labelKey: 'nav.myEvents', to: '/my-events', icon: EventAvailableOutlinedIcon, permission: Permissions.EventsRead },
      { labelKey: 'nav.myApplications', to: '/my-applications', icon: FactCheckOutlinedIcon },
    ],
  },
  {
    labelKey: 'nav.review',
    items: [
      { labelKey: 'nav.reviewApps', to: '/review', icon: HowToRegOutlinedIcon, permission: Permissions.MembershipsWrite },
      { labelKey: 'nav.clubFounding', to: '/club-applications', icon: PlaylistAddCheckOutlinedIcon, permission: Permissions.ClubsWrite },
      { labelKey: 'nav.reports', to: '/reports', icon: BarChartOutlinedIcon, permission: Permissions.ReportsRead },
    ],
  },
  {
    labelKey: 'nav.admin',
    items: [
      { labelKey: 'nav.roles', to: '/authorization/roles', icon: AdminPanelSettingsOutlinedIcon, permission: Permissions.RolesManage },
      { labelKey: 'nav.users', to: '/authorization/users', icon: ManageAccountsOutlinedIcon, permission: Permissions.RolesManage },
      { labelKey: 'nav.reference', to: '/reference', icon: CategoryOutlinedIcon, permission: Permissions.ReferenceManage },
      { labelKey: 'nav.audit', to: '/audit', icon: HistoryOutlinedIcon, permission: Permissions.AuditRead },
    ],
  },
]

export const SIDENAV_WIDTH = 248

export function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const { hasPermission } = useAuth()
  const { t } = useLocale()
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
              position: 'relative',
              borderRadius: 2,
              mx: 1,
              mb: 0.5,
              color: active ? 'primary.main' : (theme) => alpha(theme.palette.common.white, 0.82),
              '&.Mui-selected': {
                bgcolor: (theme) => alpha(theme.palette.common.white, 0.1),
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  left: 0,
                  top: 8,
                  bottom: 8,
                  width: 3,
                  borderRadius: 8,
                  bgcolor: 'primary.main',
                },
              },
              '&:hover': {
                bgcolor: (theme) => alpha(theme.palette.common.white, 0.08),
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 36, color: 'inherit' }}>
              <item.icon fontSize="small" />
            </ListItemIcon>
            <ListItemText slotProps={{ primary: { sx: { fontSize: 14, fontWeight: active ? 700 : 500 } } }}>{t(item.labelKey)}</ListItemText>
          </ListItemButton>
        )
      })

  return (
    <Box
      sx={{
        height: '100%',
        color: 'common.white',
        display: 'flex',
        flexDirection: 'column',
        background: (theme) =>
          `linear-gradient(180deg, ${theme.palette.secondary.main} 0%, ${theme.palette.primary.dark} 140%)`,
      }}
    >
      <Toolbar sx={{ px: 2, gap: 1.25, minHeight: 72 }}>
        <BrandMark light to="/panel" />
      </Toolbar>

      <Box sx={{ flex: 1, overflowY: 'auto', py: 1 }}>
        {/* Bir grubun tüm öğeleri izin filtresine takılırsa başlığı da çizilmez. */}
        {visibleGroups.map((group) => (
          <List
            key={group.labelKey ?? 'root'}
            dense
            disablePadding
            subheader={group.labelKey ? <NavGroupLabel text={t(group.labelKey)} /> : undefined}
            sx={{ mb: 1 }}
          >
            {renderItems(group.items)}
          </List>
        ))}
      </Box>

      {hasPermission(Permissions.HangfireDashboard) && (
        <>
          <Divider sx={{ borderColor: (theme) => alpha(theme.palette.common.white, 0.12) }} />
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
            <ListItemIcon sx={{ minWidth: 36, color: (theme) => alpha(theme.palette.common.white, 0.85) }}>
              <OpenInNewOutlinedIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText slotProps={{ primary: { sx: { fontSize: 13, color: (theme) => alpha(theme.palette.common.white, 0.85) } } }}>{t('nav.hangfire')}</ListItemText>
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
      sx={{ px: 3, pt: 1, display: 'block', lineHeight: 1.6, color: (theme) => alpha(theme.palette.common.white, 0.48), fontSize: 11, letterSpacing: 1 }}
    >
      {text}
    </Typography>
  )
}
