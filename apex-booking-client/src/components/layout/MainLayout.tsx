import React, { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { Footer } from './Footer';

export const MainLayout: React.FC = () => {
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  // Handle responsive behavior for sidebar
  useEffect(() => {
    const handleResize = () => {
      const mobile = window.innerWidth < 768;
      setIsMobile(mobile);
      
      // Auto-collapse sidebar on mobile devices
      if (mobile) {
        setIsCollapsed(true);
      } else {
        // Expand by default on desktop for better usability
        setIsCollapsed(false);
      }
    };

    // Run once on mount to set initial state
    handleResize();

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  const toggleSidebar = () => {
    setIsCollapsed(prev => !prev);
  };

  return (
    <div className="d-flex vh-100 bg-light overflow-hidden">
      {/* Sidebar Visibility Logic:
        On mobile, we only show the sidebar if it is NOT collapsed.
        On desktop, we always show it (it just changes width).
      */}
      {(!isMobile || !isCollapsed) && (
        <Sidebar isCollapsed={isCollapsed} />
      )}
      
      {/* Main Container */}
      <div className="flex-grow-1 d-flex flex-column min-w-0">
        {/* Top Navigation / Header */}
        <Header onToggleSidebar={toggleSidebar} />
        
        {/* Page Content Area 
          The 'Outlet' component is where your child routes 
          (Dashboard, Services, etc.) will render.
        */}
        <main className="flex-grow-1 overflow-auto p-3 p-md-4">
          <Outlet /> 
        </main>
        
        {/* Bottom Navigation / Footer */}
        <Footer />
      </div>

      {/* Mobile Backdrop: Closes sidebar when clicking outside on mobile */}
      {isMobile && !isCollapsed && (
        <div 
          className="position-fixed top-0 start-0 w-100 vh-100 bg-dark opacity-50"
          style={{ zIndex: 998 }}
          onClick={toggleSidebar}
        />
      )}
    </div>
  );
};