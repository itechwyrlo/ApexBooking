import { authClient } from '../api/clients/authClient'
import type { IRefundRequest } from '../interfaces/IRefundRequest'
import type { IRefundLogEntry } from '../interfaces/IRefundLogEntry'
import type { IPagedResult, IPageParams } from '../interfaces/IPagedResult'

// Wire shape from ApexBooking.Core.Application.Features.RefundRequests.Queries.GetPendingRefundRequests.RefundRequestSummaryDto
// matches IRefundRequest field-for-field (camelCase JSON naming policy), so no mapper is needed.

export async function getRefundRequests(params: IPageParams = {}): Promise<IPagedResult<IRefundRequest>> {
  const response = await authClient.get<IPagedResult<IRefundRequest>>('/api/refund-requests', {
    params: { pageNumber: params.pageNumber ?? 1, pageSize: params.pageSize ?? 10 },
  })
  return response.data
}

export async function confirmRefundRequest(id: string, receipt: File): Promise<void> {
  const formData = new FormData()
  formData.append('receipt', receipt)
  await authClient.post(`/api/refund-requests/${id}/confirm`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
}

export async function rejectRefundRequest(id: string, reason: string): Promise<void> {
  await authClient.post(`/api/refund-requests/${id}/reject`, { reason })
}

export async function getRefundLog(limit = 20): Promise<IRefundLogEntry[]> {
  const response = await authClient.get<IRefundLogEntry[]>('/api/refund-requests/log', { params: { limit } })
  return response.data
}
