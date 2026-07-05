import { Alert, Box, Button, Grid, MenuItem, Paper, Slider, TextField, Typography } from '@mui/material'
import SendIcon from '@mui/icons-material/Send'
import axios from 'axios'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { apiClient } from '../api/client'
import { fallbackModelCatalog, providerDefaults } from '../api/modelCatalog'
import { SectionHeader } from '../components/SectionHeader'

export function ChatPage() {
  const [provider, setProvider] = useState('Gemini')
  const [model, setModel] = useState(providerDefaults.Gemini.model)
  const [baseUrl, setBaseUrl] = useState(providerDefaults.Gemini.baseUrl)
  const [temperature, setTemperature] = useState(0.2)
  const [prompt, setPrompt] = useState('Explain how an agentic platform can generate end-to-end test scripts.')
  const models = useQuery({
    queryKey: ['llmModels', 'chat', provider],
    queryFn: () => apiClient.getModels(provider, true),
  })

  useEffect(() => {
    const availableOptions = models.data?.length
      ? models.data.map((option) => option.id)
      : fallbackModelCatalog[provider] ?? []

    if (!availableOptions.length || availableOptions.includes(model)) {
      return
    }

    setModel(availableOptions[0])
  }, [model, models.data, models.isError, provider])

  const chat = useMutation({
    mutationFn: () =>
      apiClient.chat({
        provider,
        model,
        prompt,
        temperature,
        maxTokens: 2048,
        topP: 0.9,
        systemPrompt: 'You are PratsPilot, a precise AI assistant for building agentic workflows.',
        baseUrl,
      }),
  })

  function changeProvider(nextProvider: string) {
    const defaults = providerDefaults[nextProvider]
    setProvider(nextProvider)
    setModel(defaults.model)
    setBaseUrl(defaults.baseUrl)
  }

  const liveModelOptions = models.data?.filter((option) => option.provider === provider) ?? []
  const fallbackOptions = fallbackModelCatalog[provider] ?? []
  const modelOptions = liveModelOptions.length ? liveModelOptions.map((option) => option.id) : fallbackOptions
  const allModelOptions = modelOptions.length ? modelOptions : [model].filter(Boolean)
  const modelLabels = new Map(liveModelOptions.map((option) => [option.id, option.name]))

  return (
    <Box>
      <SectionHeader eyebrow="LLM Playground" title="Ask any model before turning it into an agent" />
      <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <Paper sx={{ p: 3 }}>
            <Grid container spacing={2}>
              <Grid size={12}>
                <TextField select fullWidth label="Provider" value={provider} onChange={(event) => changeProvider(event.target.value)}>
                  <MenuItem value="Gemini">Gemini</MenuItem>
                  <MenuItem value="Groq">Groq</MenuItem>
                  <MenuItem value="OpenRouter">OpenRouter</MenuItem>
                </TextField>
              </Grid>
              <Grid size={12}>
                <TextField select fullWidth label="Model" value={model} onChange={(event) => setModel(event.target.value)}>
                  {allModelOptions.map((option) => (
                    <MenuItem key={option} value={option}>
                      {modelLabels.get(option) ?? option}
                    </MenuItem>
                  ))}
                </TextField>
                {provider === 'OpenRouter' && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.8 }}>
                    OpenRouter uses Auto Free by default. Free model availability can still change or rate limit.
                  </Typography>
                )}
              </Grid>
              <Grid size={12}>
                <TextField fullWidth label="Base URL" value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} />
              </Grid>
              <Grid size={12}>
                <Typography variant="body2" sx={{ fontWeight: 800, mb: 1 }}>
                  Temperature: {temperature}
                </Typography>
                <Slider min={0} max={2} step={0.1} value={temperature} onChange={(_, value) => setTemperature(value as number)} />
              </Grid>
              <Grid size={12}>
                <TextField
                  label="Question"
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                  multiline
                  minRows={8}
                  fullWidth
                />
              </Grid>
              <Grid size={12}>
                <Button variant="contained" startIcon={<SendIcon />} onClick={() => chat.mutate()} disabled={chat.isPending}>
                  {chat.isPending ? 'Thinking...' : 'Ask Model'}
                </Button>
              </Grid>
            </Grid>
          </Paper>
        </Grid>
        <Grid size={{ xs: 12, lg: 7 }}>
          <Paper sx={{ p: 3, minHeight: 530 }}>
            <Typography variant="h5">Response</Typography>
            {chat.isError && <Alert severity="error" sx={{ mt: 2 }}>{getErrorMessage(chat.error)}</Alert>}
            <TextField
              value={chat.data?.content ?? ''}
              multiline
              minRows={20}
              fullWidth
              sx={{ mt: 2, '& textarea': { lineHeight: 1.7 } }}
            />
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

function getErrorMessage(error: unknown) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { detail?: string; title?: string } | undefined
    return data?.detail ?? data?.title ?? error.message
  }

  return error instanceof Error ? error.message : 'The provider request failed.'
}
