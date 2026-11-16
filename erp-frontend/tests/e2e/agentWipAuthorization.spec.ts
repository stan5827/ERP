import { expect, test } from '@playwright/test';
import { appOrigin, prepareAgentWipPage } from './agentWipFixtures';

test('mocked backend 403 is shown without exposing WIP facts to a denied user', async ({ page }) => {
  const { token } = await prepareAgentWipPage(page, {
    status: 403,
    body: { message: '您目前不在 WIP 助理試用名單中' },
  });
  let authorization: string | undefined;

  page.on('request', request => {
    if (new URL(request.url()).pathname.includes('/agent-wip/work-orders/'))
      authorization = request.headers()['authorization'];
  });

  const panel = page.getByRole('region', { name: 'WIP 結構化事實' });
  await page.getByRole('textbox', { name: '工單編號' }).fill('WO-E2E-DENIED');
  await page.getByRole('button', { name: '查詢' }).click();

  await expect(panel.getByRole('alert')).toHaveText('您目前不在 WIP 助理試用名單中');
  await expect(panel.getByText('60.00 BRANCH')).toHaveCount(0);
  expect(authorization).toBe(`Bearer ${token}`);
});

test('anonymous user is sent to login without issuing a WIP request', async ({ page }) => {
  await page.route('**/*', async route => {
    if (new URL(route.request().url()).origin === appOrigin)
      await route.continue();
    else
      await route.abort('blockedbyclient');
  });
  let wipRequestCount = 0;
  page.on('request', request => {
    if (new URL(request.url()).pathname.includes('/agent-wip/work-orders/'))
      wipRequestCount++;
  });

  await page.goto('/');

  await expect(page.getByRole('textbox', { name: '請輸入帳號' })).toBeVisible();
  await expect(page.getByRole('button', { name: '登入系統' })).toBeVisible();
  expect(wipRequestCount).toBe(0);
});
