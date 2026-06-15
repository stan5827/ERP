ERP系統完整分析報告
系統概述
這是一個全端模組化ERP系統，專為現代製造業與跨國企業設計，涵蓋從企業營運神經中樞到基層生產作業的九大核心循環模組。
技術棧
- 前端: React + Vite + TypeScript
- 後端: Node.js + Express + Prisma ORM
- 資料庫: SQLite (開發/測試)，可切換至 SQL Server
- 快取: Redis + Memory Cache 雙層架構
- 即時通訊: LINE Messaging API 為簽核與異常通知
- API文件: Axios封裝的統一服務層
功能模組
1. 企業入口大廳 (Portal) - 待辦事項、公告欄、快捷方式
2. 人事管理 (HRM) - 員工檔案、考勤、薪資、Line異常簽核
3. 庫存管理 (Inventory) - SKU主檔、異動記錄、智慧盤點、AS/RS對接
4. 生產與品質 (MES & QA) - 生產工單、BOM管理、品質檢驗、WIP報工
5. 銷貨與收款 (Sales) - 客戶管理、報價、出貨、帳款控管
6. 採購付款 (Procurement) - 無紙化請購、智慧詢價、PO管理、三方勾稽
7. 研發專案 (R&D) - PLM專案管理、文件庫、ECN變更管理
8. 預算規劃 (Budgeting) - 銷售預測、OPEX/CAPEX、人力預算
9. 數位轉型與 ESG - AI洞察分析、異常偵測、GHG盤查
10. 供應商入口網站 - 外部隔離入口、PO協作、雙向溝通
架構特色
- 前後端分離：React前端與Express後端透過RESTful API通信
- 模組化設計：每個功能模組獨立開發且可擴充
- 權限管理：基於角色的細粒度權限控制系統
- 即時通知：整合LINE API進行異常警報與簽核推送
- 雙層快取：Redis作為主要快取，Memory Cache作為備援
- 資料完整性：使用Prisma ORM確保類型安全與資料關聯
- 国际化支持：預設支援TWD幣別，可擴充多語系
資料流程
1. 前端透過API服務層(api.ts)向後端發送請求
2. 後端Express路由接收請求並進行權限驗證
3. Prisma Client執行資料庫操作（CRUD）
4. 部分複雜業務邏輯（如員工編號生成、AI預測）直接在路由處理器中實作
5. 結果透過JSON格式返回前端
6. 前端React組件接收資料並更新UI狀態
7. LINE訊息通知透過非同步處理發送，不影響主要請求回應
部署說明
1. 後端：cd backend && npm install && npx prisma generate && npx prisma db push && npm run dev
2. 前端：cd frontend && npm install && npm run dev
3. 後端預設運行於 http://localhost:5000
4. 前端預設運行於 http://localhost:5173
此系統採用現代網頁開發最佳實踐，具備良好的擴充性和維護性，適合製造業企業進行數位轉型。