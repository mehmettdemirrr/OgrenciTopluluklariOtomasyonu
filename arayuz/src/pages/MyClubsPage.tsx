import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Card, CardActions, CardContent, Chip, Grid, Stack, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { CardGridSkeleton } from '../components/ui/CardGridSkeleton'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { ClubRoleChip } from '../components/ui/StatusChip'
import type { MyClubMembershipDto } from '../api/types'
import { useDocumentTitle } from '../hooks/useDocumentTitle'

export function MyClubsPage() {
  useDocumentTitle('Kulüplerim')

  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [leaveTarget, setLeaveTarget] = useState<MyClubMembershipDto | null>(null)

  const myClubsQuery = useQuery({
    queryKey: ['clubs-mine'],
    queryFn: async () => (await apiClient.get<MyClubMembershipDto[]>('/clubs/mine')).data,
  })

  const leaveMutation = useMutation({
    mutationFn: async (clubId: number) => {
      await apiClient.delete(`/clubs/${clubId}/membership`)
    },
    onSuccess: () => {
      notify({ message: 'Topluluktan ayrıldınız.', severity: 'success' })
      setLeaveTarget(null)
      queryClient.invalidateQueries({ queryKey: ['clubs-mine'] })
    },
    onError: (error) => {
      // Son başkan ayrılamaz (409) — mesaj API'den gelir (Y-35).
      notify({ message: extractErrorMessage(error, 'Topluluktan ayrılınamadı.'), severity: 'error' })
      setLeaveTarget(null)
    },
  })

  const items = myClubsQuery.data ?? []

  return (
    <>
      {/* §22.3: liste artık yalnızca güncel dönemi gösteriyor — hangi dönem olduğu başlıkta yazar. */}
      <PageHeader
        title="Kulüplerim"
        description={
          items.length > 0
            ? `${items[0].academicTermName} döneminde üyesi olduğunuz topluluklar ve bu topluluklardaki rolünüz.`
            : 'Güncel dönemde üyesi olduğunuz topluluklar ve bu topluluklardaki rolünüz.'
        }
      />

      {myClubsQuery.isLoading ? (
        <CardGridSkeleton count={3} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={GroupsOutlinedIcon}
          title="Henüz bir topluluğa üye değilsiniz"
          description="Kulüpler sayfasından ilginizi çeken bir topluluğa üyelik başvurusu yapabilirsiniz."
        />
      ) : (
        <Grid container spacing={2}>
          {items.map((membership) => (
            <Grid key={membership.clubId} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card variant="outlined" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                <CardContent sx={{ flex: 1 }}>
                  <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', mb: 1 }}>
                    <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                      {membership.clubName}
                    </Typography>
                    <ClubRoleChip role={membership.clubRole} />
                  </Stack>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    {!membership.clubIsActive && <Chip size="small" label="Pasif" variant="outlined" />}
                    <Typography variant="caption" color="text.secondary">
                      {new Date(membership.joinedAtUtc).toLocaleDateString('tr-TR')} tarihinden beri üye
                    </Typography>
                  </Stack>
                </CardContent>
                <CardActions sx={{ px: 2, pb: 2, gap: 0.5 }}>
                  <Button size="small" component={RouterLink} to={`/clubs/${membership.clubId}`}>
                    Detay
                  </Button>
                  <Button size="small" color="error" onClick={() => setLeaveTarget(membership)}>
                    Ayrıl
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <ConfirmDialog
        open={leaveTarget !== null}
        title="Topluluktan ayrıl"
        description={
          leaveTarget
            ? `"${leaveTarget.clubName}" topluluğundan ayrılmak istediğinize emin misiniz? Yeniden üye olmak için tekrar başvurmanız gerekir.`
            : undefined
        }
        confirmLabel="Ayrıl"
        destructive
        loading={leaveMutation.isPending}
        onConfirm={() => leaveTarget && leaveMutation.mutate(leaveTarget.clubId)}
        onCancel={() => setLeaveTarget(null)}
      />
    </>
  )
}
