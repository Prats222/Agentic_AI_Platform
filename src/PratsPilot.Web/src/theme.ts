import { createTheme } from '@mui/material/styles'
import type { PaletteMode } from '@mui/material'

export function createPratsPilotTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      background: {
        default: isDark ? '#0B0F19' : '#F6F7FB',
        paper: isDark ? 'rgba(21, 27, 40, 0.92)' : 'rgba(255, 255, 255, 0.94)',
      },
      primary: {
        main: '#7C5CFC',
        contrastText: '#FFFFFF',
      },
      secondary: {
        main: '#4F8CFF',
        contrastText: '#FFFFFF',
      },
      success: {
        main: '#3DDC97',
      },
      warning: {
        main: '#F9C74F',
      },
      error: {
        main: '#FF6B6B',
      },
      text: {
        primary: isDark ? '#F7F8FC' : '#121826',
        secondary: isDark ? '#A7B0C0' : '#5C677A',
      },
      divider: isDark ? 'rgba(148, 163, 184, 0.16)' : 'rgba(37, 47, 66, 0.14)',
    },
    shape: {
      borderRadius: 8,
    },
    typography: {
      fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif',
      h1: { letterSpacing: 0, fontWeight: 850 },
      h2: { letterSpacing: 0, fontWeight: 850 },
      h3: { letterSpacing: 0, fontWeight: 850 },
      h4: { letterSpacing: 0, fontWeight: 850 },
      h5: { letterSpacing: 0, fontWeight: 850 },
      h6: { letterSpacing: 0, fontWeight: 850 },
      button: { textTransform: 'none', fontWeight: 700, letterSpacing: 0 },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            '--app-bg': isDark ? '#0B0F19' : '#F6F7FB',
            '--app-bg-a': isDark ? 'rgba(124, 92, 252, 0.16)' : 'rgba(124, 92, 252, 0.12)',
            '--app-bg-b': isDark ? 'rgba(79, 140, 255, 0.12)' : 'rgba(79, 140, 255, 0.1)',
            '--app-bg-c': isDark ? 'rgba(255, 93, 177, 0.08)' : 'rgba(255, 93, 177, 0.08)',
            '--app-scroll-track': isDark ? '#0E1420' : '#E9EDF5',
            '--app-scroll-thumb': isDark ? '#39445A' : '#B9C2D4',
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            backgroundImage: 'none',
            border: isDark ? '1px solid rgba(148, 163, 184, 0.16)' : '1px solid rgba(37, 47, 66, 0.12)',
            boxShadow: isDark ? '0 22px 70px rgba(0,0,0,0.34)' : '0 18px 50px rgba(17, 24, 39, 0.1)',
            transition: 'border-color 180ms ease, box-shadow 180ms ease, transform 180ms ease',
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 8,
            boxShadow: 'none',
          },
          contained: {
            backgroundImage: 'linear-gradient(135deg, #7C5CFC 0%, #4F8CFF 100%)',
            '&:hover': {
              boxShadow: isDark ? '0 0 30px rgba(124, 92, 252, 0.28)' : '0 14px 30px rgba(79, 140, 255, 0.22)',
            },
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
            borderColor: isDark ? 'rgba(148, 163, 184, 0.12)' : 'rgba(37, 47, 66, 0.12)',
          },
          head: {
            color: isDark ? '#DDE3F0' : '#2B3445',
            fontWeight: 850,
          },
        },
      },
      MuiTextField: {
        defaultProps: {
          variant: 'outlined',
        },
      },
      MuiLinearProgress: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? 'rgba(148, 163, 184, 0.14)' : 'rgba(37, 47, 66, 0.1)',
          },
        },
      },
    },
  })
}
