import React from 'react';
import { useNavigate, useLocation, useParams } from 'react-router-dom';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { getSidebarConfig } from '../../constants/sidebarConfig';
import { useAuth } from '../../context/AuthContext';

export const Sidebar: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { tenant } = useParams<{ tenant: string }>();
  const { user } = useAuth();

  const sidebarItems = getSidebarConfig(user?.role ?? '');

  const buildPath = (path?: string) => {
    if (!path) return '#';
    if (!tenant) return path;
    return `/t/${tenant}${path}`;
  };

  const isActive = (path?: string) => {
    if (!path) return false;
    return location.pathname === buildPath(path);
  };

  return (
    <aside className="apex-sidebar">
      <nav className="apex-nav">
        {sidebarItems.map(group => (
          <div key={group.id}>
            <div className="apex-nav-section">{group.label}</div>
            {group.children.map(item => (
              <div
                key={item.id}
                className={`apex-nav-item${isActive(item.path) ? ' active' : ''}`}
                onClick={() => item.path && navigate(buildPath(item.path))}
                title={item.label}
              >
                <FontAwesomeIcon icon={item.icon} />
                <span>{item.label}</span>
              </div>
            ))}
          </div>
        ))}
      </nav>
    </aside>
  );
};
