import PublishIcon from '@mui/icons-material/Publish'
import { Alert, Button, Snackbar, Tooltip } from '@mui/material'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ADMIN_REALM_ID, apiClient } from '../api/client'
import type { ArtifactType } from '../api/types'
import { useAuth } from '../state/AuthContext'

type Props = {
  artifactType: ArtifactType
  artifactId: string
  artifactName: string
  realmId: string
}

export function PublishArtifactButton({ artifactType, artifactId, artifactName, realmId }: Props) {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [message, setMessage] = useState<{ severity: 'success' | 'error'; text: string }>()
  const publish = useMutation({
    mutationFn: () => apiClient.publishArtifact(artifactType, artifactId),
    onSuccess: (result) => {
      const dependencies = result.publishedDependencyCount
        ? ` ${result.publishedDependencyCount} required artifact(s) were included.`
        : ''
      setMessage({
        severity: 'success',
        text: `${result.name} ${result.wasCreated ? 'published' : 'updated'} in User Realm.${dependencies}`,
      })
      queryClient.invalidateQueries()
    },
    onError: () => setMessage({ severity: 'error', text: `Could not publish ${artifactName}. Check the API logs.` }),
  })

  if (!user?.roles.includes('Admin') || realmId !== ADMIN_REALM_ID) return null

  return (
    <>
      <Tooltip title="Copy this artifact and its required dependencies into User Realm">
        <Button
          size="small"
          variant="contained"
          startIcon={<PublishIcon />}
          disabled={publish.isPending}
          onClick={() => publish.mutate()}
        >
          {publish.isPending ? 'Publishing' : 'Publish'}
        </Button>
      </Tooltip>
      <Snackbar
        open={Boolean(message)}
        autoHideDuration={5000}
        onClose={() => setMessage(undefined)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert severity={message?.severity} variant="filled" onClose={() => setMessage(undefined)}>
          {message?.text}
        </Alert>
      </Snackbar>
    </>
  )
}
