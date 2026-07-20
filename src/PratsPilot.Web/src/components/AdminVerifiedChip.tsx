import VerifiedIcon from '@mui/icons-material/Verified'
import { Chip, Tooltip } from '@mui/material'

type Props = {
  publishedAt?: string
  publishedByDisplayName?: string
}

export function AdminVerifiedChip({ publishedAt, publishedByDisplayName }: Props) {
  if (!publishedAt) return null

  const publisher = publishedByDisplayName || 'a PratsPilot administrator'
  const timestamp = new Date(publishedAt).toLocaleString()
  return (
    <Tooltip title={`Published from Admin Realm by ${publisher} on ${timestamp}`}>
      <Chip
        size="small"
        color="primary"
        variant="outlined"
        icon={<VerifiedIcon />}
        label="Admin Verified"
        sx={{ fontWeight: 800 }}
      />
    </Tooltip>
  )
}
