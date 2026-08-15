import type { IPublicBranch } from '../interfaces/publicBooking/IPublicBranch'

function branchAddress(branch: IPublicBranch): string {
  return [branch.street, branch.barangay, branch.city, branch.province].filter(Boolean).join(', ')
}

export function buildDirectionsUrl(branch: IPublicBranch): string {
  const address = [branchAddress(branch), branch.zipCode].filter(Boolean).join(' ')
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`
}
