$path = 'D:\ERP\erp-frontend\src\pages\Procurement.tsx'
$lines = Get-Content $path -Encoding UTF8

$lines[206] = '      {/* 頁首 */}'
$lines[209] = '          <h2 className="module-page-title">採購及付款</h2>'
$lines[210] = '          <p className="module-page-desc">請購單 → RFQ 詢價 → 採購訂單 → 進貨點收 → 應付帳款</p>'
$lines[213] = '          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>'
$lines[217] = '          <button className="btn btn-primary btn-sm" onClick={() => {'
$lines[221] = '      {/* 統計卡片 */}'
$lines[235] = '            <FileText size={12} style={{ marginRight: 4 }} />請購單列表'
$lines[237] = '            <Package size={12} style={{ marginRight: 4 }} />採購單列表'
$lines[239] = '            <Users size={12} style={{ marginRight: 4 }} />供應商列表'
$lines[245] = '            <span style={{ fontSize: 12, color: "var(--slate-400)" }}>鎖定選項:</span>'
$lines[247] = '              <option value="all">全部</option>'
$lines[248] = '              <option value="unlocked">未鎖定</option>'
$lines[249] = '              <option value="locked">已鎖定</option>'

[System.IO.File]::WriteAllLines($path, $lines, [System.Text.Encoding]::UTF8)
