const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let c = fs.readFileSync(p, 'utf8').split('\n');

// Line 884 (index 883)
c[883] = "            'supplierPortal': (";
// Line 887 (index 886)
c[886] = "                <div style={{ fontSize: 13, color: 'var(--slate-300)', marginBottom: 16 }}>";
// Line 888 (index 887) is the garbled text
c[887] = "                  若要為此供應商建立門戶網站帳號，請填寫以下資訊。此帳號將允許供應商在線查看採購單。";
// Line 889 (index 888)
c[888] = "                </div>";
// Line 903 (index 902)
c[902] = "                        <span style={{ fontSize: 12, color: 'var(--slate-400)' }}>帳號啟用狀態</span>";

fs.writeFileSync(p, c.join('\n'), 'utf8');
console.log('Fixed');
