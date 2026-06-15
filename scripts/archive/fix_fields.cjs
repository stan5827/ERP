const fs = require('fs');
const path = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let content = fs.readFileSync(path, 'utf8');
let lines = content.split(/\r?\n/);

// Fix poBasicFields (indices 94-97)
lines[94] = "    { name: 'supplierId', label: '供應商', type: 'select', required: true, options: suppliers, description: '選擇發放此採購單的供應商對象。系統將自動帶出該供應商的預設幣別、付款條件等資訊。' },";
lines[95] = "    { name: 'poDate', label: '採購日期', type: 'date', description: '採購單據的開立建檔日期，也是計算帳款的起始日。' },";
lines[96] = "    { name: 'currency', label: '幣別', type: 'select', options: [{ value: 'TWD', label: 'TWD 新台幣' }, { value: 'USD', label: 'USD 美元' }, { value: 'JPY', label: 'JPY 日圓' }, { value: 'EUR', label: 'EUR 歐元' }, { value: 'CNY', label: 'CNY 人民幣' }], description: '決定此採購單收貨與請款時的計算基準貨幣，與供應商報價幣別一致。' },";
lines[97] = "    { name: 'exchangeRate', label: '匯率', type: 'number', placeholder: '1.0', description: '外幣兌換本地貨幣(TWD)的匯率。若為TWD則填1。此匯率將用於進貨時計算本地成本。' }";

// Fix poPaymentLogisticsFields (indices 101-105)
lines[101] = "    { name: 'paymentTerms', label: '付款條件', type: 'text', placeholder: '如 月結30天', description: '與供應商議定的應付帳款(AP)結帳期限，如月結30天、票到付款、貨到付款等。' },";
lines[102] = "    { name: 'tradeTerms', label: '貿易條件', type: 'select', options: [{ value: 'EXW', label: 'EXW - 工廠交貨' }, { value: 'FOB', label: 'FOB - 船上交貨' }, { value: 'CIF', label: 'CIF - 成本保險運費' }, { value: 'DDP', label: 'DDP - 完稅交貨' }], description: '國際貿易術語(Incoterms)，界定運費與風險移轉時點。' },";
lines[103] = "    { name: 'taxCode', label: '稅碼', type: 'select', options: [{ value: 'TX1', label: '應稅5%' }, { value: 'TX0', label: '免稅' }, { value: 'TX2', label: '零稅率' }], description: '稅務類型代碼：TX1=應稅5%(可扣抵)、TX0=免稅(不可扣抵)、TX2=零稅率(外銷)。' },";
lines[104] = "    { name: 'expectedDeliveryDate', label: '預計交貨日', type: 'date', description: '供應商承諾將貨品送達我方倉庫的日期，影響後續排程與庫存預測。' },";
lines[105] = "    { name: 'deliveryWarehouseId', label: '收貨倉庫', type: 'select', options: warehouses, description: '指定此採購單的收貨入庫倉庫，決定進貨後的庫存存放位置。' }";

// Fix prFields (indices 151-154)
lines[152] = "    { name: 'departmentId', label: '申請部門', type: 'select', required: true, options: departments, description: '發起此採購需求的內部單位，用於成本分攤、預算控制或權限判定。' },";
lines[153] = "    { name: 'projectCode', label: '專案代號', type: 'text', placeholder: '如: PROJ-2023-01', description: '若此採購需求歸屬於特定專案，請填寫專案代號以利後續專案成本分析與收益認列。' },";
lines[154] = "    { name: 'requestReason', label: '請購原因', type: 'textarea', placeholder: '說明為何需要此採購...', description: '說明請購的業務理由，如：庫存不足、生產需求、設備維修等，供主管審核參考。' },";

// Fix supplierBasicFields
lines[159] = "    { name: 'supplierCode', label: '供應商代碼', type: 'text', required: true, placeholder: '如: SUP-001', description: '供應商的唯一識別碼，用於系統內的搜尋與關聯。' },";
lines[160] = "    { name: 'supplierName', label: '供應商名稱', type: 'text', required: true, placeholder: '輸入供應商全名', description: '供應商的公司或商號名稱。' },";
lines[161] = "    { name: 'taxId', label: '統一編號', type: 'text', placeholder: '請輸入統一編號', description: '供應商的統一編號（公司登記號碼）。' }";

fs.writeFileSync(path, lines.join('\n'), 'utf8');
console.log('Fields fix applied successfully');
