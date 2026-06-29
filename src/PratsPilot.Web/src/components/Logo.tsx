import { Box, Typography } from '@mui/material'

export function Logo() {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.4 }}>
      <Box
        sx={{
          width: 42,
          height: 42,
          borderRadius: 2,
          display: 'grid',
          placeItems: 'center',
          color: '#061014',
          fontWeight: 900,
          background: 'linear-gradient(135deg, #36d3c9 0%, #ffb84d 100%)',
          boxShadow: '0 0 34px rgba(54,211,201,0.25)',
        }}
      >
        PP
      </Box>
      <Box>
        <Typography variant="h6" sx={{ lineHeight: 1, fontWeight: 900 }}>
          PratsPilot
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 700 }}>
          Agent Command Layer
        </Typography>
      </Box>
    </Box>
  )
}
