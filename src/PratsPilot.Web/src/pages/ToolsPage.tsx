import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteIcon from '@mui/icons-material/Delete'
import EditIcon from '@mui/icons-material/Edit'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import SaveIcon from '@mui/icons-material/Save'
import axios from 'axios'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState } from 'react'
import { apiClient } from '../api/client'
import type { Tool } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { AdminVerifiedChip } from '../components/AdminVerifiedChip'
import { ArtifactVisibilityChip, ArtifactVisibilityField } from '../components/ArtifactVisibilityField'
import { PublishArtifactButton } from '../components/PublishArtifactButton'
import { SectionHeader } from '../components/SectionHeader'
import { useAuth } from '../state/AuthContext'

export function ToolsPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const catalog = useQuery({ queryKey: ['demoCatalog'], queryFn: apiClient.getDemoCatalog })
  const formRef = useRef<HTMLDivElement>(null)
  const [selectedToolId, setSelectedToolId] = useState('')
  const [editingToolId, setEditingToolId] = useState('')
  const [inputJson, setInputJson] = useState('{"expression":"(8 + 2) * 3"}')
  const [form, setForm] = useState(emptyToolForm)

  const execute = useMutation({
    mutationFn: () => apiClient.executeTool(selectedToolId, inputJson),
  })
  const createTool = useMutation({
    mutationFn: () => apiClient.createTool(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tools'] })
      resetForm()
    },
  })
  const updateTool = useMutation({
    mutationFn: () => apiClient.updateTool(editingToolId, {
      ...form,
      secretJson: form.secretJson.trim() || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tools'] })
      resetForm()
    },
  })
  const deleteTool = useMutation({
    mutationFn: (id: string) => apiClient.deleteTool(id),
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ['tools'] })
      if (selectedToolId === id) setSelectedToolId('')
      if (editingToolId === id) resetForm()
    },
  })

  const canModify = (tool: Tool) => Boolean(user?.roles.includes('Admin') || !tool.createdByUserId || tool.createdByUserId === user?.userId)

  function resetForm() {
    setEditingToolId('')
    setForm(emptyToolForm())
  }

  function editTool(tool: Tool) {
    setEditingToolId(tool.id)
    setForm({
      name: tool.name,
      description: tool.description ?? '',
      category: tool.category,
      inputSchemaJson: tool.inputSchemaJson,
      endpointUrl: tool.endpointUrl,
      secretJson: '',
      isEnabled: tool.isEnabled,
      visibility: tool.visibility,
    })
    formRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  return (
    <Box>
      <SectionHeader eyebrow="Tool Engine" title="Built-ins and callable capabilities" />
      <Stack spacing={2.5}>
        <Paper ref={formRef} sx={{ p: 3 }}>
          <Typography variant="h5">{editingToolId ? 'Edit Tool' : 'Create Tool'}</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Python tools receive input JSON on stdin and should print JSON to stdout.
          </Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' }, gap: 2 }}>
            <TextField label="Tool Name" value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} fullWidth />
            <TextField select label="Category" value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })} fullWidth>
              <MenuItem value="PythonScript">PythonScript</MenuItem>
              <MenuItem value="Calculator">Calculator</MenuItem>
              <MenuItem value="Http">Http</MenuItem>
              <MenuItem value="WebSearch">WebSearch</MenuItem>
              <MenuItem value="FileReader">FileReader</MenuItem>
            </TextField>
            <TextField
              label="Description"
              value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              multiline
              minRows={3}
              fullWidth
              sx={resizableTextAreaSx}
            />
            <TextField
              label="Input Schema JSON"
              value={form.inputSchemaJson}
              onChange={(event) => setForm({ ...form, inputSchemaJson: event.target.value })}
              multiline
              minRows={6}
              fullWidth
              sx={{ ...resizableTextAreaSx, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace', fontSize: 13 } }}
            />
            <TextField
              label={form.category === 'PythonScript' ? 'Python Script' : 'Endpoint / Configuration'}
              value={form.endpointUrl}
              onChange={(event) => setForm({ ...form, endpointUrl: event.target.value })}
              multiline
              minRows={8}
              fullWidth
              sx={{ gridColumn: { lg: '1 / -1' }, ...resizableTextAreaSx, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace', fontSize: 13 } }}
            />
            <TextField
              label="Secrets JSON (optional)"
              value={form.secretJson}
              onChange={(event) => setForm({ ...form, secretJson: event.target.value })}
              multiline
              minRows={4}
              fullWidth
              helperText={editingToolId && !form.secretJson
                ? 'Leave empty to keep the existing stored secrets.'
                : 'Stored server-side and injected as data["secrets"] during execution. Example: {"githubToken":"..."}'}
              sx={{ gridColumn: { lg: '1 / -1' }, ...resizableTextAreaSx, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace', fontSize: 13 } }}
            />
            <ArtifactVisibilityField
              value={form.visibility}
              onChange={(visibility) => setForm({ ...form, visibility })}
            />
            <Button
              variant="contained"
              startIcon={editingToolId ? <SaveIcon /> : <AddIcon />}
              onClick={() => (editingToolId ? updateTool.mutate() : createTool.mutate())}
              disabled={!form.name || createTool.isPending || updateTool.isPending}
            >
              {editingToolId ? 'Save Tool' : 'Create Tool'}
            </Button>
            {editingToolId && <Button variant="outlined" onClick={resetForm}>Cancel</Button>}
            {createTool.isSuccess && <Alert severity="success">Tool created.</Alert>}
            {updateTool.isSuccess && <Alert severity="success">Tool updated.</Alert>}
            {deleteTool.isSuccess && <Alert severity="success">Tool deleted.</Alert>}
            {(createTool.isError || updateTool.isError || deleteTool.isError) && (
              <Alert severity="error">{getErrorMessage(createTool.error ?? updateTool.error ?? deleteTool.error)}</Alert>
            )}
          </Box>
        </Paper>
        <DataPanel<Tool>
          title="Tools"
          subtitle="The runtime selects executors by category."
          rows={tools.data?.items ?? []}
          loading={tools.isLoading}
          columns={[
            {
              key: 'name',
              label: 'Name',
              render: (row) => (
                <Box>
                  <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
                    <Typography sx={{ fontWeight: 900 }}>{row.name}</Typography>
                    <AdminVerifiedChip publishedAt={row.publishedAt} publishedByDisplayName={row.publishedByDisplayName} />
                    <ArtifactVisibilityChip visibility={row.visibility} />
                  </Stack>
                  <Typography variant="caption" color="text.secondary">
                    {row.description || 'No description'}
                  </Typography>
                </Box>
              ),
            },
            { key: 'category', label: 'Category', render: (row) => <Chip size="small" label={row.category} /> },
            { key: 'enabled', label: 'Enabled', render: (row) => <Chip size="small" color={row.isEnabled ? 'success' : 'default'} label={row.isEnabled ? 'Yes' : 'No'} /> },
            { key: 'secrets', label: 'Secrets', render: (row) => <Chip size="small" color={row.hasSecrets ? 'warning' : 'default'} label={row.hasSecrets ? 'Stored' : 'None'} /> },
            { key: 'owner', label: 'Owner', render: (row) => row.createdByDisplayName || 'system' },
            {
              key: 'action',
              label: 'Actions',
              render: (row) => (
                <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                  <PublishArtifactButton artifactType="tool" artifactId={row.id} artifactName={row.name} realmId={row.realmId} />
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => {
                      setSelectedToolId(row.id)
                      const sample = catalog.data?.tools.find((tool) => tool.id === row.id)?.sampleInputJson
                      setInputJson(sample ?? buildSampleInput(row.inputSchemaJson))
                    }}
                  >
                    Load
                  </Button>
                  {canModify(row) ? (
                    <>
                      <Button size="small" variant="outlined" startIcon={<EditIcon />} onClick={() => editTool(row)}>
                        Edit
                      </Button>
                      <Button
                        size="small"
                        color="error"
                        variant="outlined"
                        startIcon={<DeleteIcon />}
                        onClick={() => window.confirm(`Delete tool "${row.name}"? It will also be removed from agents and workflows.`) && deleteTool.mutate(row.id)}
                      >
                        Delete
                      </Button>
                    </>
                  ) : <Chip size="small" label="View only" />}
                </Stack>
              ),
            },
          ]}
        />
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
            gap: 2.5,
          }}
        >
          <TextField
            label="Tool ID"
            value={selectedToolId}
            onChange={(event) => setSelectedToolId(event.target.value)}
            fullWidth
          />
          <Button variant="contained" startIcon={<PlayArrowIcon />} onClick={() => execute.mutate()} disabled={!selectedToolId || execute.isPending}>
            Execute Tool
          </Button>
          <TextField
            label="Input JSON"
            value={inputJson}
            onChange={(event) => setInputJson(event.target.value)}
            multiline
            minRows={5}
            fullWidth
            sx={{ gridColumn: { lg: '1 / -1' }, ...resizableTextAreaSx, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
          />
          <TextField
            label="Result"
            value={execute.isError ? getErrorMessage(execute.error) : execute.data ? JSON.stringify(execute.data, null, 2) : ''}
            multiline
            minRows={8}
            fullWidth
            sx={{ gridColumn: { lg: '1 / -1' }, ...resizableTextAreaSx, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
          />
        </Box>
      </Stack>
    </Box>
  )
}

const resizableTextAreaSx = {
  '& textarea': {
    resize: 'vertical',
  },
}

function emptyToolForm() {
  return {
    name: '',
    description: '',
    category: 'PythonScript',
    inputSchemaJson: '{"type":"object"}',
    endpointUrl: pythonTemplate,
    secretJson: '{}',
    isEnabled: true,
    visibility: 'Private' as 'Private' | 'Realm',
  }
}

const pythonTemplate = `import json
import sys

data = json.load(sys.stdin)
text = data.get("text") or data.get("prompt") or ""

print(json.dumps({
    "output": text.upper(),
    "length": len(text)
}))`

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
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

  return error instanceof Error ? error.message : 'Action failed.'
}

function buildSampleInput(inputSchemaJson?: string) {
  if (!inputSchemaJson) {
    return '{}'
  }

  try {
    const schema = JSON.parse(inputSchemaJson) as {
      properties?: Record<string, { type?: string }>
      required?: string[]
    }
    const sample: Record<string, unknown> = {}
    const fields = schema.properties ?? {}

    for (const [name, definition] of Object.entries(fields)) {
      if (name.toLowerCase().includes('password')) {
        sample[name] = 'Password@123'
      } else if (definition.type === 'number' || definition.type === 'integer') {
        sample[name] = 1
      } else if (definition.type === 'boolean') {
        sample[name] = true
      } else if (definition.type === 'array') {
        sample[name] = []
      } else if (definition.type === 'object') {
        sample[name] = {}
      } else {
        sample[name] = `sample ${name}`
      }
    }

    for (const required of schema.required ?? []) {
      sample[required] ??= required.toLowerCase().includes('password') ? 'Password@123' : `sample ${required}`
    }

    return JSON.stringify(sample, null, 2)
  } catch {
    return '{}'
  }
}
