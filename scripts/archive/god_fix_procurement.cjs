const fs = require('fs');
const path = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let content = fs.readFileSync(path, 'utf8');
const lines = content.split('\n');

// 1. Fix the Property Key Error at line 884
// It should be 'supplierPortal'
for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('??叟??????:')) {
        lines[i] = lines[i].replace('??叟??????:', "'supplierPortal':");
    }
}

// 2. Fix the unbalanced label/span/div mess around line 883-918
// I will replace the internal fields mapping block with a clean version.
const startLine883 = 882; // 0-indexed
const endLine918 = 918;   // 0-indexed
const cleanSupplierPortalBlock = `            'supplierPortal': (
              <div style={{ padding: '8px 0' }}>
                <div style={{ fontSize: 13, color: 'var(--slate-300)', marginBottom: 16 }}>
                  若要為此供應商建立門戶網站帳號，請填寫以下資訊。此帳號將允許供應商在線查看採購單。
                </div>
                {supplierPortalFields.map(field => (
                  <div key={field.name} style={{ marginBottom: 14 }}>
                    <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>
                      {field.label}
                    </label>
                    {field.type === 'select' ? (
                      <select className="form-input" style={{ width: '100%' }} name={field.name} defaultValue={field.name === 'role' ? 'viewer' : ''}>
                        {(field.options || []).map((o: any) => (
                          <option key={o.value} value={o.value}>{o.label}</option>
                        ))}
                      </select>
                    ) : field.type === 'checkbox' ? (
                      <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
                        <input type="checkbox" name={field.name} defaultChecked={false} />
                        <span style={{ fontSize: 12, color: 'var(--slate-400)' }}>帳號啟用狀態</span>
                      </label>
                    ) : (
                      <input
                        type={field.type}
                        className="form-input"
                        style={{ width: '100%' }}
                        name={field.name}
                        placeholder={field.placeholder}
                        required={field.required}
                      />
                    )}
                  </div>
                ))}
              </div>
            )`;

// Replace lines 883 to 917 (since 918 is onClose)
lines.splice(883, 35, cleanSupplierPortalBlock);

// 3. Fix the poItems block (around line 609)
const startLine609 = 608;
const endLine625 = 625;
const cleanPoItemsHeader = `                  <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--slate-200)' }}>採購項目明細</div>
                  <button type="button" className="btn btn-outline btn-sm" onClick={() => setPoItems([...poItems, { skuId: '', quantity: 1, unit: 'PCS', unitPrice: 0, conversionRate: 1, inventoryUnit: 'PCS' }])}>
                    <Plus size={12} style={{ marginRight: 4 }} />新增項目
                  </button>
               </div>
               <table className="data-table" style={{ background: 'var(--slate-800)', borderRadius: 6, overflow: 'hidden' }}>
                 <thead>
                   <tr>
                     <th>品項 / 規格 *</th>
                     <th style={{ width: 100 }}>進貨數量</th>
                     <th style={{ width: 80 }}>單位</th>
                     <th style={{ width: 100 }}>採購單價</th>
                     <th style={{ width: 90 }}>轉換率</th>
                     <th style={{ width: 90 }}>庫存單位</th>
                     <th style={{ width: 110 }}>小計</th>
                     <th style={{ width: 40 }}></th>
                   </tr>
                 </thead>`;
// Find the exact line for poItems header
for (let i = 0; i < lines.length; i++) {
    if (lines[i].includes('??賹???謘踐???璇?')) {
        lines.splice(i, 17, cleanPoItemsHeader);
        break;
    }
}

// 4. Global Chinese restoration
content = lines.join('\n');
const genericFixes = [
    [/\?蝞\?\?\?\?賹\?\?\?/g, "編輯採購單"],
    [/\?\?察\?\?\?\?\?\?\?/g, "操作"],
    [/\?謢\?\?\?\?\?\?\?/g, "編號"],
    [/\?謢\?\?\?\?\?\?/g, "供應商名稱"],
    [/\?\?€謆\?\?蝞\?\?/g, "統一編號"],
    [/\?\?\?\?\?\?聯繫人/g, "聯繫人"],
    [/\?\?\?\/[^\s<]+/g, "電話/手機"],
    [/\?豯剜\?\?\?鞊莎\?暻\?/g, "付款條件"],
    [/\?\?飭\?/g, "評等"],
    [/\?\?\?璆\?\?\?/g, "銀行名稱"],
    [/\?\?\?\?\?\?\?/g, "銀行帳號"],
    [/\?\?叟\?\?\?\?\?\?/g, "門戶帳號"],
    [/\?謜\?\?\?\?雓\?\?\?\?\?\?/g, "資料載入中..."],
    [/\?蹎批\?\?\?\?雓\?\?\?\?\?/g, "無相關資料"],
    [/\?豯\?\?\?\?/g, "管理員"],
    [/\?瞏\?\?\?/g, "檢視者"],
    [/\?鞈對\?/g, "啟用"],
    [/\?獢€\?\?/g, "新增"],
    [/\?豯\?\?/g, "編輯"]
];

genericFixes.forEach(([reg, rep]) => {
    content = content.replace(reg, rep);
});

fs.writeFileSync(path, content, 'utf8');
console.log('Comprehensive restoration complete.');
