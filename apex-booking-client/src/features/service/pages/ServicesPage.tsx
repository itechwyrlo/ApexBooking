import React, { useEffect, useMemo, useState } from 'react';
import { faPlus, faBan, faEdit } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Button } from '../../../components/ui/Button';
import { Alert } from '../../../components/ui/Alert';
import { FormModal } from '../../../components/ui/modal/FormModal';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import { Pagination } from '../../../components/ui/pagination/Pagination';
import { Table } from '../../../components/ui/table/table';
import { useServices } from '../hooks/useServices';
import { useResources } from '../../resources/hooks/useResources';
import type { Service, CreateServiceRequest, UpdateServiceRequest } from '../types';
import type { Column, ModelSchema } from '../../../components/ui/table/types';

const EMPTY_FORM: CreateServiceRequest = {
  name: '',
  description: '',
  durationMinutes: 60,
  price: 0,
  currencyCode: 'USD',
  resourceIds: [],
  bufferBeforeMinutes: 0,
  bufferAfterMinutes: 0,
  minAdvanceBookingHours: undefined,
  maxAdvanceBookingDays: undefined,
};

const ServicesPage: React.FC = () => {
  const { services, total, isLoading, error, clearError, getAll, create, update, deactivate } = useServices();
  const { resources, getAll: getResources } = useResources();

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [showForm, setShowForm] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [editingService, setEditingService] = useState<Service | null>(null);
  const [targetService, setTargetService] = useState<Service | null>(null);
  const [formValue, setFormValue] = useState<CreateServiceRequest>(EMPTY_FORM);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  useEffect(() => {
    getAll(currentPage, pageSize);
    getResources();
  }, [currentPage, pageSize]);

  const handlePageChange = (page: number) => setCurrentPage(page);
  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setCurrentPage(1);
  };

  const serviceFormSchema = useMemo((): ModelSchema<CreateServiceRequest>[] => [
    { key: 'name', label: 'Name', type: 'string', required: true },
    { key: 'description', label: 'Description', type: 'textarea' },
    { key: 'durationMinutes', label: 'Duration (minutes)', type: 'number', required: true },
    { key: 'price', label: 'Price', type: 'number', required: true },
    { key: 'currencyCode', label: 'Currency Code', type: 'string', required: true },
    { key: 'bufferBeforeMinutes', label: 'Buffer Before (minutes)', type: 'number' },
    { key: 'bufferAfterMinutes', label: 'Buffer After (minutes)', type: 'number' },
    { key: 'minAdvanceBookingHours', label: 'Min Advance Booking (hours)', type: 'number' },
    { key: 'maxAdvanceBookingDays', label: 'Max Advance Booking (days)', type: 'number' },
    {
      key: 'resourceIds',
      label: 'Resources',
      type: 'multiselect',
      required: true,
      dataSource: {
        mode: 'static',
        options: resources.map(r => ({ label: r.name, value: r.id })),
      },
    },
  ], [resources]);

  const columns: Column<Service>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (value, row) => (
        <div>
          <div className="fw-medium">{value}</div>
          {row.description && <div className="text-muted small">{row.description}</div>}
        </div>
      ),
    },
    {
      key: 'durationMinutes',
      header: 'Duration',
      render: (value) => `${value} min`,
    },
    {
      key: 'price',
      header: 'Price',
      render: (value, row) => `${value.toFixed(2)} ${row.currencyCode}`,
    },
    {
      key: 'resourceIds',
      header: 'Resources',
      render: (value) => `${value.length} resource(s)`,
    },
    {
      key: 'isActive',
      header: 'Status',
      render: (value) => (
        <span className={`badge ${value ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}`}>
          {value ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      key: 'id',
      header: 'Actions',
      render: (_value, row) => (
        <div className="d-flex justify-content-end gap-2">
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

  const openCreate = () => {
    setEditingService(null);
    setFormValue(EMPTY_FORM);
    setShowForm(true);
  };

  const openEdit = (service: Service) => {
    setEditingService(service);
    setFormValue({
      name: service.name,
      description: service.description ?? '',
      durationMinutes: service.durationMinutes,
      price: service.price,
      currencyCode: service.currencyCode,
      resourceIds: service.resourceIds,
      bufferBeforeMinutes: service.bufferBeforeMinutes,
      bufferAfterMinutes: service.bufferAfterMinutes,
      minAdvanceBookingHours: service.minAdvanceBookingHours,
      maxAdvanceBookingDays: service.maxAdvanceBookingDays,
    });
    setShowForm(true);
  };

  const openDeactivate = (service: Service) => {
    setTargetService(service);
    setShowConfirm(true);
  };

  const handleSubmit = async (value: CreateServiceRequest): Promise<void> => {
    if (editingService) {
      const req: UpdateServiceRequest = {
        name: value.name,
        description: value.description,
        durationMinutes: value.durationMinutes,
        price: value.price,
        currencyCode: value.currencyCode,
        resourceIds: value.resourceIds,
        bufferBeforeMinutes: value.bufferBeforeMinutes,
        bufferAfterMinutes: value.bufferAfterMinutes,
        minAdvanceBookingHours: value.minAdvanceBookingHours,
        maxAdvanceBookingDays: value.maxAdvanceBookingDays,
      };
      const ok = await update(editingService.id, req);
      if (ok) {
        setShowForm(false);
        setSuccessMessage('Service updated.');
        await getAll(currentPage, pageSize);
      }
    } else {
      const ok = await create(value);
      if (ok) {
        setShowForm(false);
        setSuccessMessage('Service created.');
        await getAll(currentPage, pageSize);
      }
    }
  };

  const handleDeactivate = async () => {
    if (!targetService) return;
    const ok = await deactivate(targetService.id);
    if (ok) {
      setShowConfirm(false);
      setTargetService(null);
      setSuccessMessage('Service deactivated.');
      await getAll(currentPage, pageSize);
    }
  };

  return (
    <div className="container-fluid">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h4 className="fw-bold mb-1">Services</h4>
          <div className="text-muted small">Manage bookable services and their resource assignments.</div>
        </div>
        <Button variant="primary" icon={faPlus} onClick={openCreate}>
          New Service
        </Button>
      </div>

      {successMessage && (
        <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
          {error}
        </Alert>
      )}

      <div className="card border-0 shadow-sm">
        <div className="card-body p-0">
          {isLoading ? (
            <div className="p-4 text-center text-muted small">Loading services...</div>
          ) : services.length === 0 ? (
            <div className="p-4 text-center text-muted small">
              No services found. Add your first service to get started.
            </div>
          ) : (
            <Table
              data={services}
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
        title={editingService ? 'Edit Service' : 'New Service'}
        fields={serviceFormSchema}
        value={formValue}
        onChange={setFormValue}
        onSubmit={handleSubmit}
        onClose={() => setShowForm(false)}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Deactivate Service"
        message={`Deactivating "${targetService?.name}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
        onConfirm={handleDeactivate}
        onCancel={() => { setShowConfirm(false); setTargetService(null); }}
      />
    </div>
  );
};

export default ServicesPage;
// import React, { useEffect, useMemo, useState } from 'react';
// import { faPlus, faBan, faEdit } from '@fortawesome/free-solid-svg-icons';
// import { Button } from '../../../components/ui/Button';
// import { Alert } from '../../../components/ui/Alert';
// import { FormModal } from '../../../components/ui/modal/FormModal';
// import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
// import { Pagination } from '../../../components/ui/pagination/Pagination';
// import { useServices } from '../hooks/useServices';
// import { useResources } from '../../resources/hooks/useResources';
// import type { Service, CreateServiceRequest, UpdateServiceRequest } from '../types';
// import type { ModelSchema } from '../../../components/ui/table/types';
// import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

// const EMPTY_FORM: CreateServiceRequest = {
//   name: '',
//   description: '',
//   durationMinutes: 60,
//   price: 0,
//   currencyCode: 'USD',
//   resourceIds: [],
//   bufferBeforeMinutes: 0,
//   bufferAfterMinutes: 0,
//   minAdvanceBookingHours: undefined,
//   maxAdvanceBookingDays: undefined,
// };

// const PAGE_SIZE = 10;

// const ServicesPage: React.FC = () => {
//   const { services, isLoading, error, clearError, getAll, create, update, deactivate } = useServices();
//   const { resources, getAll: getResources } = useResources();

//   const [currentPage, setCurrentPage] = useState(1);
//   const [showForm, setShowForm] = useState(false);
//   const [showConfirm, setShowConfirm] = useState(false);
//   const [editingService, setEditingService] = useState<Service | null>(null);
//   const [targetService, setTargetService] = useState<Service | null>(null);
//   const [formValue, setFormValue] = useState<CreateServiceRequest>(EMPTY_FORM);
//   const [successMessage, setSuccessMessage] = useState<string | null>(null);

//   useEffect(() => {
//     getAll();
//     getResources();
//   }, [getAll, getResources]);

//   const serviceFormSchema = useMemo((): ModelSchema<CreateServiceRequest>[] => [
//     { key: 'name', label: 'Name', type: 'string', required: true },
//     { key: 'description', label: 'Description', type: 'textarea' },
//     { key: 'durationMinutes', label: 'Duration (minutes)', type: 'number', required: true },
//     { key: 'price', label: 'Price', type: 'number', required: true },
//     { key: 'currencyCode', label: 'Currency Code', type: 'string', required: true },
//     { key: 'bufferBeforeMinutes', label: 'Buffer Before (minutes)', type: 'number' },
//     { key: 'bufferAfterMinutes', label: 'Buffer After (minutes)', type: 'number' },
//     { key: 'minAdvanceBookingHours', label: 'Min Advance Booking (hours)', type: 'number' },
//     { key: 'maxAdvanceBookingDays', label: 'Max Advance Booking (days)', type: 'number' },
//     {
//       key: 'resourceIds',
//       label: 'Resources',
//       type: 'multiselect',
//       required: true,
//       dataSource: {
//         mode: 'static',
//         options: resources.map(r => ({ label: r.name, value: r.id })),
//       },
//     },
//   ], [resources]);

//   const totalPages = Math.max(1, Math.ceil(services.length / PAGE_SIZE));
//   const paged = services.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

//   const openCreate = () => {
//     setEditingService(null);
//     setFormValue(EMPTY_FORM);
//     setShowForm(true);
//   };

//   const openEdit = (service: Service) => {
//     setEditingService(service);
//     setFormValue({
//       name: service.name,
//       description: service.description ?? '',
//       durationMinutes: service.durationMinutes,
//       price: service.price,
//       currencyCode: service.currencyCode,
//       resourceIds: service.resourceIds,
//       bufferBeforeMinutes: service.bufferBeforeMinutes,
//       bufferAfterMinutes: service.bufferAfterMinutes,
//       minAdvanceBookingHours: service.minAdvanceBookingHours,
//       maxAdvanceBookingDays: service.maxAdvanceBookingDays,
//     });
//     setShowForm(true);
//   };

//   const openDeactivate = (service: Service) => {
//     setTargetService(service);
//     setShowConfirm(true);
//   };

//   const handleSubmit = async (value: CreateServiceRequest): Promise<void> => {
//     if (editingService) {
//       const req: UpdateServiceRequest = {
//         name: value.name,
//         description: value.description,
//         durationMinutes: value.durationMinutes,
//         price: value.price,
//         currencyCode: value.currencyCode,
//         resourceIds: value.resourceIds,
//         bufferBeforeMinutes: value.bufferBeforeMinutes,
//         bufferAfterMinutes: value.bufferAfterMinutes,
//         minAdvanceBookingHours: value.minAdvanceBookingHours,
//         maxAdvanceBookingDays: value.maxAdvanceBookingDays,
//       };
//       const ok = await update(editingService.id, req);
//       if (ok) {
//         setShowForm(false);
//         setSuccessMessage('Service updated.');
//       }
//     } else {
//       const ok = await create(value);
//       if (ok) {
//         setShowForm(false);
//         setSuccessMessage('Service created.');
//       }
//     }
//   };

//   const handleDeactivate = async () => {
//     if (!targetService) return;
//     const ok = await deactivate(targetService.id);
//     if (ok) {
//       setShowConfirm(false);
//       setTargetService(null);
//       setSuccessMessage('Service deactivated.');
//     }
//   };

//   return (
//     <div className="container-fluid">
//       <div className="d-flex justify-content-between align-items-center mb-4">
//         <div>
//           <h4 className="fw-bold mb-1">Services</h4>
//           <div className="text-muted small">Manage bookable services and their resource assignments.</div>
//         </div>
//         <Button variant="primary" icon={faPlus} onClick={openCreate}>
//           New Service
//         </Button>
//       </div>

//       {successMessage && (
//         <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
//           {successMessage}
//         </Alert>
//       )}

//       {error && (
//         <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
//           {error}
//         </Alert>
//       )}

//       <div className="card border-0 shadow-sm">
//         <div className="card-body p-0">
//           {isLoading ? (
//             <div className="p-4 text-center text-muted small">Loading services...</div>
//           ) : services.length === 0 ? (
//             <div className="p-4 text-center text-muted small">
//               No services found. Add your first service to get started.
//             </div>
//           ) : (
//             <div className="table-responsive">
//               <table className="table table-hover align-middle mb-0">
//                 <thead className="table-light">
//                   <tr>
//                     <th className="ps-4 fw-semibold small text-muted">Name</th>
//                     <th className="fw-semibold small text-muted">Duration</th>
//                     <th className="fw-semibold small text-muted">Price</th>
//                     <th className="fw-semibold small text-muted">Resources</th>
//                     <th className="fw-semibold small text-muted">Status</th>
//                     <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
//                   </tr>
//                 </thead>
//                 <tbody>
//                   {paged.map(service => (
//                     <tr key={service.id}>
//                       <td className="ps-4">
//                         <div className="fw-medium">{service.name}</div>
//                         {service.description && (
//                           <div className="text-muted small">{service.description}</div>
//                         )}
//                       </td>
//                       <td className="text-muted small">{service.durationMinutes} min</td>
//                       <td className="text-muted small">{service.price.toFixed(2)} {service.currencyCode}</td>
//                       <td className="text-muted small">{service.resourceIds.length} resource(s)</td>
//                       <td>
//                         <span className={`badge ${service.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}`}>
//                           {service.isActive ? 'Active' : 'Inactive'}
//                         </span>
//                       </td>
//                       <td className="text-end pe-4">
//                         <div className="d-flex justify-content-end gap-2">
//                           <button
//                             className="btn btn-sm btn-outline-primary"
//                             title="Edit"
//                             onClick={() => openEdit(service)}
//                           >
//                             <FontAwesomeIcon icon={faEdit} />
//                           </button>
//                           {service.isActive && (
//                             <button
//                               className="btn btn-sm btn-outline-danger"
//                               title="Deactivate"
//                               onClick={() => openDeactivate(service)}
//                             >
//                               <FontAwesomeIcon icon={faBan} />
//                             </button>
//                           )}
//                         </div>
//                       </td>
//                     </tr>
//                   ))}
//                 </tbody>
//               </table>
//             </div>
//           )}
//         </div>

//         {services.length > PAGE_SIZE && (
//           <div className="card-footer bg-white border-top-0">
//             <Pagination
//               currentPage={currentPage}
//               totalPages={totalPages}
//               pageSize={PAGE_SIZE}
//               totalItems={services.length}
//               onPageChange={setCurrentPage}
//               onPageSizeChange={() => {}}
//             />
//           </div>
//         )}
//       </div>

//       <FormModal
//         isOpen={showForm}
//         title={editingService ? 'Edit Service' : 'New Service'}
//         fields={serviceFormSchema}
//         value={formValue}
//         onChange={setFormValue}
//         onSubmit={handleSubmit}
//         onClose={() => setShowForm(false)}
//       />

//       <ConfirmModal
//         isOpen={showConfirm}
//         title="Deactivate Service"
//         message={`Deactivating "${targetService?.name}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
//         onConfirm={handleDeactivate}
//         onCancel={() => { setShowConfirm(false); setTargetService(null); }}
//       />
//     </div>
//   );
// };

// export default ServicesPage;