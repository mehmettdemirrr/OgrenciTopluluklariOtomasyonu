import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Stack, Tab, Tabs, Typography } from '@mui/material'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { EmptyState } from '../components/ui/EmptyState'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import { ApplicationStatusChip } from '../components/ui/StatusChip'
import type { ClubApplicationListItemDto, MembershipApplicationListItemDto } from '../api/types'

export function MyApplicationsPage() {
  const [tab, setTab] = useState(0)

  return (
    <>
      <PageHeader title="Başvurularım" description="Üyelik ve topluluk kurma başvurularınızın durumu." />

      <Tabs value={tab} onChange={(_, value: number) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Üyelik Başvurularım" />
        <Tab label="Topluluk Kurma" />
      </Tabs>

      {tab === 0 && <MembershipApplicationsTab />}
      {tab === 1 && <ClubApplicationsTab />}
    </>
  )
}

function MembershipApplicationsTab() {
  const queryClient = useQueryClient()
  const notify = useNotifier()
  const [withdrawTarget, setWithdrawTarget] = useState<MembershipApplicationListItemDto | null>(null)

  const applicationsQuery = useQuery({
    queryKey: ['membership-applications-mine'],
    queryFn: async () => (await apiClient.get<MembershipApplicationListItemDto[]>('/membership-applications/mine')).data,
  })

  const withdrawMutation = useMutation({
    mutationFn: async (applicationId: number) => {
      await apiClient.delete(`/membership-applications/${applicationId}`)
    },
    onSuccess: () => {
      notify({ message: 'Başvurunuz geri çekildi.', severity: 'success' })
      setWithdrawTarget(null)
      queryClient.invalidateQueries({ queryKey: ['membership-applications-mine'] })
    },
    onError: (error) => {
      notify({ message: extractErrorMessage(error, 'Başvuru geri çekilemedi.'), severity: 'error' })
      setWithdrawTarget(null)
    },
  })

  const items = applicationsQuery.data ?? []

  if (!applicationsQuery.isLoading && items.length === 0) {
    return (
      <EmptyState
        icon={GroupsOutlinedIcon}
        title="Henüz bir üyelik başvurunuz yok"
        description="Kulüpler sayfasından ilginizi çeken bir topluluğa başvurabilirsiniz."
      />
    )
  }

  return (
    <>
      <Stack spacing={2}>
        {items.map((application) => (
          <SectionCard
            key={application.id}
            action={
              application.status === 'Pending' && (
                <Button size="small" color="error" onClick={() => setWithdrawTarget(application)}>
                  Geri Çek
                </Button>
              )
            }
          >
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                {application.clubName}
              </Typography>
              <ApplicationStatusChip status={application.status} />
            </Stack>
            <Typography variant="caption" color="text.secondary">
              {new Date(application.appliedAtUtc).toLocaleString('tr-TR')} tarihinde başvuruldu
              {application.reviewedAtUtc
                ? ` · ${new Date(application.reviewedAtUtc).toLocaleString('tr-TR')} tarihinde karara bağlandı`
                : ''}
            </Typography>
          </SectionCard>
        ))}
      </Stack>

      <ConfirmDialog
        open={withdrawTarget !== null}
        title="Başvuruyu geri çek"
        description={
          withdrawTarget
            ? `"${withdrawTarget.clubName}" topluluğuna yaptığınız başvuruyu geri çekmek istediğinize emin misiniz? Daha sonra tekrar başvurabilirsiniz.`
            : undefined
        }
        confirmLabel="Geri Çek"
        destructive
        loading={withdrawMutation.isPending}
        onConfirm={() => withdrawTarget && withdrawMutation.mutate(withdrawTarget.id)}
        onCancel={() => setWithdrawTarget(null)}
      />
    </>
  )
}

function ClubApplicationsTab() {
  const myApplicationsQuery = useQuery({
    queryKey: ['club-applications-mine'],
    queryFn: async () => (await apiClient.get<ClubApplicationListItemDto[]>('/club-applications/mine')).data,
  })

  const items = myApplicationsQuery.data ?? []

  if (!myApplicationsQuery.isLoading && items.length === 0) {
    return (
      <EmptyState
        icon={GroupsOutlinedIcon}
        title="Henüz bir topluluk kurma başvurunuz yok"
        description="Kulüpler sayfasındaki “Topluluk Kurmak İstiyorum” butonuyla başvurabilirsiniz."
      />
    )
  }

  return (
    <Stack spacing={2}>
      {items.map((application) => (
        <SectionCard key={application.id}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1 }}>
            <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
              {application.proposedName}
            </Typography>
            <ApplicationStatusChip status={application.status} />
          </Stack>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Önerilen danışman: {application.proposedAdvisorTitle}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {new Date(application.appliedAtUtc).toLocaleString('tr-TR')} tarihinde başvuruldu
          </Typography>
          {application.reviewNote && (
            <Typography variant="body2" sx={{ mt: 1 }}>
              Not: {application.reviewNote}
            </Typography>
          )}
        </SectionCard>
      ))}
    </Stack>
  )
}
