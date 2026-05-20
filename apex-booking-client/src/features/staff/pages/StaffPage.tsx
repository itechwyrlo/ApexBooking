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
import { StaffSkeleton } from "../components/StaffSkeleton";
import { useStaff } from "../hooks/useStaff";
import type { Column } from "../../../components/ui/table/types";
import type { StaffDto, CreateStaffRequest, UpdateStaffRequest } from "../types";
import type { ModelSchema } from "../../../components/ui/table/types";

const staffFormSchema: ModelSchema<CreateStaffRequest>[] = [
  { key: "firstName", label: "First Name", type: "string", required: true },
  { key: "lastName", label: "Last Name", type: "string", required: true },
  { key: "email", label: "Email", type: "string", required: false },
  { key: "contactNumber", label: "Contact", type: "phone", required: false },
  { key: "capacity", label: "Capacity", type: "number", required: true },
  { key: "description", label: "Description", type: "textarea" },
];

const EMPTY_FORM: CreateStaffRequest = {
  firstName: "",
  lastName: "",
  email: "",
  contactNumber: "",
  capacity: 1,
  description: "",
};

const PAGE_SIZE = 10;

const StaffPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const navigate = useNavigate();
  const {
    staff,
    total,
    isLoading,
    error,
    clearError,
    getAll,
    create,
    update,
    deactivate,
  } = useStaff();

  const [currentPage, setCurrentPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffDto | null>(null);
  const [targetStaff, setTargetStaff] = useState<StaffDto | null>(null);
  const [formValue, setFormValue] = useState<CreateStaffRequest>(EMPTY_FORM);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  useEffect(() => {
    getAll(currentPage, PAGE_SIZE);
  }, [getAll, currentPage]);

  const openCreate = () => {
    setEditingStaff(null);
    setFormValue(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (member: StaffDto) => {
    setEditingStaff(member);
    setFormValue({
      firstName: member.firstName,
      lastName: member.lastName,
      email: member.email,
      contactNumber: member.contactNumber,
      capacity: member.capacity,
      description: member.description ?? "",
    });
    setShowForm(true);
  };

  const openDeactivate = (member: StaffDto) => {
    setTargetStaff(member);
    setShowConfirm(true);
  };

  const handleSubmit = async (value: CreateStaffRequest): Promise<void> => {
    if (editingStaff) {
      const req: UpdateStaffRequest = {
        firstName: value.firstName,
        lastName: value.lastName,
        email: value.email,
        contactNumber: value.contactNumber,
        capacity: value.capacity,
        description: value.description,
      };
      const ok = await update(editingStaff.id, req);
      if (ok) {
        setShowForm(false);
        setSuccessMessage("Staff updated.");
        await getAll(currentPage, PAGE_SIZE);
      }
    } else {
      const ok = await create(value);
      if (ok) {
        setShowForm(false);
        setSuccessMessage(
          `${value.firstName} ${value.lastName} has been added. An invitation email has been sent to ${value.email}.`
        );
        await getAll(currentPage, PAGE_SIZE);
      }
    }
  };

  const handleDeactivate = async () => {
    if (!targetStaff) return;
    const ok = await deactivate(targetStaff.id);
    if (ok) {
      setShowConfirm(false);
      setTargetStaff(null);
      setSuccessMessage("Staff deactivated.");
      await getAll(currentPage, PAGE_SIZE);
    }
  };

  const columns: Column<StaffDto>[] = [
    {
      key: "name",
      header: "Name",
      render: (_value, row) => `${row.firstName} ${row.lastName}`.trim(),
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
            onClick={() => navigate(`/t/${tenant}/staff/${row.id}/availability`)}
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
    <div className="container-fluid px-3 px-md-4 py-4">
      <div className="row mb-4 align-items-center">
        <div className="col">
          <h5 className="fw-bold mb-0">Staff</h5>
          <small className="text-muted">Manage bookable staff members.</small>
        </div>
        <div className="col-auto">
          <Button variant="primary" icon={faPlus} onClick={openCreate}>
            Add Staff
          </Button>
        </div>
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
        <div className="alert alert-danger alert-dismissible d-flex align-items-center mb-3" role="alert">
          {error}
          <button type="button" className="btn-close ms-auto" onClick={clearError} aria-label="Dismiss" />
        </div>
      )}

      <div className="row">
        <div className="col-12">
          <div className="card border-0 shadow-sm">
            <div className="card-body p-0">
              {isLoading ? (
                <StaffSkeleton />
              ) : staff.length === 0 ? (
                <div className="p-4 text-center text-muted small">
                  No staff found. Add your first staff member to get started.
                </div>
              ) : (
                <Table data={staff} columns={columns} getRowId={(row) => row.id} />
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
        </div>
      </div>

      <FormModal
        isOpen={showForm}
        title={editingStaff ? "Edit Staff" : "Add Staff"}
        fields={staffFormSchema}
        value={formValue}
        onChange={setFormValue}
        onSubmit={handleSubmit}
        onClose={() => setShowForm(false)}
        notice={!editingStaff ? "An invitation email with a setup link will be sent to this staff member." : undefined}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Deactivate Staff"
        message={`Deactivating "${targetStaff?.firstName}" will remove them from the booking flow. Existing bookings are not cancelled automatically.`}
        onConfirm={handleDeactivate}
        onCancel={() => {
          setShowConfirm(false);
          setTargetStaff(null);
        }}
      />
    </div>
  );
};

export default StaffPage;
