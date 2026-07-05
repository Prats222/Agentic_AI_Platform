import { Box, Paper, Typography } from '@mui/material'
import { alpha, useTheme } from '@mui/material/styles'
import type { ReactNode } from 'react'

export function MetricTile({
  label,
  value,
  icon,
  tone = 'primary',
  helper,
  caption,
}: {
  label: string
  value: string | number
  icon: ReactNode
  tone?: 'primary' | 'secondary' | 'success' | 'warning'
  helper?: string
  caption?: string
}) {
  const theme = useTheme()
  const isDark = theme.palette.mode === 'dark'

  return (
    <Paper
      sx={{
        p: 2.2,
        minHeight: 142,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        position: 'relative',
        overflow: 'hidden',
        background: isDark
          ? 'linear-gradient(150deg, rgba(21,27,40,0.96), rgba(12,17,29,0.94))'
          : `linear-gradient(150deg, ${alpha(theme.palette.background.paper, 0.98)}, ${alpha(theme.palette.secondary.main, 0.06)})`,
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: '0 auto 0 0',
          width: 3,
          background: `linear-gradient(180deg, ${theme.palette[tone].main}, ${alpha(theme.palette[tone].main, 0.08)})`,
        },
        '&:hover': {
          transform: 'translateY(-2px)',
          borderColor: alpha(theme.palette[tone].main, 0.45),
          boxShadow: isDark ? `0 24px 80px ${alpha(theme.palette[tone].main, 0.14)}` : `0 18px 48px ${alpha(theme.palette[tone].main, 0.16)}`,
        },
      }}
    >
      <Box>
        <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 700 }}>
          {label}
        </Typography>
        <Typography variant="h3" sx={{ mt: 1, fontSize: { xs: 30, md: 38 } }}>
          {value}
        </Typography>
        {helper && (
          <Typography variant="caption" color={`${tone}.main`} sx={{ display: 'block', mt: 0.5, fontWeight: 850 }}>
            {helper}
          </Typography>
        )}
        {caption && (
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.3 }}>
            {caption}
          </Typography>
        )}
      </Box>
      <Box
        sx={{
          width: 44,
          height: 44,
          display: 'grid',
          placeItems: 'center',
          borderRadius: 2,
          bgcolor: alpha(theme.palette[tone].main, isDark ? 0.18 : 0.12),
          color: `${tone}.main`,
          border: '1px solid',
          borderColor: alpha(theme.palette[tone].main, 0.22),
        }}
      >
        {icon}
      </Box>
    </Paper>
  )
}
