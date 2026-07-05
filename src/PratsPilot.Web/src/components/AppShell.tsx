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
  MenuItem,
  TextField,
  Toolbar,
  Typography,
  useMediaQuery,
} from '@mui/material'
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import DashboardIcon from '@mui/icons-material/Dashboard'
import DescriptionIcon from '@mui/icons-material/Description'
import HubIcon from '@mui/icons-material/Hub'
import LogoutIcon from '@mui/icons-material/Logout'
import MemoryIcon from '@mui/icons-material/Memory'
import MenuIcon from '@mui/icons-material/Menu'
import RuleIcon from '@mui/icons-material/Rule'
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings'
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents'
import PlayCircleIcon from '@mui/icons-material/PlayCircle'
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing'
import PsychologyIcon from '@mui/icons-material/Psychology'
import SettingsSuggestIcon from '@mui/icons-material/SettingsSuggest'
import ChatIcon from '@mui/icons-material/Chat'
import DarkModeIcon from '@mui/icons-material/DarkMode'
import LightModeIcon from '@mui/icons-material/LightMode'
import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { Logo } from './Logo'
import { useAuth } from '../state/AuthContext'
import { useTheme, alpha } from '@mui/material/styles'
import { useThemeMode } from '../state/ThemeModeContext'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient, getStoredRealmId, setRealmId } from '../api/client'

const drawerWidth = 292

const navItems = [
  { label: 'Mission Control', path: '/', icon: <DashboardIcon />, group: 'Operate' },
  { label: 'Autopilot', path: '/autopilot', icon: <AutoFixHighIcon />, group: 'Operate' },
  { label: 'Arena', path: '/arena', icon: <EmojiEventsIcon />, group: 'Operate' },
  { label: 'Approvals', path: '/approvals', icon: <RuleIcon />, group: 'Operate' },
  { label: 'Agents', path: '/agents', icon: <PsychologyIcon />, group: 'Build' },
  { label: 'Context', path: '/context', icon: <DescriptionIcon />, group: 'Build' },
  { label: 'Workflows', path: '/workflows', icon: <HubIcon />, group: 'Build' },
  { label: 'Toolbox', path: '/tools', icon: <PrecisionManufacturingIcon />, group: 'Build' },
  { label: 'Mission History', path: '/executions', icon: <PlayCircleIcon />, group: 'Observe' },
  { label: 'Chat', path: '/chat', icon: <ChatIcon />, group: 'Observe' },
  { label: 'AI Settings', path: '/ai-settings', icon: <SettingsSuggestIcon />, group: 'Observe' },
]

export function AppShell() {
  const theme = useTheme()
  const [mobileOpen, setMobileOpen] = useState(false)
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'))
  const { user, logout } = useAuth()
  const { mode, toggleMode } = useThemeMode()
  const location = useLocation()
  const isDark = mode === 'dark'
  const queryClient = useQueryClient()
  const realms = useQuery({ queryKey: ['realms'], queryFn: apiClient.getRealms })
  const [selectedRealmId, setSelectedRealmId] = useState(getStoredRealmId() ?? '11111111-1111-1111-1111-111111111111')
  const isAdmin = user?.roles.includes('Admin') ?? false
  const visibleNavItems = isAdmin
    ? [...navItems, { label: 'Admin', path: '/admin/users', icon: <AdminPanelSettingsIcon />, group: 'Admin' }]
    : navItems
  const groupedNavItems = visibleNavItems.reduce<Record<string, typeof visibleNavItems>>((groups, item) => {
    groups[item.group] = [...(groups[item.group] ?? []), item]
    return groups
  }, {})

  useEffect(() => {
    if (!realms.data?.length) return
    if (!realms.data.some((realm) => realm.id === selectedRealmId)) {
      const fallbackRealmId = realms.data[0].id
      setSelectedRealmId(fallbackRealmId)
      setRealmId(fallbackRealmId)
      queryClient.invalidateQueries()
    }
  }, [queryClient, realms.data, selectedRealmId])

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
          bgcolor: alpha(theme.palette.background.paper, isDark ? 0.5 : 0.82),
          backgroundImage: isDark
            ? `linear-gradient(145deg, ${alpha(theme.palette.primary.main, 0.11)}, ${alpha(theme.palette.secondary.main, 0.06)})`
            : `linear-gradient(145deg, ${alpha(theme.palette.primary.main, 0.08)}, ${alpha(theme.palette.secondary.main, 0.06)})`,
        }}
      >
        <Chip size="small" icon={<MemoryIcon />} label=".NET 10 Runtime" color="primary" variant="outlined" />
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1.5 }}>
          Mission-ready surface for agents, workflows, tools, and execution telemetry.
        </Typography>
      </Box>
      <List sx={{ mt: 2, display: 'grid', gap: 1 }}>
        {Object.entries(groupedNavItems).map(([group, items]) => (
          <Box key={group}>
            <Typography variant="caption" color="text.secondary" sx={{ display: 'block', px: 1.5, mb: 0.5, fontWeight: 850 }}>
              {group}
            </Typography>
            {items.map((item) => {
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
                    minHeight: 44,
                    mb: 0.3,
                    color: selected ? 'primary.main' : 'text.secondary',
                    '&:hover': {
                      color: 'text.primary',
                      bgcolor: alpha(theme.palette.primary.main, isDark ? 0.08 : 0.07),
                    },
                    '&.Mui-selected': {
                      bgcolor: alpha(theme.palette.primary.main, isDark ? 0.14 : 0.12),
                      color: 'primary.main',
                      boxShadow: `inset 3px 0 0 ${theme.palette.primary.main}`,
                    },
                  }}
                >
                  <ListItemIcon sx={{ color: 'inherit', minWidth: 40 }}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} slotProps={{ primary: { sx: { fontWeight: 800 } } }} />
                </ListItemButton>
              )
            })}
          </Box>
        ))}
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
            bgcolor: isDark ? 'rgba(11, 15, 25, 0.86)' : 'rgba(255, 255, 255, 0.82)',
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
            bgcolor: isDark ? 'rgba(11, 15, 25, 0.68)' : 'rgba(255, 255, 255, 0.78)',
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
            <Typography variant="body2" sx={{ fontWeight: 850 }}>
              Good {getDayPart()}, {user?.displayName?.split(' ')[0] || 'Pilot'}
            </Typography>
            <Typography variant="caption" color="text.secondary" sx={{ display: { xs: 'none', sm: 'block' } }}>
              Ready to launch your AI crew?
            </Typography>
          </Box>
          <Chip
            icon={<AutoAwesomeIcon />}
            label="Agentic Runtime Online"
            color="success"
            variant="outlined"
            sx={{ display: { xs: 'none', sm: 'inline-flex' } }}
          />
          <TextField
            select
            size="small"
            value={selectedRealmId}
            onChange={(event) => {
              const nextRealmId = event.target.value
              setSelectedRealmId(nextRealmId)
              setRealmId(nextRealmId)
              queryClient.invalidateQueries()
            }}
            sx={{ minWidth: 170, display: { xs: 'none', md: 'block' } }}
          >
            {(realms.data ?? []).map((realm) => (
              <MenuItem key={realm.id} value={realm.id}>
                {realm.name}
              </MenuItem>
            ))}
          </TextField>
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

function getDayPart() {
  const hour = new Date().getHours()
  if (hour < 5) return 'night'
  if (hour < 12) return 'morning'
  if (hour < 17) return 'afternoon'
  if (hour < 21) return 'evening'
  return 'night'
}
