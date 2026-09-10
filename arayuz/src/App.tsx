import { CssBaseline } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Navigate, Route, BrowserRouter, Routes } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { AuthLayout } from './components/layout/AuthLayout'
import { PublicLayout } from './components/layout/PublicLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { Permissions } from './auth/permissions'
import { NotifierProvider } from './notifications/NotifierProvider'
import { LocaleProvider } from './i18n/LocaleContext'
import { ThemeModeProvider } from './theme'
import { AccessibilityProvider } from './a11y/AccessibilityContext'
import { AccessibilityFab } from './a11y/AccessibilityFab'
import { SkipToContentLink } from './a11y/SkipToContentLink'
import { AnnouncementsPage } from './pages/AnnouncementsPage'
import { AnnouncementFormPage } from './pages/forms/AnnouncementFormPage'
import { AuditLogPage } from './pages/AuditLogPage'
import { RolesPage } from './pages/RolesPage'
import { UsersPage } from './pages/UsersPage'
import { ClubApplicationsReviewPage } from './pages/ClubApplicationsReviewPage'
import { ClubDetailPage } from './pages/ClubDetailPage'
import { ClubApplicationPage } from './pages/ClubApplicationPage'
import { ClubsPage } from './pages/ClubsPage'
import { ConfirmEmailPage } from './pages/ConfirmEmailPage'
import { DashboardPage } from './pages/DashboardPage'
import { EventDetailPage } from './pages/EventDetailPage'
import { EventsPage } from './pages/EventsPage'
import { EventFormPage } from './pages/forms/EventFormPage'
import { ForgotPasswordPage } from './pages/ForgotPasswordPage'
import { LoginPage } from './pages/LoginPage'
import { MembershipReviewPage } from './pages/MembershipReviewPage'
import { MyApplicationsPage } from './pages/MyApplicationsPage'
import { MyClubsPage } from './pages/MyClubsPage'
import { MyEventsPage } from './pages/MyEventsPage'
import { ProfilePage } from './pages/ProfilePage'
import { HomePage } from './pages/public/HomePage'
import { PublicAnnouncementDetailPage } from './pages/public/PublicAnnouncementDetailPage'
import { PublicAnnouncementsPage } from './pages/public/PublicAnnouncementsPage'
import { PublicClubDetailPage } from './pages/public/PublicClubDetailPage'
import { PublicClubsPage } from './pages/public/PublicClubsPage'
import { PublicEventDetailPage } from './pages/public/PublicEventDetailPage'
import { PublicEventsPage } from './pages/public/PublicEventsPage'
import { ReferenceDataPage } from './pages/ReferenceDataPage'
import { RegisterPage } from './pages/RegisterPage'
import { ReportsPage } from './pages/ReportsPage'
import { ResetPasswordPage } from './pages/ResetPasswordPage'

const queryClient = new QueryClient()

function App() {
  return (
    <LocaleProvider>
      <AccessibilityProvider>
      <ThemeModeProvider>
        <CssBaseline />
        <QueryClientProvider client={queryClient}>
          <NotifierProvider>
            <BrowserRouter>
              <SkipToContentLink />
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
                  <Route path="/etkinlikler/:id" element={<PublicEventDetailPage />} />
                  <Route path="/duyurular" element={<PublicAnnouncementsPage />} />
                  <Route path="/duyurular/:id" element={<PublicAnnouncementDetailPage />} />
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
                  <Route path="/my-applications" element={<MyApplicationsPage />} />
                  <Route path="/clubs" element={<ClubsPage />} />
                  {/* K-37: başvuru artık diyalog değil, evrak yüklemeli tam sayfa. */}
                  <Route path="/club-applications/new" element={<ClubApplicationPage />} />
                  <Route path="/clubs/:id" element={<ClubDetailPage />} />
                  <Route path="/profile" element={<ProfilePage />} />

                  <Route
                    path="/club-applications"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.ClubsWrite}>
                        <ClubApplicationsReviewPage />
                      </ProtectedRoute>
                    }
                  />

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

                  {/*
                    Faz 29: "Yetki Matrisi" tek sayfa iki sekmeden iki rotaya ayrıldı.
                    Eski /authorization adresi yönlendirilir — dışarıda kalmış bir link
                    (yer imi, e-posta) kırılmasın.
                  */}
                  <Route path="/authorization" element={<Navigate to="/authorization/roles" replace />} />

                  <Route
                    path="/authorization/roles"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.RolesManage}>
                        <RolesPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/authorization/users"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.RolesManage}>
                        <UsersPage />
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
                    path="/events/new"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsWrite}>
                        <EventFormPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/events/:id/edit"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.EventsWrite}>
                        <EventFormPage />
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
                    path="/announcements/new"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.AnnouncementsWrite}>
                        <AnnouncementFormPage />
                      </ProtectedRoute>
                    }
                  />

                  <Route
                    path="/announcements/:id/edit"
                    element={
                      <ProtectedRoute requiredPermission={Permissions.AnnouncementsWrite}>
                        <AnnouncementFormPage />
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
              <AccessibilityFab />
              </AuthProvider>
            </BrowserRouter>
          </NotifierProvider>
        </QueryClientProvider>
      </ThemeModeProvider>
      </AccessibilityProvider>
    </LocaleProvider>
  )
}

export default App
