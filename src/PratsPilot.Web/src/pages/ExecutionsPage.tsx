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
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import ReplayIcon from '@mui/icons-material/Replay'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiClient } from '../api/client'
import type { Execution, ExecutionLog } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'

type WorkflowStepOutput = {
  stepName?: string
  order?: number
  stepType?: string
  input?: unknown
  output?: unknown
  error?: string
}

type ParsedExecutionOutput = {
  finalText: string
  finalJson?: unknown
  steps: WorkflowStepOutput[]
  raw: string
}

export function ExecutionsPage() {
  const queryClient = useQueryClient()
  const [pageSize, setPageSize] = useState(10)
  const executions = useQuery({ queryKey: ['executions', pageSize], queryFn: () => apiClient.getExecutions(pageSize), refetchInterval: 4000 })
  const [selectedId, setSelectedId] = useState('')

  const retry = useMutation({
    mutationFn: (id: string) => apiClient.retryExecution(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['executions'] })
    },
  })

  const selected = useMemo(
    () => executions.data?.items.find((execution) => execution.id === selectedId) ?? executions.data?.items[0],
    [executions.data, selectedId],
  )
  const parsed = selected ? parseExecutionOutput(selected) : undefined

  return (
    <Box>
      <SectionHeader eyebrow="Telemetry" title="Execution history and outputs" />
      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 7 }}>
          <DataPanel<Execution>
            title="Recent Executions"
            subtitle={`Showing latest ${pageSize} runs`}
            rows={executions.data?.items ?? []}
            loading={executions.isLoading}
            columns={[
              {
                key: 'id',
                label: 'Execution',
                render: (row) => (
                  <Box onClick={() => setSelectedId(row.id)} sx={{ cursor: 'pointer' }}>
                    <Typography sx={{ fontWeight: 900 }}>{row.targetType}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {row.id}
                    </Typography>
                    {row.errorMessage && (
                      <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.4 }}>
                        {row.errorMessage}
                      </Typography>
                    )}
                  </Box>
                ),
              },
              {
                key: 'status',
                label: 'Status',
                render: (row) => (
                  <Chip
                    size="small"
                    color={row.status === 'Completed' ? 'success' : row.status === 'Failed' ? 'error' : 'primary'}
                    label={row.status}
                  />
                ),
              },
              { key: 'created', label: 'Created', render: (row) => new Date(row.createdAt).toLocaleString() },
              {
                key: 'actions',
                label: 'Actions',
                render: (row) => (
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<ReplayIcon />}
                    onClick={() => retry.mutate(row.id)}
                    disabled={row.status !== 'Failed' || retry.isPending}
                  >
                    Retry
                  </Button>
                ),
              },
            ]}
          />
          <TextField
            select
            size="small"
            label="History Size"
            value={pageSize}
            onChange={(event) => setPageSize(Number(event.target.value))}
            sx={{ mt: 2, width: 180 }}
          >
            <MenuItem value={10}>Latest 10</MenuItem>
            <MenuItem value={25}>Latest 25</MenuItem>
            <MenuItem value={100}>Latest 100</MenuItem>
          </TextField>
        </Grid>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3, minHeight: 500 }}>
            <Typography variant="h5">Output Viewer</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Select an execution to inspect final output, step outputs, and runtime logs.
            </Typography>

            {!selected && <Chip label={executions.isLoading ? 'Loading executions...' : 'No execution selected'} />}
            {selected && parsed && (
              <Stack spacing={2}>
                <Paper variant="outlined" sx={{ p: 2, bgcolor: 'rgba(54,211,201,0.05)' }}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
                    <Typography variant="h6">Final Output</Typography>
                    <Stack direction="row" sx={{ gap: 1, alignItems: 'center' }}>
                      <Chip size="small" color={selected.status === 'Completed' ? 'success' : selected.status === 'Failed' ? 'error' : 'primary'} label={selected.status} />
                      {selected.status === 'Failed' && (
                        <Button size="small" variant="contained" startIcon={<ReplayIcon />} onClick={() => retry.mutate(selected.id)} disabled={retry.isPending}>
                          Retry
                        </Button>
                      )}
                    </Stack>
                  </Stack>
                  {selected.errorMessage && <Alert severity="error" sx={{ mt: 1.5 }}>{selected.errorMessage}</Alert>}
                  <Typography sx={{ mt: 1.5, whiteSpace: 'pre-wrap', lineHeight: 1.7 }}>
                    {parsed.finalText || 'No final output has been persisted yet.'}
                  </Typography>
                </Paper>

                {parsed.steps.length > 0 && (
                  <Box>
                    <Typography variant="h6" sx={{ mb: 1 }}>Agent / Step Outputs</Typography>
                    <Stack spacing={1.2}>
                      {parsed.steps.map((step, index) => (
                        <Accordion key={`${step.stepName}-${index}`} disableGutters>
                          <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                            <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                              <Chip size="small" color={step.error ? 'error' : step.stepType === 'Agent' ? 'primary' : 'secondary'} label={`${step.order ?? index + 1}. ${step.stepType ?? 'Step'}`} />
                              <Typography sx={{ fontWeight: 900 }}>{step.stepName ?? `Step ${index + 1}`}</Typography>
                            </Stack>
                          </AccordionSummary>
                          <AccordionDetails>
                            {step.error && <Alert severity="error" sx={{ mb: 1.2 }}>{step.error}</Alert>}
                            <Typography variant="body2" color="text.secondary" sx={{ mb: 0.8 }}>Output</Typography>
                            <Typography sx={{ whiteSpace: 'pre-wrap', lineHeight: 1.7 }}>
                              {extractText(step.output) || formatValue(step.output)}
                            </Typography>
                            <Accordion sx={{ mt: 1.5 }}>
                              <AccordionSummary expandIcon={<ExpandMoreIcon />}>Input / raw step JSON</AccordionSummary>
                              <AccordionDetails>
                                <TextField value={formatValue({ input: step.input, output: step.output })} multiline minRows={6} fullWidth sx={monoSx} />
                              </AccordionDetails>
                            </Accordion>
                          </AccordionDetails>
                        </Accordion>
                      ))}
                    </Stack>
                  </Box>
                )}

                <Accordion>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>Raw output JSON</AccordionSummary>
                  <AccordionDetails>
                    <TextField value={parsed.raw} multiline minRows={10} fullWidth sx={monoSx} />
                  </AccordionDetails>
                </Accordion>

                <Box>
                  <Typography variant="h6" sx={{ mb: 1 }}>Runtime Logs</Typography>
                  <Stack spacing={1.2}>
                    {(selected.logs ?? []).map((log) => (
                      <LogAccordion key={log.id} log={log} />
                    ))}
                  </Stack>
                </Box>
              </Stack>
            )}
          </Paper>
        </Grid>
      </Grid>
      <Snackbar open={retry.isSuccess} autoHideDuration={3000} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
        <Alert severity="success" variant="filled">Retry queued.</Alert>
      </Snackbar>
      <Snackbar open={retry.isError} autoHideDuration={4500} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
        <Alert severity="error" variant="filled">Retry failed. Check login permissions and API logs.</Alert>
      </Snackbar>
    </Box>
  )
}

function LogAccordion({ log }: { log: ExecutionLog }) {
  return (
    <Accordion disableGutters>
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', gap: 1, width: '100%' }}>
          <Stack direction="row" sx={{ gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
            <Chip size="small" label={log.level} color={log.level === 'Error' ? 'error' : log.level === 'Warning' ? 'warning' : 'default'} />
            <Typography variant="body2">{log.message}</Typography>
          </Stack>
          <Typography variant="caption" color="text.secondary">
            {new Date(log.createdAt).toLocaleTimeString()}
          </Typography>
        </Stack>
      </AccordionSummary>
      <AccordionDetails>
        <TextField value={log.detailsJson ? formatJson(log.detailsJson) : 'No details recorded for this log.'} multiline minRows={4} fullWidth sx={monoSx} />
      </AccordionDetails>
    </Accordion>
  )
}

function parseExecutionOutput(execution: Execution): ParsedExecutionOutput {
  const raw = execution.outputJson ? formatJson(execution.outputJson) : ''
  const parsed = tryParse(execution.outputJson)

  if (!parsed || typeof parsed !== 'object') {
    return { finalText: execution.outputJson ?? '', steps: [], raw }
  }

  const record = parsed as Record<string, unknown>
  const finalJson = record.finalOutput ?? record.output ?? parsed
  const steps = Array.isArray(record.steps) ? (record.steps as WorkflowStepOutput[]) : []

  return {
    finalText: extractText(finalJson),
    finalJson,
    steps,
    raw,
  }
}

function extractText(value: unknown): string {
  if (typeof value === 'string') return value
  if (!value || typeof value !== 'object') return ''

  const record = value as Record<string, unknown>
  for (const key of ['output', 'content', 'finalOutput', 'result', 'text']) {
    if (typeof record[key] === 'string') return record[key] as string
  }

  return formatValue(value)
}

function formatJson(value: string) {
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

function formatValue(value: unknown) {
  if (typeof value === 'string') return value
  return JSON.stringify(value ?? {}, null, 2)
}

function tryParse(value?: string) {
  if (!value) return undefined
  try {
    return JSON.parse(value)
  } catch {
    return undefined
  }
}

const monoSx = {
  '& textarea': {
    fontFamily: 'ui-monospace, Consolas, monospace',
    fontSize: 13,
    lineHeight: 1.65,
  },
}
