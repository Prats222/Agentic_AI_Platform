import { Alert, Box, Button, Paper, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'
import LoginIcon from '@mui/icons-material/Login'
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import { Logo } from '../components/Logo'
import { useAuth } from '../state/AuthContext'

export function LoginPage() {
  const { login, signUp, isAuthenticated } = useAuth()
  const [mode, setMode] = useState<'login' | 'signup'>('login')
  const [displayName, setDisplayName] = useState('Platform Builder')
  const [email, setEmail] = useState('admin@agenticplatform.local')
  const [password, setPassword] = useState('Admin@12345')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      await login(email, password)
    } catch {
      setError('Login failed. Check that the API is running and the credentials are correct.')
    } finally {
      setLoading(false)
    }
  }

  async function handleSignUp(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      await signUp(displayName, email, password)
    } catch {
      setError('Registration failed. Use a unique email and a strong password.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: 2,
      }}
    >
      <Paper
        sx={{
          width: 'min(980px, 100%)',
          minHeight: 560,
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
          overflow: 'hidden',
          background: 'linear-gradient(145deg, rgba(13,22,36,0.98), rgba(7,11,18,0.96))',
        }}
      >
        <Box sx={{ p: { xs: 3, md: 5 }, display: 'flex', flexDirection: 'column' }}>
          <Logo />
          <Box sx={{ my: 'auto', py: 5 }}>
            <Typography variant="overline" color="primary.main" sx={{ fontWeight: 900 }}>
              Agentic AI Platform
            </Typography>
            <Typography variant="h3" sx={{ mt: 1 }}>
              Launch the pilot console.
            </Typography>
            <Typography color="text.secondary" sx={{ mt: 2, maxWidth: 420 }}>
              Operate agents, tools, workflows, executions, and provider settings from one focused command surface.
            </Typography>
          </Box>
          <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center', color: 'secondary.main' }}>
            <RocketLaunchIcon />
            <Typography variant="body2" sx={{ fontWeight: 800 }}>
              Connected to ASP.NET Core + SQL Server
            </Typography>
          </Stack>
        </Box>
        <Box
          component="form"
          onSubmit={mode === 'login' ? handleSubmit : handleSignUp}
          sx={{
            p: { xs: 3, md: 5 },
            bgcolor: 'rgba(255,255,255,0.035)',
            borderLeft: { md: '1px solid' },
            borderColor: 'divider',
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            gap: 2.2,
          }}
        >
          <Typography variant="h5">{mode === 'login' ? 'Sign in' : 'Create account'}</Typography>
          <Tabs value={mode} onChange={(_, value) => setMode(value)} sx={{ minHeight: 42 }}>
            <Tab value="login" label="Admin demo" />
            <Tab value="signup" label="Register" />
          </Tabs>
          {mode === 'signup' && (
            <TextField
              label="Display Name"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              fullWidth
            />
          )}
          <TextField label="Email" value={email} onChange={(event) => setEmail(event.target.value)} fullWidth />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            fullWidth
          />
          {error && <Alert severity="error">{error}</Alert>}
          <Button type="submit" size="large" variant="contained" startIcon={<LoginIcon />} disabled={loading}>
            {loading ? 'Working...' : mode === 'login' ? 'Enter PratsPilot' : 'Create and enter'}
          </Button>
          <Typography variant="caption" color="text.secondary">
            Admin demo credentials stay visible. New registrations become builder accounts that can create agents and workflows.
          </Typography>
        </Box>
      </Paper>
    </Box>
  )
}
