import { Box, useMediaQuery, useTheme } from '@mui/material'
import { DataGrid, type DataGridProps, type GridValidRowModel } from '@mui/x-data-grid'
import { trTR } from '@mui/x-data-grid/locales'
import { useMemo } from 'react'
import { EmptyState } from './EmptyState'

interface DataTableProps<R extends GridValidRowModel> extends DataGridProps<R> {
  emptyTitle?: string
  emptyDescription?: string
  height?: number | string
  /**
   * docs/PLAN-V4.md §23.2 (A-52): dar ekranda (sm altı) gizlenecek ikincil kolonlar.
   * Tablolar 375px'te yatay taşıyordu; gizlenen bilgi zaten satırın detay sayfasında var.
   */
  mobileHiddenFields?: string[]
}

export function DataTable<R extends GridValidRowModel>({
  emptyTitle = 'Kayıt bulunamadı',
  emptyDescription,
  height = 480,
  mobileHiddenFields,
  sx,
  ...gridProps
}: DataTableProps<R>) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))

  const columnVisibilityModel = useMemo(() => {
    const base = gridProps.columnVisibilityModel ?? {}
    if (!isMobile || !mobileHiddenFields?.length) {
      return base
    }

    return { ...base, ...Object.fromEntries(mobileHiddenFields.map((field) => [field, false])) }
  }, [gridProps.columnVisibilityModel, isMobile, mobileHiddenFields])

  return (
    <Box sx={{ height, width: '100%' }}>
      <DataGrid
        localeText={trTR.components.MuiDataGrid.defaultProps.localeText}
        disableRowSelectionOnClick
        slots={{
          noRowsOverlay: () => <EmptyState title={emptyTitle} description={emptyDescription} />,
        }}
        sx={{
          border: 'none',
          '& .MuiDataGrid-columnHeader': {
            bgcolor: 'grey.50',
            fontWeight: 700,
          },
          '& .MuiDataGrid-row:hover': {
            bgcolor: (theme) => theme.palette.action.hover,
          },
          '& .MuiDataGrid-cell:focus, & .MuiDataGrid-columnHeader:focus': {
            outline: 'none',
          },
          ...sx,
        }}
        {...gridProps}
        columnVisibilityModel={columnVisibilityModel}
      />
    </Box>
  )
}
