import { userManager } from '../auth/oidc';
import type {
  PurchaseOrderDetail,
  PurchaseOrderResponseSubmission,
  PurchaseOrderSummary,
  CapaAttachment,
  CapaAttachmentUpload,
  CapaResponseSaved,
  CapaResponseSubmission,
  QuoteLineSubmission,
  RfqDetail,
  RfqSummary,
  SupplierComplaintDetail,
  SupplierComplaintSummary,
  SupplierPortalSession,
  PortalApiResponse,
  SupplierApEligibleSource,
  SupplierApSubmissionAttachment,
  SupplierApSubmissionAttachmentUpload,
  SupplierApSubmissionDetail,
  SupplierApSubmissionDraftRequest,
  SupplierApSubmissionPage,
  SupplierApSubmissionResult,
} from './types';

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL?.trim() || '/api/supplier-portal')
  .replace(/\/+$/, '');

export class PortalApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly payload: unknown = null,
  ) {
    super(message);
    this.name = 'PortalApiError';
  }
}

async function requestWithMetadata<T>(
  path: string,
  init?: RequestInit,
  responseType: 'json' | 'blob' = 'json',
): Promise<PortalApiResponse<T>> {
  if (!userManager)
    throw new PortalApiError('Portal 尚未設定 Keycloak。', 503);

  const user = await userManager.getUser();
  if (!user || user.expired || !user.access_token)
    throw new PortalApiError('登入工作階段已逾期，請重新登入。', 401);

  let response: Response;
  try {
    const headers = new Headers(init?.headers);
    headers.set('Accept', 'application/json');
    if (typeof init?.body === 'string')
      headers.set('Content-Type', 'application/json');
    headers.set('Authorization', `Bearer ${user.access_token}`);

    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers,
    });
  } catch {
    throw new PortalApiError('目前無法連線至服務，請確認網路後重試。', 0);
  }

  if (!response.ok) {
    const payload = await response.json().catch(() => null) as
      | { message?: string; detail?: string; title?: string }
      | null;
    const invitationSetupFailed = path === '/invitations/complete' && response.status === 403;
    const fallback = response.status === 401
      ? '登入工作階段無效或已逾期，請重新登入。'
      : response.status === 403
        ? invitationSetupFailed
          ? '請先完成邀請信中的 Email 驗證及必要的密碼／雙因素驗證設定；若已完成仍無法登入，請聯絡採購窗口。'
          : '此帳號目前沒有供應商入口使用權限。'
        : response.status === 404
          ? '找不到資料，或該資料不在目前帳號的授權範圍內。'
          : response.status === 409
            ? '資料狀態已變更，請重新載入後再試。'
            : response.status === 413
              ? '檔案超過系統允許的大小，請縮小檔案後重試。'
              : response.status === 422
                ? '資料未通過系統政策或安全檢查，請檢查內容後重試。'
                : response.status === 503
                  ? '安全掃描或服務目前無法使用，請稍後再試；檔案不會被視為已通過檢查。'
                  : '服務暫時無法完成要求，請稍後重試。';
    throw new PortalApiError(payload?.message || payload?.detail || fallback, response.status, payload);
  }

  if (response.status === 204)
    return { data: undefined as T, etag: response.headers.get('ETag') };
  if (responseType === 'blob')
    return { data: await response.blob() as T, etag: response.headers.get('ETag') };
  return { data: await response.json() as T, etag: response.headers.get('ETag') };
}

async function request<T>(
  path: string,
  init?: RequestInit,
  responseType: 'json' | 'blob' = 'json',
): Promise<T> {
  return (await requestWithMetadata<T>(path, init, responseType)).data;
}

export const portalApi = {
  completeInvitation: () =>
    request('/invitations/complete', { method: 'POST' }),
  getSession: () =>
    request<SupplierPortalSession>('/session'),
  getRfqs: () =>
    request<RfqSummary[]>('/rfqs'),
  getRfq: (rfqId: string) =>
    request<RfqDetail>(`/rfqs/${encodeURIComponent(rfqId)}`),
  submitQuote: (rfqId: string, items: QuoteLineSubmission[]) =>
    request(`/rfqs/${encodeURIComponent(rfqId)}/quotes`, {
      method: 'POST',
      body: JSON.stringify({ items }),
    }),
  getPurchaseOrders: () =>
    request<PurchaseOrderSummary[]>('/purchase-orders'),
  getPurchaseOrder: (id: string) =>
    request<PurchaseOrderDetail>(`/purchase-orders/${encodeURIComponent(id)}`),
  submitPurchaseOrderResponse: (id: string, body: PurchaseOrderResponseSubmission) =>
    request(`/purchase-orders/${encodeURIComponent(id)}/responses`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  getComplaints: () =>
    request<SupplierComplaintSummary[]>('/complaints'),
  getComplaint: (complaintId: string) =>
    request<SupplierComplaintDetail>(`/complaints/${encodeURIComponent(complaintId)}`),
  saveCapaDraft: (complaintId: string, body: CapaResponseSubmission) =>
    request<CapaResponseSaved>(`/complaints/${encodeURIComponent(complaintId)}/capa`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }),
  submitCapaResponse: (complaintId: string, body: CapaResponseSubmission) =>
    request<CapaResponseSaved>(`/complaints/${encodeURIComponent(complaintId)}/capa/submit`, {
      method: 'POST',
      body: JSON.stringify(body),
    }),
  getCapaAttachments: (complaintId: string) =>
    request<CapaAttachment[]>(`/complaints/${encodeURIComponent(complaintId)}/capa/attachments`),
  uploadCapaAttachment: (complaintId: string, file: File) => {
    const body = new FormData();
    body.append('file', file);
    return request<CapaAttachmentUpload>(
      `/complaints/${encodeURIComponent(complaintId)}/capa/attachments`,
      { method: 'POST', body },
    );
  },
  downloadCapaAttachment: (complaintId: string, attachmentId: string) =>
    request<Blob>(
      `/complaints/${encodeURIComponent(complaintId)}/capa/attachments/${encodeURIComponent(attachmentId)}/content`,
      undefined,
      'blob',
    ),
  revokeCapaAttachment: (complaintId: string, attachmentId: string) =>
    request<void>(
      `/complaints/${encodeURIComponent(complaintId)}/capa/attachments/${encodeURIComponent(attachmentId)}`,
      { method: 'DELETE' },
    ),
  getApSubmissions: (query: URLSearchParams) =>
    request<SupplierApSubmissionPage>(`/ap-submissions?${query.toString()}`),
  getEligibleApSources: () =>
    request<SupplierApEligibleSource[]>('/ap-submissions/eligible-sources'),
  getApSubmission: (submissionId: string) =>
    requestWithMetadata<SupplierApSubmissionDetail>(
      `/ap-submissions/${encodeURIComponent(submissionId)}`,
    ),
  createApSubmission: (body: SupplierApSubmissionDraftRequest, idempotencyKey: string) =>
    requestWithMetadata<SupplierApSubmissionResult>('/ap-submissions', {
      method: 'POST',
      headers: { 'Idempotency-Key': idempotencyKey },
      body: JSON.stringify(body),
    }),
  updateApSubmissionDraft: (
    submissionId: string,
    body: SupplierApSubmissionDraftRequest,
    etag: string,
  ) =>
    requestWithMetadata<SupplierApSubmissionResult>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/draft`,
      { method: 'PUT', headers: { 'If-Match': etag }, body: JSON.stringify(body) },
    ),
  submitApSubmission: (submissionId: string, etag: string) =>
    requestWithMetadata<SupplierApSubmissionResult>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/submit`,
      { method: 'POST', headers: { 'If-Match': etag } },
    ),
  cancelApSubmission: (submissionId: string, etag: string, reason: string) =>
    requestWithMetadata<SupplierApSubmissionResult>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/cancel`,
      { method: 'POST', headers: { 'If-Match': etag }, body: JSON.stringify({ reason }) },
    ),
  getApSubmissionAttachments: (submissionId: string) =>
    request<SupplierApSubmissionAttachment[]>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/attachments`,
    ),
  uploadApSubmissionAttachment: (submissionId: string, file: File) => {
    const body = new FormData();
    body.append('file', file);
    return request<SupplierApSubmissionAttachmentUpload>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/attachments`,
      { method: 'POST', body },
    );
  },
  revokeApSubmissionAttachment: (submissionId: string, attachmentId: string) =>
    request<void>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/attachments/${encodeURIComponent(attachmentId)}`,
      { method: 'DELETE' },
    ),
  downloadApSubmissionAttachment: (submissionId: string, attachmentId: string) =>
    request<Blob>(
      `/ap-submissions/${encodeURIComponent(submissionId)}/attachments/${encodeURIComponent(attachmentId)}/content`,
      undefined,
      'blob',
    ),
};
