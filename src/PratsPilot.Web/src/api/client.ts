import axios from 'axios'
import type {
  Agent,
  ArtifactPublishResult,
  ArtifactType,
  AISettings,
  AdminUsersPage,
  ApiResponse,
  AuthResponse,
  ArenaChallenge,
  ChatConversation,
  ContextDocument,
  CreateArenaChallengeRequest,
  CreateAgentRequest,
  CreateToolRequest,
  CreateWorkflowRequest,
  CreateWorkflowStepRequest,
  DemoCatalog,
  DirectChatRequest,
  DirectChatResult,
  Execution,
  HumanApprovalRequest,
  LLMModel,
  PagedResult,
  Realm,
  RegistrationResult,
  SignUpRequest,
  StreamChatRequest,
  Tool,
  UserAccess,
  UpdateAgentRequest,
  UpdateWorkflowRequest,
  Workflow,
} from './types'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7167/api/v1'
export const API_ORIGIN = API_BASE_URL.replace(/\/api\/v\d+\/?$/i, '')
export const USER_REALM_ID = '11111111-1111-1111-1111-111111111111'
export const ADMIN_REALM_ID = '22222222-2222-2222-2222-222222222222'
const REALM_STORAGE_KEY = 'pratspilot.realmId'

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

const storedRealmId = localStorage.getItem(REALM_STORAGE_KEY)
if (storedRealmId) {
  api.defaults.headers.common['X-Realm-Id'] = storedRealmId
}

export function setAccessToken(token?: string) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`
    return
  }

  delete api.defaults.headers.common.Authorization
}

export function setRealmId(realmId?: string) {
  if (realmId) {
    localStorage.setItem(REALM_STORAGE_KEY, realmId)
    api.defaults.headers.common['X-Realm-Id'] = realmId
    return
  }

  localStorage.removeItem(REALM_STORAGE_KEY)
  delete api.defaults.headers.common['X-Realm-Id']
}

export function getStoredRealmId() {
  return localStorage.getItem(REALM_STORAGE_KEY)
}

async function unwrap<T>(request: Promise<{ data: ApiResponse<T> }>) {
  const response = await request
  return response.data.data
}

export const apiClient = {
  login: (email: string, password: string) =>
    unwrap<AuthResponse>(api.post('/auth/login', { email, password })),
  signUp: (request: SignUpRequest) => unwrap<RegistrationResult>(api.post('/auth/signup', request)),
  confirmEmail: (userId: string, code: string) =>
    unwrap<Record<string, never>>(api.post('/auth/confirm-email', { userId, code })),
  resendConfirmation: (email: string) =>
    unwrap<Record<string, never>>(api.post('/auth/resend-confirmation', { email })),
  refreshToken: (refreshToken: string) =>
    unwrap<AuthResponse>(api.post('/auth/refresh-token', { refreshToken })),
  getRealms: () => unwrap<Realm[]>(api.get('/realms', { params: noCacheParams() })),
  getAdminUsers: (pageNumber = 1, pageSize = 25, timezoneOffsetMinutes = 0, search = '') =>
    unwrap<AdminUsersPage>(api.get('/admin/users/paged', {
      params: { pageNumber, pageSize, timezoneOffsetMinutes, search, ...noCacheParams() },
    })),
  updateUserAccess: (id: string, isAdmin: boolean) =>
    unwrap<UserAccess>(api.put(`/admin/users/${id}/access`, { isAdmin })),
  sendWelcomeGuide: (id: string) =>
    unwrap<UserAccess>(api.post(`/admin/users/${id}/welcome-guide`)),
  publishArtifact: (artifactType: ArtifactType, id: string) =>
    unwrap<ArtifactPublishResult>(api.post(`/admin/artifacts/${artifactType}/${id}/publish`)),
  getArenaChallenges: () => unwrap<ArenaChallenge[]>(api.get('/arena/challenges', { params: noCacheParams() })),
  createArenaChallenge: (request: CreateArenaChallengeRequest) =>
    unwrap<ArenaChallenge>(api.post('/arena/challenges', request)),
  submitArenaEntry: (challengeId: string, agentId: string) =>
    unwrap<ArenaChallenge>(api.post(`/arena/challenges/${challengeId}/entries`, { agentId })),
  runArenaBattle: (challengeId: string) =>
    unwrap<ArenaChallenge>(api.post(`/arena/challenges/${challengeId}/run`)),
  getDemoCatalog: () => unwrap<DemoCatalog>(api.get('/demo/catalog', { params: noCacheParams() })),
  getAgents: () => unwrap<PagedResult<Agent>>(api.get('/agents', { params: { pageSize: 50, ...noCacheParams() } })),
  createAgent: (request: CreateAgentRequest) => unwrap<Agent>(api.post('/agents', request)),
  updateAgent: (id: string, request: UpdateAgentRequest) => unwrap<Agent>(api.put(`/agents/${id}`, request)),
  setAgentTools: (id: string, toolIds: string[]) => unwrap<Agent>(api.put(`/agents/${id}/tools`, { toolIds })),
  setAgentContextDocuments: (id: string, contextDocumentIds: string[]) =>
    unwrap<Agent>(api.put(`/agents/${id}/context-documents`, { contextDocumentIds })),
  deleteAgent: (id: string) => api.delete(`/agents/${id}`),
  getContextDocuments: () => unwrap<ContextDocument[]>(api.get('/context-documents', { params: noCacheParams() })),
  uploadContextDocument: (file: File, name: string | undefined, visibility: 'Private' | 'Realm') => {
    const formData = new FormData()
    formData.append('file', file)
    if (name) formData.append('name', name)
    formData.append('visibility', visibility)
    return unwrap<ContextDocument>(api.post('/context-documents', formData, { headers: { 'Content-Type': 'multipart/form-data' } }))
  },
  deleteContextDocument: (id: string) => api.delete(`/context-documents/${id}`),
  getTools: () => unwrap<PagedResult<Tool>>(api.get('/tools', { params: { pageSize: 50, ...noCacheParams() } })),
  createTool: (request: CreateToolRequest) => unwrap<Tool>(api.post('/tools', request)),
  updateTool: (id: string, request: CreateToolRequest) => unwrap<Tool>(api.put(`/tools/${id}`, request)),
  deleteTool: (id: string) => api.delete(`/tools/${id}`),
  getWorkflows: () => unwrap<PagedResult<Workflow>>(api.get('/workflows', { params: { pageSize: 50, ...noCacheParams() } })),
  getWorkflow: (id: string) => unwrap<Workflow>(api.get(`/workflows/${id}`)),
  createWorkflow: (request: CreateWorkflowRequest) => unwrap<Workflow>(api.post('/workflows', request)),
  updateWorkflow: (id: string, request: UpdateWorkflowRequest) => unwrap<Workflow>(api.put(`/workflows/${id}`, request)),
  deleteWorkflow: (id: string) => api.delete(`/workflows/${id}`),
  createWorkflowStep: (workflowId: string, request: CreateWorkflowStepRequest) =>
    unwrap<Workflow>(api.post(`/workflows/${workflowId}/steps`, request)),
  updateWorkflowStep: (workflowId: string, stepId: string, request: CreateWorkflowStepRequest) =>
    unwrap(api.put(`/workflows/${workflowId}/steps/${stepId}`, request)),
  deleteWorkflowStep: (workflowId: string, stepId: string) => api.delete(`/workflows/${workflowId}/steps/${stepId}`),
  getExecutions: (pageSize = 10) => unwrap<PagedResult<Execution>>(api.get('/executions', { params: { pageSize, ...noCacheParams() } })),
  getExecution: (id: string) => unwrap<Execution>(api.get(`/executions/${id}`)),
  getAISettings: () => unwrap<AISettings>(api.get('/ai-settings')),
  getModels: (provider: string, freeOnly = true) =>
    unwrap<LLMModel[]>(api.get('/ai-settings/models', { params: { provider, freeOnly } })),
  updateAISettings: (settings: Partial<AISettings> & { apiKey?: string }) =>
    unwrap<AISettings>(api.put('/ai-settings', settings)),
  startExecution: (targetType: 'Agent' | 'Workflow', targetId: string, inputJson: string) =>
    unwrap<Execution>(api.post('/executions', { targetType, targetId, inputJson })),
  retryExecution: (id: string) => unwrap<Execution>(api.post(`/executions/${id}/retry`)),
  getHumanApprovals: (pendingOnly = false) =>
    unwrap<HumanApprovalRequest[]>(api.get('/human-approvals', { params: { pendingOnly, ...noCacheParams() } })),
  approveHumanApproval: (id: string, comment?: string) =>
    unwrap<HumanApprovalRequest>(api.post(`/human-approvals/${id}/approve`, { comment })),
  rejectHumanApproval: (id: string, comment?: string) =>
    unwrap<HumanApprovalRequest>(api.post(`/human-approvals/${id}/reject`, { comment })),
  executeTool: (toolId: string, inputJson: string) =>
    unwrap(api.post(`/tools/${toolId}/execute`, { inputJson })),
  chat: (request: DirectChatRequest) => unwrap<DirectChatResult>(api.post('/ai-settings/chat', request)),
  getChatConversations: () =>
    unwrap<ChatConversation[]>(api.get('/chat/conversations', { params: noCacheParams() })),
  deleteChatConversation: (id: string) => api.delete(`/chat/conversations/${id}`),
  streamChat: (request: StreamChatRequest, handlers: ChatStreamHandlers, signal: AbortSignal) =>
    streamChat(request, handlers, signal),
}

export type ChatStreamHandlers = {
  onConversation: (value: { conversationId: string; title: string }) => void
  onDelta: (content: string) => void
  onStatus: (message: string) => void
  onDone: () => void
  onError: (message: string) => void
}

async function streamChat(request: StreamChatRequest, handlers: ChatStreamHandlers, signal: AbortSignal) {
  const authorization = api.defaults.headers.common.Authorization
  const realmId = getStoredRealmId()
  const response = await fetch(`${API_BASE_URL}/chat/stream`, {
    method: 'POST',
    signal,
    headers: {
      'Content-Type': 'application/json',
      ...(authorization ? { Authorization: String(authorization) } : {}),
      ...(realmId ? { 'X-Realm-Id': realmId } : {}),
    },
    body: JSON.stringify(request),
  })

  if (!response.ok || !response.body) {
    throw new Error(response.status === 401 ? 'Your session expired. Please sign in again.' : 'The chat stream could not be started.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  while (true) {
    const { value, done } = await reader.read()
    buffer += decoder.decode(value, { stream: !done }).replace(/\r\n/g, '\n')
    const events = buffer.split('\n\n')
    buffer = events.pop() ?? ''
    events.forEach((eventBlock) => dispatchSseEvent(eventBlock, handlers))
    if (done) {
      if (buffer.trim()) dispatchSseEvent(buffer, handlers)
      break
    }
  }
}

function dispatchSseEvent(eventBlock: string, handlers: ChatStreamHandlers) {
  const lines = eventBlock.split('\n')
  const eventName = lines.find((line) => line.startsWith('event:'))?.slice(6).trim()
  const rawData = lines.filter((line) => line.startsWith('data:')).map((line) => line.slice(5).trim()).join('\n')
  if (!eventName || !rawData) return

  const data = JSON.parse(rawData) as Record<string, string>
  if (eventName === 'conversation') handlers.onConversation({ conversationId: data.conversationId, title: data.title })
  if (eventName === 'delta') handlers.onDelta(data.content)
  if (eventName === 'status') handlers.onStatus(data.message)
  if (eventName === 'search') handlers.onStatus(`Live context: ${data.provider}`)
  if (eventName === 'done') handlers.onDone()
  if (eventName === 'error') handlers.onError(data.message)
}

function noCacheParams() {
  return { _: Date.now() }
}
