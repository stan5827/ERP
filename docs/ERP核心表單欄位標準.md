# ERP 核心表單欄位標準化文件

> 版本：v1.1  
> 更新日期：2026-03-27  
> 維護單位：資訊管理部

---

## 零、系統優化記錄 (2026-03-27)

### 後端優化

| 項目 | 檔案 | 說明 |
|------|------|------|
| SKU 建立重構 | `SkusController.cs` | 改用 `[FromBody]` + 強型別 DTO，移除手動 JSON Deserialize |
| IQC 庫存邏輯修正 | `GoodsReceiptsController.cs` | 只對 `IqcRequired=false` 的品項直接入庫，避免重複計算 |
| 客戶信用額度檢查 | `SalesOrdersController.cs` | 建立訂單前檢查客户未結帳單+新訂單金額是否超過信用額度 |
| API 日誌監控 | `RequestLoggingMiddleware.cs` | 新增請求日誌 Middleware，記錄每個 API 呼叫 |
| 操作稽核紀錄 | `PermissionAuthorizeFilter.cs` | 新增 AuditLogFilter，自動記錄所有 CRUD 操作 |
| 權限授權框架 | `PermissionAuthorizeFilter.cs` | 新增 `[PermissionAuthorize]` 屬性與授權過濾器 |

### 前端優化

| 項目 | 檔案 | 說明 |
|------|------|------|
| HR 表單驗證強化 | `HR.tsx` | 新增員工表單加入手機號碼驗證 (`/^09\d{8}$/`) |
| 離職功能 | `HR.tsx` | 新增員工離職按鈕與處理 Modal |
| 請假/出勤功能 | `HR.tsx` | 修復「新增請假」「新增出勤」按鈕缺少 onClick 問題 |
| BOM 表單建立 | `RD.tsx` | 新增 BOM 表管理的新增表單與功能 |

---

## 一、物料清單 (BOM) 欄位標準

### 1.1 BOM 主檔欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 成品料號 | product_no | String | Y | 唯一識別碼，對應 SKU 主檔 |
| 原料料號 | component_no | String | Y | 組成零件的料號 |
| 原料名稱 | component_name | String | Y | 零件名稱 |
| 規格 | specification | String | N | 零件規格描述 |
| 單位用量 | qty_per_assembly | Decimal | Y | 每單位成品所需數量 |
| 損耗率 | scrap_rate | Decimal | N | 百分比，預設 0% |
| 版本編號 | version | String | Y | BOM 版本，如 V1.0 |
| 生效日期 | effective_date | Date | Y | BOM 開始生效日期 |
| 失效日期 | expiry_date | Date | N | BOM 停止生效日期 |
| 階層 | level | Int | Y | BOM 階層深度，根為 0 |
| 低階碼 | low_level_code | Int | N | MRP 展開用最低階層 |
| 是否虛擬件 | is_phantom | Boolean | N | Y=不領料直接展開 |
| 備註 | remark | String | N | 備註說明 |

### 1.2 BOM 變更單欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 變更單號 | ecn_no | String | Y | ECN 唯一識別碼 |
| 變更類型 | change_type | Enum | Y | replace/add/remove |
| 變更原因 | change_reason | String | Y | 變更原因說明 |
| 申請日期 | request_date | Date | Y | 申請日期 |
| 生效日期 | effective_date | Date | Y | 預計生效日期 |
| 審批狀態 | status | Enum | Y | draft/approved/implemented |

---

## 二、庫存管理表單欄位標準

### 2.1 庫存主檔 (SKU) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 品項代碼 | sku_number | String | Y | 唯一識別碼 |
| 品項名稱 | sku_name | String | Y | 品項名稱 |
| 規格型號 | specification | String | N | 規格描述 |
| 單位 | unit | String | Y | 存單位，如 PCS、KG |
| 類別 | category | String | N | 物料分類 |
| 安全庫存水位 | safety_stock | Int | Y | 最低補貨點 |
| 最高庫存量 | max_stock | Int | N | 最大庫存限制 |
| 當前庫存量 | qty_on_hand | Int | Y | 現有庫存數量 |
| 庫別 | warehouse_id | String | Y | 所屬倉庫 |
| 儲位 | location | String | N | 儲位編號 |
| 補貨點 (ROP) | reorder_point | Int | N | 自動補貨觸發點 |
| 前置時間 | lead_time_days | Int | N | 採購前置天數 |
| 標準成本 | standard_cost | Decimal | N | 標準單位成本 |
| 狀態 | status | Enum | Y | active/inactive |

### 2.2 批號庫存欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 批號 | lot_number | String | Y | 批次識別碼 |
| 品項代碼 | sku_number | String | Y | 關聯 SKU |
| 批號數量 | lot_qty | Int | Y | 該批號庫存數量 |
| 製造日期 | manufacture_date | Date | N | 生產日期 |
| 有效日期 | expiry_date | Date | N | 有效期限 |
| 倉庫 | warehouse_id | String | Y | 所屬倉庫 |
| 儲位 | location | String | N | 儲位 |
| 批號狀態 | status | Enum | Y | active/used/expired |

### 2.3 序號庫存欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 序號 | serial_number | String | Y | 唯一序號 |
| 品項代碼 | sku_number | String | Y | 關聯 SKU |
| 倉庫 | warehouse_id | String | Y | 所屬倉庫 |
| 儲位 | location | String | N | 儲位 |
| 批號 | lot_number | String | N | 關聯批號 |
| 序號狀態 | status | Enum | Y | in_stock/sold/in_warranty/scrapped |
| 保固到期日 | warranty_expiry | Date | N | 保固截止日 |

### 2.4 庫存異動記錄欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 異動單號 | trx_number | String | Y | 異動唯一識別碼 |
| 異動類型 | trx_type | Enum | Y | 領料/退料/補料/調撥/入庫/出庫 |
| 品項代碼 | sku_number | String | Y | 關聯 SKU |
| 異動數量 | qty_change | Int | Y | 數量變化 (+/-) |
| 異動前庫存 | qty_before | Int | Y | 異動前數量 |
| 異動後庫存 | qty_after | Int | Y | 異動後數量 |
| 批號 | lot_number | String | N | 異動批號 |
| 關聯單據 | ref_doc_type | String | Y | 來源單據類型 |
| 關聯單號 | ref_doc_number | String | Y | 來源單據號碼 |
| 異動日期 | trx_date | DateTime | Y | 異動時間 |
| 異動人 | handler | String | Y | 執行人員 |
| 備註 | remark | String | N | 異動說明 |

---

## 三、採購/銷售訂單欄位標準

### 3.1 採購訂單 (PO) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 採購單號 | po_number | String | Y | 採購單唯一識別 |
| 供應商編號 | supplier_code | String | Y | 供應商主檔編號 |
| 供應商名稱 | supplier_name | String | Y | 供應商全名 |
| 統一編號 | supplier_tax_id | String | N | 供應商統一編號 |
| 交易日期 | order_date | Date | Y | 訂單日期 |
| 預計交期 | expected_date | Date | Y | 預計到貨日期 |
| 付款條件 | payment_terms | String | Y | 如月結 30 天 |
| 幣別 | currency | String | Y | 幣別代碼 |
| 匯率 | exchange_rate | Decimal | N | 外幣匯率 |
| 稅別 | tax_type | Enum | Y | 應稅/免稅 |
| 金額 | total_amount | Decimal | Y | 訂單總金額 |
| 稅額 | tax_amount | Decimal | Y | 稅額 |
| 簽核狀態 | status | Enum | Y | draft/approved/closed |
| 採購人員 | buyer | String | Y | 負責採購員 |
| 成本中心 | cost_center | String | N | 歸屬成本中心 |
| 備註 | remark | String | N | 訂單備註 |

### 3.2 採購明細欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 項次 | line_number | Int | Y | 明細項次 |
| 品項編號 | sku_number | String | Y | 採購料號 |
| 品名規格 | description | String | Y | 品名與規格 |
| 數量 | quantity | Decimal | Y | 採購數量 |
| 單位 | unit | String | Y | 單位 |
| 單價 | unit_price | Decimal | Y | 單價 |
| 小計 | line_total | Decimal | Y | 數量×單價 |
| 稅別 | tax_type | Enum | Y | 應稅/免稅 |
| 預計交期 | expected_date | Date | N | 分批交貨日期 |
| 已交數量 | received_qty | Decimal | Y | 已交貨數量 |
| 狀態 | status | Enum | Y | partial/completed |

### 3.3 銷售訂單 (SO) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 訂單單號 | so_number | String | Y | 銷售訂單唯一識別 |
| 客戶編號 | customer_code | String | Y | 客戶主檔編號 |
| 客戶名稱 | customer_name | String | Y | 客戶全名 |
| 客戶 PO 號 | customer_po_number | String | N | 客戶採購單號 |
| 訂單日期 | order_date | Date | Y | 訂單日期 |
| 預計交期 | customer_due_date | Date | Y | 客戶要求交期 |
| 確認交期 | confirmed_delivery_date | Date | N | 確認可交日期 |
| 付款條件 | payment_terms | String | Y | 收款條件 |
| 貿易條件 | trade_terms | String | N | Incoterms 如 CIF |
| 幣別 | currency | String | Y | 幣別 |
| 匯率 | exchange_rate | Decimal | N | 匯率 |
| 小計 | subtotal | Decimal | Y | 未稅金額 |
| 稅額 | tax_amount | Decimal | Y | 稅額 |
| 總金額 | total_amount | Decimal | Y | 含稅總額 |
| 運費 | freight | Decimal | N | 運費 |
| 訂金 | advance_deposit | Decimal | N | 預收訂金 |
| 訂單狀態 | status | Enum | Y | draft/confirmed/partial/shipped/closed |
| 業務人員 | salesperson | String | Y | 負責業務 |
| 部門 | department | String | N | 所屬部門 |
| 信用檢核 | credit_checked | Boolean | Y | Y=已通過信用審核 |
| 備註 | remark | String | N | 訂單備註 |

### 3.4 銷售明細欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 項次 | line_number | Int | Y | 明細項次 |
| 品項編號 | product_id | String | Y | 產品料號 |
| 品名 | product_name | String | Y | 產品名稱 |
| 客戶料號 | customer_item_code | String | N | 客戶端料號 |
| 數量 | quantity | Decimal | Y | 訂購數量 |
| 單位 | unit | String | Y | 單位 |
| 單價 | unit_price | Decimal | Y | 單價 |
| 折扣率 | discount_rate | Decimal | N | 折扣百分比 |
| 小計 | subtotal | Decimal | Y | 金額小計 |
| 已出數量 | delivered_qty | Decimal | Y | 已出貨數量 |
| 待出數量 | backorder_qty | Decimal | Y | 待出貨數量 |
| 稅碼 | tax_code | String | N | 稅碼 |
| 稅率 | tax_rate | Decimal | N | 税率 |
| 倉庫 | warehouse_id | String | N | 出貨倉庫 |
| BOM 版本 | bom_version | String | N | 生產 BOM 版本 |
| 預計開工日 | planned_start_date | Date | N | 預計開工日 |
| 預計完工日 | planned_finish_date | Date | N | 預計完工日 |
| 需生產 | need_manufacturing | Boolean | N | 是否需生成工單 |
| 需委外 | need_outsourcing | Boolean | N | 是否需委外加工 |
| 備註 | line_remark | String | N | 明細備註 |

---

## 四、財務核算表單欄位標準

### 4.1 會汁科目主檔欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 科目編號 | account_code | String | Y | 會計科目代碼 |
| 科目名稱 | account_name | String | Y | 科目名稱 |
| 科目類別 | account_type | Enum | Y | asset/liability/equity/revenue/expense |
| 層級 | level | Int | Y | 科目階層 |
| 上層科目 | parent_code | String | N | 上層科目代碼 |
| 餘額方向 | balance_type | Enum | Y | debit/credit |
| 是否明細科目 | is_detail | Boolean | Y | Y=明細科目 |
| 成本中心適用 | cost_center_applicable | Boolean | N | 是否需輸入成本中心 |
| 狀態 | status | Enum | Y | active/inactive |

### 4.2 固定資產欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 資產編號 | asset_number | String | Y | 固定資產唯一識別 |
| 資產名稱 | asset_name | String | Y | 資產名稱 |
| 規格型號 | specification | String | N | 型號 |
| 製造商 | manufacturer | String | N | 製造廠商 |
| 取得日期 | acquisition_date | Date | Y | 購進日期 |
| 取得成本 | acquisition_cost | Decimal | Y | 購入成本+資本化費用 |
| 耐用年限 | useful_life_years | Int | Y | 預估使用年數 |
| 殘值 | salvage_value | Decimal | Y | 期末殘值 |
| 折舊方法 | depreciation_method | Enum | Y | straight_line/declining_balance |
| 累計折舊 | accumulated_depreciation | Decimal | Y | 累計折舊額 |
| 帳面價值 | book_value | Decimal | Y | 成本-累計折舊 |
| 存放地點 | location | String | Y | 放置地點 |
| 保管部門 | custodian_dept | String | Y | 保管部門 |
| 保管人 | custodian_person | String | N | 保管人 |
| 資產大類 | asset_category | String | Y | 如機械設備、运输設備 |
| 關聯 PO | po_number | String | N | 採購單號 |
| 狀態 | status | Enum | Y | in_use/idle/disposed |

### 4.3 折舊計算欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 期間 | period | String | Y | YYYYMM 格式 |
| 資產編號 | asset_number | String | Y | 關聯資產 |
|本期折舊|depreciation_expense|Decimal|Y|本期提列折舊金額|
| 累計折舊 | accumulated_depreciation | Decimal | Y | 累計總額 |
| 帳面價值 | book_value | Decimal | Y |期末帳面價值|
| 折舊費用科目 | expense_account | String | Y | 折舊費用科目 |
| 成本中心 | cost_center | String | N | 歸屬成本中心 |

---

## 五、人事建檔欄位標準

### 5.1 員工主檔欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 員工編號 | emp_number | String | Y | 員工唯一識別 |
| 中文姓名 | name | String | Y | 中文姓名 |
| 英文姓名 | english_name | String | N | 護照英文名 |
| 性別 | gender | Enum | Y | male/female |
| 出生日期 | birth_date | Date | Y | 生日 |
| 身分證字號 | id_number | String | Y | 身份證字號 |
| 聯絡電話 | phone | String | Y | 聯絡電話 |
| 手機號碼 | mobile | String | Y | 手機 |
| 戶籍地址 | permanent_address | String | Y | 戶籍地址 |
| 通訊地址 | mailing_address | String | N | 通訊地址 |
| 緊急聯絡人 | emergency_contact | String | Y | 緊急聯絡人 |
| 緊急聯絡人電話 | emergency_phone | String | Y | 緊急聯絡人電話 |
| 所屬部門 | department | String | Y | 部門代碼 |
| 職稱 | position | String | Y | 職稱 |
| 職等 | job_level | String | N | 職等 |
| 職務類別 | job_type | Enum | Y | direct/indirect/contract |
| 直屬主管 | supervisor | String | N | 直屬主管編號 |
| 入職日期 | hire_date | Date | Y | 报到日期 |
| 試用期截止日 | probation_end_date | Date | N | 試用期滿日 |
| 薪資計費方式 | pay_type | Enum | Y | monthly/daily/hourly/piece |
| 銀行帳號 | bank_account | String | Y | 薪資轉帳帳號 |
| 銀行代碼 | bank_code | String | Y | 銀行代碼 |
| 勞保投保級別 | labor_insurance_level | Int | N | 勞保投保級別 |
| 健保投保級別 | health_insurance_level | Int | N | 健保投保級別 |
| 勞退提繳率 | labor_pension_rate | Decimal | N | 雇主提繳率 6% |
| 所得稅扣繳 | tax_withholding | String | N | 扣繳方式 |
| 撫養人數 | dependent_count | Int | N | 撫養人數 |
| 在職狀態 | status | Enum | Y | active/inactive/leave |
| 离职日期 | resign_date | Date | N | 離職日期 |
| 离职原因 | resign_reason | String | N | 離職原因 |

### 5.2 出勤相關欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 班表類別 | shift_type | Enum | Y | fixed/rotating/day |
| 加班權限 | overtime_allowed | Boolean | N | 是否可申請加班 |
| 卡號 | card_number | String | N | 刷卡卡號 |
| 特休天數 | annual_leave_balance | Decimal | N | 特休餘額 |
| 補休天數 | compensatory_leave_balance | Decimal | N | 補休餘額 |

### 5.3 學經歷欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 最高學歷 | education_level | String | N | 學歷 |
| 學校名稱 | school_name | String | N | 學校 |
| 科系 | department | String | N | 科系 |
| 畢業年度 | graduate_year | Int | N | 畢業年 |
| 專業證照 | certifications | JSON | N | 證照列表含效期 |
| 經歷 | work_experience | JSON | N | 過往工作經驗 |

---

## 六、生產工單欄位標準

### 6.1 工單主檔欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 工單編號 | wo_number | String | Y | 工單唯一識別 |
| 成品料號 | product_number | String | Y | 生產成品料號 |
| 成品名稱 | product_name | String | Y | 成品名稱 |
| 計畫數量 | planned_qty | Int | Y | 計畫生產數量 |
| 實際產量 | actual_qty | Int | Y | 實際完成數量 |
| 已領料數量 | issued_qty | Int | Y | 已領用數量 |
| 工單狀態 | status | Enum | Y | pending/released/in_progress/completed/closed |
| 優先順序 | priority | Enum | Y | urgent/high/normal/low |
| 預計開工日 | planned_start_date | Date | Y | 預計開工日 |
| 預計完工日 | planned_finish_date | Date | Y | 預計完工日 |
| 實際開工日 | actual_start_date | Date | N | 實際開工日 |
| 實際完工日 | actual_finish_date | Date | N | 實際完工日 |
| 工作中心 | work_center | String | Y | 生產線/工作中心 |
| BOM 版本 | bom_version | String | Y | 使用 BOM 版本 |
| 工藝路線 ID | routing_id | String | N | 關聯工藝路線 |
| 標準工時 | standard_hours | Decimal | N | 標準完成工時 |
| 關聯訂單編號 | so_number | String | N | 關聯銷售訂單 |
| 機台編號 | machine_id | String | N | 指定生產機台 |
| 工單類型 | wo_type | Enum | Y | normal/rework/recycling |
| 備註 | remark | String | N | 備註 |

### 6.2 領料單主檔欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 領料單號 | issue_number | String | Y | 領料單唯一識別 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 領料日期 | issue_date | Date | Y | 領料日期 |
| 領料人 | handler | String | Y | 領料人員 |
| 發料人 | issuer | String | Y | 發料人員 |
| 部門 | department | String | Y | 領料部門 |
| 倉庫 | warehouse_id | String | Y | 發料倉庫 |
| 領料狀態 | status | Enum | Y | draft/issued/cancelled |

### 6.3 領料單明細欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 項次 | line_number | Int | Y | 明細項次 |
| 品項編號 | sku_number | String | Y | 物料料號 |
| 品名 | sku_name | String | Y | 物料名稱 |
| 儲位編號 | location | String | N | 發料儲位 |
| 批號 | lot_number | String | N | 物料批號 |
| 請領數量 | requested_qty | Int | Y | 請領數量 |
| 實領數量 | issued_qty | Int | Y | 實發數量 |
| 缺料標記 | shortage_flag | Boolean | N | 是否欠料 |
| 欠料數量 | shortage_qty | Int | N | 欠料數量 |
| 單位 | unit | String | Y | 單位 |

### 6.3 完工入庫單欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 完工單號 | completion_number | String | Y | 完工單唯一識別 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 成品料號 | product_number | String | Y | 成品料號 |
| 入庫數量 | received_qty | Int | Y | 入庫數量 |
| 入庫日期 | receive_date | Date | Y | 入庫日期 |
| 入庫倉庫 | warehouse_id | String | Y | 入庫倉庫 |
| 批號 | lot_number | String | N | 批次編號 |
| 製造日期 | manufacture_date | Date | N | 製造日期 |
| 有效日期 | expiry_date | Date | N | 有效期限 |
| 品檢結果 | qa_result | Enum | Y | pass/fail/conditional |
| 完工狀態 | status | Enum | Y | draft/qa_pending/received/cancelled |

### 6.4 工藝路線表 (Routing) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 工藝路線編號 | routing_id | String | Y | 工藝路線唯一識別 |
| 產品編號 | product_number | String | Y | 適用產品料號 |
| 版本 | version | String | Y | 工藝版本 |
| 工序序號 | operation_seq | Int | Y | 工序順序 |
| 工序編號 | operation_code | String | Y | 工序代碼 |
| 工序說明 | operation_name | String | Y | 工序名稱 |
| 工作中心 | work_center | String | Y | 所屬工作中心 |
| 機具編號 | machine_id | String | N | 所需機台 |
| 準備時間(分鐘) | setup_time | Decimal | N | 換線準備時間 |
| 加工時間(分鐘) | run_time | Decimal | N | 單件加工時間 |
| 是否 QC 點 | is_qc_point | Boolean | N | Y=此工序需品檢 |
| 優先順序 | priority | Int | N | 工序優先級 |
| 備註 | remark | String | N | 備註說明 |

### 6.5 報工單 (Production Reporting) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 報工單號 | pr_number | String | Y | 報工單唯一識別 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 工序序號 | operation_seq | Int | Y | 報工工序 |
| 工序名稱 | operation_name | String | Y | 工序名稱 |
| 機台編號 | machine_id | String | Y | 作業機台 |
| 操作員 ID | operator_id | String | Y | 作業人員 |
| 班別 | shift | Enum | Y | day/night/swing |
| 報工日期 | report_date | Date | Y | 報工日期 |
| 實際人工工時 | labor_hours | Decimal | Y | 實際人工小時 |
| 實際機台工時 | machine_hours | Decimal | N | 實際機台小時 |
| 完工合格數 | good_qty | Int | Y | 合格數量 |
| 不合格數 | reject_qty | Int | N | 不合格數量 |
| 報廢數 | scrap_qty | Int | N | 報廢數量 |
| 不良原因代碼 | defect_code | String | N | 不良原因 |
| 良率 | yield_rate | Decimal | N | 良率百分比 |
| 備註 | remark | String | N | 報工備註 |

### 6.6 派工單 (Dispatch) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 派工單號 | dispatch_number | String | Y | 派工單唯一識別 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 工作中心 | work_center | String | Y | 派工工作中心 |
| 機台編號 | machine_id | String | Y | 指定機台 |
| 預定開始時間 | planned_start_time | DateTime | Y | 預計開始時間 |
| 預定結束時間 | planned_end_time | DateTime | Y | 預計結束時間 |
| 優先順序 | priority | Enum | Y | urgent/high/normal/low |
| 預估產能負荷% | load_percentage | Int | N | 機台負荷評估 |
| 派工狀態 | status | Enum | Y | pending/assigned/in_progress/completed |
| 備註 | remark | String | N | 備註 |

---

## 七、品質管理表單欄位標準

### 7.1 進料檢驗單 (IQC) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 檢驗單號 | iqc_number | String | Y | IQC 檢驗單唯一識別 |
| 進貨單號 | gr_number | String | Y | 關聯進貨單 |
| 採購單號 | po_number | String | Y | 關聯採購單 |
| 供應商編號 | supplier_code | String | Y | 供應商代碼 |
| 供應商名稱 | supplier_name | String | Y | 供應商名稱 |
| 檢驗日期 | inspection_date | Date | Y | 檢驗日期 |
| 檢驗員 | inspector | String | Y | 檢驗人員 |
| 檢驗水準 | aql_level | String | Y | AQL 等級，如 II、III |
| 抽樣數 | sample_size | Int | Y | 抽樣數量 |
| 允收數 (Ac) | accept_qty | Int | Y | 允收數量 |
| 拒收數 (Re) | reject_qty | Int | Y | 拒收數量 |
| 判定結果 | result | Enum | Y | pass/fail/conditional |
| 特採說明 | conditional_remark | String | N | 特採原因 |
| 附件 | attachment | String | N | 檢驗報告附件 |

### 7.2 製程品檢單 (IPQC) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 檢驗單號 | ipqc_number | String | Y | IPQC 檢驗單唯一識別 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 批次編號 | lot_number | String | Y | 生產批次 |
| 工序別 | operation_name | String | Y | 檢驗工序 |
| 機台編號 | machine_id | String | Y | 機台編號 |
| 檢驗類型 | inspection_type | Enum | Y | first/ Patrol/final |
| 檢驗日期 | inspection_date | Date | Y | 檢驗日期 |
| 檢驗員 | inspector | String | Y | 檢驗人員 |
| 檢驗項目 | item_name | String | Y | 檢驗項目名稱 |
| 規格值 | spec_value | String | Y | 規格說明 |
| 實測值 | measured_value | Decimal | Y | 實測數據 |
| 單位 | unit | String | Y | 單位 |
| 公差上限 | upper_limit | Decimal | N | 規格上限 |
| 公差下限 | lower_limit | Decimal | N | 規格下限 |
| 判定結果 | result | Enum | Y | pass/fail |
| CPK 值 | cpk_value | Decimal | N | 製程能力指數 |
| 異常描述 | defect_description | String | N | 異常說明 |
| 處置建議 | disposition | Enum | Y | continue/reject/hold |
| 備註 | remark | String | N | 備註 |

### 7.3 成品驗收單 (FQC/OQC) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 驗收單號 | fqc_number | String | Y | FQC 驗收單唯一識別 |
| 入庫單號 | completion_number | String | Y | 關聯完工入庫單 |
| 工單編號 | wo_number | String | Y | 關聯工單 |
| 成品料號 | product_number | String | Y | 成品料號 |
| 批次編號 | lot_number | String | Y | 生產批次 |
| 驗收日期 | inspection_date | Date | Y | 驗收日期 |
| 驗收員 | inspector | String | Y | 驗收人員 |
| 抽樣數 | sample_size | Int | Y | 抽樣數量 |
| 合格數 | pass_qty | Int | Y | 合格數量 |
| 包裝檢查 | packaging_check | Enum | Y | pass/fail |
| 標籤檢查 | label_check | Enum | Y | pass/fail |
| 可靠度測試 | reliability_test | String | N | 測試結果 |
| 最終判定 | result | Enum | Y | pass/fail/pending |
| 備註 | remark | String | N | 備註 |

### 7.4 品質追溯矩陣 (Traceability) 欄位

| 欄位名稱 | 欄位代碼 | 資料類型 | 必填 | 說明 |
|---------|---------|---------|------|------|
| 追溯鍵 | trace_key | String | Y | 成品序列號或批號 |
| 成品料號 | product_number | String | Y | 成品料號 |
| 成品名稱 | product_name | String | Y | 成品名稱 |
| 生產日期 | production_date | Date | Y | 生產日期 |
| 原料來源鏈 | material_trace | JSON | Y | 原料批號→供應商→IQC單號 |
| 製程鏈 | process_trace | JSON | Y | 工序→機台→作業員→IPQC結果 |
| 品質歷史 | quality_history | JSON | Y | 所有 QC 判定紀錄 |
| 追溯狀態 | status | Enum | Y | complete/incomplete |

---

## 八、欄位命名規範

### 7.1 命名原則

1. **語義清晰**：欄位名稱需能表達其用途
2. **使用英文**：除非有特殊需求，原則使用英文命名
3. **駝峰式命名**：如 `customerName`、`orderDate`
4. **統一縮寫**：同一專案內使用一致的縮寫

### 7.2 常用縮寫對照

| 縮寫 | 全名 |
|-----|------|
| qty | quantity |
| amt | amount |
| dt | date |
| tm | time |
| no | number |
| cd | code |
| nm | name |
| desc | description |
| qty | quantity |
| addr | address |
| tel | telephone |
| mbl | mobile |

### 7.3 資料類型對照

| 類型 | 說明 | 範例 |
|-----|------|------|
| String | 字串 | "ABC123" |
| Int | 整數 | 100 |
| Decimal | 小數 | 99.50 |
| Date | 日期 | 2026-03-27 |
| DateTime | 日期時間 | 2026-03-27 10:30:00 |
| Boolean | 布林值 | true/false |
| Enum | 列舉值 | draft/approved/closed |
| JSON | JSON 物件 | {"key": "value"} |

---

## 八、版本紀錄

| 版本 | 日期 | 修改內容 | 修改人 |
|-----|------|---------|-------|
| v1.0 | 2026-03-27 | 初始版本 | System |
| v1.1 | 2026-03-27 | 離散製造優化：新增工藝路線、報工單、派工單、領料明細、品質管理(IQC/IPQC/FQC/追溯)欄位 | System |
