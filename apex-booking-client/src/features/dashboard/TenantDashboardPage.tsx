import React, { useEffect, useState } from "react";
import axiosInstance from "../../services/axiosInstance";
import { useAuth } from "../../context/AuthContext";

interface DashboardStats {
  totalBookingsToday: number;
  upcomingBookings: number;
  pendingPayments: number;
  activeServices: number;
  activeResources: number;
}

const TenantDashboardPage: React.FC = () => {
  const { accessToken, tenantSlug } = useAuth();

  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        setLoading(true);

        const res = await axiosInstance.get("/dashboard/summary", {
          headers: {
            Authorization: `Bearer ${accessToken}`,
            "X-Tenant": tenantSlug,
          },
        });

        if (res.data?.isSuccess) {
          setStats(res.data.data);
        }
      } catch (err) {
        console.error("Dashboard load failed", err);
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();
  }, [accessToken, tenantSlug]);

  if (loading) {
    return <div className="p-3">Loading dashboard...</div>;
  }

  if (!stats) {
    return <div className="p-3">No dashboard data available</div>;
  }

  return (
    <div className="container-fluid">
      <div className="mb-4">
        <h4 className="fw-bold mb-1">Tenant Dashboard</h4>
        <div className="text-muted small">Operational overview</div>
      </div>

      <div className="row g-3">
        <div className="col-md-4 col-lg-3">
          <div className="card p-3">
            <div className="text-muted small">Today Bookings</div>
            <div className="fs-4 fw-bold">{stats.totalBookingsToday}</div>
          </div>
        </div>

        <div className="col-md-4 col-lg-3">
          <div className="card p-3">
            <div className="text-muted small">Upcoming</div>
            <div className="fs-4 fw-bold">{stats.upcomingBookings}</div>
          </div>
        </div>

        <div className="col-md-4 col-lg-3">
          <div className="card p-3">
            <div className="text-muted small">Pending Payments</div>
            <div className="fs-4 fw-bold">{stats.pendingPayments}</div>
          </div>
        </div>

        <div className="col-md-4 col-lg-3">
          <div className="card p-3">
            <div className="text-muted small">Services</div>
            <div className="fs-4 fw-bold">{stats.activeServices}</div>
          </div>
        </div>

        <div className="col-md-4 col-lg-3">
          <div className="card p-3">
            <div className="text-muted small">Resources</div>
            <div className="fs-4 fw-bold">{stats.activeResources}</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TenantDashboardPage;