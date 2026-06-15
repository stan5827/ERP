import React from 'react';
import { Loader2 } from 'lucide-react';

interface GoodsReceiptsPanelProps {
  grLoading: boolean;
  filteredReceipts: any[];
}

const GoodsReceiptsPanel: React.FC<GoodsReceiptsPanelProps> = ({
  grLoading,
  filteredReceipts
}) => {
  return (
    <div className="glass-card no-lift">
      <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h3 style={{ margin: 0, fontSize: '16px' }}>收貨單列表 (GR)</h3>
          <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>整合 PO 與 ASN 的正式入庫記錄</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <span className="badge-lux badge-info">共 {filteredReceipts.length} 筆</span>
        </div>
      </div>
      <table className="lux-table">
        <thead>
          <tr>
            <th>收貨單號</th><th>來源單據</th><th>供應商</th><th>倉庫</th><th>建立時間</th><th>IQC</th><th>狀態</th>
          </tr>
        </thead>
        <tbody>
          {grLoading ? <tr><td colSpan={7} className="td-loading"><Loader2 className="spin" /></td></tr> :
            filteredReceipts.length === 0 ? <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無收貨單資料</td></tr> :
            filteredReceipts.map((gr: any) => (
              <tr key={gr.id}>
                <td>
                  <div className="mono-label">{gr.grNumber}</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{gr.asnNumber ? `ASN ${gr.asnNumber}` : 'PO 直接收貨'}</div>
                </td>
                <td>
                  <div className="emp-label">{gr.purchaseOrderNumber || '—'}</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{gr.items?.length || 0} 項物料</div>
                </td>
                <td>{gr.supplierName || '—'}</td>
                <td>{gr.warehouseName || gr.warehouseCode || '—'}</td>
                <td className="date-label">{(gr.createdAt || gr.grDate)?.slice(0, 10)}</td>
                <td><span className={`badge-lux ${gr.iqcStatus === 'pending' ? 'badge-warning' : 'badge-success'}`}>{gr.iqcStatus || 'pending'}</span></td>
                <td><span className={`badge-lux ${gr.status === 'received' ? 'badge-success' : 'badge-info'}`}>{gr.status}</span></td>
              </tr>
            ))
          }
        </tbody>
      </table>
    </div>
  );
};

export default GoodsReceiptsPanel;
