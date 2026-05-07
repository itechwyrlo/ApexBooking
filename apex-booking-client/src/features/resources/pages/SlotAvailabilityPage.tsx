// import React, { useState } from 'react';
// import { Alert } from '../../../components/ui/Alert';
// import { Button } from '../../../components/ui/Button';
// import { useSlots } from '../hooks/useSlots';

// interface SlotAvailabilityPageProps {
//   tenantId: string;
//   services: { serviceId: string; name: string }[];
//   resources: { resourceId: string; name: string }[];
// }

// const SlotAvailabilityPage: React.FC<SlotAvailabilityPageProps> = ({
//   tenantId,
//   services,
//   resources,
// }) => {
//   const { slots, isLoading, error, clearError, fetchSlots } = useSlots();

//   const [serviceId, setServiceId] = useState('');
//   const [resourceId, setResourceId] = useState('');
//   const [date, setDate] = useState('');
//   const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
//   const [validationError, setValidationError] = useState<string | null>(null);

//   const today = new Date().toISOString().split('T')[0];

//   const handleSearch = async () => {
//     setValidationError(null);
//     setSelectedSlot(null);

//     if (!serviceId) { setValidationError('Please select a service.'); return; }
//     if (!resourceId) { setValidationError('Please select a resource.'); return; }
//     if (!date) { setValidationError('Please select a date.'); return; }

//     await fetchSlots(serviceId, resourceId, date, tenantId);
//   };

//   return (
//     <div className="container" style={{ maxWidth: 640 }}>
//       <div className="mb-4">
//         <h4 className="fw-bold mb-1">Check Availability</h4>
//         <div className="text-muted small">Select a service, resource, and date to see open slots.</div>
//       </div>

//       {validationError && (
//         <Alert variant="warning" dismissible onDismiss={() => setValidationError(null)} className="mb-3">
//           {validationError}
//         </Alert>
//       )}

//       {error && (
//         <Alert variant="error" dismissible onDismiss={clearError} className="mb-3">
//           {error}
//         </Alert>
//       )}

//       <div className="card border-0 shadow-sm mb-4">
//         <div className="card-body">
//           <div className="row g-3">
//             <div className="col-md-6">
//               <label className="form-label small fw-medium mb-1">
//                 Service <span className="text-danger">*</span>
//               </label>
//               <select
//                 className="form-select"
//                 value={serviceId}
//                 onChange={e => { setServiceId(e.target.value); setSelectedSlot(null); }}
//               >
//                 <option value="">Select a service</option>
//                 {services.map(s => (
//                   <option key={s.serviceId} value={s.serviceId}>{s.name}</option>
//                 ))}
//               </select>
//             </div>

//             <div className="col-md-6">
//               <label className="form-label small fw-medium mb-1">
//                 Resource <span className="text-danger">*</span>
//               </label>
//               <select
//                 className="form-select"
//                 value={resourceId}
//                 onChange={e => { setResourceId(e.target.value); setSelectedSlot(null); }}
//               >
//                 <option value="">Select a resource</option>
//                 {resources.map(r => (
//                   <option key={r.resourceId} value={r.resourceId}>{r.name}</option>
//                 ))}
//               </select>
//             </div>

//             <div className="col-md-6">
//               <label className="form-label small fw-medium mb-1">
//                 Date <span className="text-danger">*</span>
//               </label>
//               <input
//                 type="date"
//                 className="form-control"
//                 min={today}
//                 value={date}
//                 onChange={e => { setDate(e.target.value); setSelectedSlot(null); }}
//               />
//             </div>

//             <div className="col-md-6 d-flex align-items-end">
//               <Button
//                 variant="primary"
//                 onClick={handleSearch}
//                 loading={isLoading}
//                 className="w-100"
//               >
//                 Check Availability
//               </Button>
//             </div>
//           </div>
//         </div>
//       </div>

//       {slots && (
//         <div className="card border-0 shadow-sm">
//           <div className="card-header bg-white border-bottom py-3">
//             <div className="fw-semibold">Available Slots</div>
//             <div className="text-muted small mt-1">
//               {slots.availableSlots.length} slot{slots.availableSlots.length !== 1 ? 's' : ''} available
//               on {slots.date} · {slots.durationMinutes} min each
//             </div>
//           </div>
//           <div className="card-body">
//             {slots.availableSlots.length === 0 ? (
//               <div className="text-center text-muted small py-3">
//                 No available slots on this date. Try a different date or resource.
//               </div>
//             ) : (
//               <div className="d-flex flex-wrap gap-2">
//                 {slots.availableSlots.map(slot => (
//                   <button
//                     key={slot}
//                     onClick={() => setSelectedSlot(slot)}
//                     className={`btn btn-sm ${
//                       selectedSlot === slot
//                         ? 'btn-primary'
//                         : 'btn-outline-primary'
//                     }`}
//                     style={{ minWidth: 80 }}
//                   >
//                     {slot}
//                   </button>
//                 ))}
//               </div>
//             )}

//             {selectedSlot && (
//               <div className="mt-4 p-3 bg-primary bg-opacity-10 rounded border border-primary border-opacity-25">
//                 <div className="small fw-semibold text-primary mb-1">Selected Slot</div>
//                 <div className="fw-bold">{selectedSlot}</div>
//                 <div className="text-muted small">{slots.date} · {slots.durationMinutes} min</div>
//               </div>
//             )}
//           </div>
//         </div>
//       )}
//     </div>
//   );
// };

// export default SlotAvailabilityPage;