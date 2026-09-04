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
  // K-41/A-70: danışman satırı varsa başlık metni "üyesi" demeyecek şekilde kurulur.
  const hasMemberRow = items.some((item) => item.relationship === 'Member')
  const hasAdvisorRow = items.some((item) => item.relationship === 'Advisor')
  const memberTerm = items.find((item) => item.relationship === 'Member')?.academicTermName

  const headerDescription = (() => {
    if (hasMemberRow && hasAdvisorRow) {
      return `${memberTerm} döneminde üyesi veya danışmanı olduğunuz topluluklar ve bu topluluklardaki rolünüz.`
    }
    if (hasAdvisorRow) {
      return 'Danışmanı olduğunuz topluluklar.'
    }
    if (hasMemberRow) {
      return `${memberTerm} döneminde üyesi olduğunuz topluluklar ve bu topluluklardaki rolünüz.`
    }
    return 'Güncel dönemde üyesi veya danışmanı olduğunuz topluluklar ve bu topluluklardaki rolünüz.'
  })()

  return (
    <>
      {/* §22.3: liste artık yalnızca güncel dönemi gösteriyor — hangi dönem olduğu başlıkta yazar. */}
      <PageHeader title="Kulüplerim" description={headerDescription} />

      {myClubsQuery.isLoading ? (
        <CardGridSkeleton count={3} />
      ) : items.length === 0 ? (
        <EmptyState
          icon={GroupsOutlinedIcon}
          title="Görüntülenecek topluluk yok"
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
                    {membership.relationship === 'Advisor' ? (
                      <Chip size="small" color="secondary" label="Danışman" sx={{ flexShrink: 0 }} />
                    ) : membership.clubRoleName ? (
                      // K-36: unvan varsa onu göster, yetki seviyesi rozeti yanında kalır.
                      <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', flexShrink: 0 }}>
                        <Chip size="small" label={membership.clubRoleName} />
                        <ClubRoleChip role={membership.clubRole} />
                      </Stack>
                    ) : (
                      <ClubRoleChip role={membership.clubRole} />
                    )}
                  </Stack>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    {!membership.clubIsActive && <Chip size="small" label="Pasif" variant="outlined" />}
                    {membership.relationship !== 'Advisor' && (
                      <Typography variant="caption" color="text.secondary">
                        {new Date(membership.joinedAtUtc).toLocaleDateString('tr-TR')} tarihinden beri üye
                      </Typography>
                    )}
                  </Stack>
                </CardContent>
                <CardActions sx={{ px: 2, pb: 2, gap: 0.5 }}>
                  <Button size="small" component={RouterLink} to={`/clubs/${membership.clubId}`}>
                    Detay
                  </Button>
                  {/* Y-77: danışmanlıktan "ayrılınmaz" — sonlandırma AdvisorId değişikliğiyle olur. */}
                  {membership.relationship !== 'Advisor' && (
                    <Button size="small" color="error" onClick={() => setLeaveTarget(membership)}>
                      Ayrıl
                    </Button>
                  )}
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
