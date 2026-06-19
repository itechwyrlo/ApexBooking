import { Link, useLocation } from "react-router-dom";

export const CustomerRegisterLink: React.FC<{ searchParams: URLSearchParams }> = ({ searchParams }) => {
    const location = useLocation();
    const customerLoginMatch = location.pathname.match(/^\/book\/([^/]+)\/customer\/login/);

    if (!customerLoginMatch) return null;

    const tenantSlug = customerLoginMatch[1];
    const returnTo = searchParams.get('returnTo');
    const registerPath = returnTo
      ? `/book/${tenantSlug}/customer/register?returnTo=${encodeURIComponent(returnTo)}`
      : `/book/${tenantSlug}/customer/register`;

    return (
      <p className="text-muted small text-center mt-3 mb-0">
        Don't have an account?{' '}
        <Link to={registerPath} className="text-decoration-none">
          Create one
        </Link>
      </p>
    );
  };