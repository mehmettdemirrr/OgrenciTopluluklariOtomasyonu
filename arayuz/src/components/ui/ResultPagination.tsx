import { Pagination, Stack, Typography } from '@mui/material'
import { useLocale } from '../../i18n/LocaleContext'

interface ResultPaginationProps {
  pageIndex: number
  pageCount: number
  totalCount: number
  onChange: (pageIndex: number) => void
}

/**
 * docs/MIMARI.md · Y-62: kart galerilerinin sayfalama kontrolü. Toplam sayı **sunucudan gelen**
 * `totalCount`'tur — "gördüğün kadarı var" yanılsaması bilinçli olarak kaldırılmıştır.
 */
export function ResultPagination({ pageIndex, pageCount, totalCount, onChange }: ResultPaginationProps) {
  const { t } = useLocale()
  if (totalCount === 0) {
    return null
  }

  return (
    <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', mt: 3.5, flexWrap: 'wrap', gap: 1 }}>
      <Typography variant="body2" color="text.secondary">
        {t('common.totalRecords', { count: totalCount })}
      </Typography>
      {pageCount > 1 && (
        <Pagination
          count={pageCount}
          page={pageIndex + 1}
          onChange={(_, page) => onChange(page - 1)}
          color="primary"
          shape="rounded"
        />
      )}
    </Stack>
  )
}
