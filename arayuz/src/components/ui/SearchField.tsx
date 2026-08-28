import { InputAdornment, TextField } from '@mui/material'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'

interface SearchFieldProps {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  /** Sunucu yeni sonucu getirirken kutunun altında ince bir uyarı yerine sessiz kalınır — sayfa zıplamaz. */
  disabled?: boolean
}

/** docs/MIMARI.md · A-50: arama kutusu; metin debounce'lanıp sunucuya `search=` olarak gider (Y-62). */
export function SearchField({ value, onChange, placeholder = 'Ara…', disabled }: SearchFieldProps) {
  return (
    <TextField
      placeholder={placeholder}
      value={value}
      disabled={disabled}
      onChange={(event) => onChange(event.target.value)}
      sx={{
        minWidth: { xs: '100%', sm: 280 },
        maxWidth: 440,
        '& .MuiOutlinedInput-root': {
          borderRadius: 999,
          bgcolor: 'background.paper',
        },
      }}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchOutlinedIcon fontSize="small" color="action" />
            </InputAdornment>
          ),
        },
      }}
    />
  )
}
