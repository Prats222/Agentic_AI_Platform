import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  Grid,
  LinearProgress,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { alpha, useTheme } from '@mui/material/styles'
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
import type { Agent } from '../api/types'

export function DashboardPage() {
  const theme = useTheme()
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
  const selectedAgentInputFields = targetType === 'Agent' ? getAgentInputFields(selectedTarget as Agent | undefined) : []
  const inputValues = useMemo(() => parseInputJson(inputJson), [inputJson])

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
      return
    }

    if (targetType === 'Agent') {
      const selectedAgent = targets.find((target) => target.id === selectedTargetId) as Agent | undefined
      const fields = getAgentInputFields(selectedAgent)
      if (fields.length) {
        setInputJson(JSON.stringify(Object.fromEntries(fields.map((field) => [field.name, ''])), null, 2))
      }
    }
  }, [catalog.data, selectedTargetId, targetType, targets])

  const runExecution = useMutation({
    mutationFn: () => apiClient.startExecution(targetType, selectedTargetId, inputJson),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['executions'] })
    },
  })

  const statusCounts = useMemo(() => {
    const items = executions.data?.items ?? []
    const completed = items.filter((item) => item.status === 'Completed')
    const failed = items.filter((item) => item.status === 'Failed')
    const running = items.filter((item) => item.status === 'Running' || item.status === 'Pending')
    const finished = completed.length + failed.length
    const durations = completed
      .map((item) => item.durationMs)
      .filter((duration): duration is number => typeof duration === 'number' && duration > 0)

    return {
      completed: completed.length,
      running: running.length,
      failed: failed.length,
      successRate: finished ? Math.round((completed.length / finished) * 100) : 0,
      averageRuntimeMs: durations.length ? Math.round(durations.reduce((sum, duration) => sum + duration, 0) / durations.length) : 0,
    }
  }, [executions.data])
  const recentExecutions = (executions.data?.items ?? []).slice(0, 5)
  const activeCount = (agents.data?.items ?? []).filter((agent) => agent.status === 'Active').length
  const activeWorkflows = (workflows.data?.items ?? []).filter((workflow) => workflow.status === 'Active').length
  const enabledTools = (tools.data?.items ?? []).filter((tool) => tool.isEnabled).length
  const isInitialLoading = [catalog, agents, tools, workflows, executions, settings].some((query) => query.isLoading)

  return (
    <Box>
      <SectionHeader
        eyebrow="Mission Control"
        title="Pilot agents, workflows, and tools from one surface"
        action={<Chip color="primary" variant="outlined" label={settings.data ? `${settings.data.provider} / ${settings.data.model}` : 'AI settings'} />}
      />
      {isInitialLoading && (
        <Alert severity="info" sx={{ mb: 2.5 }}>
          Waking the free backend and loading live platform data. This can take a little longer after the app has been idle.
        </Alert>
      )}

      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile
            label="Agents"
            value={agents.isLoading ? '...' : agents.data?.totalCount ?? 0}
            icon={<PsychologyIcon />}
            helper={`${activeCount} active`}
            caption="Crew available for workflows"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile
            label="Workflows"
            value={workflows.isLoading ? '...' : workflows.data?.totalCount ?? 0}
            icon={<AccountTreeIcon />}
            tone="secondary"
            helper={`${activeWorkflows} active`}
            caption="Multi-agent missions"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile
            label="Tools"
            value={tools.isLoading ? '...' : tools.data?.totalCount ?? 0}
            icon={<MemoryIcon />}
            tone="success"
            helper={`${enabledTools} enabled`}
            caption="Callable capabilities"
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
          <MetricTile
            label="Mission Runs"
            value={executions.isLoading ? '...' : executions.data?.totalCount ?? 0}
            icon={<TerminalIcon />}
            tone="warning"
            helper={`${statusCounts.successRate}% success`}
            caption={`${formatDuration(statusCounts.averageRuntimeMs)} avg runtime`}
          />
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3, minHeight: 430 }}>
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
                {selectedAgentInputFields.length > 0 && (
                  <Box
                    sx={{
                      p: 2,
                      mb: 2,
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 2,
                      bgcolor: alpha(theme.palette.primary.main, 0.05),
                    }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 900, mb: 1 }}>
                      Agent Inputs
                    </Typography>
                    <Grid container spacing={1.5}>
                      {selectedAgentInputFields.map((field) => (
                        <Grid key={field.name} size={{ xs: 12, sm: 6 }}>
                          <TextField
                            label={field.name}
                            value={String(inputValues[field.name] ?? '')}
                            onChange={(event) => setInputJson(updateJsonField(inputJson, field.name, event.target.value))}
                            helperText={field.required ? 'Required by agent prompt' : 'Optional'}
                            fullWidth
                          />
                        </Grid>
                      ))}
                    </Grid>
                  </Box>
                )}
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
          <Paper sx={{ p: 3, minHeight: 430 }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
              <Box>
                <Typography variant="h5">Runtime Pulse</Typography>
                <Typography variant="body2" color="text.secondary">
                  Live health from your latest missions.
                </Typography>
              </Box>
              <Chip color="success" variant="outlined" label={`${statusCounts.successRate}% success`} />
            </Stack>
            <Stack spacing={2.1} sx={{ mt: 3 }}>
              <Pulse label="Completed" value={statusCounts.completed} max={recentExecutions.length || 1} color="success" />
              <Pulse label="Running / Pending" value={statusCounts.running} max={recentExecutions.length || 1} color="primary" />
              <Pulse label="Failed" value={statusCounts.failed} max={recentExecutions.length || 1} color="error" />
            </Stack>
            <Grid container spacing={1.3} sx={{ mt: 3 }}>
              <Grid size={6}>
                <MiniStat label="Average runtime" value={formatDuration(statusCounts.averageRuntimeMs)} />
              </Grid>
              <Grid size={6}>
                <MiniStat label="Active missions" value={String(statusCounts.running)} />
              </Grid>
            </Grid>
            <Divider sx={{ my: 2.5 }} />
            <Typography variant="body2" sx={{ fontWeight: 900, mb: 1.4 }}>
              Recent Executions
            </Typography>
            <Stack spacing={1}>
              {recentExecutions.length === 0 ? (
                <Box sx={{ p: 2, border: '1px dashed', borderColor: 'divider', borderRadius: 2 }}>
                  <Typography variant="body2" color="text.secondary">
                    No missions yet. Start a workflow to light up this feed.
                  </Typography>
                </Box>
              ) : (
                recentExecutions.map((execution) => (
                  <Box
                    key={execution.id}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.4,
                      p: 1.2,
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 2,
                      bgcolor: alpha(theme.palette.background.default, 0.38),
                    }}
                  >
                    <Box sx={{ flex: 1, minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 850 }} noWrap>
                        {execution.targetType}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
                        {formatAgo(execution.createdAt)} • {execution.provider ?? 'Platform'}
                      </Typography>
                    </Box>
                    <Chip size="small" color={statusChipColor(execution.status)} label={execution.status} />
                  </Box>
                ))
              )}
            </Stack>
            <Box sx={{ mt: 2.5 }}>
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                Demo catalog
              </Typography>
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                <Chip size="small" label={`${catalog.data?.tools.length ?? 0} tools`} />
                <Chip size="small" label={`${catalog.data?.agents.length ?? 0} agents`} />
                <Chip size="small" label={`${catalog.data?.workflows.length ?? 0} workflows`} />
              </Stack>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

function getAgentInputFields(agent?: Agent) {
  if (!agent?.inputSchemaJson) {
    return []
  }

  try {
    const schema = JSON.parse(agent.inputSchemaJson) as {
      properties?: Record<string, unknown>
      required?: string[]
    }
    const required = new Set(schema.required ?? [])
    return Object.keys(schema.properties ?? {}).map((name) => ({ name, required: required.has(name) }))
  } catch {
    return []
  }
}

function parseInputJson(inputJson: string): Record<string, unknown> {
  try {
    const parsed = JSON.parse(inputJson)
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {}
  } catch {
    return {}
  }
}

function updateJsonField(inputJson: string, fieldName: string, value: string) {
  const parsed = parseInputJson(inputJson)
  return JSON.stringify({ ...parsed, [fieldName]: value }, null, 2)
}

function Pulse({ label, value, max, color }: { label: string; value: number; max: number; color: 'primary' | 'success' | 'error' }) {
  const progress = Math.min(100, Math.round((value / Math.max(max, 1)) * 100))
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

function MiniStat({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" sx={{ mt: 0.3 }}>
        {value}
      </Typography>
    </Box>
  )
}

function formatDuration(durationMs: number) {
  if (!durationMs) return '0 sec'
  if (durationMs < 1000) return `${durationMs} ms`
  return `${(durationMs / 1000).toFixed(durationMs < 10000 ? 1 : 0)} sec`
}

function formatAgo(date: string) {
  const diffMs = Date.now() - new Date(date).getTime()
  const minutes = Math.max(0, Math.floor(diffMs / 60000))
  if (minutes < 1) return 'just now'
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} hr ago`
  return `${Math.floor(hours / 24)} d ago`
}

function statusChipColor(status: string): 'default' | 'primary' | 'success' | 'error' | 'warning' {
  if (status === 'Completed') return 'success'
  if (status === 'Failed') return 'error'
  if (status === 'Running' || status === 'Pending') return 'primary'
  return 'default'
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
