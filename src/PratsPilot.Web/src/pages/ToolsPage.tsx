import { Alert, Box, Button, Chip, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import axios from 'axios'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiClient } from '../api/client'
import type { Tool } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { SectionHeader } from '../components/SectionHeader'

export function ToolsPage() {
  const queryClient = useQueryClient()
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const catalog = useQuery({ queryKey: ['demoCatalog'], queryFn: apiClient.getDemoCatalog })
  const [selectedToolId, setSelectedToolId] = useState('')
  const [inputJson, setInputJson] = useState('{"expression":"(8 + 2) * 3"}')
  const [form, setForm] = useState({
    name: '',
    description: '',
    category: 'PythonScript',
    inputSchemaJson: '{"type":"object"}',
    endpointUrl: pythonTemplate,
    isEnabled: true,
  })

  const execute = useMutation({
    mutationFn: () => apiClient.executeTool(selectedToolId, inputJson),
  })
  const createTool = useMutation({
    mutationFn: () => apiClient.createTool(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tools'] })
      setForm({
        name: '',
        description: '',
        category: 'PythonScript',
        inputSchemaJson: '{"type":"object"}',
        endpointUrl: pythonTemplate,
        isEnabled: true,
      })
    },
  })

  return (
    <Box>
      <SectionHeader eyebrow="Tool Engine" title="Built-ins and callable capabilities" />
      <Stack spacing={2.5}>
        <Paper sx={{ p: 3 }}>
          <Typography variant="h5">Create Tool</Typography>
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
            <TextField label="Description" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} fullWidth />
            <TextField
              label="Input Schema JSON"
              value={form.inputSchemaJson}
              onChange={(event) => setForm({ ...form, inputSchemaJson: event.target.value })}
              fullWidth
              sx={{ '& input': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
            />
            <TextField
              label={form.category === 'PythonScript' ? 'Python Script' : 'Endpoint / Configuration'}
              value={form.endpointUrl}
              onChange={(event) => setForm({ ...form, endpointUrl: event.target.value })}
              multiline
              minRows={8}
              fullWidth
              sx={{ gridColumn: { lg: '1 / -1' }, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace', fontSize: 13 } }}
            />
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => createTool.mutate()} disabled={!form.name || createTool.isPending}>
              Create Tool
            </Button>
            {createTool.isSuccess && <Alert severity="success">Tool created.</Alert>}
            {createTool.isError && <Alert severity="error">{getErrorMessage(createTool.error)}</Alert>}
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
                  <Typography sx={{ fontWeight: 900 }}>{row.name}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {row.endpointUrl}
                  </Typography>
                </Box>
              ),
            },
            { key: 'category', label: 'Category', render: (row) => <Chip size="small" label={row.category} /> },
            { key: 'enabled', label: 'Enabled', render: (row) => <Chip size="small" color={row.isEnabled ? 'success' : 'default'} label={row.isEnabled ? 'Yes' : 'No'} /> },
            {
              key: 'action',
              label: 'Try',
              render: (row) => (
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
            sx={{ gridColumn: { lg: '1 / -1' }, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
          />
          <TextField
            label="Result"
            value={execute.isError ? getErrorMessage(execute.error) : execute.data ? JSON.stringify(execute.data, null, 2) : ''}
            multiline
            minRows={8}
            fullWidth
            sx={{ gridColumn: { lg: '1 / -1' }, '& textarea': { fontFamily: 'ui-monospace, Consolas, monospace' } }}
          />
        </Box>
      </Stack>
    </Box>
  )
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
