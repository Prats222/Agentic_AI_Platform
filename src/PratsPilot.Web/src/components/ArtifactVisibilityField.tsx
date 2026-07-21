import { Chip, MenuItem, Stack, TextField, Typography } from '@mui/material'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import PublicOutlinedIcon from '@mui/icons-material/PublicOutlined'
import type { ArtifactVisibility } from '../api/types'

type Props = {
  value: ArtifactVisibility
  onChange: (value: ArtifactVisibility) => void
}

export function ArtifactVisibilityField({ value, onChange }: Props) {
  return (
    <TextField
      select
      label="Visibility"
      value={value}
      onChange={(event) => onChange(event.target.value as ArtifactVisibility)}
      helperText={value === 'Private' ? 'Only you and administrators can access it.' : 'Everyone in the User Realm can view and use it.'}
      fullWidth
    >
      <MenuItem value="Private">
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <LockOutlinedIcon fontSize="small" />
          <Typography>Only me</Typography>
        </Stack>
      </MenuItem>
      <MenuItem value="Realm">
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <PublicOutlinedIcon fontSize="small" />
          <Typography>User Realm</Typography>
        </Stack>
      </MenuItem>
    </TextField>
  )
}

export function ArtifactVisibilityChip({ visibility }: { visibility: ArtifactVisibility }) {
  return (
    <Chip
      size="small"
      variant="outlined"
      icon={visibility === 'Private' ? <LockOutlinedIcon /> : <PublicOutlinedIcon />}
      label={visibility === 'Private' ? 'Only me' : 'User Realm'}
      color={visibility === 'Private' ? 'default' : 'primary'}
    />
  )
}
