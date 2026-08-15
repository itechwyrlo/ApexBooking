import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { NAV_ITEMS } from '../../config/navigation'
import { Button } from '../common/Button'
import { scrollToPricing } from '../../utils/scrollToPricing'

export function Header() {
  const [isScrolled, setIsScrolled] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const sentinelRef = useRef<HTMLDivElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const node = sentinelRef.current
    if (!node) {
      return
    }

    const observer = new IntersectionObserver(([entry]) => {
      setIsScrolled(!entry.isIntersecting)
    })

    observer.observe(node)

    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    if (!isMenuOpen) {
      return
    }

    const previouslyFocused = document.activeElement as HTMLElement | null
    menuRef.current?.querySelector<HTMLElement>('button, a')?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setIsMenuOpen(false)
      }
    }

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.body.style.overflow = previousOverflow
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocused?.focus()
    }
  }, [isMenuOpen])

  function closeMenu() {
    setIsMenuOpen(false)
  }

  return (
    <>
      <div ref={sentinelRef} className="scroll-sentinel" aria-hidden="true" />

      <header className={`site-header ${isScrolled ? 'site-header--scrolled' : ''}`}>
        <div className="container d-flex align-items-center justify-content-between py-3">
          <a className="navbar-brand d-flex align-items-center gap-2" href="#home">
            <img src="/favicon.svg" alt="ApexBooking logo" width={32} height={32} />
            <span className="fw-bold fs-5 font-display">ApexBooking</span>
          </a>

          <nav className="d-none d-md-flex align-items-center gap-4">
            {NAV_ITEMS.map((item) => (
              <a key={item.href} className="nav-link fw-medium" href={item.href}>
                {item.label}
              </a>
            ))}
          </nav>

          <div className="d-none d-md-flex align-items-center gap-2">
            <Button to="/find-workspace" variant="outline-primary">
              Log In
            </Button>
            <Button to="/#pricing" onClick={scrollToPricing}>
              Request Access
            </Button>
          </div>

          <button
            type="button"
            className="mobile-nav-toggle d-md-none"
            aria-expanded={isMenuOpen}
            aria-controls="mobile-nav-menu"
            aria-label={isMenuOpen ? 'Close menu' : 'Open menu'}
            onClick={() => setIsMenuOpen((open) => !open)}
          >
            <span className={`mobile-nav-toggle__bar ${isMenuOpen ? 'mobile-nav-toggle__bar--open' : ''}`} />
          </button>
        </div>
      </header>

      <div
        id="mobile-nav-menu"
        ref={menuRef}
        className={`mobile-nav-menu ${isMenuOpen ? 'mobile-nav-menu--open' : ''}`}
        role="dialog"
        aria-modal="true"
        aria-label="Site menu"
      >
        <div className="mobile-nav-menu__bar">
          <span className="fw-bold fs-5 font-display text-white">ApexBooking</span>
          <button type="button" className="mobile-nav-menu__close" aria-label="Close menu" onClick={closeMenu}>
            <span aria-hidden="true">&times;</span>
          </button>
        </div>

        <ul className="list-unstyled d-flex flex-column gap-4 mb-0 mobile-nav-menu__links">
          {NAV_ITEMS.map((item) => (
            <li key={item.href}>
              <a href={item.href} className="mobile-nav-menu__link font-display" onClick={closeMenu}>
                {item.label}
              </a>
            </li>
          ))}
          <li>
            <Link
              to="/find-workspace"
              className="mobile-nav-menu__link mobile-nav-menu__link--muted font-display"
              onClick={closeMenu}
            >
              Log In
            </Link>
          </li>
        </ul>

        <Button
          to="/#pricing"
          size="lg"
          className="mobile-nav-menu__cta w-100"
          onClick={(event) => {
            closeMenu()
            scrollToPricing(event)
          }}
        >
          Get Started
        </Button>
      </div>
    </>
  )
}
