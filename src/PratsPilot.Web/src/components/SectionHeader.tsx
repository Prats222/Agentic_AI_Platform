import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'

export function SectionHeader({
  title,
  eyebrow,
  action,
}: {
  title: string
  eyebrow?: string
  action?: ReactNode
}) {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: { xs: 'flex-start', sm: 'center' },
        flexDirection: { xs: 'column', sm: 'row' },
        gap: 2,
        mb: 3,
      }}
    >
      <Box>
        {eyebrow && (
          <Typography variant="overline" color="primary.main" sx={{ fontWeight: 900 }}>
            {eyebrow}
          </Typography>
        )}
        <Typography variant="h4">{title}</Typography>
      </Box>
      {action}
    </Box>
  )
}
