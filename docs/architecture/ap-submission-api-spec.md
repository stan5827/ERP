# 供應商請款送審（AP Submission）資料模型與 API 規格草案

> **狀態：** 後端 API 已實作但尚未對外啟用；財務／採購／資安政策仍待確認，migration 未套用至業務資料庫
> **範圍：** 供應商提交發票資料、內部審核、核准後建立既有正式 AP
> **不代表：** Portal 已完成瀏覽器／端到端驗收、財務政策已核准或可對外上線

## 1. 目標與設計原則

供應商可透過已驗證的 Portal 提交請款與佐證資料，並追蹤補件及審核結果；內部人員負責三方核對與決策。核准前的資料屬於送審工作資料，不是正式應付帳款；只有核准交易成功後，才建立既有 `AccountsPayable`，並以其作為後續付款、帳齡與會計流程的唯一正式 AP 來源。

設計遵循以下原則：

1. **不建立第二套正式帳務**：送審記錄保存申請、修訂和審核證據；核准後連結至既有 `AccountsPayable`、`PaymentBatch`、`PaymentRecord`。
2. **信任伺服器端身分**：供應商 ID、提交者、審核者及狀態由後端決定，不接受 Portal 自行指定。
3. **提交內容可追溯**：每次送出形成不可變的修訂快照；退回後的修改與再送審不得覆寫舊快照或審核意見。
4. **核准與會計過帳分離**：核准只建立／連結正式 AP，不呼叫會直接過帳傳票的 `CreateVoucherFromAPAsync`，也不建立付款批次。
5. **敏感附件安全預設**：附件使用私有儲存；掃描狀態非 clean 時不可下載或進入核准。
6. **逐階段啟用**：本草案假設初期限制單一 PO、單一收貨／驗收單及 PO 同幣別；任何偏離需先完成財務政策確認。

## 2. 現況與範圍基線

**Discovery method：** 定向檢視既有 `AccountsPayable`／付款／PO／收貨實體及 AP 查詢服務，並檢查供應商 Portal 身分、ASN、CAPA 與附件 API 模式及既有供應商入口財務規劃。

**Relevant capability areas discovered：** 3（正式 AP／付款、採購收貨與驗收、供應商 Portal／附件）。

**Items in scope：** 1 個供應商請款送審流程，涵蓋供應商送件、內部審核、附件安全及核准後建立正式 AP。

### 2.1 已有系統能力

- 正式 AP 使用 `AccountsPayable`，具有供應商、PO／收貨單、供應商發票號碼／日期、幣別、未稅／稅額／總額、到期日、付款與三方勾稽狀態。
- `AccountsPayableItem` 可記錄 SKU、數量、單價及金額；`PaymentRecord` 關聯既有付款批次。
- `PurchaseOrder` 及 `GoodsReceipt`／`GoodsReceiptItem` 保存採購及收貨／驗收來源。
- Portal 已有獨立 Keycloak 驗證及後端 `SupplierPortalIdentityContext` 供應商範圍；供應商 ASN、CAPA 已有 Portal API 模式。
- CAPA 附件已驗證私有物件儲存與惡意程式掃描流程，但其 CAPA 關聯資料不可直接充當請款附件記錄。
- AP 查詢基礎已修正為以 AP、已過帳付款及有效借項計算；本文件不重複定義或另建付款帳本。

### 2.2 本次草案涵蓋

| 項目 | 納入 | 說明 |
|---|---:|---|
| 供應商建立、儲存及查詢請款草稿 | 是 | 僅查詢登入者所屬供應商案件。 |
| 提交、補件、重送、取消與內部審核 | 是 | 保留不可變修訂及審核歷程。 |
| 發票欄位與 PO／收貨來源關聯 | 是 | 初期採單一 PO、單一收貨／驗收單的工作假設。 |
| 私有憑證附件 | 是 | 掃描 clean 才可進入審核；檔案規則仍需業務確認。 |
| 三方核對結果與差異處理 | 部分 | 定義狀態及資料契約；金額、稅額容差與核准政策待財務確認。 |
| 核准後建立正式 AP | 是 | 同一審核交易內冪等建立並回寫 AP 關聯。 |
| Portal 付款進度／付款通知 | 否 | 後續功能，應透過供應商範圍 API 讀取正式 AP 及 posted 付款資料。 |
| 電子發票政府平台介接、稅額自動計算、外幣匯率換算 | 否 | 等財務政策、匯率來源及法規契約確認後另行規劃。 |
| 供應商建立付款批次、過帳傳票或修改正式 AP | 否 | 僅由內部財務系統流程負責。 |

## 3. 使用者情境與驗收

### 情境 1 — 供應商建立並送出請款（P1）

供應商從自有且可請款的 PO／收貨資料選取來源，輸入發票識別及金額，附上必要憑證，檢視核對摘要後送審。

**驗收**

1. Given 供應商已完成 Portal 驗證，When 查詢來源單據，Then 僅回傳該供應商、狀態允許請款且尚未被其他有效請款占用的來源。
2. Given 請款資料或附件不完整，When 提交，Then 系統拒絕送審並指出可修正的欄位；不得建立正式 AP。
3. Given 請款有效且附件掃描通過，When 供應商送審，Then 系統凍結該修訂、建立審核待辦並回傳提交時間及案件編號。

### 情境 2 — 內部審查、退回及重送（P1）

財務／採購人員檢視發票、來源單據、核對差異及歷程，選擇退回補件、拒絕或核准。

**驗收**

1. Given 案件處於 submitted，When 審核者退回並提供原因，Then 案件轉為 returned、原因可供供應商查看，且已提交修訂仍不可變。
2. Given 案件為 returned，When 供應商修改並重送，Then 系統建立新的修訂快照，舊內容與退回意見仍可追溯。
3. Given 案件存在未解決的核對差異或附件非 clean，When 審核者嘗試核准，Then 系統依尚待確認的財務規則拒絕核准，不建立正式 AP。

### 情境 3 — 核准後與既有 AP 接續（P1）

核准完成後，內部財務可從既有 AP 流程繼續驗證、付款及過帳；Portal 只能讀取本供應商的申請處理結果。

**驗收**

1. Given 案件核准，When 核准交易完成，Then 恰有一筆正式 AP 連結至該申請，原始發票及 PO／收貨來源可互相追溯。
2. Given 核准請求因重試而重送，When 同一案件再次處理，Then 系統回傳原核准結果，不建立重複 AP。
3. Given 案件尚未核准，When 查詢正式 AP 或付款，Then 不會因送審草稿而產生 AP、付款紀錄或會計傳票。

## 4. 功能需求

- **REQ-001**：系統 MUST 僅依驗證後的 Portal 身分解析 `SupplierId` 與提交者；拒絕由 request body 覆寫這些值。
- **REQ-002**：系統 MUST 僅允許供應商選取其名下、狀態符合請款政策且具有有效收貨／驗收證據的 PO／收貨來源。
- **REQ-003**：系統 MUST 保存發票號碼、發票日期、幣別、未稅金額、稅額、含稅總額及來源單據識別；總額及精度需符合已核定的財務政策。
- **REQ-004**：系統 MUST 以草稿及不可變送審修訂保存請款資料，不得在送出後原地覆寫歷史內容。
- **REQ-005**：系統 MUST 為送審、退回、補件重送、拒絕、取消及核准保存不可變的操作者、時間、結果與原因歷程。
- **REQ-006**：系統 MUST 在送審前驗證必要附件存在且狀態為 clean；掃描服務不可用時採 fail-closed，不得將附件視為安全。
- **REQ-007**：系統 MUST 產生可供內部審核及 Portal 查詢的三方核對結果與差異清單；任何自動容差及放行規則須受財務設定政策約束。
- **REQ-008**：系統 MUST 在內部核准的原子交易中建立或取得唯一正式 `AccountsPayable`，保存來源連結及申請至 AP 的唯一關聯。
- **REQ-009**：核准流程 MUST NOT 建立 `PaymentBatch`／`PaymentRecord`、呼叫 AP 傳票自動過帳，或允許供應商直接修改正式 AP。
- **REQ-010**：系統 MUST 防止同一供應商發票被重複提交或核准，並讓重試的建立、送審及核准請求具冪等性。
- **REQ-011**：系統 MUST 使用版本／併發控制避免供應商與內部人員基於過期版本同時更新案件。
- **REQ-012**：系統 MUST 對所有 Portal 清單、明細、附件與操作執行供應商範圍檢查；跨供應商資源回應不得洩露資料存在與否。
- **REQ-013**：系統 MUST 讓員工審核 API 使用員工身分及明確財務／採購權限，並保存審核者識別。
- **REQ-014**：系統 MUST 對核准後的 AP 顯示正式 AP 編號及有限摘要；供應商付款查詢須另由具供應商範圍的唯讀 API 實作。

## 5. 建議資料模型

以下為新增工作流程資料的邏輯模型；目前已建立對應 EF Core entities 與 migration。正式 AP、付款與會計仍重用現有模型，migration 尚未套用至任何業務資料庫。

### 5.1 `SupplierApSubmission` — 請款案件主檔

一筆代表一張供應商發票的完整送審生命週期；不複製正式 AP 的付款狀態。

| 欄位 | 型別／規則 | 用途 |
|---|---|---|
| `Id` | UUID，PK | 案件識別。 |
| `SubmissionNumber` | 字串，必要、唯一 | ERP 產生的可讀案件編號；不可由供應商指定。 |
| `SupplierId` | UUID，FK `Supplier`，必要 | 建立後不可由客戶端變更。 |
| `SubmittedBySupplierUserId` | UUID，FK `SupplierUser`，必要 | 首次建立者；每次提交者另存 revision。 |
| `Status` | 字串／受控狀態，必要 | `draft`、`submitted`、`returned`、`approved`、`rejected`、`cancelled`。審核中以 `submitted` 表示，MVP 不另設指派狀態。 |
| `CurrentRevisionNumber` | 整數，必要 | 指向目前草稿或最近提交修訂。 |
| `GeneratedAccountsPayableId` | UUID，nullable，唯一 FK `AccountsPayable` | 僅核准成功後填入；是送審轉正式 AP 的冪等關聯。 |
| `IdempotencyKey` | 字串，必要 | 由建立請求提供；與 `SupplierId` 組合唯一，保存建立冪等性。 |
| `IdempotencyRequestHash` | SHA-256 字串，必要 | 防止同一冪等鍵被不同建立內容重用。 |
| `SupplierInvoiceNumberKey` | 字串，可空 | 發票號碼正規化鍵；同一供應商於全部案件狀態不可重複，草稿尚未填發票時為空。 |
| `CreatedAt`、`UpdatedAt` | UTC timestamp | 案件稽核時間。 |
| `ConcurrencyToken` | 資料庫併發 token／整數版本 | 保護草稿編輯及審核狀態轉換。 |

建議索引：`SubmissionNumber` 唯一；`(SupplierId, CreatedAt)`；`(SupplierId, Status, CreatedAt)`；`GeneratedAccountsPayableId` 唯一且可空；`(SupplierId, IdempotencyKey)` 唯一。

### 5.2 `SupplierApSubmissionRevision` — 請款內容修訂

每次提交／重送產生一份不可變快照。尚未提交的 current draft 可編輯；按下提交時，寫入完整快照並鎖定。退回案件開始修改時建立下一版草稿，不能變更先前已提交版本。

| 欄位 | 型別／規則 | 用途 |
|---|---|---|
| `Id` | UUID，PK | 修訂識別。 |
| `SubmissionId` | UUID，FK，必要 | 所屬案件。 |
| `RevisionNumber` | 正整數，必要 | 同一案件內遞增；`(SubmissionId, RevisionNumber)` 唯一。 |
| `SupplierInvoiceNumber` | 字串，必要，最長 50 | 供應商發票識別；同供應商唯一性由案件主檔的正規化鍵保護。正規化格式仍需財務確認。 |
| `SupplierInvoiceDate` | 日期，必要 | 發票日期，不含時區。 |
| `Currency` | ISO 4217 字串，必要 | MVP 建議須與 PO 幣別相同，不接受供應商提交匯率。 |
| `InvoiceAmount` | decimal，必要 | 發票未稅金額。 |
| `TaxAmount` | decimal，必要 | 發票申報稅額；系統不得在政策確認前自行重算或推定稅率。 |
| `TotalAmount` | decimal，必要 | 發票含稅總額；驗證關係依核准精度政策。 |
| `PurchaseOrderId` | UUID，FK，必要（MVP） | 關聯單一 PO。 |
| `GoodsReceiptId` | UUID，FK，必要（MVP） | 關聯單一已收貨／驗收單；須驗證該單據屬於同一供應商與 PO。 |
| `SubmittedBySupplierUserId` | UUID，FK，提交時必要 | 此版提交人。 |
| `SubmittedAt` | UTC timestamp，可空 | 草稿可空；送出後必要。 |
| `ContentHash` | 字串，可空 | 建議保存正規化提交內容雜湊，以支援稽核完整性。 |

建議額外的 `SupplierApSubmissionRevisionLine` 保存單據品項分攤：

| 欄位 | 型別／規則 | 用途 |
|---|---|---|
| `Id` | UUID，PK | 明細識別。 |
| `RevisionId` | UUID，FK，必要 | 所屬修訂。 |
| `GoodsReceiptItemId` | UUID，FK，必要 | 佐證收貨／驗收來源；必須隸屬修訂所選的 `GoodsReceiptId`。 |
| `PurchaseOrderItemId` | UUID，nullable FK | 若現有收貨資料可唯一對應 PO 明細則保存；目前 `GoodsReceiptItem` 未見明確 PO 明細 FK，不得僅憑客戶端提供值信任。 |
| `InvoicedQuantity` | decimal，必要 | 發票對該收貨品項請款數量。 |
| `UnitPrice` | decimal，必要 | 供應商發票單價。 |
| `NetAmount` | decimal，必要 | 該列未稅金額。 |
| `TaxAmount` | decimal，必要 | 該列稅額；是否逐列拆稅待確認。 |
| `TotalAmount` | decimal，必要 | 該列含稅金額。 |

伺服器應檢查明細加總與修訂表頭金額一致，拒絕重複或不屬於 PO／收貨單的來源明細。不得假設現有 `GoodsReceiptItem` 可無歧義地對應重複 SKU 的 PO 明細；此限制需在實作前三方確認。

### 5.3 `SupplierApSubmissionAttachment` — 請款附件索引

記錄附件與修訂的關聯及安全狀態；實際檔案放在私有物件儲存，不把檔案內容或可公開存取 URL 存入資料庫。

建議欄位：`Id`、`SubmissionId`、`RevisionId`、`OriginalFileName`、`StorageKey`、`ContentType`、`SizeBytes`、`Sha256`、`ScanStatus`、`UploadedBySupplierUserId`、`UploadedAt`、`RevokedAt`。`StorageKey` 僅供後端使用，不回傳 Portal。只允許 clean、未撤銷且屬目前提交修訂的必要附件進入審核；提交後附件不可替換，退回後新增附件須綁定新修訂。

草案初始建議 PDF 為必要憑證、單檔上限 20 MB，並沿用私有儲存與 ClamAV clean gate；檔案類型、數量上限、發票正本／電子發票附件要求須由財務與資安確認。

### 5.4 `SupplierApSubmissionReview` — 不可變審核歷程

每次內部決策新增一筆，不覆寫舊審查；供應商補件及系統狀態轉換也記錄事件或共用既有稽核機制。

建議欄位：`Id`、`SubmissionId`、`RevisionId`、`Action`（`returned`／`rejected`／`approved`）、`ReviewerUserId`、`ReviewerRole`、`Reason`、`MatchResult`、`MatchRuleVersion`、`MatchSnapshot`、`CreatedAt`。`MatchSnapshot` 保存版本化的結構化核對結果與差異，不應只保存顯示文字。核准應保存採用的 revision、核對快照及建立的 `AccountsPayableId`，供日後追溯當時依據。

### 5.5 關聯與正式 AP 映射

```text
Supplier
  └── SupplierApSubmission (1:N across invoices)
        ├── SupplierApSubmissionRevision (1:N)
        │     ├── SupplierApSubmissionRevisionLine (1:N)
        │     └── SupplierApSubmissionAttachment (1:N)
        └── SupplierApSubmissionReview (1:N)
        └── approved → AccountsPayable (0..1)

AccountsPayable
  └── existing PaymentRecord / PaymentBatch / accounting workflows
```

核准映射至少包括：供應商、PO、收貨單、供應商發票號碼／日期、幣別、未稅金額、稅額、含稅總額及品項明細。AP 編號由既有後端產生。到期日、匯率、base currency 金額、稅別／稅碼、驗證狀態及會計傳票，使用內部正式流程或經財務核准的規則；不得照抄供應商可任意指定的值。若 AP 欄位不能完整保存送審分攤或核准依據，必須保留 submission revision 關聯以追溯原始資料。

## 6. 狀態與轉換規則

| 目前狀態 | 動作 | 下一狀態 | 權限／結果 |
|---|---|---|---|
| — | 建立草稿 | `draft` | Portal 建立者；只限所屬供應商。 |
| `draft` | 編輯／附件上傳 | `draft` | 建立者或同供應商授權角色；更新併發版本。 |
| `draft` | 提交 | `submitted` | 驗證必填資料、來源單據、附件掃描及重複發票；快照凍結。 |
| `submitted` | 退回補件 | `returned` | 具權限內部審核者；必填供應商可見原因。 |
| `returned` | 建立新草稿修訂／補件 | `returned` | 供應商可編輯新 current draft，舊修訂維持唯讀。 |
| `returned` | 重送 | `submitted` | 驗證後建立下一個不可變修訂。 |
| `submitted` | 拒絕 | `rejected` | 具權限內部審核者；必須記錄原因；不可轉為已核准。 |
| `submitted` | 核准 | `approved` | 具權限內部審核者；必須通過核對政策並冪等建立正式 AP。 |
| `draft`／`returned` | 供應商取消 | `cancelled` | 建立者或同供應商授權角色；不能有已核准 AP。 |

拒絕、取消及核准為終態。需重新送審時建立新案件並連結原案件或使用核准的重開程序；不得直接重開已核准案件。內部審核者對本人提交案件的自我核准應禁止；若公司要求職務分離／雙人覆核，列為上線條件。

## 7. API 契約與目前實作狀態

### 7.1 共通規則

- Portal 路由採獨立 supplier-portal policy；`SupplierId` 從 `SupplierPortalIdentityContext.Current` 取得。員工審核路由使用獨立員工權限。
- 所有請求／回應使用 JSON camelCase；上傳使用 multipart/form-data。所有日期時間以 RFC 3339 UTC offset 傳輸；發票日期用 `YYYY-MM-DD`。
- 建立請款採 `Idempotency-Key` header；案件明細及狀態變更回應提供 `ETag`，草稿更新、提交、取消及審核要求 `If-Match`，版本衝突回 `409 Conflict`。
- 相同 idempotency key 與相同 request body 重送時回傳原結果；相同 key 搭配不同內容回 `409 Conflict`。提交及核准另以案件狀態、revision、唯一 AP 關聯和交易鎖保證重試不重複生效。
- 不支援批次接受多供應商 ID；對不存在及不屬於該供應商的案件使用一致 `404`，避免資源探測。
- 清單分頁上限建議 100 筆；按建立日倒序。回應不包含物件儲存 key、他家資料、內部權限資料或敏感銀行帳號。

#### 目前已實作的政策與上線限制

- 供應商提交角色目前限定 `sales`；員工查詢使用 `finance:view`，退回／拒絕／核准使用 `finance:approve`。
- 可請款來源限 PO 狀態 `confirmed`／`partial_received`、收貨狀態 `received`、IQC 狀態 `passed`／`not_required`，且不得鎖定；來源必須屬於登入供應商。
- 因現有收貨明細沒有明確 PO 明細外鍵，同一收貨單有重複 SKU 或無法唯一對應 PO 明細時，系統拒絕提供／接受該來源，不猜測對應關係。
- 同一供應商發票號碼在送審申請及既有正式 AP 中皆會檢查重複；正規化會去除空白並轉為大寫。需財務確認此正規化是否符合正式發票規則。
- 數量最多 2 位小數、單價最多 4 位小數、金額最多 2 位小數，且表頭與明細加總須精確相等；不設容差。
- 送審需至少一份 clean 附件；沿用 CAPA 設定，接受 PDF／JPEG／PNG，預設單檔 20 MB、每案件最多 10 個有效附件。掃描器或私有儲存未就緒時採 fail-closed。
- 核准僅允許三方核對完全相符；任何差異均不得核准。核准建立既有正式 AP，不建立付款紀錄或會計傳票。
- PostgreSQL 16 隔離 schema 已通過 migration 升級／回滾測試，且 AP Submission 服務流程已通過真實 PostgreSQL 核准整合測試；HTTP 授權／端到端測試、正式附件服務設定及財務政策簽核仍未完成，完成上線驗收前不得對外開放。

### 7.2 供應商 Portal API

Base path：`/api/supplier-portal/ap-submissions`

| 方法與路徑 | 用途 | 成功回應 | 主要失敗回應 |
|---|---|---|---|
| `GET /` | 查詢登入供應商案件；可篩 `status`、發票日期區間、`submissionNumber`，分頁。 | `200` 清單摘要及分頁資訊 | `400` 無效篩選 |
| `GET /eligible-sources` | 查詢可請款 PO／收貨單／驗收明細；可依 PO 篩選，只回傳登入供應商可用資料及伺服器計算的可請款餘量。 | `200` 來源清單 | `400` 無效篩選 |
| `POST /` | 建立草稿。伺服器決定 supplier／submitter；要求 `Idempotency-Key`。 | `201` 草稿及 `Location` | `400` 欄位錯誤、`404` 無權查看來源、`409` 重複 key／來源衝突 |
| `GET /{submissionId}` | 取得供應商可見案件、current draft、已提交版本、審核原因與附件狀態。 | `200` 詳情 | `404` 不存在或非本供應商 |
| `PUT /{submissionId}/draft` | 建立／更新可編輯草稿欄位與明細；要求目前併發版本。 | `200` 更新後草稿 | `400` 格式錯誤、`404` 範圍外、`409` 狀態或版本不允許 |
| `POST /{submissionId}/submit` | 驗證並提交 current draft；同一 idempotency key 重試回相同結果。 | `200` 或 `202` 提交結果、revision、時間、match 摘要 | `400` 欄位不完整、`404` 範圍外、`409` 狀態／重複發票／併發衝突、`422` 不符合政策、`503` 附件掃描不可用 |
| `POST /{submissionId}/cancel` | 取消 draft 或 returned 案件；`If-Match` 必填，可選原因會寫入稽核紀錄。 | `200` 新狀態及 `ETag` | `404` 範圍外、`409` 不可取消或版本衝突 |
| `GET /{submissionId}/attachments` | 列出本供應商案件附件 metadata 及掃描狀態。 | `200` 附件清單 | `404` 範圍外 |
| `POST /{submissionId}/attachments` | 上傳至 current draft；伺服器驗證大小／檔案內容並掃描。 | `201` clean 附件 metadata | `400` 格式錯誤、`404` 範圍外、`409` 狀態不允許、`422` 惡意檔、`503` 儲存／掃描服務不可用 |
| `DELETE /{submissionId}/attachments/{attachmentId}` | 僅在尚未提交的 draft 撤銷附件；以軟刪除保留稽核。 | `204` | `404` 範圍外、`409` 附件已提交或狀態不允許 |
| `GET /{submissionId}/attachments/{attachmentId}/content` | 供應商僅可串流下載其案件中 clean、未撤銷且符合修訂範圍的附件。 | `200` 檔案內容 | `404` 範圍外或不可下載 |

> 供應商不可編輯／刪除已提交內容或下載待掃描附件；下載使用後端串流，逐次驗證供應商範圍及掃描狀態。

### 7.3 內部財務／採購審核 API

Base path：`/api/erp/supplier-ap-submissions`。限定員工帳號；目前使用既有 `finance:view` 與 `finance:approve` 權限，正式啟用前需確認財務／採購角色授權及職務分離政策。

| 方法與路徑 | 用途 | 成功回應 | 主要失敗回應 |
|---|---|---|---|
| `GET /` | 查詢待審／全部案件；依狀態、供應商、發票日期、案件編號篩選。 | `200` 分頁清單及核對狀態 | `400` 無效篩選、`403` 權限不足 |
| `GET /{submissionId}` | 取得所有修訂、附件安全 metadata、來源單據、核對差異及審核歷程。 | `200` 完整內部詳情 | `404` 不存在、`403` 權限不足 |
| `GET /{submissionId}/attachments/{attachmentId}/content` | 具查詢權限的員工下載 clean 附件；逐次授權。 | `200` 檔案內容 | `403` 權限不足、`404` 不存在、`409` 非 clean |
| `POST /{submissionId}/return` | 退回補件；要求非空供應商可見原因及 `If-Match`。 | `200` 狀態與 review event | `400` 原因缺漏、`404` 不存在、`409` 狀態／版本衝突 |
| `POST /{submissionId}/reject` | 終止拒絕；要求原因及 `If-Match`。 | `200` 狀態與 review event | `400` 原因缺漏、`404` 不存在、`409` 狀態／版本衝突 |
| `POST /{submissionId}/approve` | 執行最終核對、核准及正式 AP 建立；要求 `If-Match`。 | `200` 核准結果及既有 AP 摘要 | `403` 權限／職務分離失敗、`404` 不存在、`409` 重複 AP／狀態／版本衝突、`422` 未通過財務政策 |

審核動作不可接受 request body 指定的 `SupplierId`、`AccountsPayableId`、`PaymentStatus`、`VoucherId` 或核准結果。AP ID 只由核准交易建立／回傳。

### 7.4 DTO 欄位輪廓

**`CreateSupplierApSubmissionRequest`**

```json
{
  "supplierInvoiceNumber": "INV-2026-001",
  "supplierInvoiceDate": "2026-10-20",
  "currency": "TWD",
  "invoiceAmount": 10000.00,
  "taxAmount": 500.00,
  "totalAmount": 10500.00,
  "purchaseOrderId": "guid",
  "goodsReceiptId": "guid",
  "lines": [
    {
      "goodsReceiptItemId": "guid",
      "invoicedQuantity": 10,
      "unitPrice": 1000.00,
      "netAmount": 10000.00,
      "taxAmount": 500.00,
      "totalAmount": 10500.00
    }
  ]
}
```

JSON 範例說明契約輪廓；正式 DTO 不得包含 `supplierId`、狀態、審核者、交換匯率或正式 AP 欄位。草稿更新使用同一欄位集並要求併發版本。

**`SupplierApSubmissionDetail`** 至少包含：`id`、`submissionNumber`、`status`、`currentRevisionNumber`、供應商發票欄位、PO／收貨可見摘要、發票金額、`matchStatus`、`discrepancies[]`、附件安全狀態、審核歷程及建立／提交時間。只有內部審核 DTO 可包含完整 reviewer identity 及內部註記。

**`SubmitSupplierApSubmissionResult`** 至少包含：`submissionId`、`submissionNumber`、`status`、`revisionNumber`、`submittedAt`、`matchStatus`、`discrepancies[]`。

**`ApproveSupplierApSubmissionResult`** 至少包含：`submissionId`、`status`、`approvedRevisionNumber`、`approvedAt`、`accountsPayableId`、`apNumber`。不可回傳付款或過帳成功訊息，因核准不代表已付款或已過帳。

### 7.5 差異與錯誤回應

建議 `matchStatus`：`pending`、`matched`、`variance`、`blocked`。`discrepancies[]` 使用穩定 `code`、欄位／來源識別、申請值、ERP 值、差異值與人類可理解訊息。不要只回傳格式化字串作為判斷依據。

| HTTP | 使用時機 |
|---|---|
| `400 Bad Request` | DTO 欄位格式、必要值或 query 格式錯誤。 |
| `401 Unauthorized` | 未登入或 token 無效。 |
| `403 Forbidden` | 已登入但無指定內部審核權限；供應商角色無權執行內部動作。 |
| `404 Not Found` | 案件不存在或不屬於登入供應商；避免洩露跨供應商資源。 |
| `409 Conflict` | 狀態轉換錯誤、版本衝突、重複發票、來源單據占用或核准時已有 AP。 |
| `422 Unprocessable Entity` | 欄位可解析但不符合來源單據、三方核對／核准政策或附件掃描結果。 |
| `503 Service Unavailable` | 私有檔案儲存或病毒掃描服務不可用；不得 fail-open。 |

錯誤回應只提供安全、可採取行動的訊息及 `traceId`；不可回傳資料庫錯誤、物件儲存 key、內部路徑或其他供應商資料。

## 8. 三方核對與正式 AP 建立契約

### 8.1 核對資料

每次提交和內部核准都應可追溯到同一 revision，並列出：

1. **PO**：供應商、幣別、SKU／品項、單價、訂購數量、PO 狀態。
2. **收貨／驗收**：供應商、對應 PO、收貨單狀態、SKU、已收／允收／拒收數量及 IQC 結果。
3. **發票**：供應商提交的發票號碼／日期、幣別、數量及未稅／稅額／總額。

MVP 建議只有自有且確認的 PO、已完成收貨且驗收達可請款狀態的來源可送審；是否允許部分驗收、分期請款、預付款、運費／關稅及跨收貨累計必須依財務政策核定。各來源狀態字串需實作前以現行服務規則確認。

### 8.2 核准交易

核准服務應在同一資料庫交易中：

1. 重新載入並鎖定 submission／current revision 及來源餘額，檢查版本、狀態、供應商關聯與重複請款。
2. 重新計算當下三方核對，不信任先前 Portal 傳入或快取的結果。
3. 確認核准者有權、非自我核准（若採職務分離），附件均 clean，且所有差異符合核准政策或有具權限的例外核准紀錄。
4. 建立正式 `AccountsPayable` 及明細，將 `SupplierApSubmission.GeneratedAccountsPayableId` 設定為唯一 AP，新增不可變 approved review event。
5. 任一步失敗則整筆回滾；重試讀取既有 approved AP 關聯並回傳原結果。

核准**不得**直接呼叫 `CreateVoucherFromAPAsync`，不得產生已過帳會計傳票，不得建付款批次。既有財務 AP 驗證／過帳工作流程仍是後續帳務控制點。

## 9. 非功能需求與安全

- **身分與授權**：Portal 使用既有供應商 Keycloak scheme；每次請求均以身分對照資料庫取得 Supplier scope。內部審核使用員工認證與明確 permission。
- **資料隔離**：供應商跨域案件、PO、收貨單及附件皆需服務層比對 SupplierId；不可只依 UI 隱藏。
- **稽核**：保存提交者、審核者、來源 revision、決策、原因與 UTC 時間；避免將完整附件或機密放入一般應用 log。
- **附件**：private storage、檔案內容／副檔名雙重檢查、大小及數量限制、掃描、hash、授權下載；掃描失敗時拒絕核准與下載。
- **冪等／併發**：建立使用供應商範圍的 idempotency key；送審／審核使用 ETag／版本，避免重複 AP。
- **可用性**：檔案服務失效時明確回傳 503；不得以本機暫存成功回應或略過掃描。
- **可追溯性**：submission number、revision、review event、正式 AP 與 AP 傳票／付款關係可一路追蹤；Portal 只能看該供應商安全摘要。
- **版本化**：正式 API 若啟用版本路由，沿用現有 `/api/v1/erp/...` 慣例；不得新增與現有 Portal 版本策略衝突的平行 API。

## 10. 例外情況與驗收案例

1. **跨供應商 ID 替換**：將 submission、PO、收貨單或附件 ID 換成別家資料，所有讀取／寫入均不回傳資料且不改變資料。
2. **相同 invoice 重送**：重複提交同供應商、同發票識別資料，不可建立第二個有效申請／正式 AP；唯一性比對規則按財務決策落實。
3. **被占用的收貨數量**：另一個有效送審已占用同一可請款數量時，提交或核准需重新核對並回 409／422，不可超額核准。
4. **收貨／PO 在提交後變更或取消**：核准時重新讀取權威資料；來源已失效時停止核准並保留原因。
5. **附件病毒或掃描器中斷**：感染回 422；掃描器／儲存不可用回 503；無情況可改成 clean 或繼續核准。
6. **重複核准請求**：併發兩個核准請求只會有一個 AP，另一個回傳同一結果或明確 409，不產生重複 AP。
7. **核准交易中斷**：資料庫交易回滾時 submission 不得顯示 approved，且不得留下孤兒 AP。
8. **已核准或已拒絕案件修改**：供應商不得改寫已提交歷史或終態；需重送時依新案件／重開政策辦理。
9. **未到期或無 DueDate**：送審本身不應任意讓供應商指定付款到期日；核准後 DueDate 由正式 AP 條件或財務政策產生。
10. **小數位／稅額不一致**：輸入精度超限或金額加總不符時拒絕；允差在財務規則核准前不可自行四捨五入放行。

## 11. 成功標準

- **SC-001**：每個已核准申請都能以申請編號、修訂和來源單據追溯到唯一一筆正式 AP；未核准案件正式 AP 數為零。
- **SC-002**：重複送出／併發核准測試中，任何一個送審案件最多產生一筆 AP。
- **SC-003**：跨供應商資源存取測試 100% 被拒絕，且不洩露目標案件是否存在。
- **SC-004**：所有 submitted revision 及 review decision 均可還原當時發票欄位、來源、操作者與時間，且不被後續補件覆寫。
- **SC-005**：附件未通過 clean 掃描的案件無法核准，掃描服務故障時 API 明確回報不可用。
- **SC-006**：經財務確認的容差、稅務及精度案例均有正反向驗收測試；政策未確認前，不啟用自動核准。
- **SC-007**：核准不產生付款批次或會計過帳；既有 AP 查詢金額僅以正式 AP 與 posted 付款來源計算。

## 12. 假設、待確認決策與開發前 Gate

### 12.1 本草案採用的暫定假設

- 每個 submission 代表一張供應商發票，供應商須已登入、已啟用，且關聯至單一供應商。
- MVP 每張發票只關聯一張 PO 及一張已完成收貨／驗收單，幣別須與 PO 相同；先不處理跨 PO、跨幣別及預付款。
- 供應商可建立草稿並取消未提交或退回案件；提交後不可直接修改，只能依退回流程建立新修訂。
- 金額至少保存未稅、稅額、含稅總額；稅率推算及金額容差不在財務核定前自動執行。
- 發票憑證為必需附件；暫定 PDF、單檔 20 MB 上限，沿用安全掃描原則，但正式限制仍待資安／財務確認。
- 內部核准只建立正式 AP；是否允許同一財務人員建立與核准案件、例外核准及雙人覆核依公司權限矩陣定案。

### 12.2 已核定決策與待確認事項

**已由使用者核定並反映於模型：**

1. 一筆請款修訂僅對應單一 PO 與單一收貨／驗收單。
2. 同一供應商的發票號碼在所有申請狀態下唯一；尚未填入發票號碼的草稿不占用唯一鍵。

**仍需財務／採購／資安確認：**

1. 發票號碼正規化規則（大小寫、空白、標點、年度／字軌）；目前模型以同供應商範圍的正規化鍵建立唯一索引。
2. 部分請款與累計已請款數量、價格／數量／稅額容差、稅額精度與例外核准權限；政策未確認前差異送人工，不自動核准。
3. 必要附件、檔案格式／大小／保存期限、PO 幣別限制，以及正式 AP 到期日、稅別／稅碼、匯率與 base amount 的權威來源。

### 12.3 正式啟用 Gate

- 財務與採購簽認 12.2 決策及各 PO／收貨狀態允許規則。
- 確認 `GoodsReceiptItem` 至 PO 明細的可靠關聯；目前無法唯一對應的重複 SKU 來源會被拒絕。
- 確認正式 AP 編號、稅別、到期日、base currency 欄位來源與 AP posting 邊界；核准服務不得意外呼叫自動過帳。
- 確認權限矩陣、禁止自我核准政策、重開／作廢程序及審核通知。
- 確認附件儲存／掃描部署、資料保存與惡意檔清理流程，並完成 HTTP 授權／端到端驗收。
- EF Core migration、PostgreSQL 16 隔離 schema 升級／回滾，以及 AP Submission 核准服務流程已通過 PostgreSQL 測試；migration 尚未套用到任何業務資料庫。正式套用前需核對明確目標資料庫、目前 migration 狀態、備份與核准變更窗口。

## 13. 相關文件與程式來源

- [供應商入口規劃](./supplier-portal-plan.md) — Portal 資料隔離、AP 請款流程及財務待確認事項。
- [ERP 架構標準](./standards.md) — Controller／Service 分層、介面及命名規範。
- [AccountsPayable 模型](../../src/ErpBackend/Models/Procurement/ProcurementModels.cs) — 既有 PO、收貨、AP、付款模型。
- [AccountsPayable API Controller](../../src/ErpBackend/Controllers/AccountsPayableController.cs) — 現有內部 AP API；不可直接開放供應商 Portal。
- [供應商 Portal ASN Controller](../../src/ErpBackend/Controllers/SupplierPortalAsnController.cs) — 供應商身分解析及 API 範圍控制參考。
- [供應商 Portal CAPA Controller](../../src/ErpBackend/Controllers/SupplierPortalCapaController.cs) — Portal 附件上傳、掃描及 fail-closed 回應參考。
- [供應商 Portal 請款 UI 計畫](./ap-submission-portal-ui-plan.md) — Portal 請款清單、草稿、附件、送審操作及前端契約缺口。
- [AP Submission models](../../src/ErpBackend/Models/Procurement/SupplierApSubmissionModels.cs) — 送審案件、修訂、明細、附件與審核紀錄。
- [AP Submission migration](../../src/ErpBackend/Migrations/20261027180000_AddSupplierApSubmissions.cs) — 新增送審資料表、索引與限制；已在 PostgreSQL 隔離 schema 驗證升級／回滾，尚未套用至業務資料庫。
- [AccountingService](../../src/ErpBackend/Services/AccountingService.cs) — AP 傳票建立行為；正式實作必須避免將 AP Submission 核准誤當成會計過帳。
