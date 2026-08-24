import { Autocomplete, TextField } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'

interface RemoteSelectProps<T> {
  label: string
  value: number | null
  onChange: (value: number | null) => void
  /** Sunucuya `search` ile giden yükleyici; ilk 20 sonuç yeterlidir çünkü kullanıcı yazarak daraltır. */
  fetchOptions: (search: string) => Promise<T[]>
  queryKey: readonly unknown[]
  getOptionLabel: (option: T) => string
  getOptionId: (option: T) => number
  enabled?: boolean
  error?: boolean
  helperText?: string
  placeholder?: string
  size?: 'small' | 'medium'
  fullWidth?: boolean
}

/**
 * docs/MIMARI.md · A-50/Y-62: açılır listelerin sunucu aramalı hâli.
 *
 * Önceki kalıp (`pageSize: 200` çekip hepsini `MenuItem` yapmak) iki kere yanlıştı: sunucu
 * zaten 100'e kırpıyordu (Y-11) ve 101. danışman **hiçbir şekilde seçilemiyordu** — üstelik
 * kullanıcıya bunun olduğu söylenmiyordu. Burada liste sunucudan daraltılarak gelir.
 */
export function RemoteSelect<T>({
  label,
  value,
  onChange,
  fetchOptions,
  queryKey,
  getOptionLabel,
  getOptionId,
  enabled = true,
  error,
  helperText,
  placeholder,
  size,
  fullWidth = true,
}: RemoteSelectProps<T>) {
  const [inputValue, setInputValue] = useState('')
  const [selected, setSelected] = useState<T | null>(null)
  const debouncedSearch = useDebouncedValue(inputValue)

  const optionsQuery = useQuery({
    queryKey: [...queryKey, debouncedSearch],
    enabled,
    queryFn: () => fetchOptions(debouncedSearch),
    placeholderData: (previousData) => previousData,
  })

  const options = optionsQuery.data ?? []

  // Seçili kayıt, arama daraldığında listeden düşebilir; seçimi listeye değil kendi state'ine bağlıyoruz.
  const currentValue = selected !== null && value !== null && getOptionId(selected) === value ? selected : null

  return (
    <Autocomplete
      options={options}
      value={currentValue}
      size={size}
      fullWidth={fullWidth}
      loading={optionsQuery.isFetching}
      filterOptions={(option) => option} // Filtre sunucuda — istemci ikinci kez elemesin (Y-62).
      getOptionLabel={getOptionLabel}
      isOptionEqualToValue={(option, selectedOption) => getOptionId(option) === getOptionId(selectedOption)}
      onInputChange={(_, newInput) => setInputValue(newInput)}
      onChange={(_, option) => {
        setSelected(option)
        onChange(option === null ? null : getOptionId(option))
      }}
      noOptionsText={debouncedSearch ? 'Sonuç bulunamadı' : 'Aramak için yazın'}
      loadingText="Aranıyor…"
      renderInput={(params) => (
        <TextField {...params} label={label} margin="dense" placeholder={placeholder} error={error} helperText={helperText} />
      )}
    />
  )
}
