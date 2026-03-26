export interface User {
  id: number
  username: string
  email: string
  displayName?: string
  avatarUrl?: string
}

export interface AuthResponse {
  success: boolean
  accessToken: string
  refreshToken: string
  expiresAt: string
  user: User
  errorMessage?: string
}

export interface Document {
  id: number
  title: string
  description?: string
  ownerId: number
  teamId?: number
  parentId?: number
  type: string
  thumbnailUrl?: string
  width: number
  height: number
  backgroundColor: string
  isPublic: boolean
  isArchived: boolean
  version: number
  createdAt: string
  updatedAt: string
  permission?: string
}

export interface DocumentVersion {
  id: number
  documentId: number
  versionNumber: number
  title?: string
  createdBy: number
  createdAt: string
  comment?: string
  snapshotUrl?: string
}

export interface Team {
  id: number
  name: string
  description?: string
  ownerId: number
  createdAt: string
  updatedAt: string
  memberCount?: number
}

export interface TeamMember {
  id: number
  teamId: number
  userId: number
  role: string
  joinedAt: string
  user?: User
}

export interface DocumentPermission {
  id: number
  documentId: number
  userId?: number
  teamId?: number
  permission: string
  grantedBy: number
  grantedAt: string
}

export interface CollaborationUser {
  userId: number
  username: string
  displayName?: string
  avatarUrl?: string
  sessionId: string
  cursor?: { x: number; y: number }
  selection?: { layerIds: string[]; pageId?: string }
  viewport?: { x: number; y: number; zoom: number }
  connectedAt: string
  lastActivity: string
}

export interface EditOperation {
  type: string
  layerId?: string
  changes?: Record<string, unknown>
  previousValues?: Record<string, unknown>
  sequenceNumber: number
  sessionId: string
}
