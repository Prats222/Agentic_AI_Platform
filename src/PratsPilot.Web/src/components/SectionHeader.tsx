import { Box, Typography } from '@mui/material'
import { alpha, useTheme } from '@mui/material/styles'
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
  const theme = useTheme()

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
          <Typography
            variant="overline"
            sx={{
              color: 'primary.main',
              fontWeight: 900,
              px: 1,
              py: 0.35,
              borderRadius: 1,
              bgcolor: alpha(theme.palette.primary.main, 0.1),
              border: '1px solid',
              borderColor: alpha(theme.palette.primary.main, 0.18),
            }}
          >
            {eyebrow}
          </Typography>
        )}
        <Typography variant="h4" sx={{ mt: eyebrow ? 1.4 : 0, maxWidth: 980 }}>
          {title}
        </Typography>
      </Box>
      {action}
    </Box>
  )
}
