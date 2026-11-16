import { expect, test } from '@playwright/test';
import { authorizedWipResponse, prepareAgentWipPage } from './agentWipFixtures';

test('mocked authorized response renders server-formatted WIP facts, UOM, and FactId', async ({ page }) => {
  const { token } = await prepareAgentWipPage(page);
  const queryRequests: Array<{ method: string; authorization: string | undefined; sessionId: string | undefined }> = [];
  page.on('request', request => {
    if (new URL(request.url()).pathname.includes('/agent-wip/work-orders/')) {
      queryRequests.push({
        method: request.method(),
        authorization: request.headers()['authorization'],
        sessionId: request.headers()['x-agent-session-id'],
      });
    }
  });

  const workOrderInput = page.getByRole('textbox', { name: '工單編號' });
  await workOrderInput.fill('WO-E2E-001');
  await page.getByRole('button', { name: '查詢' }).click();

  const panel = page.getByRole('region', { name: 'WIP 結構化事實' });
  const finalOperation = panel.getByRole('region', { name: '最終工序 WIP 數量' });
  await expect(panel.getByText('WO-E2E-001')).toBeVisible();
  await expect(panel.getByText('60.00 BRANCH')).toBeVisible();
  await expect(panel.getByText('5.00 BRANCH')).toBeVisible();
  await expect(panel.getByText('7.692307692307692 %')).toBeVisible();
  await expect(panel.getByText('40.00 BRANCH')).toBeVisible();
  await finalOperation.getByText('事實來源').nth(2).click();
  await expect(panel.getByText(`FactId: ${authorizedWipResponse.data.finalOperation.plannedQuantity.factId}`)).toBeVisible();

  await workOrderInput.fill('');
  await workOrderInput.fill('WO-E2E-001');
  await page.getByRole('button', { name: '查詢' }).click();
  await expect(panel.getByText('WO-E2E-001')).toBeVisible();

  expect(queryRequests).toHaveLength(2);
  expect(queryRequests[0].method).toBe('GET');
  expect(queryRequests[0].authorization).toBe(`Bearer ${token}`);
  expect(queryRequests[0].sessionId).toMatch(/^[0-9a-f-]{36}$/i);
  expect(queryRequests[1].sessionId).toMatch(/^[0-9a-f-]{36}$/i);
  expect(queryRequests[1].sessionId).not.toBe(queryRequests[0].sessionId);
});
