import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  faPlus,
  faEdit,
  faBan,
  faCalendarAlt,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { Button } from "../../../components/ui/Button";
import { Alert } from "../../../components/ui/Alert";
import { FormModal } from "../../../components/ui/modal/FormModal";
import { ConfirmModal } from "../../../components/ui/modal/ConfirmModal";
import { Pagination } from "../../../components/ui/pagination/Pagination";
import { Table } from "../../../components/ui/table/table";
import { useResources } from "../hooks/useResources";
import type { Column } from "../../../components/ui/table/types";
import type {
  Staff,
  CreateResourceRequest,
  UpdateResourceRequest,
} from "../types";
import type { ModelSchema } from "../../../components/ui/table/types";

const resourceFormSchema: ModelSchema<CreateResourceRequest>[] = [
  { key: "firstName", label: "First Name", type: "string", required: true },
  { key: "lastName", label: "Last Name", type: "string", required: true },
  { key: "email", label: "Email", type: "string", required: false },
  { key: "contactNumber", label: "Contact", type: "phone", required: false },
  { key: "capacity", label: "Capacity", type: "number", required: true },
  { key: "description", label: "Description", type: "textarea" },
];

const EMPTY_FORM: CreateResourceRequest = {
  firstName: "",
  lastName: "",
  email: "",
  contactNumber: "",
  capacity: 1,
  description: "",
};

const ResourcesPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const navigate = useNavigate();
  const {
    resources,
    total,
    isLoading,
    error,
    clearError,
    getAll,
    create,
    update,
    deactivate,
  } = useResources();

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [showForm, setShowForm] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [editingResource, setEditingResource] = useState<Staff | null>(null);
  const [targetResource, setTargetResource] = useState<Staff | null>(null);
  const [formValue, setFormValue] = useState<CreateResourceRequest>(EMPTY_FORM);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  useEffect(() => {
    getAll(currentPage, pageSize);
  }, [currentPage, pageSize]);

  const handlePageChange = (page: number) => setCurrentPage(page);
  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  };

  const openCreate = () => {
    setEditingResource(null);
    setFormValue(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (resource: Staff) => {
    setEditingResource(resource);
    debugger;
    setFormValue({
      firstName: resource.firstName,
      lastName: resource.lastName,
      email: resource.email,
      contactNumber: resource.contactNumber,
      capacity: resource.capacity,
      description: resource.description ?? "",
    });
    setShowForm(true);
  };

  const openDeactivate = (resource: Staff) => {
    setTargetResource(resource);
    setShowConfirm(true);
  };

  const handleSubmit = async (value: CreateResourceRequest): Promise<void> => {
    if (editingResource) {
      const req: UpdateResourceRequest = {
        firstName: value.firstName,
        lastName: value.lastName,
        email: value.email,
        contactNumber: value.contactNumber,
        capacity: value.capacity,
        description: value.description,
      };
      const ok = await update(editingResource.id, req);
      if (ok) {
        setShowForm(false);
        setSuccessMessage("Resource updated.");
        await getAll(currentPage, pageSize);
      }
    } else {
      const ok = await create(value);
      if (ok) {
        setShowForm(false);
        setSuccessMessage("Resource created.");
        await getAll(currentPage, pageSize);
      }
    }
  };

  const handleDeactivate = async () => {
    if (!targetResource) return;
    const ok = await deactivate(targetResource.id);
    if (ok) {
      setShowConfirm(false);
      setTargetResource(null);
      setSuccessMessage("Resource deactivated.");
      await getAll(currentPage, pageSize);
    }
  };

  const columns: Column<Staff>[] = [
    { key: "name", header: "Name",
      render: (_value, row) => `${row.firstName} ${row.lastName}`.trim()
    },
    {
      key: "description",
      header: "Description",
      render: (value) => value ?? "—",
    },
    {
      key: "isActive",
      header: "Status",
      render: (value) => (
        <span
          className={`badge ${value ? "bg-success-subtle text-success" : "bg-secondary-subtle text-secondary"}`}
        >
          {value ? "Active" : "Inactive"}
        </span>
      ),
    },
    {
      key: "id",
      header: "Actions",
      render: (_value, row) => (
        <div className="d-flex justify-content-end gap-2">
          <button
            className="btn btn-sm btn-outline-secondary"
            title="Set Availability"
            onClick={() =>
              navigate(`/t/${tenant}/resources/${row.id}/availability`)
            }
          >
            <FontAwesomeIcon icon={faCalendarAlt} />
          </button>
          <button
            className="btn btn-sm btn-outline-primary"
            title="Edit"
            onClick={() => openEdit(row)}
          >
            <FontAwesomeIcon icon={faEdit} />
          </button>
          {row.isActive && (
            <button
              className="btn btn-sm btn-outline-danger"
              title="Deactivate"
              onClick={() => openDeactivate(row)}
            >
              <FontAwesomeIcon icon={faBan} />
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="container-fluid">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold mb-1">Resources</h4>
          <div className="text-muted small">
            Manage bookable people, rooms, and equipment.
          </div>
        </div>
        <Button variant="primary" icon={faPlus} onClick={openCreate}>
          Add Staff
        </Button>
      </div>

      {successMessage && (
        <Alert
          variant="success"
          dismissible
          onDismiss={() => setSuccessMessage(null)}
          className="mb-3"
        >
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert
          variant="error"
          dismissible
          onDismiss={clearError}
          className="mb-3"
        >
          {error}
        </Alert>
      )}

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="p-4 text-center text-muted small">
              Loading resources...
            </div>
          ) : resources.length === 0 ? (
            <div className="p-4 text-center text-muted small">
              No resources found. Add your first resource to get started.
            </div>
          ) : (
            <Table
              data={resources}
              columns={columns}
              getRowId={(row) => row.id}
            />
          )}
        </div>

        {total > 0 && (
          <div className="card-footer bg-white border-top-0">
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              pageSize={pageSize}
              totalItems={total}
              onPageChange={handlePageChange}
              onPageSizeChange={handlePageSizeChange}
            />
          </div>
        )}
      </div>

      <FormModal
        isOpen={showForm}
        title={editingResource ? "Edit Staff" : "Add Staff"}
        fields={resourceFormSchema}
        value={formValue}
        onChange={setFormValue}
        onSubmit={handleSubmit}
        onClose={() => setShowForm(false)}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Deactivate Resource"
        message={`Deactivating "${targetResource?.firstName}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
        onConfirm={handleDeactivate}
        onCancel={() => {
          setShowConfirm(false);
          setTargetResource(null);
        }}
      />
    </div>
  );
};

export default ResourcesPage;