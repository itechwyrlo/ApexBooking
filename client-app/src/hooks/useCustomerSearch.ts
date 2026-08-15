import { useEffect, useState } from 'react'
import { searchCustomers } from '../services/customerService'
import type { ICustomer } from '../interfaces/ICustomer'

const MIN_TERM_LENGTH = 2
const DEBOUNCE_MS = 300

export function useCustomerSearch(term: string): { results: ICustomer[]; isLoading: boolean } {
  const [results, setResults] = useState<ICustomer[]>([])
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (term.trim().length < MIN_TERM_LENGTH) {
      setResults([])
      setIsLoading(false)
      return
    }

    let isMounted = true
    setIsLoading(true)

    const timeoutId = window.setTimeout(() => {
      searchCustomers(term.trim())
        .then((customers) => {
          if (isMounted) setResults(customers)
        })
        .catch(() => {
          if (isMounted) setResults([])
        })
        .finally(() => {
          if (isMounted) setIsLoading(false)
        })
    }, DEBOUNCE_MS)

    return () => {
      isMounted = false
      window.clearTimeout(timeoutId)
    }
  }, [term])

  return { results, isLoading }
}
