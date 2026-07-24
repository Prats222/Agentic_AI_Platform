import { Alert, Box, Button, IconButton, Paper, Stack, Tab, Tabs, TextField, Tooltip, Typography } from '@mui/material'
import LoginIcon from '@mui/icons-material/Login'
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch'
import AutoAwesomeIcon from '@mui/icons-material/AutoAwesome'
import EmojiEventsIcon from '@mui/icons-material/EmojiEvents'
import GitHubIcon from '@mui/icons-material/GitHub'
import LanguageIcon from '@mui/icons-material/Language'
import LinkedInIcon from '@mui/icons-material/LinkedIn'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate } from 'react-router-dom'
import axios from 'axios'
import { Logo } from '../components/Logo'
import { apiClient } from '../api/client'
import { useAuth } from '../state/AuthContext'

export function LoginPage() {
  const { login, signUp, isAuthenticated } = useAuth()
  const [mode, setMode] = useState<'admin' | 'user' | 'signup'>('user')
  const [displayName, setDisplayName] = useState('Platform Builder')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [loading, setLoading] = useState(false)
  const [resending, setResending] = useState(false)

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setSuccess('')
    try {
      await login(email, password)
    } catch (loginError) {
      setError(getAuthErrorMessage(loginError, 'Login failed. Check that the API is running and the credentials are correct.'))
    } finally {
      setLoading(false)
    }
  }

  async function handleSignUp(event: FormEvent) {
    event.preventDefault()
    setLoading(true)
    setError('')
    setSuccess('')
    try {
      const result = await signUp(displayName, email, password)
      setMode('user')
      setSuccess(
        result.confirmationEmailSent
          ? `Account created. You can sign in now. We also sent an optional verification link to ${result.email}.`
          : `Account created for ${result.email}. You can sign in now; email verification is temporarily unavailable.`,
      )
    } catch (signUpError) {
      setError(getAuthErrorMessage(signUpError, 'Registration failed. Use a valid email and a strong password. Password must be at least 8 characters and include uppercase, lowercase, number, and special character.'))
    } finally {
      setLoading(false)
    }
  }

  async function handleResendConfirmation() {
    if (!email.trim()) {
      setError('Enter your email address first.')
      return
    }

    setResending(true)
    setError('')
    setSuccess('')
    try {
      await apiClient.resendConfirmation(email)
      setSuccess('If that account exists and is not verified, a fresh verification link has been sent.')
    } catch (resendError) {
      setError(getAuthErrorMessage(resendError, 'Could not resend the confirmation email. Please try again shortly.'))
    } finally {
      setResending(false)
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'grid',
        placeItems: 'center',
        p: { xs: 1.5, md: 3 },
      }}
    >
      <Paper
        sx={{
          width: 'min(1500px, 100%)',
          minHeight: { xs: 'auto', md: 'calc(100vh - 48px)' },
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', md: '1.12fr 0.88fr' },
          overflow: 'hidden',
          background: 'linear-gradient(145deg, rgba(13,22,36,0.98), rgba(7,11,18,0.96))',
        }}
      >
        <Box sx={{ p: { xs: 3, md: 4 }, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
          <Logo />
          <Box sx={{ py: { xs: 3, md: 3.5 } }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: 'primary.main' }}>
              <EmojiEventsIcon fontSize="small" />
              <Typography variant="overline" sx={{ fontWeight: 900 }}>
                Creator vs creator
              </Typography>
            </Stack>
            <Typography variant="h4" sx={{ mt: 0.75, maxWidth: 540 }}>
              Build your agent. Enter the arena. Let the judge decide.
            </Typography>
          </Box>
          <Box
            sx={{
              position: 'relative',
              overflow: 'hidden',
              borderRadius: 2,
              border: '1px solid',
              borderColor: 'divider',
              bgcolor: '#070b12',
              boxShadow: '0 18px 44px rgba(0,0,0,0.34)',
            }}
          >
            <Box
              component="video"
              autoPlay
              loop
              muted
              playsInline
              preload="metadata"
              poster="/media/arena-preview.jpg"
              aria-label="PratsPilot Agent Battle Arena preview"
              sx={{ width: '100%', aspectRatio: '16 / 9', objectFit: 'cover', display: 'block' }}
            >
              <source src="/media/arena-preview.mp4" type="video/mp4" />
            </Box>
            <Box
              sx={{
                position: 'absolute',
                inset: 'auto 0 0',
                p: 2,
                pt: 5,
                background: 'linear-gradient(transparent, rgba(4,7,13,0.96))',
              }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="subtitle2" sx={{ fontWeight: 900 }}>
                    Production Bug-Fix Duel
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Two agents. One challenge. One winner.
                  </Typography>
                </Box>
                <Stack direction="row" spacing={0.6} sx={{ alignItems: 'center', color: 'secondary.main' }}>
                  <AutoAwesomeIcon fontSize="small" />
                  <Typography variant="caption" sx={{ fontWeight: 900 }}>
                    AI judged
                  </Typography>
                </Stack>
              </Stack>
            </Box>
          </Box>
          <Stack
            direction="row"
            spacing={2}
            sx={{ alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', color: 'secondary.main', mt: 'auto', pt: 3 }}
          >
            <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
              <RocketLaunchIcon />
              <Typography variant="body2" sx={{ fontWeight: 800 }}>
                Connected to ASP.NET Core + PostgreSQL
              </Typography>
            </Stack>
            <Stack direction="row" spacing={0.5} aria-label="Prateek Mishra links">
              <Tooltip title="LinkedIn">
                <IconButton
                  component="a"
                  href="https://www.linkedin.com/in/prateek-mishra-686945243/"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Open Prateek Mishra's LinkedIn profile"
                  color="inherit"
                >
                  <LinkedInIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="GitHub">
                <IconButton
                  component="a"
                  href="https://github.com/Prats222"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Open Prateek Mishra's GitHub profile"
                  color="inherit"
                >
                  <GitHubIcon />
                </IconButton>
              </Tooltip>
              <Tooltip title="Portfolio">
                <IconButton
                  component="a"
                  href="https://portfolio-prateek.vercel.app/"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Open Prateek Mishra's portfolio"
                  color="inherit"
                >
                  <LanguageIcon />
                </IconButton>
              </Tooltip>
            </Stack>
          </Stack>
        </Box>
        <Box
          component="form"
          onSubmit={mode === 'signup' ? handleSignUp : handleSubmit}
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
          <Typography variant="h5">{mode === 'signup' ? 'Create account' : mode === 'admin' ? 'Admin sign in' : 'User sign in'}</Typography>
          <Tabs
            value={mode}
            onChange={(_, value) => {
              setMode(value)
              setError('')
              setSuccess('')
              if (value === 'admin' || value === 'user') {
                setEmail('')
                setPassword('')
              }
              if (value === 'signup') {
                setEmail('')
                setPassword('')
                setDisplayName('Platform Builder')
              }
            }}
            sx={{ minHeight: 42 }}
          >
            <Tab value="admin" label="Admin login" />
            <Tab value="user" label="User login" />
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
          {success && <Alert severity="success">{success}</Alert>}
          <Button type="submit" size="large" variant="contained" startIcon={<LoginIcon />} disabled={loading}>
            {loading ? 'Working...' : mode === 'signup' ? 'Create account' : 'Enter PratsPilot'}
          </Button>
          {mode === 'user' && (
            <Button
              type="button"
              variant="text"
              onClick={handleResendConfirmation}
              disabled={resending}
            >
              {resending ? 'Sending...' : 'Resend verification email'}
            </Button>
          )}
          <Alert severity="warning" variant="outlined">
            Free hosting may take about 30 seconds to wake after inactivity. If the first sign-in attempt fails, wait briefly and try again.
          </Alert>
          {mode === 'admin' && (
            <Alert severity="info">
              Use your configured administrator account. Admin users can access both User Realm and Admin Realm.
            </Alert>
          )}
          {mode === 'user' && (
            <Typography variant="caption" color="text.secondary">
              Use the email and password created from Register. Normal users enter User Realm only.
            </Typography>
          )}
          {mode === 'signup' && (
            <Typography variant="caption" color="text.secondary">
              Use a valid email. Password needs 8+ characters with uppercase, lowercase, number, and special character.
            </Typography>
          )}
        </Box>
      </Paper>
    </Box>
  )
}

function getAuthErrorMessage(error: unknown, fallback: string) {
  if (!axios.isAxiosError(error)) {
    return fallback
  }

  const data = error.response?.data as {
    message?: string
    title?: string
    detail?: string
    errors?: Record<string, string[]> | string[]
  } | undefined

  if (Array.isArray(data?.errors) && data.errors.length) {
    return data.errors.join(' ')
  }

  if (data?.errors && !Array.isArray(data.errors)) {
    return Object.values(data.errors).flat().join(' ')
  }

  return data?.detail ?? data?.message ?? data?.title ?? fallback
}
