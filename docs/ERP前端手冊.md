# ERP 前端手冊

這份文件整合了原本的 `ERP_前端操作手冊` 與 `ERP_前端模組執行流程`，作為前端使用與畫面流程的單一入口。

## 1. 前端架構

- React + TypeScript
- Vite
- Axios
- TanStack Query
- React Router

## 2. 登入與啟動

- 登入後進入主框架
- 由 `Sidebar` 切換模組
- `RoleContext` 控制模組可見性
- `ThemeProvider` 管理主題

## 3. 路由與模組

- dashboard
- sales
- procurement
- inventory
- manufacturing
- hr
- finance
- quality
- rd
- esg
- ai
- agentflows
- collaboration
- calendar
- systemsettings

## 4. 資料流

### 4.1 使用者互動

- 使用者操作頁面
- React Query 發送請求
- `api.ts` / `baseApi.ts` 呼叫後端
- 後端回傳 `ApiResponse`
- 前端更新畫面

### 4.2 Bootstrap 項目

- 登入
- 儀表板
- 系統設定
- 主題設定
- 權限與模組存取

## 5. 常見頁面

- 儀表板
- 系統設定
- 採購
- 庫存
- 生產
- 財務
- 品質
- AI 排班
- 供應商入口

## 6. 建議 API 群組

- `/api/auth`
- `/api/erp/system-settings`
- `/api/erp/Dashboard`
- `/api/ai/scheduling`
- 以及各模組對應的 `/api/erp/...`

## 7. 補充文件

- 舊版前端流程與操作說明已歸檔到 `archive/overviews/`
- 若要追流程圖，請搭配 `ERP_前端模組執行流程.md` 的舊版內容看歷史參考

