import React, { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import PublicNav from "../components/PublicNav";
import apexbookingLogo from "../../../assets/apexbooking-logo.svg";

const LandingPage: React.FC = () => {
  const navigate = useNavigate();
  const heroRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("visible");
          }
        });
      },
      { threshold: 0.1 }
    );

    document.querySelectorAll(".reveal").forEach((el) => observer.observe(el));
    return () => observer.disconnect();
  }, []);

  const features = [
    {
      icon: "fas fa-users",
      title: "Staff Orchestration",
      desc: "Assign services to team members, manage availability, and balance workloads with precision.",
    },
    {
      icon: "fas fa-chart-line",
      title: "Revenue Analytics",
      desc: "Real-time dashboards with revenue tracking, service breakdowns, and growth insights.",
    },
    {
      icon: "fas fa-bell",
      title: "Smart Notifications",
      desc: "Automated confirmations that keep clients engaged, informed, and coming back.",
    },
    {
      icon: "fas fa-ban",
      title: "No Booking Overlaps",
      desc: "Intelligent conflict detection prevents double-bookings at the database level.",
    },
    {
      icon: "fas fa-calendar-alt",
      title: "Calendar View",
      desc: "Visualize your entire schedule in daily, weekly, and monthly views without losing context.",
    },
    {
      icon: "fas fa-lock",
      title: "Enterprise Security",
      desc: "Role-based access control so every team member sees only what they should.",
    },
  ];

  const steps = [
    {
      number: "01",
      title: "Set Up Your Profile",
      desc: "Register your business, configure services, and define your team's availability in minutes.",
    },
    {
      number: "02",
      title: "Share Your Link",
      desc: "Every business gets a clean public booking URL. Share it anywhere and start accepting appointments.",
    },
    {
      number: "03",
      title: "Grow Your Business",
      desc: "Track revenue, manage your schedule, and let clients book effortlessly around the clock.",
    },
  ];

  const industries = [
    "🏥 Clinics",
    "✂️ Salons",
    "💼 Consultants",
    "🏋️ Fitness Studios",
    "💆 Spas & Wellness",
    "🎓 Tutors",
    "📸 Photographers",
    "🦷 Dental Offices",
  ];

  return (
    <div className="lp-page">
      <PublicNav showGetStarted />

      {/* Hero */}
      <section className="hero-section" ref={heroRef}>
        <div className="container py-5">
          <div className="row align-items-center g-5">
            <div className="col-lg-6">
              <div className="hero-badge">
                <i className="fas fa-circle" />
                Multi-Vendor Booking Platform
              </div>
              <h1 className="hero-headline">
                Scheduling that<br />feels <em>effortless</em>
              </h1>
              <p className="hero-subheadline">
                The modern appointment platform for service businesses. Streamline bookings, delight clients, and grow your revenue from one beautifully simple dashboard.
              </p>
              <div className="d-flex flex-wrap gap-3">
                <button className="btn-apex-primary" onClick={() => navigate("/pricing")}>
                  See Plans <i className="fas fa-arrow-right hero-cta-icon" />
                </button>
                <button className="btn-apex-ghost" onClick={() => navigate("/login")}>
                  Sign In
                </button>
              </div>
            </div>

            <div className="col-lg-6 d-none d-lg-block">
              <div className="hero-visual">
                <div className="mock-header">
                  <div className="mock-dot mock-dot-red" />
                  <div className="mock-dot mock-dot-yellow" />
                  <div className="mock-dot mock-dot-green" />
                  <span className="ms-3 text-muted small">apexbooking.app/book/lumiere-salon</span>
                </div>
                <div className="p-3">
                  <div className="mock-booking-card">
                    <div className="d-flex align-items-center gap-3 mb-3">
                      <div className="mock-service-icon">
                        <i className="fas fa-cut" />
                      </div>
                      <div>
                        <div className="mock-service-name">Hair Cut &amp; Style</div>
                        <div className="mock-service-meta">60 min · $85.00</div>
                      </div>
                    </div>
                    <div className="mock-slots-label">Available slots — Today</div>
                    <div>
                      <span className="mock-slot">9:00 AM</span>
                      <span className="mock-slot selected">10:30 AM</span>
                      <span className="mock-slot">2:00 PM</span>
                      <span className="mock-slot">3:30 PM</span>
                    </div>
                  </div>
                  <div className="mock-booking-card mock-card-dimmed">
                    <div className="d-flex align-items-center gap-3">
                      <div className="mock-service-icon-alt">
                        <i className="fas fa-spa" />
                      </div>
                      <div>
                        <div className="mock-service-name">Deep Tissue Massage</div>
                        <div className="mock-service-meta">90 min · $120.00</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div className="mock-confirm-bar">
                  <span className="text-white fw-semibold small">10:30 AM confirmed</span>
                  <div className="mock-confirm-badge">
                    <i className="fas fa-check me-2" />Booked
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Industries ticker */}
      <div className="industry-strip">
        <div className="industry-ticker">
          {[...industries, ...industries].map((ind, i) => (
            <span key={i} className="industry-item">{ind}</span>
          ))}
        </div>
      </div>

      {/* Features */}
      <section className="lp-section-lg bg-white">
        <div className="container">
          <div className="text-center reveal">
            <div className="section-label">Features</div>
            <h2 className="section-title">Everything you need to<br /><em>run and grow</em></h2>
            <p className="section-subtitle">
              ApexBooking is equipped with everything to run your booking business. No integrations, no complexity, just results.
            </p>
          </div>
          <div className="row g-4">
            {features.map((f, i) => (
              <div key={i} className={`col-md-6 col-lg-4 reveal reveal-delay-${(i % 3) + 1}`}>
                <div className="feature-card">
                  <div className="feature-icon">
                    <i className={f.icon} />
                  </div>
                  <div className="feature-title">{f.title}</div>
                  <div className="feature-desc">{f.desc}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* How it works */}
      <section className="steps-section lp-section-lg">
        <div className="container">
          <div className="text-center reveal mb-5">
            <div className="section-label">How It Works</div>
            <h2 className="section-title">Live in 3 simple steps</h2>
          </div>
          <div className="row g-5 justify-content-center">
            {steps.map((step, i) => (
              <div key={i} className={`col-md-4 reveal reveal-delay-${i + 1}`}>
                <div className="text-center">
                  <div className="step-number">{step.number}</div>
                  <div className="step-title">{step.title}</div>
                  <div className="step-desc">{step.desc}</div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing preview */}
      <section className="lp-section-lg bg-white">
        <div className="container">
          <div className="text-center reveal mb-5">
            <div className="section-label">Pricing</div>
            <h2 className="section-title">Simple, transparent pricing</h2>
            <p className="section-subtitle">Start free. No credit card required.</p>
          </div>
          <div className="row g-4 justify-content-center lp-pricing-row">
            {[
              {
                name: "Basic",
                price: "$19",
                desc: "Everything you need to get started.",
                features: ["3 staff members", "5 services", "100 bookings / month", "Public booking page", "Online payments"],
                highlighted: false,
              },
              {
                name: "Professional",
                price: "$49",
                desc: "Scale without limits.",
                features: ["10 staff members", "20 services", "Unlimited bookings", "Public booking page", "Online payments", "Priority support"],
                highlighted: true,
              },
            ].map(plan => (
              <div key={plan.name} className="col-md-6 reveal">
                <div className={`lp-pricing-card${plan.highlighted ? " featured" : ""}`}>
                  {plan.highlighted && (
                    <div className="lp-plan-badge">Most Popular</div>
                  )}
                  <div className="fw-bold mb-1">{plan.name}</div>
                  <div className="mb-1">
                    <span className="lp-plan-price">{plan.price}</span>
                    <span className="text-muted ms-1">/mo</span>
                  </div>
                  <p className="small text-muted mb-3">{plan.desc}</p>
                  <div className="lp-plan-features">
                    {plan.features.map(f => (
                      <div key={f} className="lp-plan-feature">
                        <i className="fas fa-check-circle" />
                        {f}
                      </div>
                    ))}
                  </div>
                  <button
                    className={`lp-plan-btn ${plan.highlighted ? "lp-plan-btn-primary" : "lp-plan-btn-outline"}`}
                    onClick={() => navigate(`/request-access?plan=${plan.name}`)}
                  >
                    Get Started
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="cta-section lp-section-lg">
        <div className="container text-center lp-cta-content">
          <div className="reveal">
            <div className="cta-badge">
              <i className="fas fa-rocket" />
              For Business Owners
            </div>
            <h2 className="cta-headline">
              Turn walk-ins into<br />loyal clients
            </h2>
            <p className="cta-sub">
              Set up your branded booking page in minutes. Let clients book 24/7 while you focus on delivering great service.
            </p>
            <div className="d-flex gap-3 justify-content-center flex-wrap">
              <button className="btn-cta-primary" onClick={() => navigate("/pricing")}>
                View Pricing <i className="fas fa-arrow-right hero-cta-icon" />
              </button>
              <button className="btn-cta-ghost" onClick={() => navigate("/login")}>
                Sign In
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="apex-footer">
        <div className="container">
          <div className="row g-4 mb-5">
            <div className="col-lg-4">
              <div className="d-flex align-items-center gap-2 mb-3">
                <img src={apexbookingLogo} alt="ApexBooking" style={{ width: 36, height: 36, borderRadius: 10 }} />
                <span className="footer-logo-text">ApexBooking</span>
              </div>
              <p className="footer-brand-desc">
                The modern appointment platform for service businesses of every size.
              </p>
            </div>
            <div className="col-lg-2 col-6">
              <div className="footer-heading">Product</div>
              <a href="/pricing" className="footer-link">Pricing</a>
              <a href="/login" className="footer-link">Sign In</a>
            </div>
            <div className="col-lg-2 col-6">
              <div className="footer-heading">Company</div>
              <a href="/" className="footer-link">About</a>
              <a href="/" className="footer-link">Blog</a>
              <a href="/" className="footer-link">Careers</a>
            </div>
            <div className="col-lg-2 col-6">
              <div className="footer-heading">Legal</div>
              <a href="/" className="footer-link">Privacy</a>
              <a href="/" className="footer-link">Terms</a>
            </div>
          </div>
          <div className="footer-bottom">
            <span className="footer-copy">
              © {new Date().getFullYear()} ApexBooking. All rights reserved.
            </span>
            <div className="d-flex gap-3">
              <a href="/" className="footer-social"><i className="fab fa-twitter" /></a>
              <a href="/" className="footer-social"><i className="fab fa-linkedin" /></a>
              <a href="/" className="footer-social"><i className="fab fa-github" /></a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
