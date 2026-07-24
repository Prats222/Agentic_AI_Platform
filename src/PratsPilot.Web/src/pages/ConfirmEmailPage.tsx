import { Alert, Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead'
import { useEffect, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import axios from 'axios'
import { apiClient } from '../api/client'
import { Logo } from '../components/Logo'

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams()
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading')
  const [message, setMessage] = useState('Confirming your email...')
  const started = useRef(false)

  useEffect(() => {
    if (started.current) return
    started.current = true

    const userId = searchParams.get('userId')
    const code = searchParams.get('code')
    if (!userId || !code) {
      setStatus('error')
      setMessage('This confirmation link is incomplete.')
      return
    }

    apiClient.confirmEmail(userId, code)
      .then(() => {
        setStatus('success')
        setMessage('Email confirmed. Your PratsPilot account is ready.')
      })
      .catch((error: unknown) => {
        setStatus('error')
        setMessage(readError(error))
      })
  }, [searchParams])

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', placeItems: 'center', p: 2 }}>
      <Paper sx={{ width: 'min(560px, 100%)', p: { xs: 3, md: 5 } }}>
        <Stack spacing={3} sx={{ alignItems: 'flex-start' }}>
          <Logo />
          <Box>
            <Typography variant="overline" color="primary.main">Account activation</Typography>
            <Typography variant="h4" sx={{ mt: 0.5 }}>Confirm your mission access</Typography>
          </Box>
          {status === 'loading' && (
            <Stack direction="row" spacing={2} sx={{ alignItems: 'center' }}>
              <CircularProgress size={24} />
              <Typography>{message}</Typography>
            </Stack>
          )}
          {status === 'success' && (
            <Alert severity="success" icon={<MarkEmailReadIcon />} sx={{ width: '100%' }}>
              {message}
            </Alert>
          )}
          {status === 'error' && <Alert severity="error" sx={{ width: '100%' }}>{message}</Alert>}
          {status !== 'loading' && (
            <Button component={Link} to="/login" variant="contained" size="large">
              Continue to sign in
            </Button>
          )}
        </Stack>
      </Paper>
    </Box>
  )
}

function readError(error: unknown) {
  if (!axios.isAxiosError(error)) return 'The confirmation link could not be verified.'
  return error.response?.data?.message ?? 'The confirmation link is invalid or expired.'
}
