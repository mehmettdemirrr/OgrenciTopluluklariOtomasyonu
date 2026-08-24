import { Card, CardContent, Grid, Skeleton, Stack } from '@mui/material'

interface CardGridSkeletonProps {
  count?: number
  /** Görsel alanı olan galeriler (kulüp logosu, etkinlik afişi) için üstte blok gösterilir. */
  withMedia?: boolean
}

/**
 * docs/MIMARI.md · A-52: veri çeken her görünüm yüklenirken iskelet gösterir.
 * Boş ekran + ardından içeriğin aniden belirmesi (layout shift) yerine yerleşim baştan oturur.
 */
export function CardGridSkeleton({ count = 6, withMedia = false }: CardGridSkeletonProps) {
  return (
    <Grid container spacing={2}>
      {Array.from({ length: count }, (_, index) => (
        <Grid key={index} size={{ xs: 12, sm: 6, md: 4 }}>
          <Card variant="outlined" sx={{ height: '100%' }}>
            {withMedia && <Skeleton variant="rectangular" height={120} />}
            <CardContent>
              <Stack spacing={1}>
                <Skeleton variant="text" width="70%" height={28} />
                <Skeleton variant="text" width="45%" />
                <Skeleton variant="text" width="90%" />
                <Skeleton variant="text" width="60%" />
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  )
}
