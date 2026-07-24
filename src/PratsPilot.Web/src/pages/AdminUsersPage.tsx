import {
  Alert,
  Box,
  Button,
  Chip,
  Paper,
  Stack,
  Switch,
  TablePagination,
  Typography,
} from '@mui/material'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead'
import PeopleAltIcon from '@mui/icons-material/PeopleAlt'
import PersonAddAlt1Icon from '@mui/icons-material/PersonAddAlt1'
import SendIcon from '@mui/icons-material/Send'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { apiClient } from '../api/client'
import type { UserAccess } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'

export function AdminUsersPage() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const timezoneOffsetMinutes = -new Date().getTimezoneOffset()
  const users = useQuery({
    queryKey: ['adminUsers', page, pageSize, timezoneOffsetMinutes],
    queryFn: () => apiClient.getAdminUsers(page + 1, pageSize, timezoneOffsetMinutes),
  })
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
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
          gap: 2,
          mb: 2,
        }}
      >
        <UserMetric
          icon={<PeopleAltIcon />}
          label="Total users"
          value={users.data?.totalCount}
          detail="Registered and seeded accounts"
          loading={users.isLoading}
        />
        <UserMetric
          icon={<PersonAddAlt1Icon />}
          label="Joined today"
          value={users.data?.joinedTodayCount}
          detail="Based on your local timezone"
          loading={users.isLoading}
        />
      </Box>
      <Paper sx={{ p: 3, mb: 2 }}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          sx={{ justifyContent: 'space-between', alignItems: { xs: 'flex-start', sm: 'center' }, gap: 1, mb: 2 }}
        >
          <Box>
            <Typography variant="h5">Who joined today</Typography>
            <Typography variant="body2" color="text.secondary">
              Latest registrations from your current local day.
            </Typography>
          </Box>
          <Chip
            color="primary"
            label={`${users.data?.joinedTodayCount ?? 0} today`}
          />
        </Stack>
        {users.isLoading ? (
          <Chip label="Loading today's users..." />
        ) : (users.data?.joinedToday.length ?? 0) === 0 ? (
          <Typography variant="body2" color="text.secondary">No one has joined today yet.</Typography>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
              columnGap: 3,
            }}
          >
            {users.data?.joinedToday.map((user) => (
              <Stack
                key={user.id}
                direction="row"
                sx={{
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: 2,
                  py: 1.25,
                  borderBottom: '1px solid',
                  borderColor: 'divider',
                  minWidth: 0,
                }}
              >
                <Box sx={{ minWidth: 0 }}>
                  <Stack direction="row" sx={{ alignItems: 'center', gap: 0.8 }}>
                    <Typography sx={{ fontWeight: 800 }} noWrap>{user.displayName}</Typography>
                    {user.isDemoUser && <Chip size="small" label="Demo" />}
                  </Stack>
                  <Typography variant="caption" color="text.secondary" noWrap>{user.email}</Typography>
                </Box>
                <Typography variant="caption" color="text.secondary" sx={{ flexShrink: 0 }}>
                  {new Date(user.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </Typography>
              </Stack>
            ))}
          </Box>
        )}
      </Paper>
      <DataPanel<UserAccess>
        title="User Access Panel"
        subtitle={`Showing ${users.data?.items.length ?? 0} of ${users.data?.totalCount ?? 0} users. Grant Admin role to unlock administrative controls.`}
        rows={users.data?.items ?? []}
        loading={users.isLoading}
        columns={[
          {
            key: 'user',
            label: 'User',
            render: (row) => (
              <Box>
                <Stack direction="row" sx={{ alignItems: 'center', gap: 0.8, flexWrap: 'wrap' }}>
                  <Typography sx={{ fontWeight: 900 }}>{row.displayName}</Typography>
                  {row.isDemoUser && <Chip size="small" label="Demo" />}
                </Stack>
                <Typography variant="caption" color="text.secondary">{row.email}</Typography>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                  Joined {new Date(row.createdAt).toLocaleDateString()}
                </Typography>
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
      <Paper sx={{ mt: 2, overflow: 'hidden' }}>
        <TablePagination
          component="div"
          count={users.data?.totalCount ?? 0}
          page={page}
          onPageChange={(_, nextPage) => setPage(nextPage)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(event) => {
            setPageSize(Number(event.target.value))
            setPage(0)
          }}
          rowsPerPageOptions={[25, 50, 100]}
          labelRowsPerPage="Users per page"
        />
      </Paper>
    </Box>
  )
}

function UserMetric({
  icon,
  label,
  value,
  detail,
  loading,
}: {
  icon: ReactNode
  label: string
  value?: number
  detail: string
  loading: boolean
}) {
  return (
    <Paper sx={{ p: 2.5 }}>
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
        <Box>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
          <Typography variant="h4" sx={{ mt: 0.5 }}>{loading ? '...' : value ?? 0}</Typography>
          <Typography variant="caption" color="text.secondary">{detail}</Typography>
        </Box>
        <Box
          sx={{
            width: 46,
            height: 46,
            display: 'grid',
            placeItems: 'center',
            borderRadius: 2,
            color: 'primary.main',
            bgcolor: 'rgba(124,92,252,0.12)',
          }}
        >
          {icon}
        </Box>
      </Stack>
    </Paper>
  )
}
