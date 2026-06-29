import {
  Alert,
  Box,
  Button,
  Chip,
  Grid,
  LinearProgress,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import AccountTreeIcon from '@mui/icons-material/AccountTree'
import MemoryIcon from '@mui/icons-material/Memory'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import PsychologyIcon from '@mui/icons-material/Psychology'
import SmartToyIcon from '@mui/icons-material/SmartToy'
import TerminalIcon from '@mui/icons-material/Terminal'
import axios from 'axios'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { apiClient } from '../api/client'
import { MetricTile } from '../components/MetricTile'
import { SectionHeader } from '../components/SectionHeader'

export function DashboardPage() {
  const queryClient = useQueryClient()
  const [targetType, setTargetType] = useState<'Workflow' | 'Agent'>('Workflow')
  const [targetId, setTargetId] = useState('')
  const [inputJson, setInputJson] = useState('{"expression":"(8 + 2) * 3"}')

  const catalog = useQuery({ queryKey: ['demoCatalog'], queryFn: apiClient.getDemoCatalog })
  const agents = useQuery({ queryKey: ['agents'], queryFn: apiClient.getAgents })
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const workflows = useQuery({ queryKey: ['workflows'], queryFn: apiClient.getWorkflows })
  const executions = useQuery({ queryKey: ['executions'], queryFn: () => apiClient.getExecutions(), refetchInterval: 4000 })
  const settings = useQuery({ queryKey: ['aiSettings'], queryFn: apiClient.getAISettings })

  const targets = targetType === 'Workflow' ? workflows.data?.items ?? [] : agents.data?.items ?? []
  const selectedTargetId = targetId && targets.some((target) => target.id === targetId)
    ? targetId
    : targets[0]?.id || ''
  const selectedTarget = targets.find((target) => target.id === selectedTargetId)

  useEffect(() => {
    setTargetId('')
    setInputJson(targetType === 'Workflow' ? '{"prompt":"Explain how AI agents use tools in software platforms."}' : '{"prompt":"Explain agentic AI in two simple sentences."}')
  }, [targetType])

  useEffect(() => {
    if (!selectedTargetId) {
      return
    }

    const sample = targetType === 'Workflow'
      ? catalog.data?.workflows.find((workflow) => workflow.id === selectedTargetId)?.sampleInputJson
      : catalog.data?.agents.find((agent) => agent.id === selectedTargetId)?.sampleInputJson

    if (sample) {
      setInputJson(sample)
    }
  }, [catalog.data, selectedTargetId, targetType])

  const runExecution = useMutation({
    mutationFn: () => apiClient.startExecution(targetType, selectedTargetId, inputJson),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['executions'] })
    },
  })

  const statusCounts = useMemo(() => {
    const items = executions.data?.items ?? []
    return {
      completed: items.filter((item) => item.status === 'Completed').length,
      running: items.filter((item) => item.status === 'Running' || item.status === 'Pending').length,
      failed: items.filter((item) => item.status === 'Failed').length,
    }
  }, [executions.data])

  return (
    <Box>
      <SectionHeader
        eyebrow="Mission Control"
        title="Pilot agents, workflows, and tools from one surface"
        action={<Chip color="primary" label={settings.data ? `${settings.data.provider} / ${settings.data.model}` : 'AI settings'} />}
      />

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile label="Agents" value={agents.isLoading ? '...' : agents.data?.totalCount ?? 0} icon={<PsychologyIcon />} />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile label="Workflows" value={workflows.isLoading ? '...' : workflows.data?.totalCount ?? 0} icon={<AccountTreeIcon />} tone="secondary" />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile label="Tools" value={tools.isLoading ? '...' : tools.data?.totalCount ?? 0} icon={<MemoryIcon />} tone="success" />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile label="Recent Runs" value={executions.isLoading ? '...' : executions.data?.totalCount ?? 0} icon={<TerminalIcon />} tone="warning" />
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3, minHeight: 390 }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2.5 }}>
              <Box>
                <Typography variant="h5">Run Console</Typography>
                <Typography variant="body2" color="text.secondary">
                  Trigger seeded demos or your own agents and workflows.
                </Typography>
              </Box>
              <SmartToyIcon color="primary" />
            </Stack>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, sm: 4 }}>
                <TextField select fullWidth label="Target Type" value={targetType} onChange={(e) => setTargetType(e.target.value as 'Workflow' | 'Agent')}>
                  <MenuItem value="Workflow">Workflow</MenuItem>
                  <MenuItem value="Agent">Agent</MenuItem>
                </TextField>
              </Grid>
              <Grid size={{ xs: 12, sm: 8 }}>
                <TextField select fullWidth label="Target" value={selectedTargetId} onChange={(e) => setTargetId(e.target.value)}>
                  {targets.length === 0 && (
                    <MenuItem value="" disabled>
                      {workflows.isLoading || agents.isLoading ? 'Loading targets...' : 'No targets available'}
                    </MenuItem>
                  )}
                  {targets.map((target) => (
                    <MenuItem key={target.id} value={target.id}>
                      {target.name}
                    </MenuItem>
                  ))}
                </TextField>
                {selectedTarget && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.8 }}>
                    Selected {targetType.toLowerCase()}: {selectedTarget.name}
                  </Typography>
                )}
              </Grid>
              <Grid size={12}>
                <TextField
                  label="Input JSON"
                  value={inputJson}
                  onChange={(event) => setInputJson(event.target.value)}
                  multiline
                  minRows={7}
                  fullWidth
                  spellCheck={false}
                  sx={{ '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
                />
              </Grid>
              <Grid size={12}>
                <Button
                  variant="contained"
                  startIcon={<PlayArrowIcon />}
                  onClick={() => runExecution.mutate()}
                  disabled={!selectedTargetId || runExecution.isPending || workflows.isLoading || agents.isLoading}
                >
                  {runExecution.isPending ? 'Queued...' : 'Start Execution'}
                </Button>
              </Grid>
              {runExecution.isError && (
                <Grid size={12}>
                  <Alert severity="error">{getExecutionStartError(runExecution.error)}</Alert>
                </Grid>
              )}
              {runExecution.isSuccess && (
                <Grid size={12}>
                  <Alert severity="success">Execution queued. Watch the Executions page for output.</Alert>
                </Grid>
              )}
            </Grid>
          </Paper>
        </Grid>

        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3, minHeight: 390 }}>
            <Typography variant="h5">Runtime Pulse</Typography>
            <Stack spacing={2.2} sx={{ mt: 3 }}>
              <Pulse label="Completed" value={statusCounts.completed} color="success" />
              <Pulse label="Running / Pending" value={statusCounts.running} color="primary" />
              <Pulse label="Failed" value={statusCounts.failed} color="error" />
            </Stack>
            <Box sx={{ mt: 4 }}>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                Demo catalog
              </Typography>
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                <Chip label={`${catalog.data?.tools.length ?? 0} tools`} />
                <Chip label={`${catalog.data?.agents.length ?? 0} agents`} />
                <Chip label={`${catalog.data?.workflows.length ?? 0} workflows`} />
              </Stack>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

function Pulse({ label, value, color }: { label: string; value: number; color: 'primary' | 'success' | 'error' }) {
  const progress = Math.min(100, value * 18)
  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.8 }}>
        <Typography variant="body2" sx={{ fontWeight: 800 }}>
          {label}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {value}
        </Typography>
      </Stack>
      <LinearProgress variant="determinate" value={progress} color={color} sx={{ height: 8, borderRadius: 8 }} />
    </Box>
  )
}

function getExecutionStartError(error: unknown) {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 401) {
      return 'Your login session expired. Log in again and start the execution once more.'
    }

    if (error.response?.status === 403) {
      return 'Your account can view executions, but it does not have permission to start them.'
    }

    const data = error.response?.data as {
      message?: string
      title?: string
      detail?: string
      errors?: Record<string, string[]> | string[]
    } | undefined

    if (Array.isArray(data?.errors) && data.errors.length) {
      return data.errors.join(' ')
    }

    if (data?.errors && !Array.isArray(data.errors)) {
      return Object.values(data.errors).flat().join(' ')
    }

    return data?.detail ?? data?.message ?? data?.title ?? error.message
  }

  return error instanceof Error ? error.message : 'Execution could not be started.'
}
