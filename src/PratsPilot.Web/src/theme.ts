import { createTheme } from '@mui/material/styles'
import type { PaletteMode } from '@mui/material'

export function createPratsPilotTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      background: {
        default: isDark ? '#070b12' : '#edf5f7',
        paper: isDark ? 'rgba(11, 18, 29, 0.92)' : 'rgba(255, 255, 255, 0.92)',
      },
      primary: {
        main: '#36d3c9',
        contrastText: '#061014',
      },
      secondary: {
        main: '#ffb84d',
        contrastText: '#12100a',
      },
      success: {
        main: isDark ? '#63e6a8' : '#1c9b67',
      },
      warning: {
        main: '#ffb84d',
      },
      error: {
        main: '#ff6b7a',
      },
      text: {
        primary: isDark ? '#eef7fb' : '#12202e',
        secondary: isDark ? '#91a4b7' : '#52687a',
      },
      divider: isDark ? 'rgba(146, 221, 230, 0.14)' : 'rgba(36, 71, 86, 0.16)',
    },
    shape: {
      borderRadius: 8,
    },
    typography: {
      fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif',
      h1: { letterSpacing: 0, fontWeight: 800 },
      h2: { letterSpacing: 0, fontWeight: 800 },
      h3: { letterSpacing: 0, fontWeight: 800 },
      h4: { letterSpacing: 0, fontWeight: 800 },
      h5: { letterSpacing: 0, fontWeight: 800 },
      h6: { letterSpacing: 0, fontWeight: 800 },
      button: { textTransform: 'none', fontWeight: 700, letterSpacing: 0 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            '--app-bg': isDark ? '#070b12' : '#edf5f7',
            '--app-bg-a': isDark ? 'rgba(54, 211, 201, 0.14)' : 'rgba(54, 211, 201, 0.18)',
            '--app-bg-b': isDark ? 'rgba(255, 184, 77, 0.1)' : 'rgba(255, 184, 77, 0.16)',
            '--app-scroll-track': isDark ? '#09111c' : '#dbe8ec',
            '--app-scroll-thumb': isDark ? '#2b5361' : '#8ab6c0',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            border: isDark ? '1px solid rgba(146, 221, 230, 0.14)' : '1px solid rgba(36, 71, 86, 0.12)',
            boxShadow: isDark ? '0 18px 60px rgba(0,0,0,0.28)' : '0 18px 50px rgba(34, 67, 83, 0.12)',
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 8,
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            fontWeight: 700,
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            borderColor: isDark ? 'rgba(146, 221, 230, 0.12)' : 'rgba(36, 71, 86, 0.12)',
          },
        },
      },
    },
  })
}
