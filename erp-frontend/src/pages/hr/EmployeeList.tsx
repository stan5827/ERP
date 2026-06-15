import React from 'react';
import { Search, Plus, Eye, Edit2, X } from 'lucide-react';
import { employeesApi } from '../../api/api';

interface EmployeeListProps {
  search: string;
  setSearch: (s: string) => void;
  statusFilter: string;
  setStatusFilter: (s: string) => void;
  setShowAdd: (b: boolean) => void;
  isLoading: boolean;
  employees: any[];
  statusConfig: Record<string, { label: string; cls: string }>;
  setViewingEmployee: (emp: any) => void;
  setEditingEmployee: (emp: any) => void;
  setTerminatingEmployee: (emp: any) => void;
  setTerminateDate: (d: string) => void;
  setTerminateReason: (r: string) => void;
}

const EmployeeList: React.FC<EmployeeListProps> = ({
  search,
  setSearch,
  statusFilter,
  setStatusFilter,
  setShowAdd,
  isLoading,
  employees,
  statusConfig,
  setViewingEmployee,
  setEditingEmployee,
  setTerminatingEmployee,
  setTerminateDate,
  setTerminateReason
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
        <select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}
          style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }}>
          <option value="">全部狀態</option>
          <option value="active">在職</option>
          <option value="leave">留職停薪</option>
          <option value="terminated">已離職</option>
        </select>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={13} />新增員工</button>
      </div>

      <div className="glass-card" style={{ padding: '18px' }}>
        <div style={{ marginBottom: 14 }}>
          <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>員工名冊</h3>
          <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {employees.length} 筆員工資料</div>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>員工編號</th><th>姓名</th><th>部門</th><th>職位</th><th>狀態</th><th>手機</th><th>入職日</th><th>操作</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
            ) : employees.length === 0 ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無員工資料</td></tr>
            ) : (
              employees.map(emp => {
                const empStatus = (emp.status || '').toLowerCase();
                const sc = statusConfig[empStatus];
                const displayLabel = sc?.label || emp.status || '未知';
                const displayCls = sc?.cls || 'neutral';
                return (
                  <tr key={emp.id}>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{emp.employeeNo}</td>
                    <td style={{ fontWeight: 600, fontSize: 13 }}>{emp.chineseName}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{emp.departmentName ?? '—'}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{emp.jobTitleName ?? emp.positionTitle ?? '—'}</td>
                    <td><span className={`status-badge status-${displayCls}`}>{displayLabel}</span></td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{emp.mobile ?? '—'}</td>
                    <td className="mono" style={{ fontSize: 11, color: 'var(--slate-500)' }}>{emp.hireDate?.slice(0, 10) ?? '—'}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <button className="icon-action-btn" title="查看" onClick={() => { employeesApi.get(emp.id).then(res => setViewingEmployee(res.data?.data)).catch(console.error); }}><Eye size={14} /></button>
                        <button className="icon-action-btn" title="編輯" onClick={() => { employeesApi.get(emp.id).then(res => setEditingEmployee(res.data?.data)).catch(console.error); }}><Edit2 size={14} /></button>
                        {emp.status?.toLowerCase() === 'active' && (
                          <button className="icon-action-btn" title="離職" onClick={() => { employeesApi.get(emp.id).then(res => { setTerminatingEmployee(res.data?.data); setTerminateDate(new Date().toISOString().slice(0, 10)); setTerminateReason(''); }).catch(console.error); }} style={{ color: 'var(--crimson-400)' }}><X size={14} /></button>
                        )}
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

export default EmployeeList;
