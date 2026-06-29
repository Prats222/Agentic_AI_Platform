import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import axios from 'axios'
import { api, apiClient, setAccessToken } from '../api/client'
import type { AuthResponse } from '../api/types'

type AuthContextValue = {
  user?: AuthResponse
  token?: string
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  signUp: (displayName: string, email: string, password: string) => Promise<void>
  logout: () => void
}

const STORAGE_KEY = 'pratspilot.auth'
const AuthContext = createContext<AuthContextValue | undefined>(undefined)
let refreshRequest: Promise<AuthResponse> | undefined

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | undefined>(() => {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AuthResponse) : undefined
  })

  useEffect(() => {
    setAccessToken(user?.accessToken)
  }, [user])

  useEffect(() => {
    const interceptor = api.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (!axios.isAxiosError(error) || error.response?.status !== 401 || error.config?.url?.includes('/auth/')) {
          return Promise.reject(error)
        }

        const originalRequest = error.config as typeof error.config & { _retry?: boolean }
        if (originalRequest._retry) {
          clearSession(setUser)
          return Promise.reject(error)
        }

        originalRequest._retry = true
        const currentSession = readSession()
        if (!currentSession?.refreshToken) {
          clearSession(setUser)
          return Promise.reject(error)
        }

        try {
          refreshRequest ??= apiClient.refreshToken(currentSession.refreshToken)
          const refreshedSession = await refreshRequest
          refreshRequest = undefined
          persistSession(refreshedSession)
          setUser(refreshedSession)
          originalRequest.headers = originalRequest.headers ?? {}
          originalRequest.headers.Authorization = `Bearer ${refreshedSession.accessToken}`
          return api(originalRequest)
        } catch (refreshError) {
          refreshRequest = undefined
          clearSession(setUser)
          return Promise.reject(refreshError)
        }
      },
    )

    return () => {
      api.interceptors.response.eject(interceptor)
    }
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token: user?.accessToken,
      isAuthenticated: Boolean(user?.accessToken),
      login: async (email, password) => {
        const response = await apiClient.login(email, password)
        persistSession(response)
        setUser(response)
      },
      signUp: async (displayName, email, password) => {
        const response = await apiClient.signUp({ displayName, email, password })
        persistSession(response)
        setUser(response)
      },
      logout: () => {
        clearSession(setUser)
      },
    }),
    [user],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function readSession() {
  const raw = localStorage.getItem(STORAGE_KEY)
  return raw ? (JSON.parse(raw) as AuthResponse) : undefined
}

function persistSession(response: AuthResponse) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(response))
  setAccessToken(response.accessToken)
}

function clearSession(setUser: (user: AuthResponse | undefined) => void) {
  localStorage.removeItem(STORAGE_KEY)
  setUser(undefined)
  setAccessToken()
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider')
  }
  return context
}
