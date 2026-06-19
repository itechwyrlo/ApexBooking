import React, { useCallback, useEffect, useMemo, useState } from 'react';

import { Alert } from '../../../components/ui/Alert';
import { Pagination } from '../../../components/ui/pagination/Pagination';
import { Table } from '../../../components/ui/table/table';
import ClientDetailDrawer from '../components/ClientDetailDrawer';

import { useClients } from '../hooks/useClients';
import type { ClientSummaryDto } from '../types';
import type { Column } from '../../../components/ui/table/types';
import './ClientsPage.styles.css';

const PAGE_SIZE = 10;

const getInitials = (fullName: string): string => {
  const parts = fullName.trim().split(' ');
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
};

const formatDate = (iso: string): string =>
  new Date(iso + 'T00:00:00').toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  });

const ClientsPage: React.FC = () => {
  const { clients, total, isLoading, error, clearError, getAll } = useClients();

  const [currentPage, setCurrentPage] = useState(1);
  const [selectedClient, setSelectedClient] = useState<ClientSummaryDto | null>(null);
  const [search, setSearch] = useState('');

  useEffect(() => {
    getAll(currentPage, PAGE_SIZE);
  }, [getAll, currentPage]);

  const filtered = useMemo(() => {
    const term = search.toLowerCase();
    if (!term) return clients;
    return clients.filter(c =>
      c.fullName.toLowerCase().includes(term) ||
      c.email.toLowerCase().includes(term)
    );
  }, [clients, search]);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  const handleSearch = useCallback((value: string) => {
    setSearch(value);
    setCurrentPage(1);
  }, []);

  const columns: Column<ClientSummaryDto>[] = [
    {
      key: 'fullName',
      header: 'Client',
      render: (_value, row) => (
        <div className="d-flex align-items-center gap-2">
          <div className="clients-avatar">{getInitials(row.fullName)}</div>
          <div>
            <div className="fw-medium small">{row.fullName}</div>
            <div className="text-muted clients-sub-text">{row.email}</div>
          </div>
        </div>
      ),
    },
    {
      key: 'totalBookings',
      header: 'Total Bookings',
      render: (value) => <span className="small">{value}</span>,
    },
    {
      key: 'lastVisit',
      header: 'Last Visit',
      render: (value) => (
        <span className="small text-muted">
          {value ? formatDate(value as string) : '—'}
        </span>
      ),
    },
    {
      key: 'totalSpent',
      header: 'Total Spent',
      render: (_value, row) => (
        <span className="small">
          {row.currencyCode} {Number(row.totalSpent).toFixed(2)}
        </span>
      ),
    },
  ];

  return (
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4 align-items-center">
        <div className="col">
          <h5 className="fw-bold mb-0">Clients</h5>
        </div>
      </div>

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
          {error}
        </Alert>
      )}

      <div className="row mb-3">
        <div className="col-12 col-md-5">
          <input
            type="text"
            className="form-control form-control-sm"
            placeholder="Search by name or email…"
            value={search}
            onChange={e => handleSearch(e.target.value)}
          />
        </div>
      </div>

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="p-4 text-center text-muted small">Loading clients…</div>
          ) : filtered.length === 0 ? (
            <div className="p-4 text-center text-muted small">No clients found.</div>
          ) : (
            <Table
              data={filtered}
              columns={columns}
              getRowId={(c) => c.email}
              onRowClick={(c) => setSelectedClient(c)}
            />
          )}
        </div>

        {total > PAGE_SIZE && (
          <div className="card-footer bg-white border-top-0">
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              pageSize={PAGE_SIZE}
              totalItems={total}
              onPageChange={setCurrentPage}
            />
          </div>
        )}
      </div>

      <ClientDetailDrawer
        client={selectedClient}
        onClose={() => setSelectedClient(null)}
      />
    </div>
  );
};

export default ClientsPage;
