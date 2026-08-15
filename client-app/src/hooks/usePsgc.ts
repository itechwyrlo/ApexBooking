import { useEffect, useState } from 'react'
import { getPsgcBarangays, getPsgcCities, getPsgcProvinces } from '../services/psgcService'
import type { IPsgcBarangay, IPsgcCity, IPsgcProvince } from '../interfaces/IPsgc'

interface IUsePsgcListResult<T> {
  items: T[]
  isLoading: boolean
}

export function usePsgcProvinces(): IUsePsgcListResult<IPsgcProvince> {
  const [items, setItems] = useState<IPsgcProvince[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let isMounted = true

    getPsgcProvinces()
      .then((result) => {
        if (isMounted) setItems(result)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [])

  return { items, isLoading }
}

export function usePsgcCities(provCode: string | null): IUsePsgcListResult<IPsgcCity> {
  const [items, setItems] = useState<IPsgcCity[]>([])
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!provCode) {
      setItems([])
      return
    }

    let isMounted = true
    setIsLoading(true)

    getPsgcCities(provCode)
      .then((result) => {
        if (isMounted) setItems(result)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [provCode])

  return { items, isLoading }
}

export function usePsgcBarangays(munCityCode: string | null): IUsePsgcListResult<IPsgcBarangay> {
  const [items, setItems] = useState<IPsgcBarangay[]>([])
  const [isLoading, setIsLoading] = useState(false)

  useEffect(() => {
    if (!munCityCode) {
      setItems([])
      return
    }

    let isMounted = true
    setIsLoading(true)

    getPsgcBarangays(munCityCode)
      .then((result) => {
        if (isMounted) setItems(result)
      })
      .finally(() => {
        if (isMounted) setIsLoading(false)
      })

    return () => {
      isMounted = false
    }
  }, [munCityCode])

  return { items, isLoading }
}
