import { Alert, Autocomplete, Box, Button, Checkbox, Chip, Grid, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import SaveIcon from '@mui/icons-material/Save'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState } from 'react'
import { apiClient } from '../api/client'
import { fallbackModelCatalog, providerDefaults } from '../api/modelCatalog'
import type { Agent, Tool } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'
import { useAuth } from '../state/AuthContext'

export function AgentsPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const agents = useQuery({ queryKey: ['agents'], queryFn: apiClient.getAgents })
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const contextDocuments = useQuery({ queryKey: ['contextDocuments'], queryFn: apiClient.getContextDocuments })
  const [projectFilter, setProjectFilter] = useState('')
  const [editingAgentId, setEditingAgentId] = useState<string | undefined>()
  const [selectedToolIds, setSelectedToolIds] = useState<string[]>([])
  const [selectedContextDocumentIds, setSelectedContextDocumentIds] = useState<string[]>([])
  const formRef = useRef<HTMLDivElement | null>(null)
  const [form, setForm] = useState({
    name: '',
    projectName: '',
    role: '',
    goal: '',
    expectedOutput: '',
    tags: '',
    description: '',
    useGlobalAISettings: true,
    aiProvider: 'Gemini',
    aiModel: providerDefaults.Gemini.model,
    aiBaseUrl: providerDefaults.Gemini.baseUrl,
    aiTemperature: 0.2,
  })
  const models = useQuery({
    queryKey: ['llmModels', 'agent', form.aiProvider],
    queryFn: () => apiClient.getModels(form.aiProvider, true),
    enabled: !form.useGlobalAISettings,
  })

  useEffect(() => {
    const availableOptions = models.data?.length
      ? models.data.map((model) => model.id)
      : fallbackModelCatalog[form.aiProvider] ?? []

    if (form.useGlobalAISettings || !availableOptions.length || availableOptions.includes(form.aiModel)) {
      return
    }

    setForm((current) => ({ ...current, aiModel: availableOptions[0] }))
  }, [form.aiModel, form.aiProvider, form.useGlobalAISettings, models.data, models.isError])

  const createAgent = useMutation({
    mutationFn: async () => {
      const agent = await apiClient.createAgent(buildAgentRequest(form))
      await apiClient.setAgentTools(agent.id, selectedToolIds)
      await apiClient.setAgentContextDocuments(agent.id, selectedContextDocumentIds)
      return agent
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] })
      resetForm()
    },
  })

  const updateAgent = useMutation({
    mutationFn: async () => {
      const agent = await apiClient.updateAgent(editingAgentId!, buildAgentRequest(form))
      await apiClient.setAgentTools(agent.id, selectedToolIds)
      await apiClient.setAgentContextDocuments(agent.id, selectedContextDocumentIds)
      return agent
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] })
      resetForm()
    },
  })

  const deleteAgent = useMutation({
    mutationFn: (id: string) => apiClient.deleteAgent(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] })
    },
  })

  const filteredAgents = useMemo(() => {
    const items = agents.data?.items ?? []
    if (!projectFilter.trim()) return items
    return items.filter((agent) => agent.projectName?.toLowerCase().includes(projectFilter.toLowerCase()))
  }, [agents.data, projectFilter])

  function setProvider(provider: string) {
    const defaults = providerDefaults[provider]
    setForm((current) => ({ ...current, aiProvider: provider, aiModel: defaults.model, aiBaseUrl: defaults.baseUrl }))
  }

  function resetForm() {
    setEditingAgentId(undefined)
    setForm((current) => ({
      ...current,
      name: '',
      projectName: '',
      role: '',
      goal: '',
      expectedOutput: '',
      tags: '',
      description: '',
      useGlobalAISettings: true,
    }))
    setSelectedToolIds([])
    setSelectedContextDocumentIds([])
  }

  function editAgent(agent: Agent) {
    setEditingAgentId(agent.id)
    setForm({
      name: agent.name,
      projectName: agent.projectName ?? '',
      role: agent.role ?? '',
      goal: agent.goal ?? '',
      expectedOutput: agent.expectedOutput ?? '',
      tags: agent.tags ?? '',
      description: agent.description ?? '',
      useGlobalAISettings: agent.useGlobalAISettings,
      aiProvider: agent.aiProvider ?? 'Gemini',
      aiModel: agent.aiModel ?? providerDefaults.Gemini.model,
      aiBaseUrl: '',
      aiTemperature: 0.2,
    })
    setSelectedToolIds(agent.toolIds ?? [])
    setSelectedContextDocumentIds(agent.contextDocumentIds ?? [])
    window.setTimeout(() => formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0)
  }

  const liveModelOptions = models.data?.filter((model) => model.provider === form.aiProvider) ?? []
  const fallbackOptions = fallbackModelCatalog[form.aiProvider] ?? []
  const modelOptions = liveModelOptions.length ? liveModelOptions.map((model) => model.id) : fallbackOptions
  const allModelOptions = modelOptions.length ? modelOptions : [form.aiModel].filter(Boolean)
  const modelLabels = new Map(liveModelOptions.map((model) => [model.id, model.name]))
  const toolOptions = useMemo(() => sortTools(tools.data?.items ?? []), [tools.data])
  const latestCustomToolIds = useMemo(() => new Set(getLatestCustomTools(tools.data?.items ?? []).map((tool) => tool.id)), [tools.data])
  const selectedTools = toolOptions.filter((tool) => selectedToolIds.includes(tool.id))
  const selectedContextDocuments = (contextDocuments.data ?? []).filter((document) => selectedContextDocumentIds.includes(document.id))
  const inputFields = extractInputFields([form.description, form.goal, form.expectedOutput].join('\n'))
  const canModify = (agent: Agent) => Boolean(user?.roles.includes('Admin') || !agent.createdByUserId || agent.createdByUserId === user?.userId)

  return (
    <Box>
      <SectionHeader eyebrow="Agent Builder" title={editingAgentId ? 'Edit specialized agent' : 'Create specialized agents for projects and workflows'} />
      <Paper ref={formRef} sx={{ p: 3, mb: 2.5 }}>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField label="Agent Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} fullWidth />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField label="Project Name" value={form.projectName} onChange={(e) => setForm({ ...form, projectName: e.target.value })} fullWidth />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField label="Role" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })} fullWidth />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField label="Goal" value={form.goal} onChange={(e) => setForm({ ...form, goal: e.target.value })} multiline minRows={4} fullWidth helperText="Use {{fieldName}} to request dynamic input fields." sx={resizableTextAreaSx} />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField label="Expected Output" value={form.expectedOutput} onChange={(e) => setForm({ ...form, expectedOutput: e.target.value })} multiline minRows={4} fullWidth helperText="Example: summarize {{documentTopic}} for {{audience}}." sx={resizableTextAreaSx} />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} multiline minRows={5} fullWidth helperText={inputFields.length ? `Input fields: ${inputFields.join(', ')}` : 'Type {{ to define custom input fields.'} sx={resizableTextAreaSx} />
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <TextField label="Tags" value={form.tags} onChange={(e) => setForm({ ...form, tags: e.target.value })} multiline minRows={2} fullWidth placeholder="qa, playwright, regression" sx={resizableTextAreaSx} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField select label="AI Mode" value={form.useGlobalAISettings ? 'global' : 'custom'} onChange={(e) => setForm({ ...form, useGlobalAISettings: e.target.value === 'global' })} fullWidth>
              <MenuItem value="global">Use global settings</MenuItem>
              <MenuItem value="custom">Override for this agent</MenuItem>
            </TextField>
          </Grid>
          {!form.useGlobalAISettings && (
            <>
              <Grid size={{ xs: 12, md: 4 }}>
                <TextField select label="Provider" value={form.aiProvider} onChange={(e) => setProvider(e.target.value)} fullWidth>
                  <MenuItem value="Gemini">Gemini</MenuItem>
                  <MenuItem value="Groq">Groq</MenuItem>
                  <MenuItem value="Cerebras">Cerebras</MenuItem>
                  <MenuItem value="OpenRouter">OpenRouter</MenuItem>
                </TextField>
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <TextField select label="Model" value={form.aiModel} onChange={(e) => setForm({ ...form, aiModel: e.target.value })} fullWidth>
                  {allModelOptions.map((model) => (
                    <MenuItem key={model} value={model}>
                      {modelLabels.get(model) ?? model}
                    </MenuItem>
                  ))}
                </TextField>
                {form.aiProvider === 'OpenRouter' && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.8 }}>
                    OpenRouter uses Auto Free by default. Free model availability can still change or rate limit.
                  </Typography>
                )}
              </Grid>
            </>
          )}
          <Grid size={12}>
            <Typography variant="body2" sx={{ fontWeight: 900, mb: 1 }}>
              Optional Tools
            </Typography>
            <Autocomplete
              multiple
              disableCloseOnSelect
              loading={tools.isLoading}
              options={toolOptions}
              value={selectedTools}
              groupBy={(option) => {
                if (isBuiltInTool(option)) return 'Built-in tools'
                if (latestCustomToolIds.has(option.id)) return 'Latest custom tools'
                return 'All custom tools'
              }}
              getOptionLabel={(option) => `${option.name} (${option.category})`}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              onChange={(_, value) => setSelectedToolIds(value.map((tool) => tool.id))}
              renderOption={(props, option, { selected }) => {
                const { key, ...optionProps } = props
                return (
                  <Box component="li" key={key} {...optionProps}>
                    <Checkbox checked={selected} sx={{ mr: 1 }} />
                    <Box sx={{ minWidth: 0 }}>
                      <Typography variant="body2" sx={{ fontWeight: 800 }}>
                        {option.name}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {option.category}{latestCustomToolIds.has(option.id) && !isBuiltInTool(option) ? ' - recently created' : ''}
                      </Typography>
                    </Box>
                  </Box>
                )
              }}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Search and select tools"
                  placeholder={selectedTools.length ? '' : 'Search by tool name or category...'}
                  helperText="Built-ins stay first, followed by the latest five custom tools. Type to search all tools."
                />
              )}
            />
          </Grid>
          <Grid size={12}>
            <Typography variant="body2" sx={{ fontWeight: 900, mb: 1 }}>
              Context Documents
            </Typography>
            <Autocomplete
              multiple
              loading={contextDocuments.isLoading}
              options={contextDocuments.data ?? []}
              value={selectedContextDocuments}
              getOptionLabel={(option) => `${option.name} (${option.fileExtension})`}
              isOptionEqualToValue={(option, value) => option.id === value.id}
              onChange={(_, value) => setSelectedContextDocumentIds(value.map((document) => document.id))}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label="Attach context documents"
                  placeholder={selectedContextDocuments.length ? '' : 'Project docs, specs, PDFs, Markdown...'}
                  helperText="Upload documents in Context Library, then attach them here."
                />
              )}
            />
          </Grid>
          <Grid size={12}>
            <Stack direction="row" sx={{ gap: 1.2, flexWrap: 'wrap', alignItems: 'center' }}>
              <Button
                variant="contained"
                startIcon={editingAgentId ? <SaveIcon /> : <AddIcon />}
                onClick={() => (editingAgentId ? updateAgent.mutate() : createAgent.mutate())}
                disabled={!form.name || createAgent.isPending || updateAgent.isPending}
              >
                {editingAgentId ? 'Save Agent' : 'Create Agent'}
              </Button>
              {editingAgentId && (
                <Button variant="outlined" onClick={resetForm}>
                  Cancel
                </Button>
              )}
              {(createAgent.isError || updateAgent.isError || deleteAgent.isError) && (
                <Alert severity="error">Agent action failed. It may still be referenced by a workflow or execution.</Alert>
              )}
            </Stack>
          </Grid>
        </Grid>
      </Paper>
      <TextField label="Filter by project" value={projectFilter} onChange={(e) => setProjectFilter(e.target.value)} sx={{ mb: 2, width: { xs: '100%', md: 360 } }} />
      <DataPanel<Agent>
        title="Agents"
        subtitle="Agents can inherit global provider settings or override them individually."
        rows={filteredAgents}
        loading={agents.isLoading}
        columns={[
          {
            key: 'name',
            label: 'Name',
            render: (row) => (
              <Box>
                <Typography sx={{ fontWeight: 900 }}>{row.name}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {row.projectName || 'No project'} · {row.role || row.description || 'No role'}
                </Typography>
              </Box>
            ),
          },
          { key: 'status', label: 'Status', render: (row) => <Chip size="small" color="success" label={row.status} /> },
          {
            key: 'provider',
            label: 'Provider',
            render: (row) => (
              <Stack spacing={0.5}>
                <Typography variant="body2">{row.useGlobalAISettings ? 'Global settings' : row.aiProvider}</Typography>
                <Typography variant="caption" color="text.secondary">
                  {row.aiModel || row.modelName}
                </Typography>
                {(row.toolNames ?? []).length > 0 && (
                  <Typography variant="caption" color="text.secondary">
                    Tools: {row.toolNames.join(', ')}
                  </Typography>
                )}
                {(row.contextDocumentNames ?? []).length > 0 && (
                  <Typography variant="caption" color="text.secondary">
                    Context: {row.contextDocumentNames.join(', ')}
                  </Typography>
                )}
              </Stack>
            ),
          },
          {
            key: 'created',
            label: 'Created',
            render: (row) => new Date(row.createdAt).toLocaleString(),
          },
          {
            key: 'owner',
            label: 'Owner',
            render: (row) => row.createdByDisplayName || 'system',
          },
          {
            key: 'actions',
            label: 'Actions',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                {canModify(row) ? (
                  <>
                    <Button size="small" variant="outlined" startIcon={<EditIcon />} onClick={() => editAgent(row)}>
                      Edit
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      variant="outlined"
                      startIcon={<DeleteIcon />}
                      onClick={() => window.confirm(`Delete agent "${row.name}"?`) && deleteAgent.mutate(row.id)}
                    >
                      Delete
                    </Button>
                  </>
                ) : (
                  <Chip size="small" label="View only" />
                )}
              </Stack>
            ),
          },
        ]}
      />
    </Box>
  )
}

const builtInCategories = new Set(['Calculator', 'Http', 'REST API', 'WebSearch', 'FileReader'])

function isBuiltInTool(tool: Tool) {
  return builtInCategories.has(tool.category)
}

function sortTools(tools: Tool[]) {
  const latestCustomToolIds = new Set(getLatestCustomTools(tools).map((tool) => tool.id))

  return [...tools].sort((left, right) => {
    const groupCompare = Number(isBuiltInTool(right)) - Number(isBuiltInTool(left))
    if (groupCompare !== 0) return groupCompare

    const latestCompare = Number(latestCustomToolIds.has(right.id)) - Number(latestCustomToolIds.has(left.id))
    if (latestCompare !== 0) return latestCompare

    if (latestCustomToolIds.has(left.id) && latestCustomToolIds.has(right.id)) {
      return new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime()
    }

    return left.name.localeCompare(right.name)
  })
}

function getLatestCustomTools(tools: Tool[]) {
  return tools
    .filter((tool) => !isBuiltInTool(tool))
    .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
    .slice(0, 5)
}

const resizableTextAreaSx = {
  '& textarea': {
    resize: 'vertical',
  },
}

function buildAgentRequest(form: {
  name: string
  description?: string
  projectName?: string
  role?: string
  goal?: string
  expectedOutput?: string
  tags?: string
  useGlobalAISettings: boolean
  aiProvider: string
  aiModel: string
  aiBaseUrl?: string
  aiTemperature?: number
}) {
  return {
    ...form,
    modelProvider: form.useGlobalAISettings ? 'Global' : form.aiProvider,
    modelName: form.useGlobalAISettings ? 'Global default' : form.aiModel,
    modelConfigJson: '{}',
    inputSchemaJson: buildInputSchema([form.description, form.goal, form.expectedOutput].join('\n')),
    aiProvider: form.useGlobalAISettings ? undefined : form.aiProvider,
    aiModel: form.useGlobalAISettings ? undefined : form.aiModel,
    aiBaseUrl: form.useGlobalAISettings ? undefined : form.aiBaseUrl,
    aiTemperature: form.useGlobalAISettings ? undefined : form.aiTemperature,
    aiMaxTokens: 2048,
    aiTopP: 0.9,
    aiSystemPrompt: buildSystemPrompt(form.role, form.goal, form.expectedOutput),
    status: 'Active',
  }
}

function extractInputFields(value: string) {
  return Array.from(value.matchAll(/\{\{\s*([a-zA-Z][a-zA-Z0-9_]*)\s*\}\}/g))
    .map((match) => match[1])
    .filter((field, index, fields) => fields.indexOf(field) === index)
}

function buildInputSchema(value: string) {
  const fields = extractInputFields(value)
  return JSON.stringify({
    type: 'object',
    properties: Object.fromEntries(fields.map((field) => [field, { type: 'string' }])),
    required: fields,
  })
}

function buildSystemPrompt(role?: string, goal?: string, expectedOutput?: string) {
  return [
    role ? `Role: ${role}` : '',
    goal ? `Goal: ${goal}` : '',
    expectedOutput ? `Expected output: ${expectedOutput}` : '',
    'Follow the user request carefully and be practical.',
  ]
    .filter(Boolean)
    .join('\n')
}
