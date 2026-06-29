import axios from 'axios'
import type {
  Agent,
  AISettings,
  ApiResponse,
  AuthResponse,
  CreateAgentRequest,
  CreateToolRequest,
  CreateWorkflowRequest,
  CreateWorkflowStepRequest,
  DemoCatalog,
  DirectChatRequest,
  DirectChatResult,
  Execution,
  LLMModel,
  PagedResult,
  SignUpRequest,
  Tool,
  UpdateAgentRequest,
  UpdateWorkflowRequest,
  Workflow,
} from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7167/api/v1'

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
})

export function setAccessToken(token?: string) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`
    return
  }

  delete api.defaults.headers.common.Authorization
}

async function unwrap<T>(request: Promise<{ data: ApiResponse<T> }>) {
  const response = await request
  return response.data.data
}

export const apiClient = {
  login: (email: string, password: string) =>
    unwrap<AuthResponse>(api.post('/auth/login', { email, password })),
  signUp: (request: SignUpRequest) => unwrap<AuthResponse>(api.post('/auth/signup', request)),
  refreshToken: (refreshToken: string) =>
    unwrap<AuthResponse>(api.post('/auth/refresh-token', { refreshToken })),
  getDemoCatalog: () => unwrap<DemoCatalog>(api.get('/demo/catalog', { params: noCacheParams() })),
  getAgents: () => unwrap<PagedResult<Agent>>(api.get('/agents', { params: { pageSize: 50, ...noCacheParams() } })),
  createAgent: (request: CreateAgentRequest) => unwrap<Agent>(api.post('/agents', request)),
  updateAgent: (id: string, request: UpdateAgentRequest) => unwrap<Agent>(api.put(`/agents/${id}`, request)),
  setAgentTools: (id: string, toolIds: string[]) => unwrap<Agent>(api.put(`/agents/${id}/tools`, { toolIds })),
  deleteAgent: (id: string) => api.delete(`/agents/${id}`),
  getTools: () => unwrap<PagedResult<Tool>>(api.get('/tools', { params: { pageSize: 50, ...noCacheParams() } })),
  createTool: (request: CreateToolRequest) => unwrap<Tool>(api.post('/tools', request)),
  getWorkflows: () => unwrap<PagedResult<Workflow>>(api.get('/workflows', { params: { pageSize: 50, ...noCacheParams() } })),
  getWorkflow: (id: string) => unwrap<Workflow>(api.get(`/workflows/${id}`)),
  createWorkflow: (request: CreateWorkflowRequest) => unwrap<Workflow>(api.post('/workflows', request)),
  updateWorkflow: (id: string, request: UpdateWorkflowRequest) => unwrap<Workflow>(api.put(`/workflows/${id}`, request)),
  deleteWorkflow: (id: string) => api.delete(`/workflows/${id}`),
  createWorkflowStep: (workflowId: string, request: CreateWorkflowStepRequest) =>
    unwrap<Workflow>(api.post(`/workflows/${workflowId}/steps`, request)),
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
  executeTool: (toolId: string, inputJson: string) =>
    unwrap(api.post(`/tools/${toolId}/execute`, { inputJson })),
  chat: (request: DirectChatRequest) => unwrap<DirectChatResult>(api.post('/ai-settings/chat', request)),
}

function noCacheParams() {
  return { _: Date.now() }
}
