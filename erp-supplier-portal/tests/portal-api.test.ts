import { User } from 'oidc-client-ts';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../src/auth/oidc', () => ({
  userManager: { getUser: vi.fn() },
}));

import { userManager } from '../src/auth/oidc';
import { PortalApiError, portalApi } from '../src/api/client';

const fetchMock = vi.fn<typeof fetch>();

describe('supplier portal API client', () => {
  beforeEach(() => {
    if (!userManager)
      throw new Error('OIDC test mock was not initialized.');

    vi.mocked(userManager.getUser).mockReset();
    vi.mocked(userManager.getUser).mockResolvedValue(new User({
      access_token: 'test-access-token',
      token_type: 'Bearer',
      expires_at: Math.floor(Date.now() / 1000) + 3600,
      profile: { sub: 'test-supplier-subject' },
    }));

    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends the Keycloak access token to the supplier session endpoint', async () => {
    const session = {
      supplierUserId: 'test-user-id',
      username: 'supplier.test',
      contactName: 'Test Supplier',
      role: 'sales',
      supplierCode: 'TEST-001',
      supplierName: 'Test Supplier Co.',
    };
    fetchMock.mockResolvedValue(new Response(JSON.stringify(session), { status: 200 }));

    await expect(portalApi.getSession()).resolves.toEqual(session);

    const requestInit = fetchMock.mock.calls[0]?.[1];
    expect(new Headers(requestInit?.headers).get('Authorization')).toBe('Bearer test-access-token');
  });

  it('preserves authorization errors as typed portal API errors', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 403 }));

    await expect(portalApi.getSession()).rejects.toMatchObject({
      name: 'PortalApiError',
      status: 403,
    });
  });

  it('uses the AP submission idempotency key and preserves the response ETag', async () => {
    const body = {
      supplierInvoiceNumber: 'INV-100',
      supplierInvoiceDate: '2026-10-27',
      currency: 'TWD',
      invoiceAmount: 100,
      taxAmount: 5,
      totalAmount: 105,
      purchaseOrderId: 'po-id',
      goodsReceiptId: 'receipt-id',
      lines: [{
        goodsReceiptItemId: 'receipt-item-id',
        invoicedQuantity: 1,
        unitPrice: 100,
        netAmount: 100,
        taxAmount: 5,
        totalAmount: 105,
      }],
    };
    const result = {
      submissionId: 'submission-id',
      submissionNumber: 'APS-001',
      status: 'draft',
      revisionNumber: 1,
      version: 1,
      matchResult: 'pending',
      discrepancies: [],
      submittedAt: null,
      accountsPayableId: null,
      accountsPayableNumber: null,
    };
    fetchMock.mockResolvedValue(new Response(JSON.stringify(result), {
      status: 201,
      headers: { ETag: '"1"' },
    }));

    await expect(portalApi.createApSubmission(body, 'request-key'))
      .resolves.toEqual({ data: result, etag: '"1"' });

    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe('/api/supplier-portal/ap-submissions');
    expect(new Headers(init?.headers).get('Idempotency-Key')).toBe('request-key');
    expect(JSON.parse(String(init?.body))).toEqual(body);
  });

  it('sends If-Match for AP submission mutations and preserves attachment scan errors', async () => {
    const result = {
      submissionId: 'submission/id',
      submissionNumber: 'APS-002',
      status: 'draft',
      revisionNumber: 1,
      version: 2,
      matchResult: 'pending',
      discrepancies: [],
      submittedAt: null,
      accountsPayableId: null,
      accountsPayableNumber: null,
    };
    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify(result), {
      status: 200,
      headers: { ETag: '"2"' },
    }));

    await expect(portalApi.submitApSubmission('submission/id', '"1"'))
      .resolves.toMatchObject({ data: result, etag: '"2"' });
    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe('/api/supplier-portal/ap-submissions/submission%2Fid/submit');
    expect(new Headers(init?.headers).get('If-Match')).toBe('"1"');

    fetchMock.mockResolvedValueOnce(new Response(JSON.stringify({
      message: '附件未通過安全掃描。',
      attachment: {
        id: 'attachment-id',
        originalFileName: 'unsafe.pdf',
        scanStatus: 'rejected',
      },
    }), { status: 422 }));
    const file = new File(['%PDF-1.7'], 'unsafe.pdf', { type: 'application/pdf' });

    await expect(portalApi.uploadApSubmissionAttachment('submission/id', file))
      .rejects.toMatchObject({
        name: 'PortalApiError',
        status: 422,
        payload: {
          attachment: { id: 'attachment-id', scanStatus: 'rejected' },
        },
      } satisfies Partial<PortalApiError>);

    const [, uploadInit] = fetchMock.mock.calls[1] ?? [];
    expect(new Headers(uploadInit?.headers).has('Content-Type')).toBe(false);
    expect(uploadInit?.body).toBeInstanceOf(FormData);
  });

  it('uploads CAPA evidence as multipart data with the Portal access token', async () => {
    const upload = {
      attachmentId: 'attachment-id',
      originalFileName: 'evidence.pdf',
      scanStatus: 'clean',
      uploadedAt: '2026-10-27T09:00:00Z',
    };
    const file = new File(['%PDF-1.7'], upload.originalFileName, { type: 'application/pdf' });
    fetchMock.mockResolvedValue(new Response(JSON.stringify(upload), { status: 201 }));

    await expect(portalApi.uploadCapaAttachment('complaint/id', file)).resolves.toEqual(upload);

    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe('/api/supplier-portal/complaints/complaint%2Fid/capa/attachments');
    expect(new Headers(init?.headers).get('Authorization')).toMatch(/^Bearer .+/);
    expect(new Headers(init?.headers).has('Content-Type')).toBe(false);
    expect(init?.body).toBeInstanceOf(FormData);
    if (!(init?.body instanceof FormData))
      throw new Error('CAPA upload must use multipart form data.');
    expect(init.body.get('file')).toBe(file);
  });

  it('downloads CAPA evidence as a blob from the authenticated complaint route', async () => {
    const file = new Blob(['%PDF-1.7'], { type: 'application/pdf' });
    fetchMock.mockResolvedValue(new Response(file, { status: 200 }));

    await expect(portalApi.downloadCapaAttachment('complaint-id', 'attachment-id'))
      .resolves.toEqual(file);

    const [url, init] = fetchMock.mock.calls[0] ?? [];
    expect(url).toBe('/api/supplier-portal/complaints/complaint-id/capa/attachments/attachment-id/content');
    expect(new Headers(init?.headers).get('Authorization')).toMatch(/^Bearer .+/);
  });
});
