import { Link, useLocation } from "react-router-dom";

export const CustomerRegisterLink: React.FC<{ searchParams: URLSearchParams }> = ({ searchParams }) => {
    const location = useLocation();
    const customerLoginMatch = location.pathname.match(/^\/book\/([^/]+)\/customer\/login/);
  
    if (customerLoginMatch) {
      const tenantSlug = customerLoginMatch[1];
      const returnTo = searchParams.get('returnTo');
      const registerPath = returnTo
        ? `/book/${tenantSlug}/customer/register?returnTo=${encodeURIComponent(returnTo)}`
        : `/book/${tenantSlug}/customer/register`;
  
      return (
        <p className="text-muted small">
          Or{' '}
          <Link to={registerPath} className="text-decoration-none">
            create a new account
          </Link>
        </p>
      );
    }
  
    return (
      <p className="text-muted small">
        Or{' '}
        <Link to="/register" className="text-decoration-none">
          create a new account
        </Link>
      </p>
    );
  };