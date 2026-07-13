import { Alert, Box, Button, Checkbox, Chip, Paper, Stack, TextField, Typography } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiClient } from '../api/client'
import { providerDefaults } from '../api/modelCatalog'
import { SectionHeader } from '../components/SectionHeader'

export function AutopilotPage() {
  const queryClient = useQueryClient()
  const tools = useQuery({ queryKey: ['tools'], queryFn: apiClient.getTools })
  const [goal, setGoal] = useState('Create an end-to-end workflow that generates code, reviews it, and returns final output.')
  const [workflowName, setWorkflowName] = useState('Autopilot Workflow')
  const [includeApproval, setIncludeApproval] = useState(false)

  const agentDrafts = useMemo(() => buildAgentDrafts(goal, workflowName), [goal, workflowName])
  const webSearchTool = useMemo(
    () => (tools.data?.items ?? []).find((tool) => tool.category === 'WebSearch'),
    [tools.data],
  )
  const shouldAttachWebSearch = needsWebSearch(goal) && Boolean(webSearchTool)

  const createDraft = useMutation({
    mutationFn: async () => {
      const createdAgents = []
      for (const draft of agentDrafts) {
        const agent = await apiClient.createAgent(buildCreateAgentRequest(draft, goal))
        if (shouldAttachWebSearch && webSearchTool && draft.attachWebSearch) {
          await apiClient.setAgentTools(agent.id, [webSearchTool.id])
        }
        createdAgents.push(agent)
      }

      const workflow = await apiClient.createWorkflow({
        name: workflowName,
        description: `Autopilot generated: ${goal}`,
        status: 'Active',
      })

      let order = 1
      for (const [index, agent] of createdAgents.entries()) {
        await apiClient.createWorkflowStep(workflow.id, {
          name: agent.name,
          order: order++,
          stepType: 'Agent',
          agentId: agent.id,
          inputMappingJson: index === 0 ? '{"source":"original"}' : '{}',
          configurationJson: '{}',
          continueOnError: false,
        })

        if (includeApproval && index < createdAgents.length - 1) {
          await apiClient.createWorkflowStep(workflow.id, {
            name: `Approve ${agent.name} Output`,
            order: order++,
            stepType: 'HumanApproval',
            inputMappingJson: '{}',
            configurationJson: '{"instructions":"Review this step output before the next agent continues."}',
            continueOnError: false,
          })
        }
      }

      return workflow
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agents'] })
      queryClient.invalidateQueries({ queryKey: ['workflows'] })
    },
  })

  return (
    <Box>
      <SectionHeader eyebrow="Workflow Autopilot" title="Describe the outcome. Let PratsPilot draft the workflow." />
      <Paper sx={{ p: 3 }}>
        <Stack spacing={2.2}>
          <TextField label="Workflow Name" value={workflowName} onChange={(event) => setWorkflowName(event.target.value)} fullWidth />
          <TextField label="Desired Outcome" value={goal} onChange={(event) => setGoal(event.target.value)} multiline minRows={5} fullWidth />
          <Stack direction="row" sx={{ alignItems: 'center', gap: 1 }}>
            <Checkbox checked={includeApproval} onChange={(event) => setIncludeApproval(event.target.checked)} />
            <Typography>Insert human approval gates between matched agents</Typography>
          </Stack>
          <Box>
            <Typography variant="h6" sx={{ mb: 1 }}>Autopilot Will Create</Typography>
            <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
              {agentDrafts.map((agent, index) => (
                <Chip key={agent.name} color="primary" label={`${index + 1}. ${agent.name}`} />
              ))}
              {shouldAttachWebSearch && <Chip color="secondary" label={`Attaches ${webSearchTool?.name}`} />}
            </Stack>
          </Box>
          <Button
            variant="contained"
            startIcon={<AutoFixHighIcon />}
            onClick={() => createDraft.mutate()}
            disabled={!workflowName || agentDrafts.length === 0 || createDraft.isPending}
          >
            Create Agents and Workflow
          </Button>
          {createDraft.isSuccess && <Alert severity="success">Fresh agents and workflow created. Open Workflows to refine it.</Alert>}
          {createDraft.isError && <Alert severity="error">Autopilot could not create the workflow draft.</Alert>}
        </Stack>
      </Paper>
    </Box>
  )
}

type AgentDraft = {
  name: string
  role: string
  goal: string
  expectedOutput: string
  attachWebSearch?: boolean
}

function buildAgentDrafts(goal: string, workflowName: string): AgentDraft[] {
  const count = Math.min(Math.max(Number(goal.match(/(\d+)\s+agents?/i)?.[1] ?? 2), 1), 4)
  const text = goal.toLowerCase()
  const prefix = workflowName.trim() || 'Autopilot'

  if (text.includes('kb') || text.includes('knowledge') || text.includes('document') || text.includes('summar')) {
    return [
      {
        name: `${prefix} Knowledge Summarizer`,
        role: 'Knowledge summarization agent',
        goal: 'Read the attached knowledge base context and produce a concise, grounded summary.',
        expectedOutput: 'A clear summary with the most important points from the KB only.',
      },
      {
        name: `${prefix} Validation Analyst`,
        role: 'Evidence validation agent',
        goal: 'Review the prior summary and extract valid, useful, non-duplicate points grounded in the KB.',
        expectedOutput: 'Validated bullet points with weak or unsupported claims removed.',
      },
      {
        name: `${prefix} Final Packager`,
        role: 'Final response formatter',
        goal: 'Convert the validated points into a polished final answer.',
        expectedOutput: 'A clean final response ready for the user.',
      },
    ].slice(0, count)
  }

  if (text.includes('code') || text.includes('script') || text.includes('test')) {
    return [
      {
        name: `${prefix} Solution Builder`,
        role: 'Implementation agent',
        goal: 'Create the requested code or automation solution from the user input.',
        expectedOutput: 'Working code with minimal explanation unless requested.',
      },
      {
        name: `${prefix} Code Reviewer`,
        role: 'Quality review agent',
        goal: 'Review the generated solution for correctness, edge cases, clarity, and constraints.',
        expectedOutput: 'A concise review plus corrected code if improvements are needed.',
      },
      {
        name: `${prefix} Finalizer`,
        role: 'Delivery agent',
        goal: 'Prepare the final answer by applying the reviewer feedback.',
        expectedOutput: 'Final production-ready output.',
      },
    ].slice(0, count)
  }

  if (needsWebSearch(goal)) {
    return [
      {
        name: `${prefix} Web Researcher`,
        role: 'Live research agent',
        goal: 'Use attached web search context to collect current facts relevant to the user request.',
        expectedOutput: 'Relevant current findings with sources when available.',
        attachWebSearch: true,
      },
      {
        name: `${prefix} Insight Synthesizer`,
        role: 'Synthesis agent',
        goal: 'Turn the research findings into a clear answer with uncertainty called out.',
        expectedOutput: 'A crisp answer grounded in the search results.',
      },
    ].slice(0, count)
  }

  return [
    {
      name: `${prefix} Planner`,
      role: 'Planning agent',
      goal: 'Break the user goal into a practical plan and identify the key information needed.',
      expectedOutput: 'A concise plan with assumptions and required outputs.',
    },
    {
      name: `${prefix} Executor`,
      role: 'Execution agent',
      goal: 'Complete the task using the planner output and produce the requested result.',
      expectedOutput: 'The final task result.',
    },
    {
      name: `${prefix} Reviewer`,
      role: 'Review agent',
      goal: 'Check the result for completeness, correctness, and clarity.',
      expectedOutput: 'Final validated output.',
    },
  ].slice(0, count)
}

function buildCreateAgentRequest(draft: AgentDraft, originalGoal: string) {
  return {
    name: draft.name,
    description: `Autopilot-created agent for: ${originalGoal}`,
    projectName: 'Autopilot',
    role: draft.role,
    goal: draft.goal,
    expectedOutput: draft.expectedOutput,
    tags: 'autopilot, generated',
    modelProvider: 'Global',
    modelName: 'Global default',
    modelConfigJson: '{}',
    inputSchemaJson: '{}',
    useGlobalAISettings: true,
    aiProvider: undefined,
    aiModel: undefined,
    aiBaseUrl: providerDefaults.Groq.baseUrl,
    aiTemperature: 0.2,
    aiMaxTokens: 2048,
    aiTopP: 0.9,
    aiSystemPrompt: `Role: ${draft.role}\nGoal: ${draft.goal}\nExpected output: ${draft.expectedOutput}`,
    status: 'Active',
  }
}

function needsWebSearch(value: string) {
  const text = value.toLowerCase()
  return ['web', 'search', 'latest', 'current', 'today', 'news', 'weather', 'score', 'live'].some((word) => text.includes(word))
}
