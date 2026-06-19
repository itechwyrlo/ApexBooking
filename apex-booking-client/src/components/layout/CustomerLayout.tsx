// import React from "react";
// import { Outlet, Link, useParams } from "react-router-dom";
// import { useLogout } from "../../features/auth/hooks/useLogout";

// export const CustomerLayout: React.FC = () => {
//   const { tenant } = useParams<{ tenant: string }>();
//   const { logout, isLoading } = useLogout();

//   return (
//     <div className="min-vh-100 bg-light">
//       <nav className="navbar navbar-expand-lg navbar-light bg-white border-bottom shadow-sm">
//         <div className="container">
//           <Link className="navbar-brand fw-bold" to={`/book/${tenant}`}>
//             ApexBooking
//           </Link>
//           <div className="collapse navbar-collapse">
//             <ul className="navbar-nav ms-auto align-items-center">
//               <li className="nav-item">
//                 <Link className="nav-link" to={`/book/${tenant}/customer/bookings`}>
//                   My Bookings
//                 </Link>
//               </li>
//               <li className="nav-item">
//                 <Link className="nav-link" to={`/book/${tenant}/customer/profile`}>
//                   Profile
//                 </Link>
//               </li>
//               <li className="nav-item">
//                 <Link className="nav-link" to={`/book/${tenant}`}>
//                   Back to Booking
//                 </Link>
//               </li>
//               <li className="nav-item ms-2">
//                 <button
//                   className="btn btn-outline-danger btn-sm"
//                   onClick={logout}
//                   disabled={isLoading}
//                 >
//                   {isLoading ? "Logging out..." : "Logout"}
//                 </button>
//               </li>
//             </ul>
//           </div>
//         </div>
//       </nav>
//       <main className="container py-4">
//         <Outlet />
//       </main>
//     </div>
//   );
// };
