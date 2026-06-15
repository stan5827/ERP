import { test, expect } from '@playwright/test';

test.describe('ERP Smoke Tests', () => {
  const username = process.env.E2E_ADMIN_USERNAME || 'admin';
  const password = process.env.E2E_ADMIN_PASSWORD || 'admin123';

  test('Login and navigate to MRP', async ({ page }) => {
    // 1. 導向登入頁
    await page.goto('http://localhost:5173/');
    
    // 2. 登入
    await page.fill('input[placeholder="請輸入帳號"]', username);
    await page.fill('input[placeholder="請輸入密碼"]', password);
    await page.click('button:has-text("登入系統")');
    
    // 3. 驗證是否進入儀表板 (不檢查 URL，檢查頁面元素)
    await expect(page.locator('text=系統儀表板')).toBeVisible({ timeout: 10000 });
    
    // 4. 點擊側邊欄進入 MRP (修正標籤為 "MRP 需求規劃")
    await page.click('text=MRP 需求規劃');
    
    // 5. 驗證 MRP 頁面
    await expect(page.locator('h1')).toHaveText('物料需求規劃 (MRP)');
    await expect(page.locator('button:has-text("啟動 MRP 運算")')).toBeVisible();
  });

  test('Run MRP Calculation', async ({ page }) => {
    // 登入
    await page.goto('http://localhost:5173/');
    await page.fill('input[placeholder="請輸入帳號"]', username);
    await page.fill('input[placeholder="請輸入密碼"]', password);
    await page.click('button:has-text("登入系統")');
    
    // 進入 MRP
    await page.click('text=MRP 需求規劃');
    
    // 啟動 MRP 運算
    await page.click('button:has-text("啟動 MRP 運算")');
    
    // 點擊 Modal 中的啟動按鈕 (使用 force: true 避免被 Topbar 遮擋)
    await page.click('button:has-text("儲存")', { force: true }); 
    
    // 1. 驗證 Modal 是否關閉 (代表提交成功)
    await expect(page.locator('text=啟動 MRP 運算核心')).not.toBeVisible({ timeout: 10000 });
    
    // 2. 驗證 Toast 通知 (使用 getByText 更穩定)
    await expect(page.getByText('MRP 計算已啟動')).toBeVisible({ timeout: 20000 });
  });
});
