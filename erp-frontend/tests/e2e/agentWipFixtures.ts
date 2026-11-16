import type { Page, Route } from '@playwright/test';

export const appOrigin = 'http://127.0.0.1:5187';
const testToken = 'playwright-agent-wip-token';

export interface AgentWipRouteResponse {
  status: number;
  body: unknown;
}

const stringFact = (factId: string, value: string) => ({
  factId,
  value,
  sourceTable: 'WorkOrders',
  sourceField: 'Status',
  asOf: '2026-11-03T06:00:00Z',
  unit: null,
  notApplicableReason: null,
  dataClassification: 'internal',
});

const decimalFact = (factId: string, value: number, displayValue: string, unit: string | null) => ({
  factId,
  value,
  displayValue,
  sourceTable: 'WorkOrderOperations',
  sourceField: 'PlannedQty',
  asOf: '2026-11-03T06:00:00Z',
  unit,
  notApplicableReason: null,
  dataClassification: 'internal',
});

export const authorizedWipResponse = {
  success: true,
  data: {
    workOrderNumber: stringFact('fixture/work-order/number', 'WO-E2E-001'),
    productNumber: stringFact('fixture/product/number', 'FG-E2E-001'),
    productName: stringFact('fixture/product/name', '合成測試產品'),
    productionUnit: stringFact('fixture/work-order/unit', 'BRANCH'),
    status: stringFact('fixture/work-order/status', 'in_progress'),
    authorizedOperations: [{
      operationSequence: {
        ...decimalFact('fixture/operation/sequence', 10, '10', null),
        value: 10,
      },
      status: stringFact('fixture/operation/status', 'in_progress'),
    }],
    finalOperation: {
      operationSequence: {
        ...decimalFact('fixture/final/sequence', 10, '10', null),
        value: 10,
      },
      status: stringFact('fixture/final/status', 'in_progress'),
      plannedQuantity: decimalFact('fixture/final/planned', 100, '100.00', 'BRANCH'),
      goodQuantity: decimalFact('fixture/final/good', 60, '60.00', 'BRANCH'),
      defectQuantity: decimalFact('fixture/final/defect', 5, '5.00', 'BRANCH'),
      defectRatePercent: {
        ...decimalFact('fixture/final/defect-rate', 7.692307692307692, '7.692307692307692', '%'),
        sourceTable: 'WipReports',
        sourceField: 'DefectQty / (CompletedQty + DefectQty) * 100',
      },
      remainingQuantity: decimalFact('fixture/final/remaining', 40, '40.00', 'BRANCH'),
    },
  },
};

async function routeApiRequest(route: Route, response: AgentWipRouteResponse | undefined) {
  const request = route.request();
  const url = new URL(request.url());
  if (request.method() !== 'GET') {
    await route.fulfill({ status: 405, json: { message: 'Read-only test API' } });
    return;
  }

  if (url.pathname.startsWith('/api/erp/agent-wip/work-orders/')) {
    await route.fulfill({
      status: response?.status ?? 200,
      json: response?.body ?? authorizedWipResponse,
      headers: { 'Cache-Control': 'no-store' },
    });
    return;
  }

  if (url.pathname.endsWith('/system-settings/permissions/me')) {
    await route.fulfill({
      status: 200,
      json: {
        success: true,
        data: [{ moduleId: 'manufacturing', permissions: ['view'] }],
      },
    });
    return;
  }

  if (url.pathname.endsWith('/dashboard')) {
    await route.fulfill({
      status: 200,
      json: {
        success: true,
        data: {
          kpiCards: [],
          alerts: [],
          recentWorkOrders: [],
          productionTrend: [],
          attendanceTrend: [],
          totalEmployees: 0,
          todayAttendanceCount: 0,
          pendingApprovals: 0,
          activeAlerts: 0,
          sidebarBadges: {
            salesCount: 0,
            procurementCount: 0,
            hrCount: 0,
            inventoryCount: 0,
            manufacturingCount: 0,
            qualityCount: 0,
          },
        },
      },
    });
    return;
  }

  await route.fulfill({
    status: 200,
    json: { success: true, data: [], totalCount: 0, page: 1, pageSize: 20 },
  });
}

export async function prepareAgentWipPage(
  page: Page,
  response?: AgentWipRouteResponse,
) {
  await page.addInitScript(({ token }) => {
    localStorage.setItem('erp_token', token);
    localStorage.setItem('erp_user', JSON.stringify({
      userId: '00000000-0000-4000-8000-000000000001',
      employeeId: '00000000-0000-4000-8000-000000000002',
      username: 'test',
      token,
      expiresAt: '2099-01-01T00:00:00Z',
      roles: ['WIP Manufacturing Viewer'],
      permissions: ['manufacturing:view'],
      mustChangePassword: false,
    }));
  }, { token: testToken });

  await page.route('**/*', async route => {
    if (new URL(route.request().url()).origin === appOrigin)
      await route.continue();
    else
      await route.abort('blockedbyclient');
  });
  await page.route('**/api/erp/**', route => routeApiRequest(route, response));

  await page.goto('/');
  await page.waitForResponse(response =>
    response.url().endsWith('/system-settings/permissions/me'),
  );
  const manufacturingNav = page.getByRole('button', { name: /生產工單/ });
  await manufacturingNav.waitFor({ state: 'visible' });
  await manufacturingNav.focus();
  await manufacturingNav.press('Enter');
  await page.getByRole('region', { name: 'WIP 結構化事實' }).waitFor();

  return { token: testToken };
}
