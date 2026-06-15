const fs = require('fs');
const p = 'D:/ERP/erp-frontend/src/pages/Procurement.tsx';
let c = fs.readFileSync(p, 'utf8');

// 1. Fix the Property Key Error at line 884 and other places
// Match ?? followed by some chars and :
// We use a regex that targets the specific garbled pattern we see in logs.
c = c.replace(/\?\?叟\?\?\?\?\?\?\s*:/g, "'supplierPortal':");

// 2. Fix the Unterminated String at line 442
// Match title="??? until the next " or newline
c = c.replace(/title="\?+.*onClick=\{async \(\) => \{/g, 'title="產生收料單" onClick={async () => {');

// 3. Fix the duplicated view blocks and garbage texts in bulk
// I will target the table headers to restore Traditional Chinese
const headerFixes = [
    [/\?謢\?\?\?\?\?\?\?/g, "編號"],
    [/\?謢\?\?\?\?\?\?/g, "供應商名稱"],
    [/\?\?€謆\?\?蝞\?\?/g, "統一編號"],
    [/\?\?\?\?\?\?/g, "聯繫人"],
    [/\?\?\?\/[^\s<]+/g, "電話/手機"],
    [/\?豯剜\?\?\?鞊莎\?暻\?/g, "付款條件"],
    [/\?\?飭\?/g, "評等"],
    [/\?\?\?璆\?\?\?/g, "銀行名稱"],
    [/\?\?\?\?\?\?\?/g, "銀行帳號"],
    [/\?\?叟\?\?\?\?\?\?/g, "門戶帳號"],
    [/\?\?察\?\?\?\?\?\?\?/g, "操作"],
    [/\?謜\?\?\?\?雓\?\?\?\?\?\?/g, "資料載入中..."]
];

headerFixes.forEach(([reg, rep]) => {
    c = c.replace(reg, rep);
});

// 4. Fix redundant/duplicated view blocks
// To be safe, I'll just fix the tags and labels first so it COMPILES.
// Truncating duplicate blocks is harder without potentially removing good code.

// 5. Fix closing tags that might be mangled
c = c.replace(/<\/button>\s*<\/button>/g, "</button>");
c = c.replace(/<\/div>\s*<\/div>\s*<\/div>/g, "</div></div>");

fs.writeFileSync(p, c, 'utf8');
console.log('Fixed bulk errors and encoding issues.');
