const fs = require('fs');
const path = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let content = fs.readFileSync(path, 'utf8');
const lines = content.split('\n');

// Line 884 (index 883) is the broken property key
lines[883] = "            'supplierPortal': (";
lines[886] = "                <div style={{ fontSize: 13, color: 'var(--slate-300)', marginBottom: 16 }}>";
lines[887] = "                  若要為此供應商建立門戶網站帳號，請填寫以下資訊。此帳號將允許供應商在線查看採購單。";
lines[901] = "                        <span style={{ fontSize: 12, color: 'var(--slate-400)' }}>帳號啟用狀態</span>";

fs.writeFileSync(path, lines.join('\n'), 'utf8');
console.log('Fixed lines 884, 887, 888, 902');
