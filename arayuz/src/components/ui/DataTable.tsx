import { Box } from '@mui/material'
import { DataGrid, type DataGridProps, type GridValidRowModel } from '@mui/x-data-grid'
import { trTR } from '@mui/x-data-grid/locales'
import { EmptyState } from './EmptyState'

interface DataTableProps<R extends GridValidRowModel> extends DataGridProps<R> {
  emptyTitle?: string
  emptyDescription?: string
  height?: number | string
}

export function DataTable<R extends GridValidRowModel>({
  emptyTitle = 'Kayıt bulunamadı',
  emptyDescription,
  height = 480,
  sx,
  ...gridProps
}: DataTableProps<R>) {
  return (
    <Box sx={{ height, width: '100%' }}>
      <DataGrid
        localeText={trTR.components.MuiDataGrid.defaultProps.localeText}
        disableRowSelectionOnClick
        slots={{
          noRowsOverlay: () => <EmptyState title={emptyTitle} description={emptyDescription} />,
        }}
        sx={{ border: 'none', ...sx }}
        {...gridProps}
      />
    </Box>
  )
}
