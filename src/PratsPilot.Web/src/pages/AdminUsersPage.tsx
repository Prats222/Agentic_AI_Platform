import { Alert, Box, Button, Chip, Stack, Switch, Typography } from '@mui/material'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead'
import SendIcon from '@mui/icons-material/Send'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '../api/client'
import type { UserAccess } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'

export function AdminUsersPage() {
  const queryClient = useQueryClient()
  const users = useQuery({ queryKey: ['adminUsers'], queryFn: apiClient.getAdminUsers })
  const updateAccess = useMutation({
    mutationFn: ({ id, isAdmin }: { id: string; isAdmin: boolean }) => apiClient.updateUserAccess(id, isAdmin),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminUsers'] })
    },
  })
  const sendWelcomeGuide = useMutation({
    mutationFn: (id: string) => apiClient.sendWelcomeGuide(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['adminUsers'] })
    },
  })

  return (
    <Box>
      <SectionHeader eyebrow="Admin" title="Users, roles, and realm access" />
      {updateAccess.isSuccess && <Alert severity="success" sx={{ mb: 2 }}>User access updated.</Alert>}
      {updateAccess.isError && <Alert severity="error" sx={{ mb: 2 }}>Could not update user access.</Alert>}
      {sendWelcomeGuide.isSuccess && <Alert severity="success" sx={{ mb: 2 }}>Welcome guide delivered.</Alert>}
      {sendWelcomeGuide.isError && <Alert severity="error" sx={{ mb: 2 }}>Could not send the welcome guide. Check the Brevo sender and API key.</Alert>}
      <DataPanel<UserAccess>
        title="User Access Panel"
        subtitle="Grant Admin role to unlock Admin Realm and administrative controls."
        rows={users.data ?? []}
        loading={users.isLoading}
        columns={[
          {
            key: 'user',
            label: 'User',
            render: (row) => (
              <Box>
                <Typography sx={{ fontWeight: 900 }}>{row.displayName}</Typography>
                <Typography variant="caption" color="text.secondary">{row.email}</Typography>
              </Box>
            ),
          },
          {
            key: 'roles',
            label: 'Roles',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 0.8, flexWrap: 'wrap' }}>
                {row.roles.map((role) => <Chip key={role} size="small" label={role} color={role === 'Admin' ? 'primary' : 'default'} />)}
              </Stack>
            ),
          },
          {
            key: 'realms',
            label: 'Realms',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 0.8, flexWrap: 'wrap' }}>
                <Chip size="small" label="User Realm" color="success" />
                {row.canAccessAdminRealm && <Chip size="small" icon={<AdminPanelSettingsIcon />} label="Admin Realm" color="warning" />}
              </Stack>
            ),
          },
          {
            key: 'emailStatus',
            label: 'Email',
            render: (row) => (
              <Stack spacing={0.6} sx={{ alignItems: 'flex-start' }}>
                <Chip
                  size="small"
                  icon={row.emailConfirmed ? <MarkEmailReadIcon /> : undefined}
                  label={row.emailConfirmed ? 'Confirmed' : 'Unconfirmed'}
                  color={row.emailConfirmed ? 'success' : 'warning'}
                />
                {row.welcomeGuideEmailSentAt && (
                  <Typography variant="caption" color="text.secondary">
                    Guide sent {new Date(row.welcomeGuideEmailSentAt).toLocaleDateString()}
                  </Typography>
                )}
              </Stack>
            ),
          },
          {
            key: 'admin',
            label: 'Admin Access',
            render: (row) => {
              const isAdmin = row.roles.includes('Admin')
              return (
                <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
                  <Switch
                    checked={isAdmin}
                    onChange={(event) => updateAccess.mutate({ id: row.id, isAdmin: event.target.checked })}
                    disabled={updateAccess.isPending}
                  />
                  <Button
                    size="small"
                    variant={isAdmin ? 'contained' : 'outlined'}
                    onClick={() => updateAccess.mutate({ id: row.id, isAdmin: !isAdmin })}
                    disabled={updateAccess.isPending}
                  >
                    {isAdmin ? 'Admin' : 'Grant Admin'}
                  </Button>
                </Stack>
              )
            },
          },
          {
            key: 'guide',
            label: 'Welcome Guide',
            render: (row) => (
              <Button
                size="small"
                variant="outlined"
                startIcon={<SendIcon />}
                disabled={!row.emailConfirmed || sendWelcomeGuide.isPending}
                onClick={() => {
                  const action = row.welcomeGuideEmailSentAt ? 'Resend' : 'Send'
                  if (window.confirm(`${action} the PratsPilot welcome guide to ${row.email}?`)) {
                    sendWelcomeGuide.mutate(row.id)
                  }
                }}
              >
                {row.welcomeGuideEmailSentAt ? 'Resend' : 'Send guide'}
              </Button>
            ),
          },
        ]}
      />
    </Box>
  )
}
