import React from 'react';
import { Loader2, Plus, Eye } from 'lucide-react';

interface RequisitionsPanelProps {
  prLoading: boolean;
  prData: any;
  setSelectedPr: (pr: any) => void;
  setShowAddPr: (b: boolean) => void;
  setShowEditPr: (b: boolean) => void;
  setShowAddPoFromPr: (b: boolean) => void;
  setPrApprovalAction: (a: 'submit' | 'approve' | 'reject' | 'return') => void;
  setShowPrApproval: (b: boolean) => void;
  handlePrSubmit: (id: string) => void;
}

const RequisitionsPanel: React.FC<RequisitionsPanelProps> = ({
  prLoading,
  prData,
  setSelectedPr,
  setShowAddPr,
  setShowEditPr,
  setShowAddPoFromPr,
  setPrApprovalAction,
  setShowPrApproval,
  handlePrSubmit
}) => {
  return (
    <div className="glass-card no-lift">
      <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h3 style={{ margin: 0, fontSize: '16px' }}>請購單列表 (PR)</h3>
          <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>採購申請單管理與審批流程</p>
        </div>
        <button className="btn btn-primary btn-sm" onClick={() => { setSelectedPr(null); setShowAddPr(true); }}>
          <Plus size={14} style={{ marginRight: 6 }} /> 新增請購單
        </button>
      </div>
      <table className="lux-table">
        <thead>
          <tr>
            <th>請購單號</th><th>日期</th><th>部門</th><th>金額</th><th>狀態</th><th>操作</th>
          </tr>
        </thead>
        <tbody>
          {prLoading ? <tr><td colSpan={6} className="td-loading"><Loader2 className="spin" /></td></tr> : 
            (prData?.data || []).length === 0 ? <tr><td colSpan={6} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>尚無請購單資料</td></tr> :
            (prData?.data || []).map((pr: any) => (
              <tr key={pr.id}>
                <td className="mono-label">{pr.prNumber}</td>
                <td className="date-label">{pr.prDate?.slice(0, 10)}</td>
                <td className="emp-label">{pr.departmentName || '—'}</td>
                <td className="amount-label">{Number(pr.totalEstimatedAmount || 0).toLocaleString()}</td>
                <td>
                  <span className={`badge-lux badge-${
                    pr.status === 'DRAFT' ? 'neutral' : 
                    pr.status === 'SUBMITTED' || pr.status === 'UNDER_REVIEW' ? 'info' : 
                    pr.status === 'APPROVED' ? 'success' : 
                    pr.status === 'REJECTED' ? 'danger' : 'neutral'
                  }`}>
                    {pr.status === 'DRAFT' ? '草稿' : 
                     pr.status === 'SUBMITTED' ? '待審' :
                     pr.status === 'UNDER_REVIEW' ? '審核中' :
                     pr.status === 'APPROVED' ? '已核准' :
                     pr.status === 'REJECTED' ? '已駁回' :
                     pr.status === 'RETURNED' ? '退回' : pr.status}
                  </span>
                </td>
                <td>
                  <div className="action-row">
                    {pr.status === 'DRAFT' && (
                      <button className="btn-sm btn-primary" onClick={() => handlePrSubmit(pr.id)}>送出審批</button>
                    )}
                    {pr.status === 'SUBMITTED' && (
                      <>
                        <button className="btn-sm btn-primary" onClick={() => { setSelectedPr(pr); setPrApprovalAction('approve'); setShowPrApproval(true); }}>核准</button>
                        <button className="btn-sm btn-outline" style={{ marginLeft: 4 }} onClick={() => { setSelectedPr(pr); setPrApprovalAction('reject'); setShowPrApproval(true); }}>駁回</button>
                      </>
                    )}
                    {pr.status === 'APPROVED' && (
                      <button className="btn-sm btn-outline" onClick={() => { setSelectedPr(pr); setShowAddPoFromPr(true); }}>轉PO</button>
                    )}
                    <button className="action-btn" title="檢視" onClick={() => { setSelectedPr(pr); setShowEditPr(true); }}><Eye size={13} /></button>
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

export default RequisitionsPanel;
