import React from 'react';
import { Plus } from 'lucide-react';

interface SuppliersPanelProps {
  supplierData: any;
  setShowAddSupplier: (b: boolean) => void;
  setEditingSupplierId: (id: string | null) => void;
  setShowEditSupplier: (b: boolean) => void;
}

const SuppliersPanel: React.FC<SuppliersPanelProps> = ({
  supplierData,
  setShowAddSupplier,
  setEditingSupplierId,
  setShowEditSupplier
}) => {
  return (
    <div className="glass-card no-lift">
      <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h3 style={{ margin: 0, fontSize: '16px' }}>供應商列表</h3>
          <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>供應商資料維護與管理</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddSupplier(true)}>
          <Plus size={14} style={{ marginRight: 6 }} /> 新增供應商
        </button>
      </div>
      <table className="lux-table">
        <thead>
          <tr>
            <th>供應商代號</th><th>名稱</th><th>聯絡人</th><th>電話</th><th>評等</th><th>狀態</th><th>操作</th>
          </tr>
        </thead>
        <tbody>
          {(supplierData?.data || []).length === 0 ? <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無供應商資料</td></tr> :
            (supplierData?.data || []).map((s: any) => (
              <tr key={s.id}>
                <td className="mono-label">{s.supplierCode}</td>
                <td className="emp-label">{s.supplierName}</td>
                <td>{s.contactPerson || '—'}</td>
                <td>{s.phone || '—'}</td>
                <td><span className={`badge-lux ${s.rating === 'A' ? 'badge-success' : s.rating === 'B' ? 'badge-info' : 'badge-neutral'}`}>{s.rating || '—'}</span></td>
                <td><span className={`badge-lux ${s.status === 'ACTIVE' ? 'badge-success' : 'badge-neutral'}`}>{s.status === 'ACTIVE' ? '啟用' : '停用'}</span></td>
                <td>
                  <div className="action-row">
                    <button className="btn-sm btn-outline" onClick={() => { setEditingSupplierId(s.id); setShowEditSupplier(true); }}>編輯</button>
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

export default SuppliersPanel;
