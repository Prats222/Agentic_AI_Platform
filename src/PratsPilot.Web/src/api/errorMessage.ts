import axios from 'axios'

export function getApiErrorMessage(error: unknown, fallback = 'Action failed.') {
  if (!axios.isAxiosError(error)) {
    return error instanceof Error ? error.message : fallback
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
  return data?.detail ?? data?.message ?? data?.title ?? error.message ?? fallback
}
