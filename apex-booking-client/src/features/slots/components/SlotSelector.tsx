// import React, { useEffect, useState } from 'react';
// import { useSlots } from '../hooks/useSlots';

// type Props = {
//   serviceId: string;
//   resourceId: string;
//   date: string;
//   value: string;
//   onChange: (time: string) => void;
// };

// export const SlotSelector: React.FC<Props> = ({
//   serviceId,
//   resourceId,
//   date,
//   value,
//   onChange,
// }) => {
//   const { slots, getSlots, isLoading, error } = useSlots();
//   const [loaded, setLoaded] = useState(false);

//   useEffect(() => {
//     if (!serviceId || !resourceId || !date) return;

//     const load = async () => {
//       await getSlots(serviceId, resourceId, date);
//       setLoaded(true);
//     };

//     load();
//   }, [serviceId, resourceId, date, getSlots]);

//   if (isLoading) return <div className="text-muted small">Loading slots...</div>;

//   if (error) return <div className="text-danger small">{error}</div>;

//   if (!loaded) return null;

//   if (!slots || slots.availableSlots.length === 0) {
//     return <div className="text-muted small">No slots available.</div>;
//   }

//   return (
//     <div className="d-flex flex-wrap gap-2">
//       {slots.availableSlots.map((t) => (
//         <button
//           key={t}
//           type="button"
//           onClick={() => onChange(t)}
//           className={`btn btn-sm ${
//             value === t ? 'btn-primary' : 'btn-outline-primary'
//           }`}
//         >
//           {t}
//         </button>
//       ))}
//     </div>
//   );
// };