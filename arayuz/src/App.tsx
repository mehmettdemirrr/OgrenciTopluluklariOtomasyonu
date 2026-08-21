import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Navigate, Route, BrowserRouter, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { Permissions } from './auth/permissions'
import { NotifierProvider } from './notifications/NotifierProvider'
import { theme } from './theme'
import { AnnouncementsPage } from './pages/AnnouncementsPage'
import { AuthorizationPage } from './pages/AuthorizationPage'
import { ClubDetailPage } from './pages/ClubDetailPage'
import { ClubsPage } from './pages/ClubsPage'
import { EventDetailPage } from './pages/EventDetailPage'
import { EventsPage } from './pages/EventsPage'
import { LoginPage } from './pages/LoginPage'
import { MembershipReviewPage } from './pages/MembershipReviewPage'
import { ReferenceDataPage } from './pages/ReferenceDataPage'
import { ReportsPage } from './pages/ReportsPage'

const queryClient = new QueryClient()

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <NotifierProvider>
          <BrowserRouter>
            <AuthProvider>
              <Routes>
                <Route path="/login" element={<LoginPage />} />

                <Route
                  element={
                    <ProtectedRoute>
                      <AppShell />
                    </ProtectedRoute>
                  }
                >
                  <Route path="/clubs" element={<ClubsPage />} />
                  <Route path="/clubs/:id" element={<ClubDetailPage />} />

                  <Route
                    path="/review"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.MembershipsWrite}>
                        <MembershipReviewPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/reports"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.ReportsRead}>
                        <ReportsPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/authorization"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.RolesManage}>
                        <AuthorizationPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/reference"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.ReferenceManage}>
                        <ReferenceDataPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/events"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsRead}>
                        <EventsPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/events/:id"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsRead}>
                        <EventDetailPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/announcements"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.ClubsRead}>
                        <AnnouncementsPage />
                      </ProtectedRoute>
                    }
                  />
                </Route>

                <Route path="*" element={<Navigate to="/clubs" replace />} />
              </Routes>
            </AuthProvider>
          </BrowserRouter>
        </NotifierProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
