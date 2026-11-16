import { test, expect } from '@playwright/test';

test.describe('Backend API Integration', () => {
  const apiBase = process.env.ERP_API_BASE_URL ?? 'http://localhost:5100';

  test.skip(
    !process.env.ERP_API_BASE_URL,
    'Set ERP_API_BASE_URL to run backend smoke tests against a live API.',
  );

  test('GET /health returns healthy status', async ({ request }) => {
    const response = await request.get(`${apiBase}/health`);
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.status).toBe('healthy');
  });

  test('GET /api/erp/kpis/ping rejects anonymous access', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/kpis/ping`);
    expect(response.status()).toBe(401);
  });

  test('GET /api/erp/budgets/ping rejects anonymous access', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/budgets/ping`);
    expect(response.status()).toBe(401);
  });

  test('GET /api/erp/llm/ping rejects anonymous access', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/llm/ping`);
    expect(response.status()).toBe(401);
  });

  test('GET /api/erp/kpis/dashboard rejects anonymous access', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/kpis/dashboard?fromDate=2026-01-01&toDate=2026-12-31`);
    expect(response.status()).toBe(401);
  });

  test('GET /api/erp/budgets/alerts rejects anonymous access', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/budgets/alerts`);
    expect(response.status()).toBe(401);
  });
});
