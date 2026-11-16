import React, { useCallback, useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { Users, Calendar, AlertTriangle, TrendingDown, RefreshCw, Brain, Activity, Shield, Target } from 'lucide-react';
import { api } from '../api/api';

interface ScheduleSummary {
  totalEmployees: number;
  totalShifts: number;
  weekendShifts: number;
  avgShiftsPerEmployee: number;
  utilizationRate: number;
  warnings: string[];
}

interface TurnoverRisk {
  employeeId: string;
  employeeCode: string;
  employeeName: string;
  riskScore: number;
  riskLevel: string;
  riskFactors: string[];
}

const RISK_COLORS = {
  high: { bg: '#fef2f2', border: '#fca5a5', text: '#dc2626', accent: '#ef4444' },
  medium: { bg: '#fffbeb', border: '#fcd34d', text: '#d97706', accent: '#f59e0b' },
  low: { bg: '#f0fdf4', border: '#86efac', text: '#16a34a', accent: '#22c55e' },
};

export const AiScheduling: React.FC = () => {
  const [yearMonth] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}`;
  });
  const [summary, setSummary] = useState<ScheduleSummary | null>(null);
  const [risks, setRisks] = useState<TurnoverRisk[]>([]);
  const [view, setView] = useState<'schedule' | 'risks'>('schedule');

  const loadSchedule = useCallback(async () => {
    try {
      const res = await api.post('/api/ai/schedule/generate', { yearMonth, targetHeadcount: 20 });
      if (res.data.success && res.data.data) {
        setSummary(res.data.data.summary);
      }
    } catch (e) {
      console.error('Generate schedule error:', e);
    }
  }, [yearMonth]);

  const loadRisks = useCallback(async () => {
    try {
      const res = await api.get('/api/ai/schedule/risks');
      if (res.data.success && res.data.data) {
        setRisks(res.data.data || []);
      }
    } catch (e) {
      console.error('Load risks error:', e);
    }
  }, []);

  useEffect(() => {
    if (view === 'schedule') {
      void api.post('/api/ai/schedule/generate', { yearMonth, targetHeadcount: 20 })
        .then(res => {
          if (res.data.success && res.data.data) setSummary(res.data.data.summary);
        })
        .catch(error => console.error('Generate schedule error:', error));
    } else {
      void api.get('/api/ai/schedule/risks')
        .then(res => {
          if (res.data.success && res.data.data) setRisks(res.data.data || []);
        })
        .catch(error => console.error('Load risks error:', error));
    }
  }, [view, yearMonth]);

  const getRiskColor = (level: string) => RISK_COLORS[level as keyof typeof RISK_COLORS] || RISK_COLORS.low;

  const mockScheduleData = [
    { name: '王小明', 白班: 15, 夜班: 8, 中班: 5 },
    { name: '李美華', 白班: 12, 夜班: 10, 中班: 6 },
    { name: '張志偉', 白班: 18, 夜班: 4, 中班: 2 },
    { name: '林怡君', 白班: 10, 夜班: 12, 中班: 4 },
    { name: '陳建國', 白班: 14, 夜班: 6, 中班: 8 },
  ];

  const mockRisks: TurnoverRisk[] = [
    { employeeId: '1', employeeCode: 'EMP001', employeeName: '王小明', riskScore: 78, riskLevel: 'high', riskFactors: ['近期請假頻繁', '考績連續低落', '部門人員流動率高'] },
    { employeeId: '2', employeeCode: 'EMP002', employeeName: '李美華', riskScore: 55, riskLevel: 'medium', riskFactors: ['年資達3年', '近期表現下滑'] },
    { employeeId: '3', employeeCode: 'EMP003', employeeName: '張志偉', riskScore: 32, riskLevel: 'low', riskFactors: ['工作滿意度一般'] },
    { employeeId: '4', employeeCode: 'EMP004', employeeName: '林怡君', riskScore: 68, riskLevel: 'medium', riskFactors: ['薪资期望未達', '有轉職意向'] },
  ];

  const summaryData = mockScheduleData;
  const displayRisks = mockRisks.length > 0 ? mockRisks : risks;
  const displaySummary = summary || { totalEmployees: 5, totalShifts: 150, weekendShifts: 28, avgShiftsPerEmployee: 6, utilizationRate: 85, warnings: [] };

  const chartColors = {
    grid: '#e5e7eb',
    text: '#6b7280',
    bar1: '#3b82f6',
    bar2: '#10b981',
    bar3: '#f59e0b',
  };

  return (
    <div className="ai-scheduling-page">
      <style>{`
        .ai-scheduling-page {
          padding: 24px;
          background: linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%);
          min-height: 100vh;
          color: #f1f5f9;
        }
        .page-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 32px;
          padding-bottom: 20px;
          border-bottom: 1px solid rgba(255,255,255,0.1);
        }
        .page-title {
          display: flex;
          align-items: center;
          gap: 16px;
        }
        .page-title h2 {
          font-size: 28px;
          font-weight: 700;
          margin: 0;
          background: linear-gradient(135deg, #60a5fa, #a78bfa);
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
          background-clip: text;
        }
        .page-title-icon {
          width: 48px;
          height: 48px;
          border-radius: 16px;
          background: linear-gradient(135deg, #3b82f6, #8b5cf6);
          display: flex;
          align-items: center;
          justify-content: center;
          box-shadow: 0 8px 32px rgba(139, 92, 246, 0.3);
        }
        .page-desc {
          color: #94a3b8;
          font-size: 14px;
          margin-top: 4px;
        }
        .tab-bar {
          display: flex;
          gap: 8px;
          background: rgba(255,255,255,0.05);
          padding: 6px;
          border-radius: 12px;
          margin-bottom: 24px;
        }
        .tab-btn {
          padding: 12px 24px;
          border: none;
          background: transparent;
          color: #94a3b8;
          font-size: 14px;
          font-weight: 600;
          border-radius: 8px;
          cursor: pointer;
          transition: all 0.3s ease;
          display: flex;
          align-items: center;
          gap: 8px;
        }
        .tab-btn:hover {
          background: rgba(255,255,255,0.05);
          color: #e2e8f0;
        }
        .tab-btn.active {
          background: linear-gradient(135deg, #3b82f6, #8b5cf6);
          color: white;
          box-shadow: 0 4px 20px rgba(139, 92, 246, 0.4);
        }
        .stats-grid {
          display: grid;
          grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
          gap: 20px;
          margin-bottom: 32px;
        }
        .stat-card {
          background: linear-gradient(135deg, rgba(30, 41, 59, 0.8), rgba(15, 23, 42, 0.9));
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 20px;
          padding: 24px;
          position: relative;
          overflow: hidden;
          transition: all 0.3s ease;
        }
        .stat-card::before {
          content: '';
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          height: 4px;
          background: linear-gradient(90deg, #3b82f6, #8b5cf6, #ec4899);
        }
        .stat-card:hover {
          transform: translateY(-4px);
          box-shadow: 0 20px 40px rgba(0,0,0,0.3);
        }
        .stat-icon {
          width: 44px;
          height: 44px;
          border-radius: 12px;
          display: flex;
          align-items: center;
          justify-content: center;
          margin-bottom: 16px;
        }
        .stat-value {
          font-size: 32px;
          font-weight: 700;
          margin-bottom: 4px;
        }
        .stat-label {
          font-size: 13px;
          color: #94a3b8;
        }
        .chart-card {
          background: linear-gradient(135deg, rgba(30, 41, 59, 0.8), rgba(15, 23, 42, 0.9));
          border: 1px solid rgba(255,255,255,0.1);
          border-radius: 20px;
          padding: 28px;
          margin-bottom: 24px;
        }
        .chart-title {
          font-size: 18px;
          font-weight: 600;
          margin-bottom: 24px;
          display: flex;
          align-items: center;
          gap: 10px;
        }
        .chart-title-icon {
          width: 36px;
          height: 36px;
          border-radius: 10px;
          background: rgba(59, 130, 246, 0.2);
          display: flex;
          align-items: center;
          justify-content: center;
          color: #60a5fa;
        }
        .risk-card {
          padding: 20px;
          border-radius: 16px;
          margin-bottom: 16px;
          border-left: 4px solid;
          transition: all 0.3s ease;
        }
        .risk-card:hover {
          transform: translateX(8px);
        }
        .risk-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          margin-bottom: 12px;
        }
        .risk-score {
          font-size: 28px;
          font-weight: 700;
        }
        .risk-factors {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
          margin-top: 12px;
        }
        .risk-factor-tag {
          padding: 4px 12px;
          border-radius: 20px;
          font-size: 12px;
          background: rgba(255,255,255,0.1);
        }
        .action-btn {
          padding: 12px 24px;
          border: none;
          border-radius: 12px;
          font-weight: 600;
          cursor: pointer;
          transition: all 0.3s ease;
          display: flex;
          align-items: center;
          gap: 8px;
        }
        .action-btn-primary {
          background: linear-gradient(135deg, #3b82f6, #8b5cf6);
          color: white;
          box-shadow: 0 4px 20px rgba(139, 92, 246, 0.4);
        }
        .action-btn-primary:hover {
          transform: translateY(-2px);
          box-shadow: 0 8px 30px rgba(139, 92, 246, 0.5);
        }
      `}</style>

      <div className="page-header">
        <div className="page-title">
          <div className="page-title-icon">
            <Brain size={24} color="white" />
          </div>
          <div>
            <h2>AI 智能排班</h2>
            <p className="page-desc">根據歷史產能與人力配置 AI 自動排班 · 離職風險預警</p>
          </div>
        </div>
        <button className="action-btn action-btn-primary" onClick={() => view === 'schedule' ? loadSchedule() : loadRisks()}>
          <RefreshCw size={18} />
          重新產生
        </button>
      </div>

      <div className="tab-bar">
        <button className={`tab-btn ${view === 'schedule' ? 'active' : ''}`} onClick={() => setView('schedule')}>
          <Calendar size={18} />
          排班表
        </button>
        <button className={`tab-btn ${view === 'risks' ? 'active' : ''}`} onClick={() => setView('risks')}>
          <TrendingDown size={18} />
          離職風險預警
        </button>
      </div>

      <div className="stats-grid">
        <div className="stat-card" onClick={() => setView('schedule')} style={{ cursor: 'pointer' }}>
          <div className="stat-icon" style={{ background: 'rgba(59, 130, 246, 0.2)' }}>
            <Users size={22} color="#60a5fa" />
          </div>
          <div className="stat-value" style={{ color: '#60a5fa' }}>{displaySummary.totalEmployees}</div>
          <div className="stat-label">員員工數</div>
        </div>
        <div className="stat-card" onClick={() => setView('schedule')} style={{ cursor: 'pointer' }}>
          <div className="stat-icon" style={{ background: 'rgba(16, 185, 129, 0.2)' }}>
            <Calendar size={22} color="#34d399" />
          </div>
          <div className="stat-value" style={{ color: '#34d399' }}>{displaySummary.totalShifts}</div>
          <div className="stat-label">總班次數</div>
        </div>
        <div className="stat-card" onClick={() => setView('schedule')} style={{ cursor: 'pointer' }}>
          <div className="stat-icon" style={{ background: 'rgba(245, 158, 11, 0.2)' }}>
            <Activity size={22} color="#fbbf24" />
          </div>
          <div className="stat-value" style={{ color: '#fbbf24' }}>{displaySummary.weekendShifts}</div>
          <div className="stat-label">假日班次</div>
        </div>
        <div className="stat-card" onClick={() => setView('risks')} style={{ cursor: 'pointer' }}>
          <div className="stat-icon" style={{ background: 'rgba(139, 92, 246, 0.2)' }}>
            <Target size={22} color="#a78bfa" />
          </div>
          <div className="stat-value" style={{ color: '#a78bfa' }}>{displaySummary.utilizationRate}%</div>
          <div className="stat-label">平均風險值</div>
        </div>
      </div>

      {view === 'schedule' && (
        <div className="chart-card">
          <div className="chart-title">
            <div className="chart-title-icon">
              <Activity size={18} />
            </div>
            班別分布統計
          </div>
          <div style={{ height: 350 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={summaryData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="name" stroke="#94a3b8" fontSize={13} />
                <YAxis stroke="#94a3b8" fontSize={13} />
                <Tooltip 
                  contentStyle={{ 
                    background: '#1e293b', 
                    border: '1px solid #475569', 
                    borderRadius: 12,
                    boxShadow: '0 10px 40px rgba(0,0,0,0.3)'
                  }}
                  labelStyle={{ color: '#f1f5f9' }}
                />
                <Bar dataKey="白班" fill={chartColors.bar1} radius={[8, 8, 0, 0]} />
                <Bar dataKey="夜班" fill={chartColors.bar2} radius={[8, 8, 0, 0]} />
                <Bar dataKey="中班" fill={chartColors.bar3} radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {view === 'risks' && (
        <div className="chart-card">
          <div className="chart-title">
            <div className="chart-title-icon">
              <AlertTriangle size={18} />
            </div>
            離職風險預警排行
          </div>
          {displayRisks.map((risk) => {
            const colors = getRiskColor(risk.riskLevel);
            return (
              <div 
                key={risk.employeeId} 
                className="risk-card" 
                style={{ background: colors.bg, borderColor: colors.border, borderLeftColor: colors.accent, cursor: 'pointer' }}
                onClick={() => alert(`正在進行 ${risk.employeeName} 的離職干預與談話規劃...`)}
              >
                <div className="risk-header">
                  <div>
                    <div style={{ fontWeight: 600, fontSize: 16, color: colors.text }}>{risk.employeeName}</div>
                    <div style={{ fontSize: 13, color: '#64748b' }}>{risk.employeeCode}</div>
                  </div>
                  <div style={{ textAlign: 'right' }}>
                    <div className="risk-score" style={{ color: colors.accent }}>{risk.riskScore}</div>
                    <div style={{ fontSize: 12, color: colors.text }}>風險分數</div>
                  </div>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Shield size={14} style={{ color: colors.accent }} />
                  <span style={{ fontSize: 13, color: colors.text, fontWeight: 600 }}>
                    {risk.riskLevel === 'high' ? '高風險' : risk.riskLevel === 'medium' ? '中風險' : '低風險'}
                  </span>
                </div>
                <div className="risk-factors">
                  {risk.riskFactors.map((factor, i) => (
                    <span key={i} className="risk-factor-tag" style={{ color: colors.text }}>{factor}</span>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default AiScheduling;
