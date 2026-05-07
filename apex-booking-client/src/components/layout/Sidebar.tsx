import React, { useState } from "react";
import { useNavigate, useLocation, useParams } from "react-router-dom";
import { getSidebarConfig } from "../../constants/sidebarConfig";

interface SidebarProps {
  isCollapsed: boolean;
}

export const Sidebar: React.FC<SidebarProps> = ({ isCollapsed }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { tenant } = useParams(); // get tenant from URL

  const sidebarItems = getSidebarConfig();
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({});

  // central fix: always prefix tenant
  const buildPath = (path?: string) => {
    if (!path) return "#";
    if (!tenant) return path; // fallback
    return `/t/${tenant}${path}`;
  };

  const toggleGroup = (id: string, path?: string) => {
    if (path) navigate(buildPath(path));
    setOpenGroups((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  const isActive = (path?: string) => {
    if (!path) return false;
    return location.pathname === buildPath(path);
  };

  return (
    <div
      className="d-flex flex-column vh-100 bg-white border-end"
      style={{ width: isCollapsed ? "70px" : "240px", transition: "width 0.2s" }}
    >
      <div className="p-3 border-bottom d-flex align-items-center">
        <div
          className="bg-primary rounded d-flex align-items-center justify-content-center"
          style={{ width: 36, height: 36 }}
        >
          <span className="text-white fw-bold">A</span>
        </div>
        {!isCollapsed && <span className="ms-3 fw-bold">ApexBooking</span>}
      </div>

      <div className="flex-grow-1 overflow-auto">
        {sidebarItems.map((group) => {
          const isGroupOpen = openGroups[group.id] ?? true;

          return (
            <div key={group.id} className="px-2 mt-2">
              {group.isGroup && (
                <>
                  <div
                    onClick={() => !isCollapsed && toggleGroup(group.id)}
                    className="d-flex align-items-center justify-content-between px-2 py-2 text-secondary small fw-semibold"
                    style={{ cursor: "pointer" }}
                  >
                    <span>{!isCollapsed && group.label}</span>
                    {!isCollapsed && <span>{isGroupOpen ? "−" : "+"}</span>}
                  </div>

                  {!isCollapsed && isGroupOpen && (
                    <div className="ps-2">
                      {group.children?.map((item) => {
                        const isSubOpen = openGroups[item.id] ?? false;

                        return (
                          <div key={item.id}>
                            <div
                              onClick={() => toggleGroup(item.id, item.path)}
                              className={`px-3 py-2 rounded mb-1 d-flex justify-content-between align-items-center ${
                                isActive(item.path)
                                  ? "bg-primary-subtle text-primary fw-semibold"
                                  : "text-dark"
                              }`}
                              style={{ cursor: "pointer" }}
                            >
                              <span>
                                <i className={`${item.icon} me-2`}></i>
                                {item.label}
                              </span>

                              {item.children && (
                                <span>{isSubOpen ? "▾" : "▸"}</span>
                              )}
                            </div>

                            {isSubOpen &&
                              item.children?.map((child) => (
                                <div
                                  key={child.id}
                                  onClick={() =>
                                    child.path && navigate(buildPath(child.path))
                                  }
                                  className={`px-4 py-2 rounded mb-1 ms-3 small ${
                                    isActive(child.path)
                                      ? "bg-light text-primary fw-bold"
                                      : "text-secondary"
                                  }`}
                                  style={{ cursor: "pointer" }}
                                >
                                  <i className={`${child.icon} me-2`}></i>
                                  {child.label}
                                </div>
                              ))}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
};