import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Alert, Button, Chip, Stack, TextField, Typography } from '@mui/material'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { apiClient } from '../api/client'
import { extractErrorMessage } from '../api/errors'
import { useNotifier } from '../notifications/NotifierProvider'
import { PageHeader } from '../components/ui/PageHeader'
import { SectionCard } from '../components/ui/SectionCard'
import type { MeResponseDto } from '../api/types'

const strongPasswordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$/

const changePasswordSchema = z.object({
  currentPassword: z.string().min(1, 'Mevcut parolanızı girin.'),
  newPassword: z.string().regex(strongPasswordRegex, 'Parola en az 6 karakter olmalı; büyük harf, küçük harf, rakam ve alfanumerik olmayan bir karakter içermelidir.'),
})

type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>

export function ProfilePage() {
  const notify = useNotifier()

  const meQuery = useQuery({
    queryKey: ['me'],
    queryFn: async () => (await apiClient.get<MeResponseDto>('/me')).data,
  })

  const {
    control,
    handleSubmit,
    reset,
    formState: { isSubmitting },
  } = useForm<ChangePasswordFormValues>({ resolver: zodResolver(changePasswordSchema), defaultValues: { currentPassword: '', newPassword: '' } })

  const changePasswordMutation = useMutation({
    mutationFn: async (values: ChangePasswordFormValues) => {
      await apiClient.post('/auth/change-password', values)
    },
    onSuccess: () => {
      notify({ message: 'Parolanız güncellendi.', severity: 'success' })
      reset()
    },
    onError: (error) => notify({ message: extractErrorMessage(error, 'Parola güncellenemedi.'), severity: 'error' }),
  })

  return (
    <>
      <PageHeader title="Profilim" description="Hesap bilgileriniz ve parola yönetimi." />

      <Stack spacing={3}>
        <SectionCard title="Hesap Bilgileri">
          <Stack spacing={1.5}>
            <Typography variant="body2">{meQuery.data?.email}</Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
              {(meQuery.data?.roles ?? []).map((role) => (
                <Chip key={role} size="small" label={role} />
              ))}
            </Stack>
          </Stack>
        </SectionCard>

        <SectionCard title="Parola Değiştir">
          <Stack component="form" spacing={2} onSubmit={handleSubmit((values) => changePasswordMutation.mutate(values))} sx={{ maxWidth: 380 }}>
            <Controller
              name="currentPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label="Mevcut Parola" type="password" autoComplete="current-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />
            <Controller
              name="newPassword"
              control={control}
              render={({ field, fieldState }) => (
                <TextField {...field} label="Yeni Parola" type="password" autoComplete="new-password" error={!!fieldState.error} helperText={fieldState.error?.message} fullWidth />
              )}
            />
            {changePasswordMutation.isError && (
              <Alert severity="error">{extractErrorMessage(changePasswordMutation.error, 'Parola güncellenemedi.')}</Alert>
            )}
            <Button type="submit" variant="contained" disabled={isSubmitting || changePasswordMutation.isPending} sx={{ alignSelf: 'flex-start' }}>
              Parolayı Güncelle
            </Button>
          </Stack>
        </SectionCard>
      </Stack>
    </>
  )
}
