import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { Document } from '../types'
import { documentApi } from '../services/api'
import './Documents.css'

export default function DocumentsPage() {
  const [documents, setDocuments] = useState<Document[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [newTitle, setNewTitle] = useState('')
  const [newDescription, setNewDescription] = useState('')

  useEffect(() => {
    loadDocuments()
  }, [])

  const loadDocuments = async () => {
    try {
      const docs = await documentApi.list()
      setDocuments(docs)
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载失败')
    } finally {
      setLoading(false)
    }
  }

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const doc = await documentApi.create({
        title: newTitle,
        description: newDescription,
        type: 'design',
        width: 1920,
        height: 1080,
        backgroundColor: '#FFFFFF'
      })
      setDocuments([doc, ...documents])
      setShowCreate(false)
      setNewTitle('')
      setNewDescription('')
    } catch (err) {
      setError(err instanceof Error ? err.message : '创建失败')
    }
  }

  const handleDelete = async (id: number) => {
    if (!confirm('确定要删除这个文档吗？')) return
    try {
      await documentApi.delete(id)
      setDocuments(documents.filter(d => d.id !== id))
    } catch (err) {
      setError(err instanceof Error ? err.message : '删除失败')
    }
  }

  if (loading) return <div className="loading">加载中...</div>

  return (
    <div className="documents-page">
      <div className="page-header">
        <h1>我的文档</h1>
        <button className="btn-primary" onClick={() => setShowCreate(true)}>
          新建文档
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      {showCreate && (
        <div className="create-form card">
          <h3>新建文档</h3>
          <form onSubmit={handleCreate}>
            <div className="form-group">
              <label>标题</label>
              <input
                type="text"
                value={newTitle}
                onChange={e => setNewTitle(e.target.value)}
                placeholder="请输入文档标题"
                required
              />
            </div>
            <div className="form-group">
              <label>描述</label>
              <textarea
                value={newDescription}
                onChange={e => setNewDescription(e.target.value)}
                placeholder="请输入文档描述（可选）"
              />
            </div>
            <div className="form-actions">
              <button type="submit" className="btn-primary">创建</button>
              <button type="button" className="btn-secondary" onClick={() => setShowCreate(false)}>
                取消
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="documents-grid">
        {documents.length === 0 ? (
          <div className="empty-state">
            <p>暂无文档</p>
            <p>点击"新建文档"创建您的第一个设计</p>
          </div>
        ) : (
          documents.map(doc => (
            <div key={doc.id} className="document-card card">
              <div className="document-thumbnail" style={{ backgroundColor: doc.backgroundColor }}>
                <span className="document-type">{doc.type}</span>
              </div>
              <div className="document-info">
                <Link to={`/documents/${doc.id}`} className="document-title">
                  {doc.title}
                </Link>
                <p className="document-meta">
                  版本 {doc.version} • {new Date(doc.updatedAt).toLocaleDateString()}
                </p>
                {doc.permission && (
                  <span className={`permission-badge ${doc.permission}`}>
                    {doc.permission === 'owner' ? '所有者' : 
                     doc.permission === 'edit' ? '可编辑' : '只读'}
                  </span>
                )}
              </div>
              <div className="document-actions">
                <Link to={`/documents/${doc.id}`} className="btn-primary">
                  打开
                </Link>
                {doc.permission === 'owner' && (
                  <button className="btn-danger" onClick={() => handleDelete(doc.id)}>
                    删除
                  </button>
                )}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}
