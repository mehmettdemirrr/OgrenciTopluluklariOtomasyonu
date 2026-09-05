import { IconButton, Stack } from '@mui/material'
import InstagramIcon from '@mui/icons-material/Instagram'
import LanguageIcon from '@mui/icons-material/Language'
import LinkedInIcon from '@mui/icons-material/LinkedIn'
import XIcon from '@mui/icons-material/X'
import YouTubeIcon from '@mui/icons-material/YouTube'
import type { ClubSocialLinkDto, SocialPlatform } from '../../api/types'

// docs/MIMARI.md · K-44/A-74: kulüp düzenleme formu ve vitrin sayfası aynı platform kümesini/ikonu
// kullanır — burada tek yerde durur, iki kopya birbirinden ayrışmasın.
export const SOCIAL_PLATFORMS: SocialPlatform[] = ['Instagram', 'X', 'LinkedIn', 'YouTube', 'Website']

export const SOCIAL_PLATFORM_ICONS: Record<SocialPlatform, typeof InstagramIcon> = {
  Instagram: InstagramIcon,
  X: XIcon,
  LinkedIn: LinkedInIcon,
  YouTube: YouTubeIcon,
  Website: LanguageIcon,
}

export const SOCIAL_PLATFORM_LABELS: Record<SocialPlatform, string> = {
  Instagram: 'Instagram',
  X: 'X (Twitter)',
  LinkedIn: 'LinkedIn',
  YouTube: 'YouTube',
  Website: 'Web Sitesi',
}

/** Y-80: bağlantılar sunucuda doğrulanmış https adresleridir; burada yalnızca gösterilir. */
export function SocialLinkIcons({ links }: { links: ClubSocialLinkDto[] }) {
  if (links.length === 0) {
    return null
  }

  return (
    <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
      {links.map((link) => {
        const Icon = SOCIAL_PLATFORM_ICONS[link.platform]
        return (
          <IconButton
            key={`${link.platform}-${link.url}`}
            component="a"
            href={link.url}
            target="_blank"
            rel="noopener noreferrer"
            size="small"
            title={SOCIAL_PLATFORM_LABELS[link.platform]}
          >
            <Icon fontSize="small" />
          </IconButton>
        )
      })}
    </Stack>
  )
}
