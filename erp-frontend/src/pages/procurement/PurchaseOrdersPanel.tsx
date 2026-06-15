import React from 'react';
import { Loader2, Eye } from 'lucide-react';

interface PurchaseOrdersPanelProps {
  poLoading: boolean;
  filteredPo: any[];
  handleConfirmPo: (poNumber: string) => void;
  handleReceivePo: (poNumber: string) => void;
}

const PurchaseOrdersPanel: React.FC<PurchaseOrdersPanelProps> = ({
  poLoading,
  filteredPo,
  handleConfirmPo,
  handleReceivePo
}) => {
  return (
    <div className="glass-card no-lift">
      <table className="lux-table">
        <thead>
          <tr>
            <th>採購單號</th><th>供應商代碼</th><th>日期</th><th>總金額</th><th>狀態</th><th>操作</th>
          </tr>
        </thead>
        <tbody>
          {poLoading ? <tr><td colSpan={6} className="td-loading"><Loader2 className="spin" /></td></tr> : 
            filteredPo.map((po: any) => (
              <tr key={po.id || po.poNumber}>
                <td className="mono-label">{po.poNumber || po.po_no}</td>
                <td className="emp-label">{po.supplierCode || po.supplier_code}</td>
                <td className="date-label">{(po.poDate || po.po_date)?.slice(0, 10)}</td>
                <td className="amount-label">{Number(po.totalAmount || po.total_amount || 0).toLocaleString()}</td>
                <td>
                  <span className={`badge-lux badge-${
                    po.status === 'DRAFT' ? 'neutral' : 
                    po.status === 'CONFIRMED' ? 'info' : 
                    po.status === 'RECEIVED' ? 'success' : 'neutral'
                  }`}>
                    {po.status}
                  </span>
                </td>
                <td>
                  <div className="action-row">
                    {po.status === 'DRAFT' && (
                      <button className="btn-sm btn-outline" onClick={() => handleConfirmPo(po.poNumber || po.po_no)}>確認</button>
                    )}
                    {po.status === 'CONFIRMED' && (
                      <button className="btn-sm btn-primary" onClick={() => handleReceivePo(po.poNumber || po.po_no)}>收料入庫</button>
                    )}
                    <button className="action-btn" title="檢視"><Eye size={13} /></button>
                  </div>
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>
    </div>
  );
};

export default PurchaseOrdersPanel;
