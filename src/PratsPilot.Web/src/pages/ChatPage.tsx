import AddCommentOutlinedIcon from '@mui/icons-material/AddCommentOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import SendRoundedIcon from '@mui/icons-material/SendRounded'
import SmartToyOutlinedIcon from '@mui/icons-material/SmartToyOutlined'
import StopRoundedIcon from '@mui/icons-material/StopRounded'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Link,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { apiClient } from '../api/client'
import { fallbackModelCatalog, providerDefaults } from '../api/modelCatalog'
import type { ChatMessage } from '../api/types'
import { SectionHeader } from '../components/SectionHeader'

type DisplayMessage = Pick<ChatMessage, 'role' | 'content'> & { id: string }

export function ChatPage() {
  const queryClient = useQueryClient()
  const conversations = useQuery({ queryKey: ['chat-conversations'], queryFn: apiClient.getChatConversations })
  const [conversationId, setConversationId] = useState<string>()
  const [provider, setProvider] = useState('Gemini')
  const [model, setModel] = useState(providerDefaults.Gemini.model)
  const [prompt, setPrompt] = useState('')
  const [messages, setMessages] = useState<DisplayMessage[]>([])
  const [status, setStatus] = useState('')
  const [error, setError] = useState('')
  const [isStreaming, setIsStreaming] = useState(false)
  const abortController = useRef<AbortController | undefined>(undefined)
  const messagesEnd = useRef<HTMLDivElement>(null)
  const initializedHistory = useRef(false)

  const models = useQuery({
    queryKey: ['llmModels', 'chat', provider],
    queryFn: () => apiClient.getModels(provider, true),
  })
  const modelOptions = useMemo(() => {
    const live = models.data?.filter((option) => option.provider === provider) ?? []
    return live.length ? live : (fallbackModelCatalog[provider] ?? []).map((id) => ({ id, name: id }))
  }, [models.data, provider])

  const openConversation = useCallback((id: string) => {
    const conversation = conversations.data?.find((item) => item.id === id)
    if (!conversation) return
    setConversationId(conversation.id)
    setProvider(conversation.provider)
    setModel(conversation.model)
    setMessages(conversation.messages.map((message) => ({ id: message.id, role: message.role, content: message.content })))
    setError('')
  }, [conversations.data])

  useEffect(() => {
    if (!initializedHistory.current && conversations.data?.length) {
      initializedHistory.current = true
      openConversation(conversations.data[0].id)
    }
  }, [conversations.data, openConversation])

  useEffect(() => {
    if (modelOptions.length && !modelOptions.some((option) => option.id === model)) {
      setModel(modelOptions[0].id)
    }
  }, [model, modelOptions])

  useEffect(() => {
    messagesEnd.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, status])

  function startNewChat() {
    if (isStreaming) return
    setConversationId(undefined)
    setMessages([])
    setPrompt('')
    setError('')
    setStatus('')
  }

  async function deleteConversation(id: string) {
    if (isStreaming) return
    await apiClient.deleteChatConversation(id)
    if (conversationId === id) startNewChat()
    await queryClient.invalidateQueries({ queryKey: ['chat-conversations'] })
  }

  function changeProvider(nextProvider: string) {
    setProvider(nextProvider)
    setModel(providerDefaults[nextProvider].model)
  }

  async function sendMessage() {
    const text = prompt.trim()
    if (!text || isStreaming) return

    const userMessage: DisplayMessage = { id: crypto.randomUUID(), role: 'user', content: text }
    const assistantId = crypto.randomUUID()
    setMessages((current) => [...current, userMessage, { id: assistantId, role: 'assistant', content: '' }])
    setPrompt('')
    setError('')
    setStatus('Connecting...')
    setIsStreaming(true)
    abortController.current = new AbortController()

    try {
      await apiClient.streamChat(
        {
          conversationId,
          provider,
          model,
          prompt: text,
          temperature: 0.2,
          maxTokens: 2048,
          topP: 0.9,
          systemPrompt: 'You are PratsPilot, a precise AI assistant. Use conversation context, answer directly, and cite sources for live web facts.',
          baseUrl: providerDefaults[provider].baseUrl,
        },
        {
          onConversation: (value) => setConversationId(value.conversationId),
          onDelta: (content) => {
            setStatus('')
            setMessages((current) => current.map((message) => (
              message.id === assistantId ? { ...message, content: message.content + content } : message
            )))
          },
          onStatus: setStatus,
          onDone: () => setStatus(''),
          onError: (message) => {
            setError(message)
            setStatus('')
            setMessages((current) => current.map((item) => (
              item.id === assistantId && !item.content ? { ...item, content: `Request failed: ${message}` } : item
            )))
          },
        },
        abortController.current.signal,
      )
      await queryClient.invalidateQueries({ queryKey: ['chat-conversations'] })
    } catch (streamError) {
      if ((streamError as Error).name !== 'AbortError') {
        setError(streamError instanceof Error ? streamError.message : 'The chat request failed.')
      }
    } finally {
      setIsStreaming(false)
      setStatus('')
      abortController.current = undefined
    }
  }

  return (
    <Box>
      <SectionHeader eyebrow="AI CONVERSATIONS" title="Think, search, and build with PratsPilot" />
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '280px minmax(0, 1fr)' }, gap: 2.5 }}>
        <Paper sx={{ p: 2, alignSelf: 'start' }}>
          <Button fullWidth variant="contained" startIcon={<AddCommentOutlinedIcon />} onClick={startNewChat} disabled={isStreaming}>
            New Chat
          </Button>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.25, px: 0.5 }}>
            Up to 2 chats and 20 messages each. Oldest history is removed automatically.
          </Typography>
          <Divider sx={{ my: 2 }} />
          <Stack spacing={1}>
            {conversations.isLoading && <CircularProgress size={22} sx={{ alignSelf: 'center' }} />}
            {conversations.data?.map((conversation) => (
              <Box
                key={conversation.id}
                onClick={() => openConversation(conversation.id)}
                sx={{
                  p: 1.4,
                  borderRadius: 2,
                  cursor: 'pointer',
                  border: '1px solid',
                  borderColor: conversationId === conversation.id ? 'primary.main' : 'divider',
                  bgcolor: conversationId === conversation.id ? 'action.selected' : 'transparent',
                  '&:hover': { borderColor: 'primary.main' },
                }}
              >
                <Stack direction="row" sx={{ alignItems: 'flex-start', justifyContent: 'space-between', gap: 1 }}>
                  <Box sx={{ minWidth: 0 }}>
                    <Typography variant="body2" noWrap sx={{ fontWeight: 800 }}>{conversation.title}</Typography>
                    <Typography variant="caption" color="text.secondary">{conversation.provider} · {conversation.messages.length} messages</Typography>
                  </Box>
                  <Tooltip title="Delete chat">
                    <IconButton
                      size="small"
                      aria-label="Delete chat"
                      onClick={(event) => { event.stopPropagation(); void deleteConversation(conversation.id) }}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                </Stack>
              </Box>
            ))}
          </Stack>
        </Paper>

        <Paper sx={{ minHeight: 650, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ p: 2, borderBottom: '1px solid', borderColor: 'divider' }}>
            <TextField select size="small" label="Provider" value={provider} onChange={(event) => changeProvider(event.target.value)} disabled={isStreaming} sx={{ minWidth: 150 }}>
              <MenuItem value="Gemini">Gemini</MenuItem>
              <MenuItem value="Groq">Groq</MenuItem>
              <MenuItem value="Cerebras">Cerebras</MenuItem>
              <MenuItem value="OpenRouter">OpenRouter</MenuItem>
            </TextField>
            <TextField select size="small" label="Model" value={model} onChange={(event) => setModel(event.target.value)} disabled={isStreaming} sx={{ minWidth: 230 }}>
              {modelOptions.map((option) => <MenuItem key={option.id} value={option.id}>{option.name}</MenuItem>)}
            </TextField>
            <Box sx={{ flex: 1 }} />
            <Chip icon={<SmartToyOutlinedIcon />} label={status || (isStreaming ? 'Thinking...' : 'Ready')} color={isStreaming ? 'primary' : 'default'} variant="outlined" />
          </Stack>

          <Box sx={{ flex: 1, minHeight: 430, maxHeight: '58vh', overflowY: 'auto', p: { xs: 2, md: 3 } }}>
            {!messages.length && (
              <Stack sx={{ minHeight: 370, textAlign: 'center', alignItems: 'center', justifyContent: 'center' }} spacing={2}>
                <SmartToyOutlinedIcon sx={{ fontSize: 54, color: 'primary.main' }} />
                <Box>
                  <Typography variant="h5">What are we building today?</Typography>
                  <Typography color="text.secondary" sx={{ mt: 0.8 }}>Ask a question, continue an idea, or request current information from the web.</Typography>
                </Box>
                <Stack direction={{ xs: 'column', sm: 'row' }} sx={{ gap: 1 }}>
                  {['Weather in Bangalore today', 'Design an API testing workflow', 'Explain how agent approval gates work'].map((suggestion) => (
                    <Button key={suggestion} variant="outlined" size="small" onClick={() => setPrompt(suggestion)}>{suggestion}</Button>
                  ))}
                </Stack>
              </Stack>
            )}
            <Stack spacing={2.2}>
              {messages.map((message) => (
                <Box key={message.id} sx={{ display: 'flex', justifyContent: message.role === 'user' ? 'flex-end' : 'flex-start' }}>
                  <Box sx={{
                    maxWidth: { xs: '94%', md: '82%' },
                    px: 2.2,
                    py: 1.7,
                    borderRadius: message.role === 'user' ? '16px 16px 4px 16px' : '4px 16px 16px 16px',
                    bgcolor: message.role === 'user' ? 'primary.main' : 'background.default',
                    color: message.role === 'user' ? 'primary.contrastText' : 'text.primary',
                    border: message.role === 'assistant' ? '1px solid' : 'none',
                    borderColor: 'divider',
                  }}>
                    <MessageContent content={message.content || (isStreaming && message.role === 'assistant' ? 'Thinking...' : '')} />
                  </Box>
                </Box>
              ))}
              <div ref={messagesEnd} />
            </Stack>
          </Box>

          <Box sx={{ p: 2, borderTop: '1px solid', borderColor: 'divider' }}>
            {error && <Alert severity="error" sx={{ mb: 1.5 }} onClose={() => setError('')}>{error}</Alert>}
            <Stack direction="row" spacing={1.2} sx={{ alignItems: 'flex-end' }}>
              <TextField
                fullWidth
                multiline
                maxRows={5}
                value={prompt}
                placeholder="Message PratsPilot..."
                slotProps={{ htmlInput: { maxLength: 8000 } }}
                onChange={(event) => setPrompt(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    void sendMessage()
                  }
                }}
              />
              {isStreaming ? (
                <Tooltip title="Stop generation">
                  <IconButton color="error" onClick={() => abortController.current?.abort()} sx={{ width: 48, height: 48 }}><StopRoundedIcon /></IconButton>
                </Tooltip>
              ) : (
                <Tooltip title="Send message">
                  <span><IconButton color="primary" disabled={!prompt.trim()} onClick={() => void sendMessage()} sx={{ width: 48, height: 48 }}><SendRoundedIcon /></IconButton></span>
                </Tooltip>
              )}
            </Stack>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.8, textAlign: 'center' }}>
              Enter to send · Shift+Enter for a new line · Live answers may be limited by provider quotas
            </Typography>
          </Box>
        </Paper>
      </Box>
    </Box>
  )
}

function MessageContent({ content }: { content: string }) {
  const parts = content.split(/(https?:\/\/[^\s]+)/g)
  return (
    <Typography sx={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere', lineHeight: 1.72 }}>
      {parts.map((part, index) => part.startsWith('http') ? (
        <Link key={`${part}-${index}`} href={part.replace(/[),.;]+$/, '')} target="_blank" rel="noreferrer" color="inherit" underline="always">
          {part}
        </Link>
      ) : part)}
    </Typography>
  )
}
