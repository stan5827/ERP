import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ToastProviderWithInit } from './hooks/useToast'
import { ThemeProvider } from './theme/ThemeProvider'
import { RoleProvider } from './contexts/RoleContext'
import './index.css'
import App from './App.tsx'
import './theme/GlobalVisualSystem.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30000, refetchOnWindowFocus: false },
  },
})

export function Root() {
  const stored = localStorage.getItem('erp_user');
  const user = stored ? JSON.parse(stored) : null;
  const userId = user?.mustChangePassword ? undefined : user?.userId;

  return (
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <ToastProviderWithInit>
          <ThemeProvider userId={userId}>
            <RoleProvider>
              <App />
            </RoleProvider>
          </ThemeProvider>
        </ToastProviderWithInit>
      </QueryClientProvider>
    </StrictMode>
  )
}

createRoot(document.getElementById('root')!).render(<Root />)
