export function scrollToPricing(event: { preventDefault: () => void }) {
  event.preventDefault()

  const pricingSection = document.getElementById('pricing')
  if (pricingSection) {
    pricingSection.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  if (window.location.pathname !== '/') {
    window.location.assign('/#pricing')
  } else if (window.location.hash !== '#pricing') {
    window.history.pushState({}, '', '/#pricing')
  }
}
