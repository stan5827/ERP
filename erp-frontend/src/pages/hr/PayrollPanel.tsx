import React from 'react';
import { Search, Plus, Check, Eye, Edit2 } from 'lucide-react';
import type { PayrollDto } from '../../api/api';

interface PayrollPanelProps {
  search: string;
  setSearch: (s: string) => void;
  setShowAddPayroll: (b: boolean) => void;
  payrollLoading: boolean;
  payrolls: PayrollDto[];
  payrollStatusConfig: Record<string, { label: string; cls: string }>;
  handleApprovePayroll: (id: string) => void;
}

const PayrollPanel: React.FC<PayrollPanelProps> = ({
  search,
  setSearch,
  setShowAddPayroll,
  payrollLoading,
  payrolls,
  payrollStatusConfig,
  handleApprovePayroll
}) => {
  return (
    <>
      <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
        <div style={{ position: 'relative', flex: 1 }}>
          <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
          <input type="text" placeholder="搜尋員工編號、姓名..." value={search}
            onChange={e => setSearch(e.target.value)}
            style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
        </div>
        <input type="month" style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }} />
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddPayroll(true)}><Plus size={13} />新增薪資</button>
      </div>

      <div className="glass-card" style={{ padding: '18px' }}>
        <div style={{ marginBottom: 14 }}>
          <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>薪資核算</h3>
          <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {payrolls.length} 筆薪資記錄</div>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>員工編號</th><th>姓名</th><th>薪資年月</th><th>基本薪資</th><th>應發薪資</th><th>實發薪資</th><th>狀態</th><th>操作</th>
            </tr>
          </thead>
          <tbody>
            {payrollLoading ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
            ) : payrolls.length === 0 ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無薪資記錄</td></tr>
            ) : (
              payrolls.map(record => {
                const sc = payrollStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                return (
                  <tr key={record.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.employeeNo}</td>
                    <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                    <td className="mono" style={{ fontSize: 12 }}>{record.periodStart.slice(0, 7)}</td>
                    <td style={{ fontSize: 12, textAlign: 'right' }}>{record.basicSalary.toLocaleString()}</td>
                    <td style={{ fontSize: 12, textAlign: 'right', color: 'var(--emerald-400)' }}>{record.grossPay.toLocaleString()}</td>
                    <td style={{ fontSize: 12, textAlign: 'right', fontWeight: 600, color: 'var(--cobalt-300)' }}>{record.netPay.toLocaleString()}</td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {record.status === 'draft' && (
                          <button className="icon-action-btn" title="核准" onClick={() => handleApprovePayroll(record.id)}><Check size={14} /></button>
                        )}
                        <button className="icon-action-btn" title="查看"><Eye size={14} /></button>
                        <button className="icon-action-btn" title="編輯"><Edit2 size={14} /></button>
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </>
  );
};

export default PayrollPanel;
