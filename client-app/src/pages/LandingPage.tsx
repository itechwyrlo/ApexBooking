import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { Header } from '../components/landing/Header'
import { HeroSection } from '../components/landing/HeroSection'
import { ProblemSection } from '../components/landing/ProblemSection'
import { BusinessSection } from '../components/landing/BusinessSection'
import { FeaturesSection } from '../components/landing/FeaturesSection'
import { DashboardPreviewSection } from '../components/landing/DashboardPreviewSection'
import { PricingSection } from '../components/landing/PricingSection'
import { HowItWorksSection } from '../components/landing/HowItWorksSection'
import { CallToActionSection } from '../components/landing/CallToActionSection'
import { Footer } from '../components/landing/Footer'

export function LandingPage() {
  const location = useLocation()

  useEffect(() => {
    const shouldScrollToPricing = location.hash === '#pricing'

    if (!shouldScrollToPricing) {
      return
    }

    const pricingSection = document.getElementById('pricing')
    if (pricingSection) {
      pricingSection.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }, [location.hash])

  return (
    <>
      <Header />
      <main className="pt-nav">
        <HeroSection />
        <ProblemSection />
        <BusinessSection />
        <FeaturesSection />
        <DashboardPreviewSection />
        <PricingSection />
        <HowItWorksSection />
        <CallToActionSection />
      </main>
      <Footer />
    </>
  )
}
