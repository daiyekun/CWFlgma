const BASE_URL = ''

interface RequestOptions {
  method?: string
  body?: unknown
  headers?: Record<string, string>
}

async function request<T>(url: string, options: RequestOptions = {}): Promise<T> {
  const token = localStorage.getItem('token')
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...options.headers
  }
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  const response = await fetch(`${BASE_URL}${url}`, {
    method: options.method || 'GET',
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined
  })
  
  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: '请求失败' }))
    throw new Error(error.error || error.errorMessage || '请求失败')
  }
  
  return response.json()
}

export const api = {
  get: <T>(url: string) => request<T>(url),
  post: <T>(url: string, body?: unknown) => request<T>(url, { method: 'POST', body }),
  put: <T>(url: string, body?: unknown) => request<T>(url, { method: 'PUT', body }),
  delete: <T>(url: string) => request<T>(url, { method: 'DELETE' })
}

export const documentApi = {
  list: () => api.get<Document[]>('/api/documents'),
  get: (id: number) => api.get<Document>(`/api/documents/${id}`),
  create: (data: Partial<Document>) => api.post<Document>('/api/documents', data),
  update: (id: number, data: Partial<Document>) => api.put<Document>(`/api/documents/${id}`, data),
  delete: (id: number) => api.delete(`/api/documents/${id}`),
  getVersions: (id: number) => api.get<DocumentVersion[]>(`/api/documents/${id}/versions`),
  createVersion: (id: number, data: { title?: string; comment?: string }) => 
    api.post<DocumentVersion>(`/api/documents/${id}/versions`, data),
  restoreVersion: (docId: number, versionId: number) => 
    api.post(`/api/documents/${docId}/versions/${versionId}/restore`),
  getPermissions: (id: number) => api.get<DocumentPermission[]>(`/api/documents/${id}/permissions`),
  grantPermission: (id: number, data: { userId?: number; teamId?: number; permission: string }) =>
    api.post(`/api/documents/${id}/permissions`, data)
}

export const teamApi = {
  list: () => api.get<Team[]>('/api/teams'),
  get: (id: number) => api.get<Team>(`/api/teams/${id}`),
  create: (data: { name: string; description?: string }) => api.post<Team>('/api/teams', data),
  delete: (id: number) => api.delete(`/api/teams/${id}`),
  getMembers: (id: number) => api.get<TeamMember[]>(`/api/teams/${id}/members`),
  addMember: (id: number, data: { email: string; role: string }) => 
    api.post(`/api/teams/${id}/members`, data),
  removeMember: (teamId: number, userId: number) => 
    api.delete(`/api/teams/${teamId}/members/${userId}`)
}

import { Document, DocumentVersion, Team, TeamMember, DocumentPermission } from '../types'
