import { Alert, Box, Button, Chip, Grid, Paper, Stack, TextField, Typography } from '@mui/material'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import CancelIcon from '@mui/icons-material/Cancel'
import PauseCircleIcon from '@mui/icons-material/PauseCircle'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { SectionHeader } from '../components/SectionHeader'

export function ApprovalsPage() {
  const queryClient = useQueryClient()
  const approvals = useQuery({ queryKey: ['humanApprovals'], queryFn: () => apiClient.getHumanApprovals(false), refetchInterval: 5000 })
  const [selectedId, setSelectedId] = useState('')
  const [comment, setComment] = useState('')
  const selected = approvals.data?.find((item) => item.id === selectedId) ?? approvals.data?.[0]

  const approve = useMutation({
    mutationFn: () => apiClient.approveHumanApproval(selected!.id, comment),
    onSuccess: () => {
      setComment('')
      queryClient.invalidateQueries({ queryKey: ['humanApprovals'] })
      queryClient.invalidateQueries({ queryKey: ['executions'] })
    },
  })

  const reject = useMutation({
    mutationFn: () => apiClient.rejectHumanApproval(selected!.id, comment),
    onSuccess: () => {
      setComment('')
      queryClient.invalidateQueries({ queryKey: ['humanApprovals'] })
      queryClient.invalidateQueries({ queryKey: ['executions'] })
    },
  })

  return (
    <Box>
      <SectionHeader eyebrow="Human Loop" title="Review approval gates before workflows continue" />
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h5" sx={{ mb: 2 }}>Approval Queue</Typography>
            <Stack spacing={1.5}>
              {(approvals.data ?? []).map((item) => (
                <Paper
                  key={item.id}
                  onClick={() => setSelectedId(item.id)}
                  sx={{
                    p: 2,
                    cursor: 'pointer',
                    borderColor: selected?.id === item.id ? 'primary.main' : 'divider',
                    bgcolor: selected?.id === item.id ? 'action.selected' : 'background.paper',
                  }}
                >
                  <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1, alignItems: 'center' }}>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="h6" noWrap>{item.title}</Typography>
                      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                        Execution {item.executionId}
                      </Typography>
                    </Box>
                    <ApprovalChip item={item} />
                  </Stack>
                </Paper>
              ))}
              {!approvals.isLoading && !(approvals.data ?? []).length && (
                <Alert severity="info">No approval gates are waiting right now.</Alert>
              )}
            </Stack>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h5" sx={{ mb: 2 }}>Gate Review</Typography>
            {selected ? (
              <Stack spacing={2}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1, alignItems: 'center' }}>
                  <Box>
                    <Typography variant="h5">{selected.title}</Typography>
                    <Typography variant="body2" color="text.secondary">{selected.instructions}</Typography>
                  </Box>
                  <ApprovalChip item={selected} />
                </Stack>
                <TextField
                  label="Payload JSON"
                  value={formatJson(selected.payloadJson)}
                  multiline
                  minRows={10}
                  fullWidth
                  spellCheck={false}
                  sx={{ '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
                />
                <TextField label="Reviewer comment" value={comment} onChange={(event) => setComment(event.target.value)} fullWidth multiline minRows={3} />
                <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                  <Button
                    variant="contained"
                    startIcon={<CheckCircleIcon />}
                    disabled={selected.isApproved || selected.isRejected || approve.isPending || reject.isPending}
                    onClick={() => approve.mutate()}
                  >
                    Approve and Resume
                  </Button>
                  <Button
                    color="error"
                    variant="outlined"
                    startIcon={<CancelIcon />}
                    disabled={selected.isApproved || selected.isRejected || approve.isPending || reject.isPending}
                    onClick={() => reject.mutate()}
                  >
                    Reject
                  </Button>
                </Stack>
                {(approve.isSuccess || reject.isSuccess) && <Alert severity="success">Approval decision saved.</Alert>}
                {(approve.isError || reject.isError) && <Alert severity="error">Approval action failed.</Alert>}
              </Stack>
            ) : (
              <Alert severity="info">Select an approval request to inspect its payload.</Alert>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

function ApprovalChip({ item }: { item: { isApproved: boolean; isRejected: boolean } }) {
  if (item.isApproved) return <Chip color="success" label="Approved" />
  if (item.isRejected) return <Chip color="error" label="Rejected" />
  return <Chip color="warning" icon={<PauseCircleIcon />} label="Waiting" />
}

function formatJson(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}
