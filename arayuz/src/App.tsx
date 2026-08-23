import { CssBaseline, ThemeProvider } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Navigate, Route, BrowserRouter, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { AuthLayout } from './components/layout/AuthLayout'
import { PublicLayout } from './components/layout/PublicLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { Permissions } from './auth/permissions'
import { NotifierProvider } from './notifications/NotifierProvider'
import { theme } from './theme'
import { AnnouncementsPage } from './pages/AnnouncementsPage'
import { AuditLogPage } from './pages/AuditLogPage'
import { AuthorizationPage } from './pages/AuthorizationPage'
import { ClubDetailPage } from './pages/ClubDetailPage'
import { ClubsPage } from './pages/ClubsPage'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage'
import { DashboardPage } from './pages/DashboardPage'
import { EventDetailPage } from './pages/EventDetailPage'
import { EventsPage } from './pages/EventsPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LoginPage } from './pages/LoginPage'
import { MembershipReviewPage } from './pages/MembershipReviewPage'
import { MyClubsPage } from './pages/MyClubsPage'
import { MyEventsPage } from './pages/MyEventsPage'
import { ProfilePage } from './pages/ProfilePage'
import { HomePage } from './pages/public/HomePage'
import { PublicClubDetailPage } from './pages/public/PublicClubDetailPage'
import { PublicClubsPage } from './pages/public/PublicClubsPage'
import { PublicEventsPage } from './pages/public/PublicEventsPage'
import { ReferenceDataPage } from './pages/ReferenceDataPage'
import { RegisterPage } from './pages/RegisterPage'
import { ReportsPage } from './pages/ReportsPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'

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
                <Route element={<AuthLayout />}>
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />
                  <Route path="/confirm-email" element={<ConfirmEmailPage />} />
                  <Route path="/forgot-password" element={<ForgotPasswordPage />} />
                  <Route path="/reset-password" element={<ResetPasswordPage />} />
                </Route>

                <Route element={<PublicLayout />}>
                  <Route path="/" element={<HomePage />} />
                  <Route path="/kulupler" element={<PublicClubsPage />} />
                  <Route path="/kulupler/:id" element={<PublicClubDetailPage />} />
                  <Route path="/etkinlikler" element={<PublicEventsPage />} />
                </Route>

                <Route
                  element={
                    <ProtectedRoute>
                      <AppShell />
                    </ProtectedRoute>
                  }
                >
                  <Route path="/panel" element={<DashboardPage />} />
                  <Route path="/my-clubs" element={<MyClubsPage />} />
                  <Route path="/my-events" element={<MyEventsPage />} />
                  <Route path="/clubs" element={<ClubsPage />} />
                  <Route path="/clubs/:id" element={<ClubDetailPage />} />
                  <Route path="/profile" element={<ProfilePage />} />

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

                  <Route
                    path="/audit"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.AuditRead}>
                        <AuditLogPage />
                      </ProtectedRoute>
                    }
                  />
                </Route>

                <Route path="*" element={<Navigate to="/" replace />} />
              </Routes>
            </AuthProvider>
          </BrowserRouter>
        </NotifierProvider>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
