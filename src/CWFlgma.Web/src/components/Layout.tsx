import { Outlet, Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth'
import './Layout.css'

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <div className="layout">
      <header className="header">
        <div className="header-left">
          <Link to="/" className="logo">CWFlgma</Link>
          <nav className="nav">
            <Link to="/documents">文档</Link>
            <Link to="/teams">团队</Link>
          </nav>
        </div>
        <div className="header-right">
          <span className="user-info">
            {user?.displayName || user?.username}
          </span>
          <button onClick={handleLogout} className="btn-logout">退出</button>
        </div>
      </header>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
