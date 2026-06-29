export type ApiResponse<T> = {
  success: boolean
  message: string
  data: T
  errors: string[]
}

export type PagedResult<T> = {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
}

export type AuthResponse = {
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
  userId: string
  email: string
  displayName: string
  roles: string[]
}

export type SignUpRequest = {
  displayName: string
  email: string
  password: string
}

export type Agent = {
  id: string
  name: string
  description?: string
  projectName?: string
  role?: string
  goal?: string
  expectedOutput?: string
  tags?: string
  modelProvider: string
  modelName: string
  useGlobalAISettings: boolean
  aiProvider?: string
  aiModel?: string
  status: string
  toolIds: string[]
  toolNames: string[]
  createdAt: string
}

export type CreateAgentRequest = {
  name: string
  description?: string
  projectName?: string
  role?: string
  goal?: string
  expectedOutput?: string
  tags?: string
  modelProvider: string
  modelName: string
  modelConfigJson: string
  useGlobalAISettings: boolean
  aiProvider?: string
  aiModel?: string
  aiTemperature?: number
  aiMaxTokens?: number
  aiTopP?: number
  aiSystemPrompt?: string
  aiBaseUrl?: string
  status: string
}

export type UpdateAgentRequest = CreateAgentRequest

export type Tool = {
  id: string
  name: string
  description?: string
  category: string
  inputSchemaJson: string
  endpointUrl: string
  isEnabled: boolean
  createdAt: string
}

export type CreateToolRequest = {
  name: string
  description?: string
  category: string
  inputSchemaJson: string
  endpointUrl: string
  isEnabled: boolean
}

export type WorkflowStep = {
  id: string
  name: string
  description?: string
  order: number
  stepType: string
  toolId?: string
  agentId?: string
  inputMappingJson: string
}

export type Workflow = {
  id: string
  name: string
  description?: string
  status: string
  createdAt: string
  steps: WorkflowStep[]
}

export type CreateWorkflowRequest = {
  name: string
  description?: string
  status: string
}

export type UpdateWorkflowRequest = CreateWorkflowRequest

export type CreateWorkflowStepRequest = {
  name: string
  description?: string
  order: number
  stepType: string
  toolId?: string
  agentId?: string
  inputMappingJson: string
  configurationJson: string
  continueOnError: boolean
}

export type ExecutionLog = {
  id: string
  level: string
  message: string
  detailsJson?: string
  createdAt: string
}

export type Execution = {
  id: string
  targetType: string
  status: string
  agentId?: string
  workflowId?: string
  inputJson: string
  outputJson?: string
  errorMessage?: string
  startedAt?: string
  completedAt?: string
  createdAt: string
  logs: ExecutionLog[]
}

export type AISettings = {
  id: string
  provider: string
  model: string
  temperature: number
  maxTokens: number
  topP: number
  systemPrompt: string
  hasApiKey: boolean
  baseUrl?: string
}

export type DemoCatalog = {
  tools: Array<{ id: string; name: string; category: string; sampleInputJson: string }>
  agents: Array<{ id: string; name: string; sampleInputJson: string }>
  workflows: Array<{ id: string; name: string; sampleInputJson: string }>
}

export type DirectChatRequest = {
  provider: string
  model: string
  prompt: string
  temperature: number
  maxTokens: number
  topP: number
  systemPrompt: string
  baseUrl?: string
}

export type DirectChatResult = {
  provider: string
  model: string
  content: string
  rawResponseJson?: string
}

export type LLMModel = {
  id: string
  name: string
  provider: string
  description?: string
  contextLength?: number
  promptPrice: number
  completionPrice: number
  isFree: boolean
}
