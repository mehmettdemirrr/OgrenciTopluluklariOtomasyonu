import { Container } from '@mui/material'
import type { ReactNode } from 'react'
import { NavBar } from './NavBar'

export function AppLayout({ children }: { children: ReactNode }) {
  return (
    <>
      <NavBar />
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        {children}
      </Container>
    </>
  )
}
