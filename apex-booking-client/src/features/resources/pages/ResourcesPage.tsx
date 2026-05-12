import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { faPlus, faEdit, faBan, faCalendarAlt } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Button } from '../../../components/ui/Button';
import { Alert } from '../../../components/ui/Alert';
import { FormModal } from '../../../components/ui/modal/FormModal';
import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
import { Pagination } from '../../../components/ui/pagination/Pagination';
import { Table } from '../../../components/ui/table/table';
import { useResources } from '../hooks/useResources';
import type { Column } from '../../../components/ui/table/types';
import type { Resource, CreateResourceRequest, UpdateResourceRequest, ResourceType } from '../types';
import type { ModelSchema } from '../../../components/ui/table/types';

const RESOURCE_TYPE_LABELS: Record<ResourceType, string> = {
  Person: 'Person',
  Room: 'Room',
  Equipment: 'Equipment',
  SlotBased: 'Slot Based',
};

const resourceFormSchema: ModelSchema<CreateResourceRequest>[] = [
  { key: 'name', label: 'Name', type: 'string', required: true },
  {
    key: 'resourceType',
    label: 'Type',
    type: 'select',
    required: true,
    options: [
      { label: 'Person', value: 'Person' },
      { label: 'Room', value: 'Room' },
      { label: 'Equipment', value: 'Equipment' },
      { label: 'Slot Based', value: 'SlotBased' },
    ],
  },
  { key: 'capacity', label: 'Capacity', type: 'number', required: true },
  { key: 'description', label: 'Description', type: 'textarea' },
];

const EMPTY_FORM: CreateResourceRequest = {
  name: '',
  resourceType: 'Person',
  capacity: 1,
  description: '',
};

const ResourcesPage: React.FC = () => {
  const { tenant } = useParams<{ tenant: string }>();
  const navigate = useNavigate();
  const { resources, total, isLoading, error, clearError, getAll, create, update, deactivate } = useResources();

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [showForm, setShowForm] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [editingResource, setEditingResource] = useState<Resource | null>(null);
  const [targetResource, setTargetResource] = useState<Resource | null>(null);
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

  const openEdit = (resource: Resource) => {
    setEditingResource(resource);
    setFormValue({
      name: resource.name,
      resourceType: resource.resourceType,
      capacity: resource.capacity,
      description: resource.description ?? '',
    });
    setShowForm(true);
  };

  const openDeactivate = (resource: Resource) => {
    setTargetResource(resource);
    setShowConfirm(true);
  };

  const handleSubmit = async (value: CreateResourceRequest): Promise<void> => {
    if (editingResource) {
      const req: UpdateResourceRequest = {
        name: value.name,
        resourceType: value.resourceType,
        capacity: value.capacity,
        description: value.description,
      };
      const ok = await update(editingResource.id, req);
      if (ok) {
        setShowForm(false);
        setSuccessMessage('Resource updated.');
        await getAll(currentPage, pageSize);
      }
    } else {
      const ok = await create(value);
      if (ok) {
        setShowForm(false);
        setSuccessMessage('Resource created.');
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
      setSuccessMessage('Resource deactivated.');
      await getAll(currentPage, pageSize);
    }
  };

  const columns: Column<Resource>[] = [
    { key: 'name', header: 'Name' },
    {
      key: 'resourceType',
      header: 'Type',
      render: (value) => RESOURCE_TYPE_LABELS[value as ResourceType],
    },
    { key: 'capacity', header: 'Capacity' },
    {
      key: 'description',
      header: 'Description',
      render: (value) => value ?? '—',
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
            className="btn btn-sm btn-outline-secondary"
            title="Set Availability"
            onClick={() => navigate(`/t/${tenant}/resources/${row.id}/availability`)}
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
          <div className="text-muted small">Manage bookable people, rooms, and equipment.</div>
        </div>
        <Button variant="primary" icon={faPlus} onClick={openCreate}>
          Add Resource
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
            <div className="p-4 text-center text-muted small">Loading resources...</div>
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
        title={editingResource ? 'Edit Resource' : 'Add Resource'}
        fields={resourceFormSchema}
        value={formValue}
        onChange={setFormValue}
        onSubmit={handleSubmit}
        onClose={() => setShowForm(false)}
      />

      <ConfirmModal
        isOpen={showConfirm}
        title="Deactivate Resource"
        message={`Deactivating "${targetResource?.name}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
        onConfirm={handleDeactivate}
        onCancel={() => { setShowConfirm(false); setTargetResource(null); }}
      />
    </div>
  );
};

export default ResourcesPage;
// import React, { useEffect, useState } from 'react';
// import { useNavigate, useParams } from 'react-router-dom';
// import { faPlus, faEdit, faBan, faCalendarAlt } from '@fortawesome/free-solid-svg-icons';
// import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
// import { Button } from '../../../components/ui/Button';
// import { Alert } from '../../../components/ui/Alert';
// import { FormModal } from '../../../components/ui/modal/FormModal';
// import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
// import { Pagination } from '../../../components/ui/pagination/Pagination';
// import { useResources } from '../hooks/useResources';
// import type { Resource, CreateResourceRequest, UpdateResourceRequest, ResourceType } from '../types';
// import type { ModelSchema } from '../../../components/ui/table/types';

// const RESOURCE_TYPE_LABELS: Record<ResourceType, string> = {
//   Person: 'Person',
//   Room: 'Room',
//   Equipment: 'Equipment',
//   SlotBased: 'Slot Based',
// };

// const resourceFormSchema: ModelSchema<CreateResourceRequest>[] = [
//   { key: 'name', label: 'Name', type: 'string', required: true },
//   {
//     key: 'resourceType',
//     label: 'Type',
//     type: 'select',
//     required: true,
//     options: [
//       { label: 'Person', value: 'Person' },
//       { label: 'Room', value: 'Room' },
//       { label: 'Equipment', value: 'Equipment' },
//       { label: 'Slot Based', value: 'SlotBased' },
//     ],
//   },
//   { key: 'capacity', label: 'Capacity', type: 'number', required: true },
//   { key: 'description', label: 'Description', type: 'textarea' },
// ];

// const EMPTY_FORM: CreateResourceRequest = {
//   name: '',
//   resourceType: 'Person',
//   capacity: 1,
//   description: '',
// };

// const PAGE_SIZE = 10;

// const ResourcesPage: React.FC = () => {
//   const { tenant } = useParams<{ tenant: string }>();
//   const navigate = useNavigate();
//   const { resources, isLoading, error, clearError, getAll, create, update, deactivate } = useResources();

//   const [currentPage, setCurrentPage] = useState(1);
//   const [showForm, setShowForm] = useState(false);
//   const [showConfirm, setShowConfirm] = useState(false);
//   const [editingResource, setEditingResource] = useState<Resource | null>(null);
//   const [targetResource, setTargetResource] = useState<Resource | null>(null);
//   const [formValue, setFormValue] = useState<CreateResourceRequest>(EMPTY_FORM);
//   const [successMessage, setSuccessMessage] = useState<string | null>(null);

//   useEffect(() => {
//     getAll();
//   }, [getAll]);

//   const totalPages = Math.max(1, Math.ceil(resources.length / PAGE_SIZE));
//   const paged = resources.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

//   const openCreate = () => {
//     setEditingResource(null);
//     setFormValue(EMPTY_FORM);
//     setShowForm(true);
//   };

//   const openEdit = (resource: Resource) => {
//     setEditingResource(resource);
//     setFormValue({
//       name: resource.name,
//       resourceType: resource.resourceType,
//       capacity: resource.capacity,
//       description: resource.description ?? '',
//     });
//     setShowForm(true);
//   };

//   const openDeactivate = (resource: Resource) => {
//     setTargetResource(resource);
//     setShowConfirm(true);
//   };

//   const handleSubmit = async (value: CreateResourceRequest): Promise<void> => {
//     if (editingResource) {
//       const req: UpdateResourceRequest = {
//         name: value.name,
//         resourceType: value.resourceType,
//         capacity: value.capacity,
//         description: value.description,
//       };
//       const ok = await update(editingResource.id, req);
//       if (ok) {
//         setShowForm(false);
//         setSuccessMessage('Resource updated.');
//       }
//     } else {
//       const ok = await create(value);
//       if (ok) {
//         setShowForm(false);
//         setSuccessMessage('Resource created.');
//       }
//     }
//   };

//   const handleDeactivate = async () => {
//     if (!targetResource) return;
//     const ok = await deactivate(targetResource.id);
//     if (ok) {
//       setShowConfirm(false);
//       setTargetResource(null);
//       setSuccessMessage('Resource deactivated.');
//     }
//   };

//   return (
//     <div className="container-fluid">
//       <div className="d-flex justify-content-between align-items-center mb-4">
//         <div>
//           <h4 className="fw-bold mb-1">Resources</h4>
//           <div className="text-muted small">Manage bookable people, rooms, and equipment.</div>
//         </div>
//         <Button variant="primary" icon={faPlus} onClick={openCreate}>
//           Add Resource
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
//             <div className="p-4 text-center text-muted small">Loading resources...</div>
//           ) : resources.length === 0 ? (
//             <div className="p-4 text-center text-muted small">
//               No resources found. Add your first resource to get started.
//             </div>
//           ) : (
//             <div className="table-responsive">
//               <table className="table table-hover align-middle mb-0">
//                 <thead className="table-light">
//                   <tr>
//                     <th className="ps-4 fw-semibold small text-muted">Name</th>
//                     <th className="fw-semibold small text-muted">Type</th>
//                     <th className="fw-semibold small text-muted">Capacity</th>
//                     <th className="fw-semibold small text-muted">Description</th>
//                     <th className="fw-semibold small text-muted">Status</th>
//                     <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
//                   </tr>
//                 </thead>
//                 <tbody>
//                   {paged.map((resource) => (
//                     <tr key={resource.id}>
//                       <td className="ps-4 fw-medium">{resource.name}</td>
//                       <td className="text-muted small">{RESOURCE_TYPE_LABELS[resource.resourceType]}</td>
//                       <td className="text-muted small">{resource.capacity}</td>
//                       <td className="text-muted small">{resource.description ?? '—'}</td>
//                       <td>
//                         <span className={`badge ${resource.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}`}>
//                           {resource.isActive ? 'Active' : 'Inactive'}
//                         </span>
//                       </td>
//                       <td className="text-end pe-4">
//                         <div className="d-flex justify-content-end gap-2">
//                           <button
//                             className="btn btn-sm btn-outline-secondary"
//                             title="Set Availability"
//                             onClick={() => navigate(`/t/${tenant}/resources/${resource.id}/availability`)}
//                           >
//                             <FontAwesomeIcon icon={faCalendarAlt} />
//                           </button>
//                           <button
//                             className="btn btn-sm btn-outline-primary"
//                             title="Edit"
//                             onClick={() => openEdit(resource)}
//                           >
//                             <FontAwesomeIcon icon={faEdit} />
//                           </button>
//                           {resource.isActive && (
//                             <button
//                               className="btn btn-sm btn-outline-danger"
//                               title="Deactivate"
//                               onClick={() => openDeactivate(resource)}
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

//         {resources.length > PAGE_SIZE && (
//           <div className="card-footer bg-white border-top-0">
//             <Pagination
//               currentPage={currentPage}
//               totalPages={totalPages}
//               pageSize={PAGE_SIZE}
//               totalItems={resources.length}
//               onPageChange={setCurrentPage}
//               onPageSizeChange={() => {}}
//             />
//           </div>
//         )}
//       </div>

//       <FormModal
//         isOpen={showForm}
//         title={editingResource ? 'Edit Resource' : 'Add Resource'}
//         fields={resourceFormSchema}
//         value={formValue}
//         onChange={setFormValue}
//         onSubmit={handleSubmit}
//         onClose={() => setShowForm(false)}
//       />

//       <ConfirmModal
//         isOpen={showConfirm}
//         title="Deactivate Resource"
//         message={`Deactivating "${targetResource?.name}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
//         onConfirm={handleDeactivate}
//         onCancel={() => { setShowConfirm(false); setTargetResource(null); }}
//       />
//     </div>
//   );
// };

// export default ResourcesPage;
// // import React, { useEffect, useState } from 'react';
// // import { useNavigate, useParams } from 'react-router-dom';
// // import { faPlus, faEdit, faBan, faCalendarAlt } from '@fortawesome/free-solid-svg-icons';
// // import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
// // import { Button } from '../../../components/ui/Button';
// // import { Alert } from '../../../components/ui/Alert';
// // import { FormModal } from '../../../components/ui/modal/FormModal';
// // import { ConfirmModal } from '../../../components/ui/modal/ConfirmModal';
// // import { Pagination } from '../../../components/ui/pagination/Pagination';
// // import { useResources } from '../hooks/useResources';
// // import type { Resource, CreateResourceRequest, UpdateResourceRequest, ResourceType } from '../types';
// // import type { ModelSchema } from '../../../components/ui/table/types';

// // const RESOURCE_TYPE_LABELS: Record<ResourceType, string> = {
// //   Person: 'Person',
// //   Room: 'Room',
// //   Equipment: 'Equipment',
// //   SlotBased: 'Slot Based',
// // };

// // const resourceFormSchema: ModelSchema<CreateResourceRequest>[] = [
// //   { key: 'name', label: 'Name', type: 'string', required: true },
// //   {
// //     key: 'resourceType',
// //     label: 'Type',
// //     type: 'select',
// //     required: true,
// //     options: [
// //       { label: 'Person', value: 'Person' },
// //       { label: 'Room', value: 'Room' },
// //       { label: 'Equipment', value: 'Equipment' },
// //       { label: 'Slot Based', value: 'SlotBased' },
// //     ],
// //   },
// //   { key: 'capacity', label: 'Capacity', type: 'number', required: true },
// //   { key: 'description', label: 'Description', type: 'textarea' },
// // ];

// // const EMPTY_FORM: CreateResourceRequest = {
// //   name: '',
// //   resourceType: 'Person',
// //   capacity: 1,
// //   description: '',
// // };

// // const PAGE_SIZE = 10;

// // const ResourcesPage: React.FC = () => {
// //   const { tenant } = useParams<{ tenant: string }>();
// //   const navigate = useNavigate();
// //   const { resources, isLoading, error, clearError, getAll, create, update, deactivate } = useResources();

// //   const [currentPage, setCurrentPage] = useState(1);
// //   const [showForm, setShowForm] = useState(false);
// //   const [showConfirm, setShowConfirm] = useState(false);
// //   const [editingResource, setEditingResource] = useState<Resource | null>(null);
// //   const [targetResource, setTargetResource] = useState<Resource | null>(null);
// //   const [formValue, setFormValue] = useState<CreateResourceRequest>(EMPTY_FORM);
// //   const [successMessage, setSuccessMessage] = useState<string | null>(null);

// //   useEffect(() => {
// //     getAll();
// //   }, [getAll]);

// //   const totalPages = Math.max(1, Math.ceil(resources.length / PAGE_SIZE));
// //   const paged = resources.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

// //   const openCreate = () => {
// //     setEditingResource(null);
// //     setFormValue(EMPTY_FORM);
// //     setShowForm(true);
// //   };

// //   const openEdit = (resource: Resource) => {
// //     setEditingResource(resource);
// //     setFormValue({
// //       name: resource.name,
// //       resourceType: resource.resourceType,
// //       capacity: resource.capacity,
// //       description: resource.description ?? '',
// //     });
// //     setShowForm(true);
// //   };

// //   const openDeactivate = (resource: Resource) => {
// //     setTargetResource(resource);
// //     setShowConfirm(true);
// //   };

// //   const handleSubmit = async (value: CreateResourceRequest) => {
// //     if (editingResource) {
// //       const req: UpdateResourceRequest = {
// //         name: value.name,
// //         resourceType: value.resourceType,
// //         capacity: value.capacity,
// //         description: value.description,
// //       };
// //       const ok = await update(editingResource.id, req);
// //       if (ok) {
// //         setShowForm(false);
// //         setSuccessMessage('Resource updated.');
// //       }
// //     } else {
// //       const ok = await create(value);
// //       if (ok) {
// //         await getAll();
// //         setShowForm(false);
// //         setSuccessMessage('Resource created.');
// //       }
// //     }
// //   };

// //   const handleDeactivate = async () => {
// //     if (!targetResource) return;
// //     const ok = await deactivate(targetResource.id);
// //     if (ok) {
// //       setShowConfirm(false);
// //       setTargetResource(null);
// //       setSuccessMessage('Resource deactivated.');
// //     }
// //   };

// //   return (
// //     <div className="container-fluid">
// //       <div className="d-flex justify-content-between align-items-center mb-4">
// //         <div>
// //           <h4 className="fw-bold mb-1">Resources</h4>
// //           <div className="text-muted small">Manage bookable people, rooms, and equipment.</div>
// //         </div>
// //         <Button variant="primary" icon={faPlus} onClick={openCreate}>
// //           Add Resource
// //         </Button>
// //       </div>

// //       {successMessage && (
// //         <Alert variant="success" dismissible onDismiss={() => setSuccessMessage(null)} className="mb-3">
// //           {successMessage}
// //         </Alert>
// //       )}

// //       {error && (
// //         <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
// //           {error}
// //         </Alert>
// //       )}

// //       <div className="card border-0 shadow-sm">
// //         <div className="card-body p-0">
// //           {isLoading ? (
// //             <div className="p-4 text-center text-muted small">Loading resources...</div>
// //           ) : resources.length === 0 ? (
// //             <div className="p-4 text-center text-muted small">
// //               No resources found. Add your first resource to get started.
// //             </div>
// //           ) : (
// //             <div className="table-responsive">
// //               <table className="table table-hover align-middle mb-0">
// //                 <thead className="table-light">
// //                   <tr>
// //                     <th className="ps-4 fw-semibold small text-muted">Name</th>
// //                     <th className="fw-semibold small text-muted">Type</th>
// //                     <th className="fw-semibold small text-muted">Capacity</th>
// //                     <th className="fw-semibold small text-muted">Description</th>
// //                     <th className="fw-semibold small text-muted">Status</th>
// //                     <th className="fw-semibold small text-muted text-end pe-4">Actions</th>
// //                   </tr>
// //                 </thead>
// //                 <tbody>
// //                   {paged.map((resource) => (
// //                     <tr key={resource.id}>
// //                       <td className="ps-4 fw-medium">{resource.name}</td>
// //                       <td className="text-muted small">{RESOURCE_TYPE_LABELS[resource.resourceType]}</td>
// //                       <td className="text-muted small">{resource.capacity}</td>
// //                       <td className="text-muted small">{resource.description ?? '—'}</td>
// //                       <td>
// //                         <span className={`badge ${resource.isActive ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}`}>
// //                           {resource.isActive ? 'Active' : 'Inactive'}
// //                         </span>
// //                       </td>
// //                       <td className="text-end pe-4">
// //                         <div className="d-flex justify-content-end gap-2">
// //                           <button
// //                             className="btn btn-sm btn-outline-secondary"
// //                             title="Set Availability"
// //                             onClick={() => navigate(`/t/${tenant}/resources/${resource.id}/availability`)}
// //                           >
// //                             <FontAwesomeIcon icon={faCalendarAlt} />
// //                           </button>
// //                           <button
// //                             className="btn btn-sm btn-outline-primary"
// //                             title="Edit"
// //                             onClick={() => openEdit(resource)}
// //                           >
// //                             <FontAwesomeIcon icon={faEdit} />
// //                           </button>
// //                           {resource.isActive && (
// //                             <button
// //                               className="btn btn-sm btn-outline-danger"
// //                               title="Deactivate"
// //                               onClick={() => openDeactivate(resource)}
// //                             >
// //                               <FontAwesomeIcon icon={faBan} />
// //                             </button>
// //                           )}
// //                         </div>
// //                       </td>
// //                     </tr>
// //                   ))}
// //                 </tbody>
// //               </table>
// //             </div>
// //           )}
// //         </div>

// //         {resources.length > PAGE_SIZE && (
// //           <div className="card-footer bg-white border-top-0">
// //             <Pagination
// //               currentPage={currentPage}
// //               totalPages={totalPages}
// //               pageSize={PAGE_SIZE}
// //               totalItems={resources.length}
// //               onPageChange={setCurrentPage}
// //               onPageSizeChange={() => {}}
// //             />
// //           </div>
// //         )}
// //       </div>

// //       <FormModal
// //         isOpen={showForm}
// //         title={editingResource ? 'Edit Resource' : 'Add Resource'}
// //         fields={resourceFormSchema}
// //         value={formValue}
// //         onChange={setFormValue}
// //         onSubmit={handleSubmit}
// //         onClose={() => setShowForm(false)}
// //       />

// //       <ConfirmModal
// //         isOpen={showConfirm}
// //         title="Deactivate Resource"
// //         message={`Deactivating "${targetResource?.name}" will remove it from the booking flow. Existing bookings are not cancelled automatically.`}
// //         onConfirm={handleDeactivate}
// //         onCancel={() => { setShowConfirm(false); setTargetResource(null); }}
// //       />
// //     </div>
// //   );
// // };

// // export default ResourcesPage;