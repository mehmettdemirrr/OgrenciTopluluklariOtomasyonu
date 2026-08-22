import { useQuery } from '@tanstack/react-query'
import { Card, CardContent, Chip, Grid, Stack, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { apiClient } from '../api/client'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { ClubRoleChip } from '../components/ui/StatusChip'
import type { MyClubMembershipDto } from '../api/types'

export function MyClubsPage() {
  const myClubsQuery = useQuery({
    queryKey: ['clubs-mine'],
    queryFn: async () => (await apiClient.get<MyClubMembershipDto[]>('/clubs/mine')).data,
  })

  const items = myClubsQuery.data ?? []

  return (
    <>
      <PageHeader title="Kulüplerim" description="Üyesi olduğunuz topluluklar ve bu topluluklardaki rolünüz." />

      {!myClubsQuery.isLoading && items.length === 0 ? (
        <EmptyState
          icon={GroupsOutlinedIcon}
          title="Henüz bir topluluğa üye değilsiniz"
          description="Kulüpler sayfasından ilginizi çeken bir topluluğa üyelik başvurusu yapabilirsiniz."
        />
      ) : (
        <Grid container spacing={2}>
          {items.map((membership) => (
            <Grid key={membership.clubId} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card
                variant="outlined"
                component={RouterLink}
                to={`/clubs/${membership.clubId}`}
                sx={{ display: 'block', height: '100%', textDecoration: 'none', color: 'inherit' }}
              >
                <CardContent>
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
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </>
  )
}
