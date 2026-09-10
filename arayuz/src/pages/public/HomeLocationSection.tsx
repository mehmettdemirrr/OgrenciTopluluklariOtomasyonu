import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import InstagramIcon from '@mui/icons-material/Instagram'
import LinkedInIcon from '@mui/icons-material/LinkedIn'
import PlaceOutlinedIcon from '@mui/icons-material/PlaceOutlined'
import { Box, Grid, IconButton, Link, Stack, Typography } from '@mui/material'
import { useState, type ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import { campuses, type CampusId } from '../../data/campuses'
import { officeContact } from '../../data/officeContact'
import { useLocale } from '../../i18n/LocaleContext'
import { mapsEmbedUrl } from '../../utils/maps'

export function HomeLocationSection() {
  const { t } = useLocale()
  const { isAuthenticated } = useAuth()
  const [campusId, setCampusId] = useState<CampusId>('yesilyurt')
  const selectedCampus = campuses.find((campus) => campus.id === campusId) ?? campuses[0]
  const applyTo = isAuthenticated ? '/club-applications/new' : '/register'

  return (
    <Box id="kampusler" sx={{ scrollMarginTop: 96, py: { xs: 1, md: 2 } }}>
      <Grid container spacing={{ xs: 4, md: 6 }}>
        <Grid size={{ xs: 12, md: 6 }}>
          <Stack direction="row" spacing={2} sx={{ mb: 1.5, flexWrap: 'wrap' }}>
            {campuses.map((campus) => {
              const selected = campus.id === campusId
              return (
                <Typography
                  key={campus.id}
                  component="button"
                  type="button"
                  onClick={() => setCampusId(campus.id)}
                  sx={{
                    border: 0,
                    p: 0,
                    bgcolor: 'transparent',
                    cursor: 'pointer',
                    font: 'inherit',
                    fontWeight: 800,
                    color: selected ? 'secondary.main' : 'text.disabled',
                    '&:hover': { color: 'secondary.main' },
                  }}
                >
                  {t(campus.nameKey)}
                </Typography>
              )
            })}
          </Stack>
          <Box
            sx={{
              borderRadius: 3,
              overflow: 'hidden',
              border: '1px solid',
              borderColor: 'divider',
              height: { xs: 180, sm: 200 },
            }}
          >
            <Box
              component="iframe"
              title={t(selectedCampus.nameKey)}
              src={mapsEmbedUrl(selectedCampus.query)}
              loading="lazy"
              referrerPolicy="no-referrer-when-downgrade"
              sx={{ display: 'block', width: '100%', height: '100%', border: 0 }}
            />
          </Box>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 800, color: 'secondary.main', mb: 1.5 }}>
            {t('home.quickLinks')}
          </Typography>
          <Stack spacing={1.25}>
            <QuickLink to="/" label={t('home.quickHome')} />
            <QuickLink to="/kulupler" label={t('home.quickClubs')} />
            <QuickLink to="/etkinlikler" label={t('nav.events')} />
            <QuickLink to="/duyurular" label={t('nav.announcements')} />
            <QuickLink to={applyTo} label={t('home.quickApply')} />
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 800, color: 'secondary.main', mb: 1.5 }}>
            {t('home.contact')}
          </Typography>
          <Stack spacing={1.25}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <PlaceOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
              <Typography variant="body2" color="text.secondary">
                {t('home.contactCity')}
              </Typography>
            </Stack>
            <Stack
              direction="row"
              spacing={1}
              component="a"
              href={`mailto:${officeContact.email}`}
              sx={{ alignItems: 'center', color: 'text.secondary', textDecoration: 'none', '&:hover': { color: 'primary.main' } }}
            >
              <EmailOutlinedIcon sx={{ color: 'primary.main', fontSize: 20 }} />
              <Typography variant="body2">{officeContact.email}</Typography>
            </Stack>
            <Stack direction="row" spacing={1} sx={{ pt: 0.5 }}>
              <SocialCircle href={officeContact.instagramUrl} label="Instagram">
                <InstagramIcon fontSize="small" />
              </SocialCircle>
              <SocialCircle href={officeContact.linkedinUrl} label="LinkedIn">
                <LinkedInIcon fontSize="small" />
              </SocialCircle>
            </Stack>
          </Stack>
        </Grid>
      </Grid>
    </Box>
  )
}

function QuickLink({ to, label }: { to: string; label: string }) {
  return (
    <Link
      component={RouterLink}
      to={to}
      underline="none"
      color="text.secondary"
      sx={{ fontSize: 14, '&:hover': { color: 'primary.main' } }}
    >
      {label}
    </Link>
  )
}

function SocialCircle({ href, label, children }: { href: string; label: string; children: ReactNode }) {
  return (
    <IconButton
      component="a"
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      sx={{
        width: 40,
        height: 40,
        bgcolor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
        color: 'primary.main',
        '&:hover': { borderColor: 'primary.main', bgcolor: 'background.paper' },
      }}
    >
      {children}
    </IconButton>
  )
}
