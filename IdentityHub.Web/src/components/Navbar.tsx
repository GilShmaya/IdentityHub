import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import './Navbar.css';

export function Navbar() {
  const { isAuthenticated, email, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  if (!isAuthenticated) return null;

  return (
    <nav className="navbar">
      <div className="navbar-brand">
        <Link to="/"><span className="brand-icon">⬡</span> IdentityHub</Link>
      </div>
      <div className="navbar-links">
        <Link to="/" className={location.pathname === '/' ? 'active' : ''}>
          Tickets
        </Link>
        <Link to="/jira-config" className={location.pathname === '/jira-config' ? 'active' : ''}>
          Jira Settings
        </Link>
        <Link to="/api-keys" className={location.pathname === '/api-keys' ? 'active' : ''}>
          API Keys
        </Link>
        <Link to="/docs" className={location.pathname === '/docs' ? 'active' : ''}>
          About
        </Link>
      </div>
      <div className="navbar-user">
        <span className="navbar-email">{email}</span>
        <button onClick={handleLogout} className="btn-logout">Logout</button>
      </div>
    </nav>
  );
}
