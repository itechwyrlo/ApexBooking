import { NAV_ITEMS } from '../../config/navigation'

const LEGAL_LINKS = ['Privacy Policy', 'Terms of Service']

export function Footer() {
  const year = new Date().getFullYear()

  return (
    <footer id="contact" className="py-5 bg-light border-top">
      <div className="container">
        <div className="row gy-4">
          <div className="col-md-4">
            <div className="d-flex align-items-center gap-2 mb-2">
              <img src="/favicon.svg" alt="ApexBooking logo" width={28} height={28} />
              <span className="fw-bold fs-5 font-display">ApexBooking</span>
            </div>
            <p className="text-secondary mb-2">Online booking for local businesses.</p>
            <p className="text-secondary mb-0">&copy; {year} ApexBooking. All rights reserved.</p>
          </div>
          <div className="col-md-4">
            <h3 className="h6 text-uppercase text-secondary mb-3">Navigation</h3>
            <ul className="list-unstyled d-flex flex-column gap-2">
              {NAV_ITEMS.map((item) => (
                <li key={item.href}>
                  <a href={item.href} className="link-dark text-decoration-none">
                    {item.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>
          <div className="col-md-4">
            <h3 className="h6 text-uppercase text-secondary mb-3">Legal</h3>
            <ul className="list-unstyled d-flex flex-column gap-2 text-secondary">
              {LEGAL_LINKS.map((label) => (
                <li key={label}>{label}</li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </footer>
  )
}
