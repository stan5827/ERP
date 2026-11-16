import { test, expect } from '@playwright/test';

test.describe('Backend API Integration', () => {
  const apiBase = 'http://localhost:5100';

  test('GET /health returns healthy status', async ({ request }) => {
    const response = await request.get(`${apiBase}/health`);
    expect(response.status()).toBe(200);
    const body = await response.json();
    expect(body.status).toBe('healthy');
  });

  test('GET /api/erp/kpis/ping returns operational', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/kpis/ping`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/erp/budgets/ping returns operational', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/budgets/ping`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/erp/llm/ping returns pong', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/llm/ping`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/erp/kpis/dashboard returns KPI data', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/kpis/dashboard?fromDate=2026-01-01&toDate=2026-12-31`);
    expect(response.status()).toBe(200);
  });

  test('GET /api/erp/budgets/alerts returns alerts list', async ({ request }) => {
    const response = await request.get(`${apiBase}/api/erp/budgets/alerts`);
    expect(response.status()).toBe(200);
  });
});
