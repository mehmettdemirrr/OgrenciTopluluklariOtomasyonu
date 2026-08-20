import { CssBaseline, ThemeProvider, createTheme } from '@mui/material'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Navigate, Route, BrowserRouter, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AuthProvider } from './auth/AuthContext'
import { Permissions } from './auth/permissions'
import { ClubsPage } from './pages/ClubsPage'
import { LoginPage } from './pages/LoginPage'
import { MembershipReviewPage } from './pages/MembershipReviewPage'

const theme = createTheme()
const queryClient = new QueryClient()

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>
            <Routes>
              <Route path="/login" element={<LoginPage />} />

              <Route
                path="/clubs"
                element={
                  <ProtectedRoute>
                    <AppLayout>
                      <ClubsPage />
                    </AppLayout>
                  </ProtectedRoute>
                }
              />

              <Route
                path="/review"
                element={
                  <ProtectedRoute requiredPermission={Permissions.MembershipsWrite}>
                    <AppLayout>
                      <MembershipReviewPage />
                    </AppLayout>
                  </ProtectedRoute>
                }
              />

              <Route path="*" element={<Navigate to="/clubs" replace />} />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}

export default App
