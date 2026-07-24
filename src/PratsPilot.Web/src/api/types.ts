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

export type RegistrationResult = {
  email: string
  requiresEmailConfirmation: boolean
  confirmationEmailSent: boolean
}

export type ArtifactVisibility = 'Private' | 'Realm'

export type Agent = {
  id: string
  realmId: string
  name: string
  description?: string
  projectName?: string
  role?: string
  goal?: string
  expectedOutput?: string
  tags?: string
  modelProvider: string
  modelName: string
  inputSchemaJson: string
  useGlobalAISettings: boolean
  aiProvider?: string
  aiModel?: string
  status: string
  visibility: ArtifactVisibility
  toolIds: string[]
  toolNames: string[]
  contextDocumentIds: string[]
  contextDocumentNames: string[]
  createdAt: string
  createdByUserId?: string
  createdByDisplayName?: string
  publishedFromArtifactId?: string
  publishedAt?: string
  publishedByUserId?: string
  publishedByDisplayName?: string
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
  inputSchemaJson: string
  useGlobalAISettings: boolean
  aiProvider?: string
  aiModel?: string
  aiTemperature?: number
  aiMaxTokens?: number
  aiTopP?: number
  aiSystemPrompt?: string
  aiBaseUrl?: string
  status: string
  visibility: ArtifactVisibility
}

export type UpdateAgentRequest = CreateAgentRequest

export type Tool = {
  id: string
  realmId: string
  name: string
  description?: string
  category: string
  inputSchemaJson: string
  endpointUrl: string
  secretJson?: string
  hasSecrets?: boolean
  isEnabled: boolean
  visibility: ArtifactVisibility
  createdAt: string
  createdByUserId?: string
  createdByDisplayName?: string
  publishedFromArtifactId?: string
  publishedAt?: string
  publishedByUserId?: string
  publishedByDisplayName?: string
}

export type CreateToolRequest = {
  name: string
  description?: string
  category: string
  inputSchemaJson: string
  endpointUrl: string
  secretJson?: string
  isEnabled: boolean
  visibility: ArtifactVisibility
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
  realmId: string
  name: string
  description?: string
  status: string
  visibility: ArtifactVisibility
  createdAt: string
  createdByUserId?: string
  createdByDisplayName?: string
  publishedFromArtifactId?: string
  publishedAt?: string
  publishedByUserId?: string
  publishedByDisplayName?: string
  steps: WorkflowStep[]
}

export type CreateWorkflowRequest = {
  name: string
  description?: string
  status: string
  visibility: ArtifactVisibility
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
  realmId: string
  targetType: string
  status: string
  agentId?: string
  workflowId?: string
  inputJson: string
  outputJson?: string
  errorMessage?: string
  durationMs?: number
  provider?: string
  model?: string
  estimatedInputTokens?: number
  estimatedOutputTokens?: number
  estimatedCostUsd?: number
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
  hasGeminiApiKey: boolean
  hasOpenRouterApiKey: boolean
  hasGroqApiKey: boolean
  hasCerebrasApiKey: boolean
  hasDeepSeekApiKey: boolean
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

export type ChatMessage = {
  id: string
  role: 'user' | 'assistant'
  content: string
  createdAt: string
}

export type ChatConversation = {
  id: string
  title: string
  provider: string
  model: string
  createdAt: string
  updatedAt: string
  messages: ChatMessage[]
}

export type StreamChatRequest = {
  conversationId?: string
  provider: string
  model: string
  prompt: string
  temperature: number
  maxTokens: number
  topP: number
  systemPrompt: string
  baseUrl?: string
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

export type ContextDocument = {
  id: string
  realmId: string
  name: string
  fileName: string
  contentType: string
  fileExtension: string
  sizeBytes: number
  visibility: ArtifactVisibility
  createdAt: string
  createdByUserId?: string
  createdByDisplayName?: string
  publishedFromArtifactId?: string
  publishedAt?: string
  publishedByUserId?: string
  publishedByDisplayName?: string
}

export type ArtifactType = 'agent' | 'workflow' | 'tool' | 'context-document'

export type ArtifactPublishResult = {
  artifactType: string
  sourceArtifactId: string
  publishedArtifactId: string
  name: string
  wasCreated: boolean
  publishedDependencyCount: number
  publishedAt: string
}

export type HumanApprovalRequest = {
  id: string
  executionId: string
  workflowStepId: string
  title: string
  instructions: string
  payloadJson: string
  isApproved: boolean
  isRejected: boolean
  reviewerComment?: string
  reviewedAt?: string
  createdAt: string
}

export type Realm = {
  id: string
  name: string
  description: string
  isAdminOnly: boolean
}

export type UserAccess = {
  id: string
  email: string
  displayName: string
  roles: string[]
  canAccessUserRealm: boolean
  canAccessAdminRealm: boolean
  emailConfirmed: boolean
  welcomeGuideEmailSentAt?: string
  createdAt: string
  isDemoUser: boolean
}

export type AdminUsersPage = {
  items: UserAccess[]
  joinedToday: UserAccess[]
  pageNumber: number
  pageSize: number
  totalCount: number
  matchingCount: number
  joinedTodayCount: number
  totalPages: number
}

export type ArenaEntry = {
  id: string
  challengeId: string
  submittedByUserId: string
  submittedByDisplayName: string
  agentId: string
  agentName: string
  output?: string
  score?: number
  feedback?: string
  durationMs?: number
  provider?: string
  model?: string
  createdAt: string
}

export type ArenaChallenge = {
  id: string
  realmId: string
  createdByUserId: string
  createdByDisplayName: string
  title: string
  description: string
  taskPrompt: string
  rules: string
  expectedOutput: string
  judgeCriteria: string
  status: string
  winnerEntryId?: string
  judgeSummary?: string
  scorecardJson?: string
  completedAt?: string
  createdAt: string
  entries: ArenaEntry[]
}

export type CreateArenaChallengeRequest = {
  title: string
  description: string
  taskPrompt: string
  rules: string
  expectedOutput: string
  judgeCriteria: string
}
