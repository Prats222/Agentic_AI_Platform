import {
  Box,
  Button,
  Chip,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  useMediaQuery,
} from '@mui/material'
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome'
import DashboardIcon from '@mui/icons-material/Dashboard'
import HubIcon from '@mui/icons-material/Hub'
import LogoutIcon from '@mui/icons-material/Logout'
import MemoryIcon from '@mui/icons-material/Memory'
import MenuIcon from '@mui/icons-material/Menu'
import PlayCircleIcon from '@mui/icons-material/PlayCircle'
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing'
import PsychologyIcon from '@mui/icons-material/Psychology'
import SettingsSuggestIcon from '@mui/icons-material/SettingsSuggest'
import ChatIcon from '@mui/icons-material/Chat'
import DarkModeIcon from '@mui/icons-material/DarkMode'
import LightModeIcon from '@mui/icons-material/LightMode'
import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { Logo } from './Logo'
import { useAuth } from '../state/AuthContext'
import { useTheme, alpha } from '@mui/material/styles'
import { useThemeMode } from '../state/ThemeModeContext'

const drawerWidth = 292

const navItems = [
  { label: 'Dashboard', path: '/', icon: <DashboardIcon /> },
  { label: 'Agents', path: '/agents', icon: <PsychologyIcon /> },
  { label: 'Workflows', path: '/workflows', icon: <HubIcon /> },
  { label: 'Tools', path: '/tools', icon: <PrecisionManufacturingIcon /> },
  { label: 'Executions', path: '/executions', icon: <PlayCircleIcon /> },
  { label: 'Chat', path: '/chat', icon: <ChatIcon /> },
  { label: 'AI Settings', path: '/ai-settings', icon: <SettingsSuggestIcon /> },
]

export function AppShell() {
  const theme = useTheme()
  const [mobileOpen, setMobileOpen] = useState(false)
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'))
  const { user, logout } = useAuth()
  const { mode, toggleMode } = useThemeMode()
  const location = useLocation()
  const isDark = mode === 'dark'

  const drawer = (
    <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column', p: 2 }}>
      <Logo />
      <Box
        sx={{
          mt: 3,
          p: 2,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 2,
          bgcolor: alpha(theme.palette.primary.main, isDark ? 0.06 : 0.1),
        }}
      >
        <Chip size="small" icon={<MemoryIcon />} label=".NET 10 Runtime" color="primary" />
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
          Live backend control surface for agents, tools, workflows, and execution telemetry.
        </Typography>
      </Box>
      <List sx={{ mt: 2, display: 'grid', gap: 0.5 }}>
        {navItems.map((item) => {
          const selected = item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path)
          return (
            <ListItemButton
              key={item.path}
              component={NavLink}
              to={item.path}
              selected={selected}
              onClick={() => setMobileOpen(false)}
              sx={{
                borderRadius: 2,
                minHeight: 48,
                '&.Mui-selected': {
                  bgcolor: alpha(theme.palette.primary.main, isDark ? 0.14 : 0.18),
                  color: 'primary.main',
                },
              }}
            >
              <ListItemIcon sx={{ color: 'inherit', minWidth: 40 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} slotProps={{ primary: { sx: { fontWeight: 800 } } }} />
            </ListItemButton>
          )
        })}
      </List>
      <Box sx={{ flex: 1 }} />
      <Divider sx={{ my: 2 }} />
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="body2" sx={{ fontWeight: 900 }} noWrap>
            {user?.displayName}
          </Typography>
          <Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block' }}>
            {user?.email}
          </Typography>
        </Box>
        <IconButton color="primary" onClick={logout} title="Log out">
          <LogoutIcon />
        </IconButton>
      </Box>
    </Box>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Drawer
        variant={isDesktop ? 'permanent' : 'temporary'}
        open={isDesktop || mobileOpen}
        onClose={() => setMobileOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: drawerWidth,
            bgcolor: isDark ? 'rgba(7, 11, 18, 0.88)' : 'rgba(244, 251, 253, 0.9)',
            backdropFilter: 'blur(22px)',
            borderRight: '1px solid',
            borderColor: 'divider',
          },
        }}
      >
        {drawer}
      </Drawer>
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Toolbar
          sx={{
            minHeight: 72,
            px: { xs: 2, md: 4 },
            gap: 2,
            borderBottom: '1px solid',
            borderColor: 'divider',
            bgcolor: isDark ? 'rgba(7, 11, 18, 0.62)' : 'rgba(244, 251, 253, 0.76)',
            backdropFilter: 'blur(18px)',
            position: 'sticky',
            top: 0,
            zIndex: 5,
          }}
        >
          {!isDesktop && (
            <IconButton onClick={() => setMobileOpen(true)} color="primary">
              <MenuIcon />
            </IconButton>
          )}
          <Box sx={{ flex: 1 }}>
            <Typography variant="body2" color="text.secondary" sx={{ fontWeight: 800 }}>
              PratsPilot Command Center
            </Typography>
          </Box>
          <Chip
            icon={<AutoAwesomeIcon />}
            label="Agentic Runtime Online"
            color="success"
            variant="outlined"
            sx={{ display: { xs: 'none', sm: 'inline-flex' } }}
          />
          <IconButton color="primary" onClick={toggleMode} title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}>
            {isDark ? <LightModeIcon /> : <DarkModeIcon />}
          </IconButton>
          <Button
            href="https://localhost:7167/swagger"
            target="_blank"
            variant="outlined"
            size="small"
            sx={{ display: { xs: 'none', md: 'inline-flex' } }}
          >
            Swagger
          </Button>
        </Toolbar>
        <Box component="main" sx={{ p: { xs: 2, md: 4 }, maxWidth: 1500, mx: 'auto' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}
