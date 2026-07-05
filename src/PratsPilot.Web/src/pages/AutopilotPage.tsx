import { Alert, Box, Button, Checkbox, Chip, Paper, Stack, TextField, Typography } from '@mui/material'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { apiClient } from '../api/client'
import { SectionHeader } from '../components/SectionHeader'

export function AutopilotPage() {
  const queryClient = useQueryClient()
  const agents = useQuery({ queryKey: ['agents'], queryFn: apiClient.getAgents })
  const [goal, setGoal] = useState('Create an end-to-end workflow that generates code, reviews it, and returns final output.')
  const [workflowName, setWorkflowName] = useState('Autopilot Workflow')
  const [includeApproval, setIncludeApproval] = useState(true)

  const selectedAgents = useMemo(() => {
    const words = goal.toLowerCase().split(/\W+/).filter(Boolean)
    return (agents.data?.items ?? [])
      .map((agent) => ({
        agent,
        score: [agent.name, agent.role, agent.goal, agent.tags, agent.description]
          .filter(Boolean)
          .join(' ')
          .toLowerCase()
          .split(/\W+/)
          .filter((word) => words.includes(word)).length,
      }))
      .sort((left, right) => right.score - left.score)
      .slice(0, 3)
      .map((item) => item.agent)
  }, [agents.data, goal])

  const createDraft = useMutation({
    mutationFn: async () => {
      const workflow = await apiClient.createWorkflow({
        name: workflowName,
        description: goal,
        status: 'Active',
      })

      let order = 1
      for (const [index, agent] of selectedAgents.entries()) {
        await apiClient.createWorkflowStep(workflow.id, {
          name: agent.name,
          order: order++,
          stepType: 'Agent',
          agentId: agent.id,
          inputMappingJson: index === 0 ? '{"source":"original"}' : '{}',
          configurationJson: '{}',
          continueOnError: false,
        })

        if (includeApproval && index < selectedAgents.length - 1) {
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
            <Typography variant="h6" sx={{ mb: 1 }}>Suggested Agent Chain</Typography>
            <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
              {selectedAgents.map((agent, index) => (
                <Chip key={agent.id} color="primary" label={`${index + 1}. ${agent.name}`} />
              ))}
              {!selectedAgents.length && <Chip label="Create agents first to use Autopilot." />}
            </Stack>
          </Box>
          <Button
            variant="contained"
            startIcon={<AutoFixHighIcon />}
            onClick={() => createDraft.mutate()}
            disabled={!workflowName || selectedAgents.length === 0 || createDraft.isPending}
          >
            Create Workflow Draft
          </Button>
          {createDraft.isSuccess && <Alert severity="success">Workflow draft created. Open Workflows to refine it.</Alert>}
          {createDraft.isError && <Alert severity="error">Autopilot could not create the workflow draft.</Alert>}
        </Stack>
      </Paper>
    </Box>
  )
}
