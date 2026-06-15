# ERP 系統業務流程完整清單

## 一、核心業務流程圖

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           ERP 系統核心業務流程                                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│   【銷售】    【採購】    【庫存】         【生產】         【人力】                  │
│     │          │          │                │               │                      │
│     ▼          ▼          ▼                ▼               ▼                      │
│  報價單 ───► 請購單 ───► 品號主檔 ───► BOM ───► 工單 ───► 員工資料                 │
│     │          │          │                │               │                      │
│     ▼          ▼          ▼                ▼               ▼                      │
│  銷售訂單 ──► 詢價單 ──► 庫存異動 ──► 工藝路線 ──► 現場報工 ──► 請假/打卡           │
│     │          │          │                │               │                      │
│     ▼          ▼          ▼                ▼               ▼                      │
│  出貨單 ───► 採購單 ───► 領料單 ───► 完工入庫 ──► 薪資核算                       │
│     │          │          │                                                           │
│     ▼          ▼          ▼         【庫存的特殊流程】                            │
│  銷貨發票 ◄── 應付帳款   退料單 ─────────────────────────────────────────►           │
│     │          │          │                                                           │
│     ▼          ▼          ▼                                                           │
│  收款單 ◄──── 匯款通知  補料單                                                       │
│                        │                                                             │
│                        ▼                                                             │
│                     委外加工單                                                       │
│                                                                                     │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 二、各模組業務流程

### 1. 人力資源管理 (HR)

| 流程 | 說明 | 關鍵單據 |
|------|------|---------|
| 員工入職 | 建立員工資料 → 產生員工編號 | 員工資料 |
| 請假管理 | 請假申請 → 主管核准 | LeaveRequest |
| 出勤打卡 | 上班打卡 → 下班打卡 → 計算加班 | AttendanceRecord |
| 薪資核算 | 基本薪 → 加班/獎金 → 扣款 → 異常處理 → AI驗證 → 發放 | Payroll, PayrollAnomaly |
| AI排班 | 根據歷史數據自動排班 | - |

**API Endpoints:**
- `GET/POST /api/hr/employees` - 員工管理
- `PATCH /api/hr/employees/:id/terminate` - 離職
- `PATCH /api/hr/employees/:id/rehire` - 重新入職
- `GET/POST /api/hr/leaves` - 請假管理
- `GET/POST /api/hr/attendances` - 出勤打卡
- `GET/POST /api/hr/payrolls` - 薪資核算
- `POST /api/hr/ai/schedule` - AI 排班

---

### 2. 採購管理 (Procurement)

```
請購單(PR) → 詢價單(RFQ) → 採購單(PO) → 供應商確認 → ASN發貨 → 進貨點收(GR) → 應付帳款(AP) → 匯款(EDI 820)
     │              │            │              │              │           │
     ▼              ▼            ▼              ▼              ▼           ▼
  部門需求      向供應商      選擇供應商      變更管理        物流追蹤    發票勾稽
  提出          詢價          議價           (數量/交期)     (IoT監控)   (三方匹配)
```

| 流程 | 說明 | 關鍵 API |
|------|------|---------|
| 請購單 | 部門提出採購需求，可由 BOM 自動產生 | `/api/procurement/prs` |
| 詢比議價 | 向多家供應商詢價、報價、比價 | `/api/procurement/rfqs` |
| 採購訂單 | 確認供應商後建立 PO，含變更管理 | `/api/procurement/pos` |
| 進貨點收 | 供應商發貨 → ASN 到貨 → 品檢 → GR | `/api/procurement/grs` |
| 應付帳款 | 發票勾稽 → 付款 → 匯款通知 | `/api/procurement/aps` |
| 電子發票 | EDI 810 格式提交發票 | `/api/supplier/invoices` |

---

### 3. 庫存管理 (Inventory)

#### 庫存異動流程
```
入庫 ◄─────────────────────────────────────────────────────────────
 │                                                                   │
 ▼                                                                   │
 採購進貨 / 委外完工 / 生產完工 / 退料 / 補料                         │
 │                                                                   │
 ▼                                                                   │
 庫存異動紀錄 (InventoryTransaction)                                  │
   - 異動類型: 入庫/出庫/領料/退料/補料/調帳                          │
   - 關聯單據                                                         │
 │                                                                   │
 ▼                                                                   ▼
庫存數量更新 ←────────────────────────────────── 批號庫存管理        
                                                                 │
 │                                    ┌────────────────────────────┘
 ▼                                    ▼                              
 領料單 ──► 發料扣帳                  FIFO / 效期管理                   
```

#### 庫存特殊流程

| 流程 | 說明 | 關鍵 API |
|------|------|---------|
| 品號管理 | 建立/維護品號主檔，含成本、安全存量、ROP | `/api/inventory/skus` |
| 庫存異動 | 所有庫存增減都有異動紀錄 | `/api/inventory/transactions` |
| 批號管理 | 支援 FIFO、效期追蹤 | `/api/inventory/lots` |
| **領料單** | 工單 → BOM建議 → 發料扣帳 | `/api/inventory/issues` |
| **退料單** | 生產剩餘料件退還倉庫 | `/api/inventory/returns` |
| **補料單** *(新增)* | BOM理論用量 vs 實際耗用 → 自動計算補料 | `/api/inventory/supplements` |
| **委外加工單** *(新增)* | 發料 → 委外加工 → 收貨入庫 | `/api/inventory/subcontracts` |
| 盤點 | AI 點數、信心指數 | `/api/inventory/stocktakes` |
| AS/RS | 自動化倉儲任務 | `/api/inventory/asrs-tasks` |

---

### 4. 生產製造 (MES)

```
建立 BOM ──► 建立工藝路線 ──► 建立工單 ──► 領料 ──► 現場報工 ──► 完工
   │              │              │              │           │              │
   ▼              ▼              ▼              ▼           ▼              ▼
多版本管理      工序順序        狀態流轉       扣帳        產量回報        不良品
ECN變更         準備/加工時間   pending→       在庫        機台進度        紀錄
版本歷史        外協標記        released→                                      
                                in_progress→                                  
                                completed→                                   
                                closed                                       
```

| 流程 | 說明 | 關鍵 API |
|------|------|---------|
| 工作中心 | 產能、效率管理 | `/api/mes/work-centers` |
| 工序管理 | 準備/加工時間、費用、外協標記 | `/api/mes/operations` |
| 工藝路線 | 工序順序、搬運/等待時間 | `/api/mes/routings` |
| BOM | 多版本管理、ECN 變更追蹤 | `/api/mes/boms` |
| 工單 | 狀態：pending → released → in_progress → completed → closed | `/api/mes/work-orders` |
| 現場報工 | 產量、不良品、機台進度 | `/api/mes/wip` |
| 品質檢驗 | IQC / IPQC / OQC | `/api/mes/qa/*` |

---

### 5. 銷售管理 (Sales)

```
報價單 ──► 銷售訂單 ──► 出貨單 ──► 銷貨發票 ──► 收款
   │            │            │            │            │
   ▼            ▼            ▼            ▼            ▼
 客戶詢價    訂單確認    批號追溯    稅額計算    帳款勾稽
 折扣率      出貨排程    OQC檢驗    應稅/免稅   沖銷帳款
```

| 流程 | 說明 | 關鍵 API |
|------|------|---------|
| 客戶管理 | 客戶主檔、帳期、發票地址 | `/api/sales/customers` |
| 報價單 | 報價 → 折扣 → 轉訂單 | `/api/sales/quotes` |
| 銷售訂單 | 訂單確認、交期追蹤 | `/api/sales/orders` |
| 出貨單 | 批號追溯、出貨追蹤 | `/api/sales/deliveries` |
| 銷貨發票 | 稅額計算 | `/api/sales/invoices` |
| 收款 | 沖銷應收帳款 | `/api/sales/receipts` |

---

### 6. 供應商管理 (Supplier Portal)

| 流程 | 說明 | 關鍵 API |
|------|------|---------|
| 供應商登入 | 供應商入口網站 | `/api/supplier/login` |
| 供應商報價 | 回覆 RFQ | `/api/supplier/quotes` |
| ASN 發貨 | EDI 856 提前通知 | `/api/supplier/asn` |
| 變更請求 | PO 交期/數量變更 | `/api/supplier/pos/:id/change-request` |
| 品質客訴 | 8D 報告回覆 | `/api/supplier/complaints` |

---

### 7. 品質管理

| 流程 | 說明 |
|------|------|
| IQC 進料檢驗 | 抽驗、AQL |
| IPQC 製程檢驗 | 溫度、CPK |
| OQC 出貨檢驗 | 追溯資訊 |
| 品質客訴 | 8D 報告 |
| NCR 不合格報告 | 處置方式 |
| CAPA 糾正措施 | 根本原因分析 |

---

### 8. 預算管理

| 流程 | 說明 |
|------|------|
| 銷售預測 | 銷售額預測 |
| 材料成本預測 | 原材料成本趨勢 |
| 人力預算 | 人員編列、薪資 |
| 費用預算 | 部門營業費用 |
| CAPEX | 資本支出投資報酬率 |

---

### 9. 數位轉型

| 功能 | 說明 |
|------|------|
| 碳盤查 (ESG) | GHG 排放紀錄 |
| AI 洞察 | 異常偵測、預測維護 |
| AI 異常警報 | LINE 推播通知 |

---

## 三、庫存管理特殊流程說明

### 補料單流程 (Material Supplement) *(本系統新增)*
```
選擇工單 → 系統自動計算 BOM理論用量 vs 實際耗用 → 顯示超耗物料 → 填寫補料數量 → 建立補料單 → 補料入庫
```

**API:**
- `GET /api/inventory/supplements` - 取得補料單列表
- `GET /api/inventory/supplements/wo/:woNumber` - 取得補料建議
- `POST /api/inventory/supplements` - 建立補料單
- `POST /api/inventory/supplements/:id/issue` - 執行補料入庫

### 委外加工單流程 (Subcontract Order) *(本系統新增)*
```
選擇工單 → 選擇委外工序(BOM) → 選擇供應商 → 建立委外單 → [發料] → [加工中] → [收貨] → [結案]
```

**API:**
- `GET /api/inventory/subcontracts` - 取得委外單列表
- `GET /api/inventory/subcontracts/wo/:woNumber` - 取得委外建議
- `POST /api/inventory/subcontracts` - 建立委外單
- `PUT /api/inventory/subcontracts/:id` - 更新委外單
- `POST /api/inventory/subcontracts/:id/issue` - 執行委外發料
- `POST /api/inventory/subcontracts/:id/receive` - 委外加工完成收貨
- `POST /api/inventory/subcontracts/:id/close` - 關閉委外單

---

## 四、系統技術架構

| 層面 | 技術 |
|------|------|
| 前端 | React + TypeScript + Vite |
| 後端 | Express.js + TypeScript |
| 資料庫 | SQL Server + Prisma ORM |
| API 風格 | RESTful |
| 整合功能 | LINE 訊息推播、EDI 電子資料交換 |

---

## 五、資料模型總覽

### 主要資料模型

| 模組 | 模型 |
|------|------|
| HR | Employee, LeaveRequest, AttendanceRecord, Payroll, PayrollAnomaly |
| 採購 | PurchaseRequisition, Rfq, PurchaseOrder, GoodsReceipt, Asn, AccountsPayable |
| 庫存 | Sku, InventoryTransaction, InventoryLot, MaterialIssue, MaterialReturn, MaterialSupplement, SubcontractOrder |
| 生產 | WorkCenter, Operation, Routing, Bom, WorkOrder, WipReport |
| 銷售 | Customer, Quote, SalesOrder, DeliveryOrder, SalesInvoice, CashReceipt |
| 供應商 | Supplier, SupplierUser, SupplierQuote, QualityComplaint |
| 品質 | NonConformanceReport, SupplierCapa, IqcReport, IpqcReport, OqcTrace |
| 預算 | SalesForecast, MaterialCostForecast, LaborBudget, ExpenseBudget, CapexBudget |

---

*最後更新: 2026-03-16*
