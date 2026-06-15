import React, { useState } from 'react';
import { TrendingUp, AlertTriangle, Zap, ArrowUpRight, ArrowDownRight, Activity, BarChart3, Info, BellRing } from 'lucide-react';
import { useBudgetAnomalies, useBudgetAlerts, useBudgetAnalysis, useDepartments } from '../hooks/useApi';

export function BudgetAnalysis() {
  const { data: anomaliesResp, isLoading: anomaliesLoading } = useBudgetAnomalies();
  const { data: alertsResp, isLoading: alertsLoading } = useBudgetAlerts();
  const { data: deptsResp } = useDepartments({ pageSize: '100' });
  
  const [selectedDept, setSelectedDept] = useState<string>('');
  const { data: analysisResp, isLoading: analysisLoading } = useBudgetAnalysis(selectedDept, 2026);

  const anomalies = anomaliesResp || [];
  const alerts = alertsResp || [];
  const departments = deptsResp?.data || [];
  const analysis = analysisResp;

  return (
    <div className="budget-analysis-container page-enter">
      <div className="analysis-grid">
        {/* Left Column: AI Alerts & Anomalies */}
        <div className="analysis-sidebar">
          <div className="glass-card analysis-card">
            <div className="card-header-with-icon">
              <BellRing size={16} className="text-amber-400" />
              <h4 className="card-title">智慧預算告警 ({alerts.length})</h4>
            </div>
            <div className="alert-list">
              {alertsLoading ? (
                <div className="loading-state">載入中...</div>
              ) : alerts.length === 0 ? (
                <div className="empty-state">目前無異常告警</div>
              ) : (
                alerts.map((alert: any, idx: number) => (
                  <div key={idx} className={`alert-item ${alert.severity}`}>
                    <div className="alert-item-header">
                      <span className="alert-dept">{alert.costCenterName}</span>
                      <span className="alert-tag">{alert.alertType === 'Overrun' ? '預算超支' : '即將超出'}</span>
                    </div>
                    <div className="alert-msg">{alert.message}</div>
                    <div className="alert-meta">
                      <span>預算: ${alert.budgetAmount?.toLocaleString()}</span>
                      <span className="text-red-400">已用: ${alert.actualAmount?.toLocaleString()}</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          <div className="glass-card analysis-card mt-4">
            <div className="card-header-with-icon">
              <Zap size={16} className="text-cobalt-400" />
              <h4 className="card-title">AI 支出異常偵測</h4>
            </div>
            <div className="anomaly-list">
              {anomaliesLoading ? (
                <div className="loading-state">載入中...</div>
              ) : anomalies.length === 0 ? (
                <div className="empty-state">未偵測到支出異常</div>
              ) : (
                anomalies.map((anom: any, idx: number) => (
                  <div key={idx} className="anomaly-item">
                    <div className="anomaly-header">
                      <span className="anomaly-code">{anom.costCenterCode}</span>
                      <div className="anomaly-score-badge">
                        異常分數: <span className="score-val">{anom.anomalyScore.toFixed(1)}</span>
                      </div>
                    </div>
                    <div className="anomaly-reason">{anom.reason}</div>
                    <div className="anomaly-comparison">
                      <div>平均支出: ${anom.averageAmount?.toLocaleString()}</div>
                      <div className="text-red-400">本次支出: ${anom.actualAmount?.toLocaleString()}</div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Performance Analysis */}
        <div className="analysis-main">
          <div className="glass-card analysis-card h-full">
            <div className="main-header">
              <div className="header-title-group">
                <BarChart3 size={20} className="text-emerald-400" />
                <h3 className="main-title">預算執行效能分析 (AI 預測)</h3>
              </div>
              <select 
                className="dept-select"
                value={selectedDept}
                onChange={(e) => setSelectedDept(e.target.value)}
              >
                <option value="">選擇部門/成本中心...</option>
                {departments.map((d: any) => (
                  <option key={d.id} value={d.deptCode}>{d.deptName} ({d.deptCode})</option>
                ))}
              </select>
            </div>

            {!selectedDept ? (
              <div className="select-prompt">
                <Activity size={48} className="prompt-icon" />
                <p>請選擇一個部門以進行深度 AI 預算分析</p>
              </div>
            ) : analysisLoading ? (
              <div className="loading-state-large">AI 正在計算中...</div>
            ) : analysis ? (
              <div className="analysis-content">
                <div className="insight-grid">
                  <div className="insight-box">
                    <div className="insight-label">年度執行率</div>
                    <div className="insight-value">{(analysis.executionRate * 100).toFixed(1)}%</div>
                    <div className="insight-trend">
                      {analysis.executionRate > 0.9 ? (
                        <span className="text-red-400 flex items-center gap-1"><ArrowUpRight size={14} /> 偏高</span>
                      ) : (
                        <span className="text-emerald-400 flex items-center gap-1"><ArrowDownRight size={14} /> 正常</span>
                      )}
                    </div>
                  </div>
                  <div className="insight-box">
                    <div className="insight-label">剩餘可用額度</div>
                    <div className="insight-value">${analysis.remainingBudget?.toLocaleString()}</div>
                    <div className="insight-sub">總額: ${analysis.totalBudget?.toLocaleString()}</div>
                  </div>
                  <div className="insight-box">
                    <div className="insight-label">AI 預測年度結餘</div>
                    <div className={`insight-value ${analysis.predictedYearEndBalance < 0 ? 'text-red-400' : 'text-emerald-400'}`}>
                      ${analysis.predictedYearEndBalance?.toLocaleString()}
                    </div>
                    <div className="insight-trend">
                      {analysis.predictedYearEndBalance < 0 ? '預期將超支' : '預期符合預算'}
                    </div>
                  </div>
                </div>

                <div className="ai-observation-card">
                  <div className="observation-header">
                    <Zap size={14} />
                    <span>AI 觀察報告</span>
                  </div>
                  <div className="observation-body">
                    <p className="observation-text">
                      根據當前執行速度，該部門預計在第 <span className="font-bold text-amber-400">{analysis.monthsUntilOverrun?.toFixed(1) || 'N/A'}</span> 個月耗盡預算。
                    </p>
                    <div className="recommendation-list">
                      {analysis.recommendations?.map((rec: string, i: number) => (
                        <div key={i} className="rec-item">
                          <Info size={12} className="mt-1" />
                          <span>{rec}</span>
                        </div>
                      ))}
                      {analysis.recommendations?.length === 0 && (
                        <div className="rec-item">
                          <Info size={12} className="mt-1" />
                          <span>目前無特定建議。</span>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                {/* Trend Visual with Real Data */}
                <div className="trend-visual">
                  <div className="visual-header">月度支出趨勢與預測</div>
                  <div className="visual-bars">
                    {analysis.monthlyUtilization?.map((item: any, i: number) => (
                      <div key={i} className="bar-group">
                        <div className="bar-label">M{item.month}</div>
                        <div className="bar-container">
                          <div 
                            className="bar-fill" 
                            style={{ 
                              height: `${Math.min(100, (item.actual / (item.budget || 1)) * 100)}%`, 
                              background: item.actual > item.budget ? 'var(--red-500)' : 'var(--cobalt-500)' 
                            }}
                          ></div>
                        </div>
                      </div>
                    ))}
                    {/* Prediction bar */}
                    <div className="bar-group">
                      <div className="bar-label">P</div>
                      <div className="bar-container">
                        <div 
                          className="bar-fill prediction" 
                          style={{ height: `${Math.min(100, (analysis.executionRate * 100))}%` }}
                        ></div>
                      </div>
                    </div>
                  </div>
                  <div className="visual-legend">
                    <div className="legend-item"><span className="dot bg-cobalt"></span> 實際</div>
                    <div className="legend-item"><span className="dot bg-red"></span> 超支</div>
                    <div className="legend-item"><span className="dot bg-prediction"></span> AI 預測</div>
                  </div>
                </div>
              </div>
            ) : (
              <div className="empty-state">無法獲取分析資料</div>
            )}
          </div>
        </div>
      </div>

      <style>{`
        .budget-analysis-container {
          padding: 16px 0;
        }
        .analysis-grid {
          display: grid;
          grid-template-columns: 320px 1fr;
          gap: 16px;
          min-height: 600px;
        }
        .analysis-card {
          padding: 16px;
          display: flex;
          flex-direction: column;
        }
        .card-header-with-icon {
          display: flex;
          align-items: center;
          gap: 8px;
          margin-bottom: 16px;
          border-bottom: 1px solid var(--navy-700);
          padding-bottom: 10px;
        }
        .card-title {
          font-size: 14px;
          font-weight: 700;
          color: var(--slate-200);
          margin: 0;
        }
        .alert-list, .anomaly-list {
          display: flex;
          flex-direction: column;
          gap: 10px;
          overflow-y: auto;
          max-height: 400px;
        }
        .alert-item, .anomaly-item {
          background: var(--navy-850);
          border: 1px solid var(--navy-700);
          border-radius: var(--radius-md);
          padding: 12px;
          transition: transform 0.2s;
        }
        .alert-item.High { border-left: 4px solid var(--red-500); }
        .alert-item.Medium { border-left: 4px solid var(--amber-500); }
        .alert-item.Low { border-left: 4px solid var(--emerald-500); }
        
        .alert-item-header, .anomaly-header {
          display: flex;
          justify-content: space-between;
          margin-bottom: 6px;
        }
        .alert-dept, .anomaly-code {
          font-size: 12px;
          font-weight: 700;
          color: var(--cobalt-300);
        }
        .alert-tag {
          font-size: 10px;
          background: rgba(239, 68, 68, 0.1);
          color: var(--red-400);
          padding: 1px 6px;
          border-radius: 4px;
        }
        .alert-msg, .anomaly-reason {
          font-size: 12px;
          color: var(--slate-300);
          margin-bottom: 8px;
          line-height: 1.4;
        }
        .alert-meta, .anomaly-comparison {
          display: flex;
          justify-content: space-between;
          font-size: 11px;
          color: var(--slate-500);
          font-family: var(--font-mono);
        }
        .anomaly-score-badge {
          font-size: 10px;
          color: var(--slate-500);
        }
        .score-val {
          color: var(--amber-400);
          font-weight: 700;
        }
        .main-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 24px;
          padding-bottom: 16px;
          border-bottom: 1px solid var(--navy-700);
        }
        .header-title-group {
          display: flex;
          align-items: center;
          gap: 10px;
        }
        .main-title {
          font-size: 18px;
          font-weight: 700;
          color: var(--slate-100);
          margin: 0;
        }
        .dept-select {
          background: var(--navy-850);
          border: 1px solid var(--navy-700);
          color: var(--slate-200);
          font-size: 13px;
          padding: 6px 12px;
          border-radius: var(--radius-md);
          outline: none;
        }
        .select-prompt {
          flex: 1;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          color: var(--slate-500);
          gap: 16px;
        }
        .prompt-icon { opacity: 0.2; }
        .insight-grid {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: 16px;
          margin-bottom: 24px;
        }
        .insight-box {
          background: linear-gradient(145deg, var(--navy-800), var(--navy-900));
          border: 1px solid var(--navy-700);
          border-radius: var(--radius-lg);
          padding: 20px;
          position: relative;
          overflow: hidden;
        }
        .insight-label {
          font-size: 12px;
          color: var(--slate-500);
          margin-bottom: 8px;
        }
        .insight-value {
          font-size: 24px;
          font-weight: 700;
          color: var(--slate-100);
          font-family: var(--font-display);
        }
        .insight-trend {
          font-size: 12px;
          margin-top: 8px;
        }
        .insight-sub {
          font-size: 11px;
          color: var(--slate-600);
          margin-top: 4px;
        }
        .ai-observation-card {
          background: rgba(45, 110, 245, 0.05);
          border: 1px solid rgba(45, 110, 245, 0.2);
          border-radius: var(--radius-lg);
          padding: 16px;
          margin-bottom: 24px;
        }
        .observation-header {
          display: flex;
          align-items: center;
          gap: 8px;
          font-size: 13px;
          font-weight: 700;
          color: var(--cobalt-400);
          margin-bottom: 12px;
        }
        .observation-text {
          font-size: 14px;
          color: var(--slate-200);
          margin-bottom: 12px;
        }
        .recommendation-list {
          display: flex;
          flex-direction: column;
          gap: 8px;
        }
        .rec-item {
          display: flex;
          gap: 8px;
          font-size: 12px;
          color: var(--slate-400);
        }
        .trend-visual {
          border-top: 1px solid var(--navy-700);
          padding-top: 24px;
        }
        .visual-header {
          font-size: 14px;
          font-weight: 700;
          color: var(--slate-300);
          margin-bottom: 20px;
        }
        .visual-bars {
          display: flex;
          align-items: flex-end;
          gap: 12px;
          height: 150px;
          padding-bottom: 20px;
          border-bottom: 1px solid var(--navy-800);
        }
        .bar-group {
          flex: 1;
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;
        }
        .bar-container {
          width: 100%;
          height: 100px;
          background: var(--navy-850);
          border-radius: 4px;
          display: flex;
          align-items: flex-end;
          overflow: hidden;
        }
        .bar-fill {
          width: 100%;
          transition: height 1s ease-out;
        }
        .bar-fill.prediction {
          background: repeating-linear-gradient(
            45deg,
            var(--navy-700),
            var(--navy-700) 5px,
            var(--navy-600) 5px,
            var(--navy-600) 10px
          );
          border: 1px dashed var(--slate-600);
        }
        .bar-label {
          font-size: 10px;
          color: var(--slate-500);
          font-family: var(--font-mono);
        }
        .visual-legend {
          display: flex;
          gap: 16px;
          margin-top: 12px;
          justify-content: center;
        }
        .legend-item {
          display: flex;
          align-items: center;
          gap: 6px;
          font-size: 11px;
          color: var(--slate-500);
        }
        .dot { width: 8px; height: 8px; border-radius: 2px; }
        .bg-cobalt { background: var(--cobalt-500); }
        .bg-red { background: var(--red-500); }
        .bg-prediction { background: var(--navy-600); border: 1px dashed var(--slate-500); }
        
        .loading-state-large {
          flex: 1;
          display: flex;
          align-items: center;
          justify-content: center;
          color: var(--cobalt-400);
          font-weight: 600;
          letter-spacing: 0.1em;
        }
      `}</style>
    </div>
  );
}
