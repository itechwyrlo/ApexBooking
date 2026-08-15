const CURRENCY_SYMBOLS: Record<string, string> = {
  PHP: '₱',
  USD: '$',
}

export function formatMoney(amount: number, currencyCode: string): string {
  const symbol = CURRENCY_SYMBOLS[currencyCode] ?? `${currencyCode} `
  return `${symbol}${amount.toFixed(2)}`
}
