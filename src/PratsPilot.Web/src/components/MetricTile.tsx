import { Box, Paper, Typography } from '@mui/material'
import { alpha, useTheme } from '@mui/material/styles'
import type { ReactNode } from 'react'

export function MetricTile({
  label,
  value,
  icon,
  tone = 'primary',
}: {
  label: string
  value: string | number
  icon: ReactNode
  tone?: 'primary' | 'secondary' | 'success' | 'warning'
}) {
  const theme = useTheme()
  const isDark = theme.palette.mode === 'dark'

  return (
    <Paper
      sx={{
        p: 2.2,
        minHeight: 118,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        background: isDark
          ? 'linear-gradient(150deg, rgba(16,27,43,0.96), rgba(8,14,24,0.92))'
          : `linear-gradient(150deg, ${alpha(theme.palette.background.paper, 0.98)}, ${alpha(theme.palette.primary.main, 0.08)})`,
      }}
    >
      <Box>
        <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 700 }}>
          {label}
        </Typography>
        <Typography variant="h3" sx={{ mt: 1, fontSize: { xs: 30, md: 38 } }}>
          {value}
        </Typography>
      </Box>
      <Box
        sx={{
          width: 44,
          height: 44,
          display: 'grid',
          placeItems: 'center',
          borderRadius: 2,
          bgcolor: `${tone}.main`,
          color: tone === 'primary' || tone === 'secondary' ? `${tone}.contrastText` : '#061014',
        }}
      >
        {icon}
      </Box>
    </Paper>
  )
}
