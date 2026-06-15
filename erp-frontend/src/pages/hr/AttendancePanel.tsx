import React from 'react';
import { Search, Plus, Check, Eye } from 'lucide-react';
import type { AttendanceRecordDto } from '../../api/api';

interface AttendancePanelProps {
  search: string;
  setSearch: (s: string) => void;
  setShowAddAttendance: (b: boolean) => void;
  attendanceLoading: boolean;
  attendanceRecords: AttendanceRecordDto[];
  attendanceStatusConfig: Record<string, { label: string; cls: string }>;
  handleApproveAttendance: (id: string) => void;
}

const AttendancePanel: React.FC<AttendancePanelProps> = ({
  search,
  setSearch,
  setShowAddAttendance,
  attendanceLoading,
  attendanceRecords,
  attendanceStatusConfig,
  handleApproveAttendance
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
        <input type="date" style={{ padding: '8px 12px', background: 'var(--slate-800)', border: '1px solid var(--slate-700)', borderRadius: 6, color: 'var(--slate-300)', fontSize: 13, outline: 'none' }} />
        <button className="btn btn-primary btn-sm" onClick={() => setShowAddAttendance(true)}><Plus size={13} />新增出勤</button>
      </div>

      <div className="glass-card" style={{ padding: '18px' }}>
        <div style={{ marginBottom: 14 }}>
          <h3 className="section-title" style={{ fontSize: 14, fontWeight: 700, marginBottom: 2 }}>出勤記錄</h3>
          <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>共 {attendanceRecords.length} 筆出勤記錄</div>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>日期</th><th>員工編號</th><th>姓名</th><th>班別</th><th>打卡時間</th><th>狀態</th><th>加班</th><th>操作</th>
            </tr>
          </thead>
          <tbody>
            {attendanceLoading ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>載入中...</td></tr>
            ) : attendanceRecords.length === 0 ? (
              <tr><td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--slate-500)' }}>無出勤記錄</td></tr>
            ) : (
              attendanceRecords.map(record => {
                const sc = attendanceStatusConfig[record.status] ?? { label: record.status, cls: 'neutral' };
                const totalOt = (record.ot1Hours || 0) + (record.ot2Hours || 0) + (record.ot3Hours || 0);
                return (
                  <tr key={record.id}>
                    <td className="mono" style={{ fontSize: 12 }}>{record.date?.slice(0, 10)}</td>
                    <td className="mono" style={{ color: 'var(--cobalt-300)', fontSize: 12 }}>{record.employeeNo}</td>
                    <td style={{ fontWeight: 600, fontSize: 13 }}>{record.employeeName}</td>
                    <td style={{ fontSize: 12, color: 'var(--slate-400)' }}>{record.shiftType ?? '—'}</td>
                    <td className="mono" style={{ fontSize: 11 }}>
                      {record.clockIn?.slice(11, 16) ?? '—'} ~ {record.clockOut?.slice(11, 16) ?? '—'}
                    </td>
                    <td><span className={`status-badge status-${sc.cls}`}>{sc.label}</span></td>
                    <td style={{ fontSize: 12 }}>{totalOt > 0 ? `${totalOt}h` : '—'}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 4 }}>
                        {record.approvalStatus === 'pending' && (
                          <button className="icon-action-btn" title="核准" onClick={() => handleApproveAttendance(record.id)}><Check size={14} /></button>
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

export default AttendancePanel;
