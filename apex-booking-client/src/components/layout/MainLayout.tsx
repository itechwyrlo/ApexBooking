import React, { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';

export const MainLayout: React.FC = () => {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const handleResize = () => {
      const mobile = window.innerWidth <= 767;
      setIsMobile(mobile);
      if (mobile) setIsCollapsed(true);
      else setIsCollapsed(false);
    };
    handleResize();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const toggleSidebar = () => setIsCollapsed(prev => !prev);

  const layoutClass = [
    'apex-layout',
    !isMobile && isCollapsed ? 'collapsed' : '',
    isMobile && !isCollapsed ? 'mobile-open' : '',
  ].filter(Boolean).join(' ');

  return (
    <div className={layoutClass}>
      <Header onToggleSidebar={toggleSidebar} />
      <div className="apex-body">
        <Sidebar />
        <main className="apex-main p-3 p-md-4">
          <Outlet />
        </main>
      </div>
      {isMobile && !isCollapsed && (
        <div
          className="position-fixed top-0 start-0 w-100 vh-100 bg-dark"
          style={{ zIndex: 148, opacity: 0.45 }}
          onClick={toggleSidebar}
        />
      )}
    </div>
  );
};
