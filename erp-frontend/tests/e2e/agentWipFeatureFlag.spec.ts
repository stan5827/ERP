import { expect, test } from '@playwright/test';
import { prepareAgentWipPage } from './agentWipFixtures';

test('mocked feature-disabled 503 is surfaced and stale facts remain hidden', async ({ page }) => {
  await prepareAgentWipPage(page, {
    status: 503,
    body: { message: 'WIP 助理目前尚未開放' },
  });
  const panel = page.getByRole('region', { name: 'WIP 結構化事實' });
  const workOrderInput = page.getByRole('textbox', { name: '工單編號' });

  await workOrderInput.fill('WO-E2E-FLAG-OFF');
  await page.getByRole('button', { name: '查詢' }).click();

  await expect(panel.getByRole('alert')).toHaveText('WIP 助理目前尚未開放');
  await expect(panel.getByText('60.00 BRANCH')).toHaveCount(0);
});
