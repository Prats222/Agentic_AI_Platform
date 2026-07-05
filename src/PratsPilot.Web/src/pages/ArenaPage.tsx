import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  Grid,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import SportsKabaddiIcon from '@mui/icons-material/SportsKabaddi'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiClient } from '../api/client'
import type { ArenaChallenge } from '../api/types'
import { SectionHeader } from '../components/SectionHeader'

export function ArenaPage() {
  const queryClient = useQueryClient()
  const challenges = useQuery({ queryKey: ['arenaChallenges'], queryFn: apiClient.getArenaChallenges, refetchInterval: 6000 })
  const agents = useQuery({ queryKey: ['agents'], queryFn: apiClient.getAgents })
  const [selectedId, setSelectedId] = useState('')
  const [entryAgentId, setEntryAgentId] = useState('')
  const [form, setForm] = useState({
    title: 'Python Code Duel',
    description: 'Who can create the cleanest working Python agent output?',
    taskPrompt: 'Create Python code for the given input. Return only executable Python code.',
    rules: 'Return code only. No markdown. No explanation. Must handle edge cases.',
    expectedOutput: 'A complete executable Python solution.',
    judgeCriteria: 'Correctness, executable code, constraint following, edge cases, simplicity.',
  })

  const selected = useMemo(
    () => challenges.data?.find((challenge) => challenge.id === selectedId) ?? challenges.data?.[0],
    [challenges.data, selectedId],
  )

  const createChallenge = useMutation({
    mutationFn: () => apiClient.createArenaChallenge(form),
    onSuccess: (challenge) => {
      setSelectedId(challenge.id)
      queryClient.invalidateQueries({ queryKey: ['arenaChallenges'] })
    },
  })

  const submitEntry = useMutation({
    mutationFn: () => apiClient.submitArenaEntry(selected!.id, entryAgentId),
    onSuccess: () => {
      setEntryAgentId('')
      queryClient.invalidateQueries({ queryKey: ['arenaChallenges'] })
    },
  })

  const runBattle = useMutation({
    mutationFn: () => apiClient.runArenaBattle(selected!.id),
    onSuccess: (challenge) => {
      setSelectedId(challenge.id)
      queryClient.invalidateQueries({ queryKey: ['arenaChallenges'] })
    },
  })

  return (
    <Box>
      <SectionHeader eyebrow="PratsPilot Arena" title="Creator vs creator agent battles" />
      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3, mb: 2.5 }}>
            <Typography variant="h5" sx={{ mb: 2 }}>Create Challenge</Typography>
            <Stack spacing={1.5}>
              <TextField label="Title" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} fullWidth />
              <TextField label="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} fullWidth />
              <TextField label="Battle Task" value={form.taskPrompt} onChange={(event) => setForm({ ...form, taskPrompt: event.target.value })} multiline minRows={4} fullWidth />
              <TextField label="Rules" value={form.rules} onChange={(event) => setForm({ ...form, rules: event.target.value })} multiline minRows={3} fullWidth />
              <TextField label="Expected Output" value={form.expectedOutput} onChange={(event) => setForm({ ...form, expectedOutput: event.target.value })} fullWidth />
              <TextField label="Judge Criteria" value={form.judgeCriteria} onChange={(event) => setForm({ ...form, judgeCriteria: event.target.value })} multiline minRows={3} fullWidth />
              <Button variant="contained" startIcon={<SportsKabaddiIcon />} onClick={() => createChallenge.mutate()} disabled={createChallenge.isPending || !form.title || !form.taskPrompt}>
                Open Arena Challenge
              </Button>
              {createChallenge.isError && <Alert severity="error">Challenge creation failed.</Alert>}
            </Stack>
          </Paper>

          <Paper sx={{ p: 3 }}>
            <Typography variant="h5" sx={{ mb: 2 }}>Open Battles</Typography>
            <Stack spacing={1.2}>
              {(challenges.data ?? []).map((challenge) => (
                <Paper
                  key={challenge.id}
                  onClick={() => setSelectedId(challenge.id)}
                  sx={{ p: 2, cursor: 'pointer', borderColor: selected?.id === challenge.id ? 'primary.main' : 'divider' }}
                  variant="outlined"
                >
                  <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1, alignItems: 'center' }}>
                    <Box>
                      <Typography sx={{ fontWeight: 900 }}>{challenge.title}</Typography>
                      <Typography variant="caption" color="text.secondary">{challenge.entries.length} entries</Typography>
                    </Box>
                    <Chip size="small" color={challenge.status === 'Completed' ? 'success' : challenge.status === 'Failed' ? 'error' : 'primary'} label={challenge.status} />
                  </Stack>
                </Paper>
              ))}
              {!challenges.isLoading && !(challenges.data ?? []).length && <Alert severity="info">No arena challenges yet. Create the first one.</Alert>}
            </Stack>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3, minHeight: 650 }}>
            <Typography variant="h5">Battle Room</Typography>
            {!selected && <Alert severity="info" sx={{ mt: 2 }}>Select or create a challenge to enter agents.</Alert>}
            {selected && (
              <Stack spacing={2.2} sx={{ mt: 2 }}>
                <Stack direction="row" sx={{ justifyContent: 'space-between', gap: 1, alignItems: 'center' }}>
                  <Box>
                    <Typography variant="h4">{selected.title}</Typography>
                    <Typography color="text.secondary">{selected.description}</Typography>
                  </Box>
                  <Chip color={selected.status === 'Completed' ? 'success' : selected.status === 'Failed' ? 'error' : 'primary'} label={selected.status} />
                </Stack>

                <Paper variant="outlined" sx={{ p: 2 }}>
                  <Typography variant="overline" color="primary.main">Challenge</Typography>
                  <Typography sx={{ whiteSpace: 'pre-wrap' }}>{selected.taskPrompt}</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>Rules: {selected.rules}</Typography>
                </Paper>

                <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 1.2 }}>
                  <TextField select label="Enter Agent" value={entryAgentId} onChange={(event) => setEntryAgentId(event.target.value)} fullWidth>
                    {(agents.data?.items ?? []).map((agent) => (
                      <MenuItem key={agent.id} value={agent.id}>{agent.name}</MenuItem>
                    ))}
                  </TextField>
                  <Button variant="outlined" onClick={() => submitEntry.mutate()} disabled={!entryAgentId || submitEntry.isPending}>
                    Submit
                  </Button>
                  <Button variant="contained" startIcon={<EmojiEventsIcon />} onClick={() => runBattle.mutate()} disabled={selected.entries.length < 2 || runBattle.isPending}>
                    {runBattle.isPending ? 'Judging...' : 'Run Battle'}
                  </Button>
                </Stack>
                {submitEntry.isError && <Alert severity="error">Could not submit agent. It may already be entered.</Alert>}
                {runBattle.isError && <Alert severity="error">Battle failed. Check model/API key settings.</Alert>}

                {selected.winnerEntryId && <WinnerCard challenge={selected} />}

                <Box>
                  <Typography variant="h6" sx={{ mb: 1 }}>Entries</Typography>
                  <Stack spacing={1.2}>
                    {selected.entries.map((entry) => (
                      <Accordion key={entry.id} disableGutters>
                        <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                          <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap', width: '100%' }}>
                            <Chip size="small" color={selected.winnerEntryId === entry.id ? 'warning' : 'default'} label={selected.winnerEntryId === entry.id ? 'Winner' : 'Entry'} />
                            <Typography sx={{ fontWeight: 900 }}>{entry.agentName}</Typography>
                            {typeof entry.score === 'number' && <Chip size="small" color="primary" label={`${entry.score}/10`} />}
                          </Stack>
                        </AccordionSummary>
                        <AccordionDetails>
                          {entry.feedback && <Alert severity={entry.feedback.includes('Fallback judge') ? 'warning' : 'info'} sx={{ mb: 1.2 }}>{entry.feedback}</Alert>}
                          <Typography component="pre" sx={codeBoxSx}>{entry.output ?? 'Battle has not run yet.'}</Typography>
                        </AccordionDetails>
                      </Accordion>
                    ))}
                  </Stack>
                </Box>
              </Stack>
            )}
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

function WinnerCard({ challenge }: { challenge: ArenaChallenge }) {
  const winner = challenge.entries.find((entry) => entry.id === challenge.winnerEntryId)
  return (
    <Paper variant="outlined" sx={{ p: 2, bgcolor: 'rgba(255,183,77,0.08)', borderColor: 'warning.main' }}>
      <Stack direction="row" sx={{ gap: 1.5, alignItems: 'center' }}>
        <EmojiEventsIcon color="warning" />
        <Box>
          <Typography variant="h6">Winner: {winner?.agentName ?? 'Unknown Agent'}</Typography>
          <Typography color="text.secondary">{challenge.judgeSummary}</Typography>
        </Box>
      </Stack>
    </Paper>
  )
}

const codeBoxSx = {
  whiteSpace: 'pre-wrap',
  overflow: 'auto',
  maxHeight: 360,
  p: 1.5,
  borderRadius: 1.5,
  bgcolor: 'background.default',
  border: '1px solid',
  borderColor: 'divider',
  fontFamily: 'ui-monospace, Consolas, monospace',
  fontSize: 13,
  lineHeight: 1.65,
}
