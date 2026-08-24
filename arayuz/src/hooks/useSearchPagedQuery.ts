import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import { useState } from 'react'
import type { PagedResult } from '../api/types'
import { useDebouncedValue } from './useDebouncedValue'

interface UseSearchPagedQueryOptions<T> {
  queryKey: readonly unknown[]
  queryFn: (params: { pageIndex: number; pageSize: number; search: string }) => Promise<PagedResult<T>>
  pageSize?: number
  enabled?: boolean
}

interface UseSearchPagedQueryResult<T> {
  /** Kutuya yazılan ham metin — TextField.value buna bağlanır. */
  search: string
  setSearch: (value: string) => void
  /** 0 tabanlı sayfa indeksi. */
  pageIndex: number
  setPageIndex: (value: number) => void
  /** MUI Pagination 1 tabanlı çalışır. */
  pageCount: number
  totalCount: number
  items: T[]
  query: UseQueryResult<PagedResult<T>>
}

/**
 * docs/MIMARI.md · A-50/Y-62: kart galerileri için sunucu araması + gerçek sayfalama.
 * `usePagedQuery` DataGrid'in `paginationModel`'ine bağlı olduğundan galeriler için ayrı hook.
 *
 * Aramanın değişmesi sayfayı 0'a döndürür — aksi hâlde 5. sayfadayken arama yapan kullanıcı
 * 3 sonuçlu bir kümenin 5. sayfasını, yani boş ekranı görür.
 */
export function useSearchPagedQuery<T>({
  queryKey,
  queryFn,
  pageSize = 12,
  enabled = true,
}: UseSearchPagedQueryOptions<T>): UseSearchPagedQueryResult<T> {
  const [search, setSearchValue] = useState('')
  const [pageIndex, setPageIndex] = useState(0)
  const debouncedSearch = useDebouncedValue(search)

  // Sıfırlama efektle değil, sebebi olan olayın içinde yapılır — efekt fazladan bir render turu açardı.
  const setSearch = (value: string) => {
    setSearchValue(value)
    setPageIndex(0)
  }

  const query = useQuery({
    queryKey: [...queryKey, debouncedSearch, pageIndex, pageSize],
    queryFn: () => queryFn({ pageIndex, pageSize, search: debouncedSearch }),
    enabled,
    // Sayfa/arama değişirken liste boşalıp zıplamasın.
    placeholderData: (previousData) => previousData,
  })

  const totalCount = query.data?.totalCount ?? 0

  return {
    search,
    setSearch,
    pageIndex,
    setPageIndex,
    pageCount: Math.max(1, Math.ceil(totalCount / pageSize)),
    totalCount,
    items: query.data?.items ?? [],
    query,
  }
}
