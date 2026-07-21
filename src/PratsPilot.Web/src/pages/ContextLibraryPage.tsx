import { Alert, Box, Button, Chip, Paper, Stack, TextField, Typography } from '@mui/material'
import DeleteIcon from '@mui/icons-material/Delete'
import UploadFileIcon from '@mui/icons-material/UploadFile'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { apiClient } from '../api/client'
import { getApiErrorMessage } from '../api/errorMessage'
import type { ContextDocument } from '../api/types'
import { DataPanel } from '../components/DataPanel'
import { AdminVerifiedChip } from '../components/AdminVerifiedChip'
import { ArtifactVisibilityChip, ArtifactVisibilityField } from '../components/ArtifactVisibilityField'
import { PublishArtifactButton } from '../components/PublishArtifactButton'
import { SectionHeader } from '../components/SectionHeader'
import { useAuth } from '../state/AuthContext'

export function ContextLibraryPage() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const documents = useQuery({ queryKey: ['contextDocuments'], queryFn: apiClient.getContextDocuments })
  const [name, setName] = useState('')
  const [file, setFile] = useState<File | undefined>()
  const [visibility, setVisibility] = useState<'Private' | 'Realm'>('Private')
  const [dragging, setDragging] = useState(false)

  const upload = useMutation({
    mutationFn: () => apiClient.uploadContextDocument(file!, name, visibility),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contextDocuments'] })
      setName('')
      setFile(undefined)
      setVisibility('Private')
    },
  })

  const remove = useMutation({
    mutationFn: (id: string) => apiClient.deleteContextDocument(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contextDocuments'] })
    },
  })

  const canModify = (document: ContextDocument) => Boolean(
    user?.roles.includes('Admin') || !document.createdByUserId || document.createdByUserId === user?.userId,
  )

  return (
    <Box>
      <SectionHeader eyebrow="Context Layer" title="Upload reusable project knowledge" />
      <Paper sx={{ p: 3, mb: 2.5 }}>
        <Stack spacing={2}>
          <Typography variant="h5">Context Document Upload</Typography>
          <Typography variant="body2" color="text.secondary">
            Supported files: text, JSON, Markdown, CSV, PDF, DOCX, and Excel. Drag a file here to avoid slow Windows folder dialogs.
          </Typography>
          <TextField label="Document Name" value={name} onChange={(event) => setName(event.target.value)} fullWidth />
          <ArtifactVisibilityField value={visibility} onChange={setVisibility} />
          <Paper
            variant="outlined"
            onDragOver={(event) => {
              event.preventDefault()
              setDragging(true)
            }}
            onDragLeave={() => setDragging(false)}
            onDrop={(event) => {
              event.preventDefault()
              setDragging(false)
              const droppedFile = event.dataTransfer.files?.[0]
              if (droppedFile && isSupportedContextFile(droppedFile)) {
                setFile(droppedFile)
                if (!name) {
                  setName(droppedFile.name.replace(/\.[^.]+$/, ''))
                }
              }
            }}
            sx={{
              p: 3,
              textAlign: 'center',
              borderStyle: 'dashed',
              borderColor: dragging ? 'primary.main' : 'divider',
              bgcolor: dragging ? 'action.selected' : 'background.default',
            }}
          >
            <UploadFileIcon color="primary" />
            <Typography sx={{ fontWeight: 900, mt: 1 }}>
              {file ? file.name : 'Drop a context document here'}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              Allowed: .txt, .json, .md, .csv, .pdf, .docx, .xlsx
            </Typography>
          </Paper>
          <Button variant="outlined" component="label" startIcon={<UploadFileIcon />}>
            {file ? file.name : 'Choose Document'}
            <input
              hidden
              type="file"
              accept=".txt,.json,.md,.csv,.pdf,.docx,.xlsx"
              onChange={(event) => {
                const selectedFile = event.target.files?.[0]
                setFile(selectedFile)
                if (selectedFile && !name) {
                  setName(selectedFile.name.replace(/\.[^.]+$/, ''))
                }
              }}
            />
          </Button>
          <Button variant="contained" onClick={() => upload.mutate()} disabled={!file || upload.isPending}>
            Upload Context
          </Button>
          {upload.isSuccess && <Alert severity="success">Context document uploaded.</Alert>}
          {upload.isError && <Alert severity="error">{getApiErrorMessage(upload.error, 'Upload failed. Check file type and size.')}</Alert>}
        </Stack>
      </Paper>
      <DataPanel<ContextDocument>
        title="Context Documents"
        subtitle="Attach these documents to agents from the Agent Builder."
        rows={documents.data ?? []}
        loading={documents.isLoading}
        columns={[
          {
            key: 'name',
            label: 'Document',
            render: (row) => (
              <Box>
                <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
                  <Typography sx={{ fontWeight: 900 }}>{row.name}</Typography>
                  <AdminVerifiedChip publishedAt={row.publishedAt} publishedByDisplayName={row.publishedByDisplayName} />
                  <ArtifactVisibilityChip visibility={row.visibility} />
                </Stack>
                <Typography variant="caption" color="text.secondary">
                  {row.fileName}
                </Typography>
              </Box>
            ),
          },
          { key: 'type', label: 'Type', render: (row) => <Chip size="small" label={row.fileExtension} /> },
          { key: 'size', label: 'Size', render: (row) => `${Math.ceil(row.sizeBytes / 1024)} KB` },
          {
            key: 'actions',
            label: 'Actions',
            render: (row) => (
              <Stack direction="row" sx={{ gap: 1, flexWrap: 'wrap' }}>
                <PublishArtifactButton artifactType="context-document" artifactId={row.id} artifactName={row.name} realmId={row.realmId} />
                {canModify(row) ? (
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    startIcon={<DeleteIcon />}
                    onClick={() => window.confirm(`Delete context document "${row.name}"?`) && remove.mutate(row.id)}
                  >
                    Delete
                  </Button>
                ) : <Chip size="small" label="View only" />}
              </Stack>
            ),
          },
        ]}
      />
    </Box>
  )
}

function isSupportedContextFile(file: File) {
  return ['.txt', '.json', '.md', '.csv', '.pdf', '.docx', '.xlsx'].some((extension) =>
    file.name.toLowerCase().endsWith(extension),
  )
}
