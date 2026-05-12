import React, { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";

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
    <div style={{ fontFamily: "'DM Sans', sans-serif", overflowX: "hidden" }}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,300;0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;0,9..40,800;1,9..40,300&family=DM+Serif+Display:ital@0;1&display=swap');

        :root {
          --apex-blue: #0d6efd;
          --apex-blue-dark: #0a58ca;
          --apex-blue-soft: #e8f0fe;
          --apex-text: #0f172a;
          --apex-muted: #64748b;
          --apex-border: #e2e8f0;
          --apex-surface: #f8fafc;
        }

        .apex-nav {
          position: fixed;
          top: 0;
          left: 0;
          right: 0;
          z-index: 1000;
          background: rgba(255,255,255,0.92);
          backdrop-filter: blur(12px);
          border-bottom: 1px solid var(--apex-border);
          padding: 0 2rem;
          height: 64px;
          display: flex;
          align-items: center;
          justify-content: space-between;
        }

        .apex-logo {
          display: flex;
          align-items: center;
          gap: 10px;
          text-decoration: none;
        }

        .apex-logo-icon {
          width: 36px;
          height: 36px;
          background: var(--apex-blue);
          border-radius: 10px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
          color: white;
        }

        .apex-logo-text {
          font-family: 'DM Serif Display', serif;
          font-size: 1.25rem;
          color: var(--apex-text);
          font-weight: 400;
          letter-spacing: -0.02em;
        }

        .hero-section {
          min-height: 100vh;
          background: linear-gradient(160deg, #f0f7ff 0%, #ffffff 50%, #f8faff 100%);
          display: flex;
          align-items: center;
          padding-top: 64px;
          position: relative;
          overflow: hidden;
        }

        .hero-section::before {
          content: '';
          position: absolute;
          top: -200px;
          right: -200px;
          width: 600px;
          height: 600px;
          background: radial-gradient(circle, rgba(13,110,253,0.08) 0%, transparent 70%);
          border-radius: 50%;
        }

        .hero-section::after {
          content: '';
          position: absolute;
          bottom: -100px;
          left: -100px;
          width: 400px;
          height: 400px;
          background: radial-gradient(circle, rgba(13,110,253,0.05) 0%, transparent 70%);
          border-radius: 50%;
        }

        .hero-badge {
          display: inline-flex;
          align-items: center;
          gap: 8px;
          background: var(--apex-blue-soft);
          color: var(--apex-blue);
          padding: 6px 14px;
          border-radius: 100px;
          font-size: 0.8rem;
          font-weight: 600;
          letter-spacing: 0.04em;
          text-transform: uppercase;
          margin-bottom: 1.5rem;
        }

        .hero-headline {
          font-family: 'DM Serif Display', serif;
          font-size: clamp(2.8rem, 6vw, 4.5rem);
          line-height: 1.1;
          color: var(--apex-text);
          letter-spacing: -0.03em;
          margin-bottom: 1.25rem;
        }

        .hero-headline em {
          font-style: italic;
          color: var(--apex-blue);
        }

        .hero-subheadline {
          font-size: 1.125rem;
          color: var(--apex-muted);
          line-height: 1.7;
          max-width: 520px;
          margin-bottom: 2.5rem;
          font-weight: 400;
        }

        .btn-apex-primary {
          background: var(--apex-blue);
          color: white;
          border: none;
          padding: 14px 28px;
          border-radius: 10px;
          font-size: 0.95rem;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.2s;
          text-decoration: none;
          display: inline-flex;
          align-items: center;
          gap: 8px;
        }

        .btn-apex-primary:hover {
          background: var(--apex-blue-dark);
          transform: translateY(-1px);
          box-shadow: 0 8px 24px rgba(13,110,253,0.3);
          color: white;
        }

        .btn-apex-ghost {
          background: transparent;
          color: var(--apex-text);
          border: 1.5px solid var(--apex-border);
          padding: 14px 28px;
          border-radius: 10px;
          font-size: 0.95rem;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.2s;
          text-decoration: none;
          display: inline-flex;
          align-items: center;
          gap: 8px;
        }

        .btn-apex-ghost:hover {
          border-color: var(--apex-blue);
          color: var(--apex-blue);
          background: var(--apex-blue-soft);
        }

        .stats-bar {
          background: white;
          border: 1px solid var(--apex-border);
          border-radius: 16px;
          padding: 2rem 2.5rem;
          display: flex;
          gap: 3rem;
          box-shadow: 0 4px 24px rgba(0,0,0,0.06);
          max-width: 560px;
          margin-top: 3rem;
        }

        .stat-item .stat-number {
          font-family: 'DM Serif Display', serif;
          font-size: 2rem;
          color: var(--apex-text);
          line-height: 1;
          letter-spacing: -0.03em;
        }

        .stat-item .stat-label {
          font-size: 0.8rem;
          color: var(--apex-muted);
          font-weight: 500;
          margin-top: 4px;
          text-transform: uppercase;
          letter-spacing: 0.04em;
        }

        .hero-visual {
          background: white;
          border: 1px solid var(--apex-border);
          border-radius: 20px;
          box-shadow: 0 24px 64px rgba(0,0,0,0.1);
          overflow: hidden;
          position: relative;
        }

        .mock-header {
          background: var(--apex-surface);
          border-bottom: 1px solid var(--apex-border);
          padding: 12px 20px;
          display: flex;
          align-items: center;
          gap: 8px;
        }

        .mock-dot {
          width: 10px;
          height: 10px;
          border-radius: 50%;
        }

        .mock-booking-card {
          background: white;
          border: 1px solid var(--apex-border);
          border-radius: 12px;
          padding: 16px;
          margin: 12px;
        }

        .mock-slot {
          display: inline-block;
          padding: 6px 14px;
          border: 1.5px solid var(--apex-blue);
          border-radius: 8px;
          color: var(--apex-blue);
          font-size: 0.8rem;
          font-weight: 600;
          margin: 4px;
        }

        .mock-slot.selected {
          background: var(--apex-blue);
          color: white;
        }

        .section-label {
          font-size: 0.75rem;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--apex-blue);
          margin-bottom: 0.75rem;
        }

        .section-title {
          font-family: 'DM Serif Display', serif;
          font-size: clamp(2rem, 4vw, 3rem);
          color: var(--apex-text);
          letter-spacing: -0.03em;
          line-height: 1.15;
          margin-bottom: 1rem;
        }

        .section-subtitle {
          font-size: 1.05rem;
          color: var(--apex-muted);
          line-height: 1.7;
          max-width: 520px;
          margin: 0 auto 3rem;
        }

        .feature-card {
          background: white;
          border: 1px solid var(--apex-border);
          border-radius: 16px;
          padding: 1.75rem;
          transition: all 0.25s;
          height: 100%;
        }

        .feature-card:hover {
          border-color: var(--apex-blue);
          box-shadow: 0 8px 32px rgba(13,110,253,0.1);
          transform: translateY(-3px);
        }

        .feature-icon {
          width: 48px;
          height: 48px;
          background: var(--apex-blue-soft);
          border-radius: 12px;
          display: flex;
          align-items: center;
          justify-content: center;
          color: var(--apex-blue);
          font-size: 1.2rem;
          margin-bottom: 1rem;
        }

        .feature-title {
          font-size: 1rem;
          font-weight: 700;
          color: var(--apex-text);
          margin-bottom: 0.5rem;
          letter-spacing: -0.01em;
        }

        .feature-desc {
          font-size: 0.875rem;
          color: var(--apex-muted);
          line-height: 1.65;
        }

        .steps-section {
          background: var(--apex-surface);
          border-top: 1px solid var(--apex-border);
          border-bottom: 1px solid var(--apex-border);
        }

        .step-number {
          font-family: 'DM Serif Display', serif;
          font-size: 3.5rem;
          color: var(--apex-blue);
          opacity: 0.15;
          line-height: 1;
          letter-spacing: -0.04em;
        }

        .step-title {
          font-size: 1.1rem;
          font-weight: 700;
          color: var(--apex-text);
          margin-bottom: 0.5rem;
          letter-spacing: -0.01em;
        }

        .step-desc {
          font-size: 0.875rem;
          color: var(--apex-muted);
          line-height: 1.65;
        }

        .step-connector {
          position: absolute;
          top: 28px;
          right: -50%;
          width: 100%;
          height: 1px;
          background: linear-gradient(to right, var(--apex-blue), transparent);
          opacity: 0.2;
        }

        .industry-strip {
          background: white;
          padding: 1.25rem 0;
          border-top: 1px solid var(--apex-border);
          border-bottom: 1px solid var(--apex-border);
          overflow: hidden;
        }

        .industry-ticker {
          display: flex;
          gap: 2.5rem;
          animation: ticker 20s linear infinite;
          width: max-content;
        }

        @keyframes ticker {
          0% { transform: translateX(0); }
          100% { transform: translateX(-50%); }
        }

        .industry-item {
          font-size: 0.9rem;
          font-weight: 600;
          color: var(--apex-muted);
          white-space: nowrap;
          letter-spacing: 0.01em;
        }

        .cta-section {
          background: linear-gradient(135deg, var(--apex-blue) 0%, #0a4fcf 100%);
          position: relative;
          overflow: hidden;
        }

        .cta-section::before {
          content: '';
          position: absolute;
          top: -100px;
          right: -100px;
          width: 400px;
          height: 400px;
          background: rgba(255,255,255,0.05);
          border-radius: 50%;
        }

        .cta-section::after {
          content: '';
          position: absolute;
          bottom: -150px;
          left: -50px;
          width: 300px;
          height: 300px;
          background: rgba(255,255,255,0.03);
          border-radius: 50%;
        }

        .apex-footer {
          background: var(--apex-text);
          color: rgba(255,255,255,0.6);
        }

        .footer-logo-text {
          font-family: 'DM Serif Display', serif;
          font-size: 1.25rem;
          color: white;
        }

        .footer-link {
          color: rgba(255,255,255,0.5);
          text-decoration: none;
          font-size: 0.875rem;
          transition: color 0.2s;
          display: block;
          margin-bottom: 0.5rem;
        }

        .footer-link:hover {
          color: white;
        }

        .reveal {
          opacity: 0;
          transform: translateY(24px);
          transition: opacity 0.6s ease, transform 0.6s ease;
        }

        .reveal.visible {
          opacity: 1;
          transform: translateY(0);
        }

        .reveal-delay-1 { transition-delay: 0.1s; }
        .reveal-delay-2 { transition-delay: 0.2s; }
        .reveal-delay-3 { transition-delay: 0.3s; }
        .reveal-delay-4 { transition-delay: 0.4s; }
        .reveal-delay-5 { transition-delay: 0.5s; }
      `}</style>

      {/* Navigation */}
      <nav className="apex-nav">
        <a href="/" className="apex-logo">
          <div className="apex-logo-icon">
            <i className="fas fa-calendar-check" />
          </div>
          <span className="apex-logo-text">ApexBooking</span>
        </a>
        <div className="d-flex align-items-center gap-3">
          <a href="/login" className="btn-apex-ghost" style={{ padding: "9px 20px", fontSize: "0.875rem" }}>
            Sign In
          </a>
          <a href="/pricing" className="btn-apex-primary" style={{ padding: "9px 20px", fontSize: "0.875rem" }}>
            Get Started
          </a>
        </div>
      </nav>

      {/* Hero */}
      <section className="hero-section" ref={heroRef}>
        <div className="container py-5">
          <div className="row align-items-center g-5">
            <div className="col-lg-6">
              <div className="hero-badge">
                <i className="fas fa-circle" style={{ fontSize: 6 }} />
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
                  See Plans <i className="fas fa-arrow-right" style={{ fontSize: "0.8rem" }} />
                </button>
                <button className="btn-apex-ghost" onClick={() => navigate("/login")}>
                  Sign In
                </button>
              </div>
            </div>

            <div className="col-lg-6 d-none d-lg-block">
              <div className="hero-visual">
                <div className="mock-header">
                  <div className="mock-dot" style={{ background: "#ff5f57" }} />
                  <div className="mock-dot" style={{ background: "#febc2e" }} />
                  <div className="mock-dot" style={{ background: "#28c840" }} />
                  <span className="ms-3 text-muted small">apexbooking.app/book/lumiere-salon</span>
                </div>
                <div className="p-3">
                  <div className="mock-booking-card">
                    <div className="d-flex align-items-center gap-3 mb-3">
                      <div style={{ width: 40, height: 40, background: "var(--apex-blue-soft)", borderRadius: 10, display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <i className="fas fa-cut" style={{ color: "var(--apex-blue)", fontSize: 14 }} />
                      </div>
                      <div>
                        <div style={{ fontSize: "0.9rem", fontWeight: 700, color: "var(--apex-text)" }}>Hair Cut & Style</div>
                        <div style={{ fontSize: "0.75rem", color: "var(--apex-muted)" }}>60 min · $85.00</div>
                      </div>
                    </div>
                    <div style={{ fontSize: "0.75rem", fontWeight: 600, color: "var(--apex-muted)", marginBottom: 8, textTransform: "uppercase", letterSpacing: "0.06em" }}>Available slots — Today</div>
                    <div>
                      <span className="mock-slot">9:00 AM</span>
                      <span className="mock-slot selected">10:30 AM</span>
                      <span className="mock-slot">2:00 PM</span>
                      <span className="mock-slot">3:30 PM</span>
                    </div>
                  </div>
                  <div className="mock-booking-card" style={{ opacity: 0.6 }}>
                    <div className="d-flex align-items-center gap-3">
                      <div style={{ width: 40, height: 40, background: "#f0fdf4", borderRadius: 10, display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <i className="fas fa-spa" style={{ color: "#16a34a", fontSize: 14 }} />
                      </div>
                      <div>
                        <div style={{ fontSize: "0.9rem", fontWeight: 700, color: "var(--apex-text)" }}>Deep Tissue Massage</div>
                        <div style={{ fontSize: "0.75rem", color: "var(--apex-muted)" }}>90 min · $120.00</div>
                      </div>
                    </div>
                  </div>
                </div>
                <div style={{ background: "var(--apex-blue)", padding: "14px 20px", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                  <span style={{ color: "white", fontSize: "0.875rem", fontWeight: 600 }}>10:30 AM confirmed</span>
                  <div style={{ background: "rgba(255,255,255,0.2)", borderRadius: 8, padding: "6px 14px", color: "white", fontSize: "0.8rem", fontWeight: 600 }}>
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
      <section className="py-6 bg-white" style={{ padding: "80px 0" }}>
        <div className="container">
          <div className="text-center reveal">
            <div className="section-label">Features</div>
            <h2 className="section-title">Everything you need to<br /><em style={{ fontStyle: "italic", color: "var(--apex-blue)" }}>run and grow</em></h2>
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
      <section className="steps-section" style={{ padding: "80px 0" }}>
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

      {/* Pricing */}
      <section style={{ padding: "80px 0", background: "white" }}>
        <div className="container">
          <div className="text-center reveal mb-5">
            <div className="section-label">Pricing</div>
            <h2 className="section-title">Simple, transparent pricing</h2>
            <p className="section-subtitle">Start free. No credit card required.</p>
          </div>
          <div className="row g-4 justify-content-center" style={{ maxWidth: 740, margin: "0 auto" }}>
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
                <div style={{ background: "white", border: `1.5px solid ${plan.highlighted ? "var(--apex-blue)" : "var(--apex-border)"}`, borderRadius: 20, padding: "2rem", height: "100%", boxShadow: plan.highlighted ? "0 8px 40px rgba(13,110,253,0.12)" : undefined }}>
                  {plan.highlighted && (
                    <div style={{ background: "var(--apex-blue)", color: "white", fontSize: "0.72rem", fontWeight: 700, letterSpacing: "0.06em", textTransform: "uppercase", padding: "3px 10px", borderRadius: 100, display: "inline-block", marginBottom: "0.75rem" }}>
                      Most Popular
                    </div>
                  )}
                  <div style={{ fontSize: "1rem", fontWeight: 700, color: "var(--apex-text)", marginBottom: 4 }}>{plan.name}</div>
                  <div style={{ marginBottom: 4 }}>
                    <span style={{ fontFamily: "'DM Serif Display', serif", fontSize: "2.75rem", color: "var(--apex-text)", letterSpacing: "-0.04em", lineHeight: 1 }}>{plan.price}</span>
                    <span style={{ color: "var(--apex-muted)", fontWeight: 500, marginLeft: 4 }}>/mo</span>
                  </div>
                  <div style={{ fontSize: "0.85rem", color: "var(--apex-muted)", marginBottom: "1.25rem" }}>{plan.desc}</div>
                  <div style={{ marginBottom: "1.5rem" }}>
                    {plan.features.map(f => (
                      <div key={f} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: "0.875rem", color: "var(--apex-muted)", marginBottom: "0.5rem" }}>
                        <i className="fas fa-check-circle" style={{ color: "var(--apex-blue)", fontSize: "0.8rem" }} />
                        {f}
                      </div>
                    ))}
                  </div>
                  <button
                    style={{ width: "100%", padding: "12px", background: plan.highlighted ? "var(--apex-blue)" : "white", color: plan.highlighted ? "white" : "var(--apex-text)", border: plan.highlighted ? "none" : "1.5px solid var(--apex-border)", borderRadius: 10, fontSize: "0.9rem", fontWeight: 600, cursor: "pointer", transition: "all 0.2s", fontFamily: "inherit" }}
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
      <section className="cta-section" style={{ padding: "80px 0" }}>
        <div className="container text-center position-relative" style={{ zIndex: 1 }}>
          <div className="reveal">
            <div style={{ display: "inline-flex", alignItems: "center", gap: 8, background: "rgba(255,255,255,0.15)", color: "white", padding: "6px 14px", borderRadius: 100, fontSize: "0.8rem", fontWeight: 600, marginBottom: "1.5rem", letterSpacing: "0.04em", textTransform: "uppercase" }}>
              <i className="fas fa-rocket" style={{ fontSize: 10 }} />
              For Business Owners
            </div>
            <h2 style={{ fontFamily: "'DM Serif Display', serif", fontSize: "clamp(2rem, 4vw, 3rem)", color: "white", letterSpacing: "-0.03em", lineHeight: 1.15, marginBottom: "1rem" }}>
              Turn walk-ins into<br />loyal clients
            </h2>
            <p style={{ color: "rgba(255,255,255,0.75)", fontSize: "1.05rem", marginBottom: "2.5rem", maxWidth: 480, margin: "0 auto 2.5rem" }}>
              Set up your branded booking page in minutes. Let clients book 24/7 while you focus on delivering great service.
            </p>
            <div className="d-flex gap-3 justify-content-center flex-wrap">
              <button
                className="btn-apex-primary"
                onClick={() => navigate("/pricing")}
                style={{ background: "white", color: "var(--apex-blue)" }}
              >
                View Pricing <i className="fas fa-arrow-right" style={{ fontSize: "0.8rem" }} />
              </button>
              <button
                className="btn-apex-ghost"
                onClick={() => navigate("/login")}
                style={{ borderColor: "rgba(255,255,255,0.4)", color: "white" }}
              >
                Sign In
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="apex-footer" style={{ padding: "60px 0 32px" }}>
        <div className="container">
          <div className="row g-4 mb-5">
            <div className="col-lg-4">
              <div className="d-flex align-items-center gap-2 mb-3">
                <div className="apex-logo-icon">
                  <i className="fas fa-calendar-check" />
                </div>
                <span className="footer-logo-text">ApexBooking</span>
              </div>
              <p style={{ color: "rgba(255,255,255,0.45)", fontSize: "0.875rem", lineHeight: 1.65, maxWidth: 280 }}>
                The modern appointment platform for service businesses of every size.
              </p>
            </div>
            <div className="col-lg-2 col-6">
              <div style={{ color: "white", fontWeight: 600, fontSize: "0.8rem", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: "1rem" }}>Product</div>
              <a href="/pricing" className="footer-link">Pricing</a>
              <a href="/login" className="footer-link">Sign In</a>
            </div>
            <div className="col-lg-2 col-6">
              <div style={{ color: "white", fontWeight: 600, fontSize: "0.8rem", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: "1rem" }}>Company</div>
              <a href="/" className="footer-link">About</a>
              <a href="/" className="footer-link">Blog</a>
              <a href="/" className="footer-link">Careers</a>
            </div>
            <div className="col-lg-2 col-6">
              <div style={{ color: "white", fontWeight: 600, fontSize: "0.8rem", textTransform: "uppercase", letterSpacing: "0.08em", marginBottom: "1rem" }}>Legal</div>
              <a href="/" className="footer-link">Privacy</a>
              <a href="/" className="footer-link">Terms</a>
            </div>
          </div>
          <div style={{ borderTop: "1px solid rgba(255,255,255,0.08)", paddingTop: "1.5rem", display: "flex", justifyContent: "space-between", alignItems: "center", flexWrap: "wrap", gap: 12 }}>
            <span style={{ fontSize: "0.8rem", color: "rgba(255,255,255,0.3)" }}>
              © {new Date().getFullYear()} ApexBooking. All rights reserved.
            </span>
            <div className="d-flex gap-3">
              <a href="/" style={{ color: "rgba(255,255,255,0.3)", fontSize: "1rem" }}><i className="fab fa-twitter" /></a>
              <a href="/" style={{ color: "rgba(255,255,255,0.3)", fontSize: "1rem" }}><i className="fab fa-linkedin" /></a>
              <a href="/" style={{ color: "rgba(255,255,255,0.3)", fontSize: "1rem" }}><i className="fab fa-github" /></a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;