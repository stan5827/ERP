import React from 'react';
import { Loader2, RefreshCw } from 'lucide-react';

interface PlanningPanelProps {
  ropLoading: boolean;
  ropSuggestions: any[];
  isProcessing: boolean;
  handleRunRop: () => void;
  handleConvertSuggestionToPo: (s: any) => void;
}

const PlanningPanel: React.FC<PlanningPanelProps> = ({
  ropLoading,
  ropSuggestions,
  isProcessing,
  handleRunRop,
  handleConvertSuggestionToPo
}) => {
  return (
    <div className="glass-card no-lift">
      <div style={{ padding: '16px', borderBottom: '1px solid rgba(255,255,255,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h3 style={{ margin: 0, fontSize: '16px' }}>庫存水位預警與建議</h3>
          <p style={{ margin: '4px 0 0', fontSize: '12px', color: 'var(--slate-500)' }}>基於再訂購點 (ROP) 與安全庫存進行智動分析</p>
        </div>
        <button className="btn btn-sm btn-outline" onClick={handleRunRop} disabled={isProcessing}>
          {isProcessing ? <Loader2 className="spin" size={14} /> : <><RefreshCw size={14} style={{ marginRight: 6 }} /> 執行 ROP 運算</>}
        </button>
      </div>
      <table className="lux-table">
        <thead>
          <tr>
            <th>物料名稱</th><th>現有庫存</th><th>ROP 水位</th><th>安全庫存</th><th>建議採購量</th><th>預警原因</th><th>操作</th>
          </tr>
        </thead>
        <tbody>
          {ropLoading ? <tr><td colSpan={7} className="td-loading"><Loader2 className="spin" /></td></tr> : 
            ropSuggestions.length === 0 ? <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: 'var(--slate-500)' }}>目前所有物料水位正常。</td></tr> :
            ropSuggestions.map((s: any, idx: number) => (
              <tr key={idx}>
                <td>
                  <div className="emp-label">{s.sku_name}</div>
                  <div className="mono-label">{s.sku_code}</div>
                </td>
                <td className="amount-label">{Number(s.on_hand_qty).toLocaleString()}</td>
                <td className="amount-label" style={{ color: 'var(--amber-400)' }}>{Number(s.reorder_point).toLocaleString()}</td>
                <td className="amount-label" style={{ color: 'var(--rose-400)' }}>{Number(s.safety_stock).toLocaleString()}</td>
                <td className="amount-label" style={{ color: 'var(--emerald-400)', fontSize: '15px' }}>{Number(s.suggested_qty).toLocaleString()}</td>
                <td>
                  <span className={`badge-lux ${s.reason === 'SAFETY_STOCK_ALERT' ? 'badge-warning' : 'badge-info'}`}>
                    {s.reason === 'SAFETY_STOCK_ALERT' ? '低於安全庫存' : '低於再訂購點'}
                  </span>
                </td>
                <td>
                  <button className="btn-sm btn-primary" onClick={() => handleConvertSuggestionToPo(s)}>
                    轉成採購單
                  </button>
                </td>
              </tr>
            ))
          }
        </tbody>
      </table>
    </div>
  );
};

export default PlanningPanel;
