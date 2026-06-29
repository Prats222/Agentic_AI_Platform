import {
  Alert,
  Box,
  Button,
  Chip,
  Grid,
  MenuItem,
  Paper,
  Slider,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import SaveIcon from '@mui/icons-material/Save'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { apiClient } from '../api/client'
import { fallbackModelCatalog, providerDefaults } from '../api/modelCatalog'
import { SectionHeader } from '../components/SectionHeader'

export function AISettingsPage() {
  const queryClient = useQueryClient()
  const settings = useQuery({ queryKey: ['aiSettings'], queryFn: apiClient.getAISettings })
  const [form, setForm] = useState({
    provider: 'Gemini',
    model: 'gemini-2.5-flash',
    temperature: 0.2,
    maxTokens: 2048,
    topP: 0.9,
    systemPrompt: 'You are a helpful AI agent.',
    apiKey: '',
    baseUrl: 'https://generativelanguage.googleapis.com/v1beta',
  })
  const models = useQuery({
    queryKey: ['llmModels', 'global', form.provider],
    queryFn: () => apiClient.getModels(form.provider, true),
  })

  useEffect(() => {
    if (settings.data) {
      setForm((current) => ({
        ...current,
        provider: settings.data.provider,
        model: settings.data.model,
        temperature: settings.data.temperature,
        maxTokens: settings.data.maxTokens,
        topP: settings.data.topP,
        systemPrompt: settings.data.systemPrompt,
        baseUrl: settings.data.baseUrl ?? '',
      }))
    }
  }, [settings.data])

  useEffect(() => {
    const availableOptions = models.data?.length
      ? models.data.map((model) => model.id)
      : fallbackModelCatalog[form.provider] ?? []

    if (!availableOptions.length || availableOptions.includes(form.model)) {
      return
    }

    setForm((current) => ({ ...current, model: availableOptions[0] }))
  }, [form.model, form.provider, models.data, models.isError])

  const save = useMutation({
    mutationFn: () => apiClient.updateAISettings(form),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['aiSettings'] })
      setForm((current) => ({ ...current, apiKey: '' }))
    },
  })

  function setProvider(provider: string) {
    const defaults = providerDefaults[provider]
    setForm((current) => ({
      ...current,
      provider,
      model: defaults.model,
      baseUrl: defaults.baseUrl,
    }))
  }

  const liveModelOptions = models.data?.filter((model) => model.provider === form.provider) ?? []
  const fallbackOptions = fallbackModelCatalog[form.provider] ?? []
  const modelOptions = liveModelOptions.length ? liveModelOptions.map((model) => model.id) : fallbackOptions
  const allModelOptions = modelOptions.length ? modelOptions : [form.model].filter(Boolean)
  const modelLabels = new Map(liveModelOptions.map((model) => [model.id, model.name]))

  return (
    <Box>
      <SectionHeader
        eyebrow="Provider Control"
        title="Global AI settings"
        action={<Chip color={settings.data?.hasApiKey ? 'success' : 'warning'} label={settings.data?.hasApiKey ? 'API key stored' : 'No API key stored'} />}
      />
      <Paper sx={{ p: 3 }}>
        <Grid container spacing={2.4}>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField select label="Provider" value={form.provider} onChange={(event) => setProvider(event.target.value)} fullWidth>
              <MenuItem value="Gemini">Gemini</MenuItem>
              <MenuItem value="OpenRouter">OpenRouter</MenuItem>
              <MenuItem value="Ollama">Ollama</MenuItem>
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField select label="Model" value={form.model} onChange={(event) => setForm({ ...form, model: event.target.value })} fullWidth>
              {allModelOptions.map((model) => (
                <MenuItem key={model} value={model}>
                  {modelLabels.get(model) ?? model}
                </MenuItem>
              ))}
            </TextField>
            {form.provider === 'OpenRouter' && (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.8 }}>
                {models.isError ? 'Using fallback because OpenRouter model lookup failed.' : 'OpenRouter list is live and filtered to free models.'}
              </Typography>
            )}
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField label="Base URL" value={form.baseUrl} onChange={(event) => setForm({ ...form, baseUrl: event.target.value })} fullWidth />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="body2" sx={{ fontWeight: 800, mb: 1 }}>
              Temperature: {form.temperature}
            </Typography>
            <Slider min={0} max={2} step={0.1} value={form.temperature} onChange={(_, value) => setForm({ ...form, temperature: value as number })} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Typography variant="body2" sx={{ fontWeight: 800, mb: 1 }}>
              Top P: {form.topP}
            </Typography>
            <Slider min={0} max={1} step={0.05} value={form.topP} onChange={(_, value) => setForm({ ...form, topP: value as number })} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              label="Max Tokens"
              type="number"
              value={form.maxTokens}
              onChange={(event) => setForm({ ...form, maxTokens: Number(event.target.value) })}
              fullWidth
            />
          </Grid>
          <Grid size={12}>
            <TextField
              label="API Key"
              type="password"
              value={form.apiKey}
              onChange={(event) => setForm({ ...form, apiKey: event.target.value })}
              helperText="Leave empty to keep the existing stored key."
              fullWidth
            />
          </Grid>
          <Grid size={12}>
            <TextField
              label="System Prompt"
              value={form.systemPrompt}
              onChange={(event) => setForm({ ...form, systemPrompt: event.target.value })}
              multiline
              minRows={5}
              fullWidth
            />
          </Grid>
          <Grid size={12}>
            <Stack direction="row" sx={{ gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
              <Button variant="contained" startIcon={<SaveIcon />} onClick={() => save.mutate()} disabled={save.isPending}>
                Save Settings
              </Button>
              {save.isSuccess && <Alert severity="success">AI settings saved.</Alert>}
              {save.isError && <Alert severity="error">Could not save settings.</Alert>}
            </Stack>
          </Grid>
        </Grid>
      </Paper>
    </Box>
  )
}
