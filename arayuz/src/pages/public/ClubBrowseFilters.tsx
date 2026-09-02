import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { Box, Button, InputAdornment, MenuItem, Paper, Stack, TextField, Typography, alpha } from '@mui/material'
import type { SxProps, Theme } from '@mui/material/styles'
import type { FormEvent } from 'react'
import type { PublicClubCategoryDto } from '../../api/types'
import { useLocale } from '../../i18n/LocaleContext'

/** Türk alfabesi — Q/W/X yok. Harf filtresi sunucuya `letter=` olarak gider (Y-62). */
export const TURKISH_LETTERS = ['A', 'B', 'C', 'Ç', 'D', 'E', 'F', 'G', 'Ğ', 'H', 'I', 'İ', 'J', 'K', 'L', 'M', 'N', 'O', 'Ö', 'P', 'R', 'S', 'Ş', 'T', 'U', 'Ü', 'V', 'Y', 'Z'] as const

const fieldSx: SxProps<Theme> = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2.5,
    bgcolor: 'action.hover',
    '& fieldset': { borderColor: 'transparent' },
    '&:hover fieldset': { borderColor: 'transparent' },
    '&.Mui-focused fieldset': { borderColor: 'secondary.main' },
  },
}

interface ClubBrowseFiltersProps {
  categories: PublicClubCategoryDto[]
  categoryId: number | null
  onCategoryChange: (id: number | null) => void
  searchInput: string
  onSearchInputChange: (value: string) => void
  onSearchSubmit: () => void
  letter: string | null
  onLetterChange: (letter: string | null) => void
}

export function ClubBrowseFilters({
  categories,
  categoryId,
  onCategoryChange,
  searchInput,
  onSearchInputChange,
  onSearchSubmit,
  letter,
  onLetterChange,
}: ClubBrowseFiltersProps) {
  const { t } = useLocale()

  const submit = (event: FormEvent) => {
    event.preventDefault()
    onSearchSubmit()
  }

  return (
    <Paper
      elevation={0}
      sx={{
        mb: 3.5,
        p: { xs: 2, md: 3 },
        borderRadius: 4,
        border: '1px solid',
        borderColor: 'divider',
        boxShadow: (theme) => `0 16px 40px ${alpha(theme.palette.secondary.main, 0.08)}`,
      }}
    >
      <Box
        component="form"
        onSubmit={submit}
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'minmax(220px, 0.9fr) minmax(280px, 1.4fr)' },
          mb: 2.5,
        }}
      >
        <Stack spacing={0.75}>
          <Typography variant="overline" sx={{ color: 'text.secondary', letterSpacing: 1.2, fontWeight: 700, lineHeight: 1.2 }}>
            {t('public.filterByCategory')}
          </Typography>
          <TextField
            select
            hiddenLabel
            size="medium"
            fullWidth
            value={categoryId ?? 0}
            onChange={(event) => onCategoryChange(Number(event.target.value) || null)}
            sx={fieldSx}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <CategoryOutlinedIcon fontSize="small" color="action" />
                  </InputAdornment>
                ),
              },
            }}
          >
            <MenuItem value={0}>{t('public.allCategories')}</MenuItem>
            {categories.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </TextField>
        </Stack>

        <Stack spacing={0.75}>
          <Typography variant="overline" sx={{ color: 'text.secondary', letterSpacing: 1.2, fontWeight: 700, lineHeight: 1.2 }}>
            {t('public.searchByName')}
          </Typography>
          <TextField
            hiddenLabel
            size="medium"
            fullWidth
            value={searchInput}
            onChange={(event) => onSearchInputChange(event.target.value)}
            placeholder={t('public.searchPlaceholder')}
            sx={fieldSx}
            slotProps={{
              input: {
                startAdornment: (
                  <InputAdornment position="start">
                    <SearchOutlinedIcon fontSize="small" color="action" />
                  </InputAdornment>
                ),
                endAdornment: (
                  <InputAdornment position="end">
                    <Button type="submit" variant="contained" size="medium" sx={{ borderRadius: 999, px: 2.5, fontWeight: 800 }}>
                      {t('public.query')}
                    </Button>
                  </InputAdornment>
                ),
              },
            }}
          />
        </Stack>
      </Box>

      <Box
        sx={{
          display: 'flex',
          gap: 1,
          overflowX: 'auto',
          pb: 1.25,
          '&::-webkit-scrollbar': { height: 8 },
          '&::-webkit-scrollbar-thumb': {
            bgcolor: 'grey.400',
            borderRadius: 4,
          },
          '&::-webkit-scrollbar-track': {
            bgcolor: (theme) => alpha(theme.palette.secondary.main, 0.06),
            borderRadius: 4,
          },
        }}
      >
        <LetterButton selected={letter === null} onClick={() => onLetterChange(null)} wide>
          {t('public.allLetters')}
        </LetterButton>
        {TURKISH_LETTERS.map((item) => (
          <LetterButton key={item} selected={letter === item} onClick={() => onLetterChange(letter === item ? null : item)}>
            {item}
          </LetterButton>
        ))}
      </Box>
    </Paper>
  )
}

function LetterButton({
  selected,
  onClick,
  children,
  wide = false,
}: {
  selected: boolean
  onClick: () => void
  children: string
  wide?: boolean
}) {
  return (
    <Button
      type="button"
      onClick={onClick}
      sx={{
        minWidth: wide ? 64 : 40,
        width: wide ? 'auto' : 40,
        height: 40,
        px: wide ? 1.5 : 0,
        flexShrink: 0,
        borderRadius: 1.5,
        fontWeight: 800,
        color: selected ? 'common.white' : 'text.primary',
        bgcolor: selected ? 'secondary.main' : (theme) => alpha(theme.palette.secondary.main, 0.06),
        '&:hover': {
          bgcolor: selected ? 'secondary.dark' : (theme) => alpha(theme.palette.secondary.main, 0.12),
        },
      }}
    >
      {children}
    </Button>
  )
}
