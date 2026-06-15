import React from 'react';
import { Search, Plus, Check, X, Eye } from 'lucide-react';
import type { LeaveRequestDto } from '../../api/api';

interface LeavePanelProps {
  search: string;
  setSearch: (s: string) => void;
  statusFilter: string;
  setStatusFilter: (s: string) => void;
  setShowAddLeave: (b: boolean) => void;
  leaveLoading: boolean;
  leaveRequests: LeaveRequestDto[];
  leaveStatusConfig: Record<string, { label: string; cls: string }>;
  leaveTypes: { value: string; label: string }[];
  handleApproveLeave: (id: string) => void;
  handleRejectLeave: (id: string) => void;
}

const LeavePanel: React.FC<LeavePanelProps> = ({
  search,
  setSearch,
  statusFilter,
  setStatusFilter,
  setShowAddLeave,
  leaveLoading,
  leaveRequests,
  leaveStatusConfig,
  leaveTypes,
  handleApproveLeave,
  handleRejectLeave
}) => {
  return (
    <>
      <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
        <div style={{ position: 'relative', flex: 1 }}>
          <Search size={14} style={{ position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
          <input type="text" placeholder="搜尋請假單號、員工編號、姓名..." value={search}
            onChange={e => setSearch(e.target.value)}
            style={{ width: '100%', padding: '8px 12px 8px 32px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-200)', fontSize: 13, outline: 'none', fontFamily: 'inherit' }} />
        </div>
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }}>
          <option value="">全部狀態</option>
          <option value="pending">待審</option>
          <option value="approved">核准</option>
          <option value="rejected">退回</option>
        </select>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddLeave(true)}><Plus size={13} />新增請假</button>
      </div>

      <div className="glass-card" style={{ padding: '18px' }}>
        <div style={{ marginBottom: 14 }}>
          <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>請假申請</h3>
          <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {leaveRequests.length} 筆請假記錄</div>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>單號</th><th>員工編號</th><th>姓名</th><th>假別</th><th>起訖日期</th><th>時數</th><th>狀態</th><th>操作</th>
            </tr>
          </thead>
          <tbody>
            {leaveLoading ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
            ) : leaveRequests.length === 0 ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無請假記錄</td></tr>
            ) : (
              leaveRequests.map(record => {
                const sc = leaveStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                const lt = leaveTypes.find(l => l.value === record.leaveType);
                return (
                  <tr key={record.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.applicationNo}</td>
                    <td className="mono" style={{ fontSize: 12 }}>{record.employeeNo}</td>
                    <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{lt?.label ?? record.leaveType}</td>
                    <td className="mono" style={{ fontSize: 11 }}>{record.startDate.slice(0, 10)} ~ {record.endDate.slice(0, 10)}</td>
                    <td style={{ fontSize: 12 }}>{record.totalHours}h</td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {record.status === 'pending' && (
                          <>
                            <button className="icon-action-btn" title="核准" onClick={() => handleApproveLeave(record.id)}><Check size={14} /></button>
                            <button className="icon-action-btn" title="退回" onClick={() => handleRejectLeave(record.id)}><X size={14} /></button>
                          </>
                        )}
                        <button className="icon-action-btn" title="查看"><Eye size={14} /></button>
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

export default LeavePanel;
