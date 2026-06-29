import { Alert, Box, Button, Chip, Grid, Paper, Stack, TextField, Typography } from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import SaveIcon from '@mui/icons-material/Save'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import type { DragEvent } from 'react'
import { apiClient } from '../api/client'
import type { Workflow } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'

type BuilderStep = {
  id: string
  type: 'Agent' | 'Tool'
  name: string
}

export function WorkflowsPage() {
  const queryClient = useQueryClient()
  const workflows = useQuery({ queryKey: ['workflows'], queryFn: apiClient.getWorkflows })
  const agents = useQuery({ queryKey: ['agents'], queryFn: apiClient.getAgents })
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const [workflowName, setWorkflowName] = useState('')
  const [workflowDescription, setWorkflowDescription] = useState('')
  const [editingWorkflowId, setEditingWorkflowId] = useState<string | undefined>()
  const [search, setSearch] = useState('')
  const [steps, setSteps] = useState<BuilderStep[]>([])

  const searchableAgents = useMemo(() => {
    const value = search.toLowerCase()
    return (agents.data?.items ?? []).filter((agent) =>
      [agent.name, agent.projectName, agent.role, agent.tags].some((field) => field?.toLowerCase().includes(value)),
    )
  }, [agents.data, search])

  const searchableTools = useMemo(() => {
    const value = search.toLowerCase()
    return (tools.data?.items ?? []).filter((tool) => [tool.name, tool.category].some((field) => field?.toLowerCase().includes(value)))
  }, [tools.data, search])

  const createWorkflow = useMutation({
    mutationFn: async () => {
      const workflow = await apiClient.createWorkflow({
        name: workflowName,
        description: workflowDescription,
        status: 'Active',
      })

      for (const [index, step] of steps.entries()) {
        await apiClient.createWorkflowStep(workflow.id, {
          name: step.name,
          order: index + 1,
          stepType: step.type,
          agentId: step.type === 'Agent' ? step.id : undefined,
          toolId: step.type === 'Tool' ? step.id : undefined,
          inputMappingJson: index === 0 ? '{"source":"original"}' : '{}',
          configurationJson: '{}',
          continueOnError: false,
        })
      }

      return workflow
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] })
      resetWorkflowForm()
    },
  })

  const updateWorkflow = useMutation({
    mutationFn: () => apiClient.updateWorkflow(editingWorkflowId!, {
      name: workflowName,
      description: workflowDescription,
      status: 'Active',
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] })
      resetWorkflowForm()
    },
  })

  const deleteWorkflow = useMutation({
    mutationFn: (id: string) => apiClient.deleteWorkflow(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workflows'] })
    },
  })

  function dragStart(event: DragEvent, step: BuilderStep) {
    event.dataTransfer.setData('application/json', JSON.stringify(step))
  }

  function dropStep(event: DragEvent) {
    event.preventDefault()
    const raw = event.dataTransfer.getData('application/json')
    if (!raw) return
    const step = JSON.parse(raw) as BuilderStep
    setSteps((current) => [...current, step])
  }

  function resetWorkflowForm() {
    setWorkflowName('')
    setWorkflowDescription('')
    setEditingWorkflowId(undefined)
    setSteps([])
  }

  function editWorkflow(workflow: Workflow) {
    setEditingWorkflowId(workflow.id)
    setWorkflowName(workflow.name)
    setWorkflowDescription(workflow.description ?? '')
    setSteps([])
  }

  return (
    <Box>
      <SectionHeader eyebrow="Workflow Builder" title={editingWorkflowId ? 'Edit workflow details' : 'Search agents, drag steps, create orchestration'} />
      <Grid container spacing={2.5} sx={{ mb: 2.5 }}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h5">Agent and Tool Search</Typography>
            <TextField label="Search by project, role, tag, category" value={search} onChange={(e) => setSearch(e.target.value)} fullWidth sx={{ my: 2 }} />
            <Stack spacing={1.2}>
              {searchableAgents.map((agent) => (
                <StepCard key={agent.id} id={agent.id} type="Agent" name={agent.name} meta={agent.projectName || agent.role || 'Agent'} onDragStart={dragStart} />
              ))}
              {searchableTools.map((tool) => (
                <StepCard key={tool.id} id={tool.id} type="Tool" name={tool.name} meta={tool.category} onDragStart={dragStart} />
              ))}
            </Stack>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h5">{editingWorkflowId ? 'Workflow Details' : 'Workflow Canvas'}</Typography>
            <Grid container spacing={2} sx={{ mt: 0.5 }}>
              <Grid size={{ xs: 12, md: 6 }}>
                <TextField label="Workflow Name" value={workflowName} onChange={(e) => setWorkflowName(e.target.value)} fullWidth />
              </Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <TextField label="Description" value={workflowDescription} onChange={(e) => setWorkflowDescription(e.target.value)} fullWidth />
              </Grid>
            </Grid>
            {!editingWorkflowId && <Box
              onDragOver={(event) => event.preventDefault()}
              onDrop={dropStep}
              sx={{
                mt: 2,
                minHeight: 220,
                border: '1px dashed',
                borderColor: 'primary.main',
                borderRadius: 2,
                p: 2,
                bgcolor: 'rgba(54,211,201,0.05)',
              }}
            >
              {steps.length === 0 ? (
                <Typography color="text.secondary">Drag agents or tools here to build the workflow path.</Typography>
              ) : (
                <Stack spacing={1.2}>
                  {steps.map((step, index) => (
                    <Box key={`${step.type}-${step.id}-${index}`} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Chip color={step.type === 'Agent' ? 'primary' : 'secondary'} label={`${index + 1}. ${step.type}`} />
                      <Typography sx={{ flex: 1, fontWeight: 800 }}>{step.name}</Typography>
                      <Button size="small" onClick={() => setSteps((current) => current.filter((_, itemIndex) => itemIndex !== index))}>
                        Remove
                      </Button>
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>}
            {editingWorkflowId && (
              <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                Step editing will be handled in the workflow step builder. This edit updates the workflow name and description.
              </Typography>
            )}
            <Button
              sx={{ mt: 2 }}
              variant="contained"
              startIcon={<SaveIcon />}
              onClick={() => (editingWorkflowId ? updateWorkflow.mutate() : createWorkflow.mutate())}
              disabled={!workflowName || (!editingWorkflowId && steps.length === 0) || createWorkflow.isPending || updateWorkflow.isPending}
            >
              {editingWorkflowId ? 'Save Workflow' : 'Create Workflow'}
            </Button>
            {editingWorkflowId && (
              <Button sx={{ mt: 2, ml: 1 }} variant="outlined" onClick={resetWorkflowForm}>
                Cancel
              </Button>
            )}
            {(createWorkflow.isError || updateWorkflow.isError || deleteWorkflow.isError) && (
              <Alert severity="error" sx={{ mt: 2 }}>
                Workflow action failed. It may still be referenced by existing executions.
              </Alert>
            )}
          </Paper>
        </Grid>
      </Grid>
      <DataPanel<Workflow>
        title="Workflows"
        subtitle="Each workflow can chain tool and agent steps with mapped step inputs."
        rows={workflows.data?.items ?? []}
        loading={workflows.isLoading}
        columns={[
          {
            key: 'name',
            label: 'Workflow',
            render: (row) => (
              <Box>
                <Typography sx={{ fontWeight: 900 }}>{row.name}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {row.description || 'No description'}
                </Typography>
              </Box>
            ),
          },
          { key: 'status', label: 'Status', render: (row) => <Chip size="small" color="success" label={row.status} /> },
          {
            key: 'steps',
            label: 'Steps',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                {(row.steps ?? []).length ? row.steps.map((step) => <Chip key={step.id} size="small" label={`${step.order}. ${step.stepType}`} />) : <Chip size="small" label="Open details to view" />}
              </Stack>
            ),
          },
          { key: 'id', label: 'ID', render: (row) => <Typography variant="caption">{row.id}</Typography> },
          {
            key: 'actions',
            label: 'Actions',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                <Button size="small" variant="outlined" startIcon={<EditIcon />} onClick={() => editWorkflow(row)}>
                  Edit
                </Button>
                <Button
                  size="small"
                  color="error"
                  variant="outlined"
                  startIcon={<DeleteIcon />}
                  onClick={() => window.confirm(`Delete workflow "${row.name}"?`) && deleteWorkflow.mutate(row.id)}
                >
                  Delete
                </Button>
              </Stack>
            ),
          },
        ]}
      />
    </Box>
  )
}

function StepCard({
  id,
  type,
  name,
  meta,
  onDragStart,
}: {
  id: string
  type: 'Agent' | 'Tool'
  name: string
  meta: string
  onDragStart: (event: DragEvent, step: BuilderStep) => void
}) {
  return (
    <Box
      draggable
      onDragStart={(event) => onDragStart(event, { id, type, name })}
      sx={{
        border: '1px solid',
        borderColor: 'divider',
        borderRadius: 2,
        p: 1.5,
        cursor: 'grab',
        bgcolor: 'rgba(255,255,255,0.035)',
      }}
    >
      <Stack direction="row" sx={{ gap: 1, alignItems: 'center' }}>
        <Chip size="small" label={type} color={type === 'Agent' ? 'primary' : 'secondary'} />
        <Box>
          <Typography sx={{ fontWeight: 900 }}>{name}</Typography>
          <Typography variant="caption" color="text.secondary">
            {meta}
          </Typography>
        </Box>
      </Stack>
    </Box>
  )
}
