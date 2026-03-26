import { useState, useEffect, useRef } from 'react'
import { useParams, Link } from 'react-router-dom'
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import { Document, CollaborationUser, DocumentVersion } from '../types'
import { documentApi } from '../services/api'
import { useAuth } from '../hooks/useAuth'
import './Editor.css'

export default function EditorPage() {
  const { id } = useParams<{ id: string }>()
  const { user, token } = useAuth()
  const [document, setDocument] = useState<Document | null>(null)
  const [versions, setVersions] = useState<DocumentVersion[]>([])
  const [onlineUsers, setOnlineUsers] = useState<CollaborationUser[]>([])
  const [cursors, setCursors] = useState<Map<string, { x: number; y: number; username: string }>>(new Map())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showVersions, setShowVersions] = useState(false)
  const connectionRef = useRef<HubConnection | null>(null)
  const canvasRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (id) {
      loadDocument(parseInt(id))
      loadVersions(parseInt(id))
      connectToHub(id)
    }
    return () => {
      connectionRef.current?.stop()
    }
  }, [id])

  const loadDocument = async (docId: number) => {
    try {
      const doc = await documentApi.get(docId)
      setDocument(doc)
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载失败')
    } finally {
      setLoading(false)
    }
  }

  const loadVersions = async (docId: number) => {
    try {
      const vers = await documentApi.getVersions(docId)
      setVersions(vers)
    } catch (err) {
      console.error('加载版本失败:', err)
    }
  }

  const connectToHub = async (documentId: string) => {
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/collaboration', { accessTokenFactory: () => token || '' })
      .withAutomaticReconnect()
      .build()

    connection.on('UserJoined', (joinedUser: CollaborationUser) => {
      setOnlineUsers(prev => [...prev.filter(u => u.sessionId !== joinedUser.sessionId), joinedUser])
    })

    connection.on('UserLeft', (sessionId: string) => {
      setOnlineUsers(prev => prev.filter(u => u.sessionId !== sessionId))
      setCursors(prev => {
        const next = new Map(prev)
        next.delete(sessionId)
        return next
      })
    })

    connection.on('DocumentState', (state: { users: CollaborationUser[] }) => {
      setOnlineUsers(state.users)
    })

    connection.on('CursorUpdated', (sessionId: string, userId: number, x: number, y: number) => {
      const onlineUser = onlineUsers.find(u => u.sessionId === sessionId)
      if (onlineUser) {
        setCursors(prev => new Map(prev).set(sessionId, { x, y, username: onlineUser.username }))
      }
    })

    try {
      await connection.start()
      await connection.invoke('JoinDocument', documentId, user?.id, user?.username, user?.displayName, user?.avatarUrl)
      connectionRef.current = connection
    } catch (err) {
      console.error('连接失败:', err)
    }
  }

  const handleMouseMove = async (e: React.MouseEvent) => {
    if (!connectionRef.current || !canvasRef.current) return
    const rect = canvasRef.current.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top
    try {
      await connectionRef.current.invoke('UpdateCursor', id, x, y)
    } catch {
      // 静默处理
    }
  }

  const handleCreateVersion = async () => {
    if (!id) return
    try {
      const version = await documentApi.createVersion(parseInt(id), {
        title: `版本 ${versions.length + 1}`,
        comment: '手动保存'
      })
      setVersions([version, ...versions])
    } catch (err) {
      setError(err instanceof Error ? err.message : '创建版本失败')
    }
  }

  const handleRestoreVersion = async (versionId: number) => {
    if (!id || !confirm('确定要回滚到此版本吗？')) return
    try {
      await documentApi.restoreVersion(parseInt(id), versionId)
      await loadDocument(parseInt(id))
    } catch (err) {
      setError(err instanceof Error ? err.message : '回滚失败')
    }
  }

  if (loading) return <div className="loading">加载中...</div>
  if (!document) return <div className="error">文档不存在</div>

  return (
    <div className="editor-page">
      <div className="editor-header">
        <div className="header-left">
          <Link to="/documents" className="back-link">← 返回</Link>
          <h1>{document.title}</h1>
          <span className="version-badge">v{document.version}</span>
        </div>
        <div className="header-right">
          <div className="online-users">
            {onlineUsers.map(u => (
              <span key={u.sessionId} className="user-avatar" title={u.username}>
                {u.username.charAt(0).toUpperCase()}
              </span>
            ))}
          </div>
          <button onClick={() => setShowVersions(!showVersions)}>
            版本历史 ({versions.length})
          </button>
          <button className="btn-primary" onClick={handleCreateVersion}>
            保存版本
          </button>
        </div>
      </div>

      {error && <div className="error-message">{error}</div>}

      <div className="editor-content">
        <div 
          ref={canvasRef}
          className="canvas" 
          style={{ backgroundColor: document.backgroundColor }}
          onMouseMove={handleMouseMove}
        >
          <div className="canvas-info">
            <p>{document.width} × {document.height}</p>
            <p>{onlineUsers.length} 人在线</p>
          </div>
          
          {/* 渲染其他用户的光标 */}
          {Array.from(cursors.entries()).map(([sessionId, cursor]) => (
            <div
              key={sessionId}
              className="remote-cursor"
              style={{ left: cursor.x, top: cursor.y }}
            >
              <div className="cursor-icon" />
              <span className="cursor-label">{cursor.username}</span>
            </div>
          ))}
        </div>

        {showVersions && (
          <div className="versions-panel">
            <h3>版本历史</h3>
            <div className="versions-list">
              {versions.length === 0 ? (
                <p className="no-versions">暂无版本记录</p>
              ) : (
                versions.map(v => (
                  <div key={v.id} className="version-item">
                    <div className="version-info">
                      <strong>v{v.versionNumber}</strong>
                      <span>{v.title || '未命名'}</span>
                      <small>{new Date(v.createdAt).toLocaleString()}</small>
                    </div>
                    <button 
                      className="btn-secondary"
                      onClick={() => handleRestoreVersion(v.id)}
                    >
                      回滚
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
