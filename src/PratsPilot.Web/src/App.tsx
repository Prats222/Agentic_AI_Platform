import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AgentsPage } from './pages/AgentsPage'
import { AISettingsPage } from './pages/AISettingsPage'
import { AdminUsersPage } from './pages/AdminUsersPage'
import { ApprovalsPage } from './pages/ApprovalsPage'
import { ArenaPage } from './pages/ArenaPage'
import { AutopilotPage } from './pages/AutopilotPage'
import { ChatPage } from './pages/ChatPage'
import { ContextLibraryPage } from './pages/ContextLibraryPage'
import { DashboardPage } from './pages/DashboardPage'
import { ExecutionsPage } from './pages/ExecutionsPage'
import { LoginPage } from './pages/LoginPage'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage'
import { ToolsPage } from './pages/ToolsPage'
import { WorkflowsPage } from './pages/WorkflowsPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/confirm-email" element={<ConfirmEmailPage />} />
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
        <Route path="admin/users" element={<AdminUsersPage />} />
        <Route path="autopilot" element={<AutopilotPage />} />
        <Route path="approvals" element={<ApprovalsPage />} />
        <Route path="arena" element={<ArenaPage />} />
        <Route path="context" element={<ContextLibraryPage />} />
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
