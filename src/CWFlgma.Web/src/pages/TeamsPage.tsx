import { useState, useEffect } from 'react'
import { Team, TeamMember } from '../types'
import { teamApi } from '../services/api'
import './Teams.css'

export default function TeamsPage() {
  const [teams, setTeams] = useState<Team[]>([])
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null)
  const [members, setMembers] = useState<TeamMember[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showCreate, setShowCreate] = useState(false)
  const [showAddMember, setShowAddMember] = useState(false)
  const [newTeamName, setNewTeamName] = useState('')
  const [newTeamDesc, setNewTeamDesc] = useState('')
  const [memberEmail, setMemberEmail] = useState('')
  const [memberRole, setMemberRole] = useState('member')

  useEffect(() => {
    loadTeams()
  }, [])

  useEffect(() => {
    if (selectedTeam) {
      loadMembers(selectedTeam.id)
    }
  }, [selectedTeam])

  const loadTeams = async () => {
    try {
      const data = await teamApi.list()
      setTeams(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : '加载失败')
    } finally {
      setLoading(false)
    }
  }

  const loadMembers = async (teamId: number) => {
    try {
      const data = await teamApi.getMembers(teamId)
      setMembers(data)
    } catch (err) {
      console.error('加载成员失败:', err)
    }
  }

  const handleCreateTeam = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const team = await teamApi.create({ name: newTeamName, description: newTeamDesc })
      setTeams([team, ...teams])
      setShowCreate(false)
      setNewTeamName('')
      setNewTeamDesc('')
    } catch (err) {
      setError(err instanceof Error ? err.message : '创建失败')
    }
  }

  const handleDeleteTeam = async (id: number) => {
    if (!confirm('确定要删除这个团队吗？')) return
    try {
      await teamApi.delete(id)
      setTeams(teams.filter(t => t.id !== id))
      if (selectedTeam?.id === id) {
        setSelectedTeam(null)
        setMembers([])
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : '删除失败')
    }
  }

  const handleAddMember = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!selectedTeam) return
    try {
      await teamApi.addMember(selectedTeam.id, { email: memberEmail, role: memberRole })
      await loadMembers(selectedTeam.id)
      setShowAddMember(false)
      setMemberEmail('')
    } catch (err) {
      setError(err instanceof Error ? err.message : '添加失败')
    }
  }

  const handleRemoveMember = async (userId: number) => {
    if (!selectedTeam || !confirm('确定要移除这个成员吗？')) return
    try {
      await teamApi.removeMember(selectedTeam.id, userId)
      setMembers(members.filter(m => m.userId !== userId))
    } catch (err) {
      setError(err instanceof Error ? err.message : '移除失败')
    }
  }

  if (loading) return <div className="loading">加载中...</div>

  return (
    <div className="teams-page">
      <div className="teams-sidebar">
        <div className="sidebar-header">
          <h2>团队</h2>
          <button className="btn-primary" onClick={() => setShowCreate(true)}>
            新建
          </button>
        </div>

        {showCreate && (
          <form className="create-team-form" onSubmit={handleCreateTeam}>
            <input
              type="text"
              value={newTeamName}
              onChange={e => setNewTeamName(e.target.value)}
              placeholder="团队名称"
              required
            />
            <input
              type="text"
              value={newTeamDesc}
              onChange={e => setNewTeamDesc(e.target.value)}
              placeholder="团队描述（可选）"
            />
            <div className="form-actions">
              <button type="submit" className="btn-primary btn-sm">创建</button>
              <button type="button" className="btn-secondary btn-sm" onClick={() => setShowCreate(false)}>
                取消
              </button>
            </div>
          </form>
        )}

        <div className="teams-list">
          {teams.length === 0 ? (
            <p className="no-teams">暂无团队</p>
          ) : (
            teams.map(team => (
              <div
                key={team.id}
                className={`team-item ${selectedTeam?.id === team.id ? 'active' : ''}`}
                onClick={() => setSelectedTeam(team)}
              >
                <div className="team-icon">{team.name.charAt(0).toUpperCase()}</div>
                <div className="team-info">
                  <strong>{team.name}</strong>
                  <small>{team.memberCount || 0} 成员</small>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="teams-content">
        {error && <div className="error-message">{error}</div>}

        {selectedTeam ? (
          <>
            <div className="team-header">
              <div>
                <h1>{selectedTeam.name}</h1>
                {selectedTeam.description && (
                  <p className="team-description">{selectedTeam.description}</p>
                )}
              </div>
              <div className="team-actions">
                <button onClick={() => setShowAddMember(true)}>添加成员</button>
                <button className="btn-danger" onClick={() => handleDeleteTeam(selectedTeam.id)}>
                  删除团队
                </button>
              </div>
            </div>

            {showAddMember && (
              <form className="add-member-form card" onSubmit={handleAddMember}>
                <h3>添加成员</h3>
                <div className="form-row">
                  <input
                    type="email"
                    value={memberEmail}
                    onChange={e => setMemberEmail(e.target.value)}
                    placeholder="成员邮箱"
                    required
                  />
                  <select value={memberRole} onChange={e => setMemberRole(e.target.value)}>
                    <option value="member">成员</option>
                    <option value="admin">管理员</option>
                  </select>
                  <button type="submit" className="btn-primary">添加</button>
                  <button type="button" className="btn-secondary" onClick={() => setShowAddMember(false)}>
                    取消
                  </button>
                </div>
              </form>
            )}

            <div className="members-section">
              <h3>成员列表 ({members.length})</h3>
              <div className="members-list">
                {members.map(member => (
                  <div key={member.id} className="member-card card">
                    <div className="member-avatar">
                      {(member.user?.username || 'U').charAt(0).toUpperCase()}
                    </div>
                    <div className="member-info">
                      <strong>{member.user?.displayName || member.user?.username || `用户 ${member.userId}`}</strong>
                      <small>{member.user?.email}</small>
                    </div>
                    <span className={`role-badge ${member.role}`}>
                      {member.role === 'owner' ? '所有者' : 
                       member.role === 'admin' ? '管理员' : '成员'}
                    </span>
                    {member.role !== 'owner' && (
                      <button 
                        className="btn-danger btn-sm"
                        onClick={() => handleRemoveMember(member.userId)}
                      >
                        移除
                      </button>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </>
        ) : (
          <div className="no-selection">
            <p>选择一个团队查看详情</p>
          </div>
        )}
      </div>
    </div>
  )
}
