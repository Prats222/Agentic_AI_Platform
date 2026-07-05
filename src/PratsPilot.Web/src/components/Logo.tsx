import { Box, Typography } from '@mui/material'
import { alpha, useTheme } from '@mui/material/styles'

export function Logo() {
  const theme = useTheme()
  const isDark = theme.palette.mode === 'dark'

  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.4 }}>
      <Box
        sx={{
          width: 46,
          height: 46,
          borderRadius: 2.2,
          display: 'grid',
          placeItems: 'center',
          position: 'relative',
          overflow: 'hidden',
          background: 'linear-gradient(135deg, #7C5CFC 0%, #4F8CFF 58%, #FF5DB1 100%)',
          boxShadow: isDark
            ? '0 0 34px rgba(124, 92, 252, 0.32), inset 0 1px 0 rgba(255,255,255,0.2)'
            : '0 16px 36px rgba(79, 140, 255, 0.24), inset 0 1px 0 rgba(255,255,255,0.45)',
          '&::before': {
            content: '""',
            position: 'absolute',
            width: 62,
            height: 24,
            border: '1.5px solid rgba(255,255,255,0.78)',
            borderLeftColor: 'transparent',
            borderBottomColor: 'transparent',
            borderRadius: '50%',
            transform: 'rotate(-28deg)',
            right: -12,
            top: 10,
          },
          '&::after': {
            content: '""',
            position: 'absolute',
            width: 6,
            height: 6,
            borderRadius: '50%',
            bgcolor: '#FFFFFF',
            right: 9,
            top: 13,
            boxShadow: '0 0 14px rgba(255,255,255,0.95)',
          },
        }}
      >
        <Typography
          component="span"
          sx={{
            color: '#FFFFFF',
            fontSize: 25,
            fontWeight: 950,
            lineHeight: 1,
            letterSpacing: 0,
            position: 'relative',
            zIndex: 1,
            textShadow: '0 2px 12px rgba(0,0,0,0.28)',
          }}
        >
          P
        </Typography>
        <Box
          sx={{
            position: 'absolute',
            width: 14,
            height: 2.5,
            borderRadius: 999,
            bgcolor: alpha('#FFFFFF', 0.88),
            left: 18,
            bottom: 12,
            transform: 'rotate(-25deg)',
            boxShadow: '10px -5px 0 -1px rgba(255,255,255,0.5)',
          }}
        />
      </Box>
      <Box>
        <Typography variant="h6" sx={{ lineHeight: 1, fontWeight: 950 }}>
          PratsPilot
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 750 }}>
          Mission Control AI
        </Typography>
      </Box>
    </Box>
  )
}
