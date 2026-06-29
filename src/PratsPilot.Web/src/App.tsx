import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AgentsPage } from './pages/AgentsPage'
import { AISettingsPage } from './pages/AISettingsPage'
import { ChatPage } from './pages/ChatPage'
import { DashboardPage } from './pages/DashboardPage'
import { ExecutionsPage } from './pages/ExecutionsPage'
import { LoginPage } from './pages/LoginPage'
import { ToolsPage } from './pages/ToolsPage'
import { WorkflowsPage } from './pages/WorkflowsPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="agents" element={<AgentsPage />} />
        <Route path="workflows" element={<WorkflowsPage />} />
        <Route path="tools" element={<ToolsPage />} />
        <Route path="executions" element={<ExecutionsPage />} />
        <Route path="ai-settings" element={<AISettingsPage />} />
        <Route path="chat" element={<ChatPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
