import { useQuery, type UseQueryResult, type QueryObserverOptions } from '@tanstack/react-query'
import { useState } from 'react'
import type { GridPaginationModel } from '@mui/x-data-grid'
import type { PagedResult } from '../api/types'

const DEFAULT_PAGE_SIZE = 10

interface UsePagedQueryOptions<T> {
  queryKey: readonly unknown[]
  queryFn: (pageIndex: number, pageSize: number) => Promise<PagedResult<T>>
  enabled?: boolean
  pageSize?: number
  refetchInterval?: QueryObserverOptions<PagedResult<T>>['refetchInterval']
}

interface UsePagedQueryResult<T> {
  paginationModel: GridPaginationModel
  setPaginationModel: (model: GridPaginationModel) => void
  query: UseQueryResult<PagedResult<T>>
}

// 6 sayfada tekrarlanan "useState<GridPaginationModel> + server-mode useQuery" kalıbı — bkz. docs/PLAN-V2.md §8.3.
export function usePagedQuery<T>({
  queryKey,
  queryFn,
  enabled = true,
  pageSize = DEFAULT_PAGE_SIZE,
  refetchInterval,
}: UsePagedQueryOptions<T>): UsePagedQueryResult<T> {
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({ page: 0, pageSize })

  const query = useQuery({
    queryKey: [...queryKey, paginationModel.page, paginationModel.pageSize],
    queryFn: () => queryFn(paginationModel.page, paginationModel.pageSize),
    enabled,
    placeholderData: (previousData) => previousData,
    refetchInterval,
  })

  return { paginationModel, setPaginationModel, query }
}
