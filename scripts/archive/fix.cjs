const fs = require('fs');
const path = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let content = fs.readFileSync(path, 'utf8');
let lines = content.split(/\r?\n/);

// Fix statusConfig (Indices 14-19 are 1-indexed lines 15-20)
lines[14] = "  pending:   { label: '待確認', cls: 'neutral' },";
lines[15] = "  confirmed: { label: '已確認', cls: 'info' },";
lines[16] = "  partial:   { label: '部分到貨', cls: 'warning' },";
lines[17] = "  received:  { label: '已到貨', cls: 'active' },";
lines[18] = "  closed:    { label: '已結案', cls: 'neutral' },";

// Fix prStatusConfig
lines[22] = "  pending:   { label: '待審核', cls: 'neutral' },";
lines[23] = "  approved:  { label: '已核准', cls: 'info' },";
lines[24] = "  rejected:  { label: '已駁回', cls: 'warning' },";
lines[25] = "  converted: { label: '已拋轉', cls: 'active' },";

// Fix stats
lines[31] = "  { label: '本月採購金額', value: 'NT$1.42M', color: 'var(--cobalt-400)' },";
lines[32] = "  { label: '在途採購單',  value: 12,           color: 'var(--amber-400)' },";
lines[33] = "  { label: '本月到貨率', value: '94.8%',       color: 'var(--emerald-400)' },";
lines[34] = "  { label: '評核供應商數', value: 47,           color: 'var(--slate-300)' },";

// Fix Procurement Header & Tabs (Check actual lines first to be safe)
// Based on last view: 210, 211, etc.
const headerIdx = lines.findIndex(l => l.includes('module-page-header'));
if (headerIdx !== -1) {
    lines[headerIdx + 2] = "          <h2 className=\"module-page-title\">採購及付款</h2>";
    lines[headerIdx + 3] = "          <p className=\"module-page-desc\">請購單 → RFQ 詢價 → 採購訂單 → 進貨點收 → 應付帳款</p>";
}

fs.writeFileSync(path, lines.join('\n'), 'utf8');
console.log('Fix applied successfully');
