import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { User, AuthResponse } from '../types'
import { api } from '../services/api'

interface AuthContextType {
  user: User | null
  token: string | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, username: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'))
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const storedUser = localStorage.getItem('user')
    if (storedUser && token) {
      setUser(JSON.parse(storedUser))
    }
    setLoading(false)
  }, [token])

  const login = async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/api/auth/login', { email, password })
    if (response.success) {
      setToken(response.accessToken)
      setUser(response.user)
      localStorage.setItem('token', response.accessToken)
      localStorage.setItem('user', JSON.stringify(response.user))
    } else {
      throw new Error(response.errorMessage || '登录失败')
    }
  }

  const register = async (email: string, password: string, username: string) => {
    const response = await api.post<AuthResponse>('/api/auth/register', { email, password, username })
    if (response.success) {
      setToken(response.accessToken)
      setUser(response.user)
      localStorage.setItem('token', response.accessToken)
      localStorage.setItem('user', JSON.stringify(response.user))
    } else {
      throw new Error(response.errorMessage || '注册失败')
    }
  }

  const logout = () => {
    setToken(null)
    setUser(null)
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  return (
    <AuthContext.Provider value={{ user, token, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
