import React, { useMemo } from 'react';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  Cell, ReferenceLine, LineChart as ReLineChart, Line, ComposedChart
} from 'recharts';
import { 
  BarChart2, AlertCircle, CheckCircle2, TrendingUp, Clock, 
  Filter, Download, ChevronRight, Activity, Zap, Factory, ShieldAlert
} from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { mpsApi, AggregateRccpDto, ResourceAggregateLoadDto } from '../api/api';
import './RccpDashboard.css';

export const RccpDashboard: React.FC = () => {
  // 獲取未來 12 週的彙總產能資料
  const { data: rccpData, isLoading } = useQuery({
    queryKey: ['aggregate-rccp'],
    queryFn: () => mpsApi.getAggregateRccp().then(res => res.data?.data)
  });

  const bottlenecks = useMemo(() => {
    if (!rccpData) return [];
    return rccpData.resources
      .filter(r => r.maxLoadPercentage > 90)
      .sort((a, b) => b.maxLoadPercentage - a.maxLoadPercentage);
  }, [rccpData]);

  const summary = useMemo(() => {
    if (!rccpData) return { total: 0, critical: 0, warning: 0, ok: 0 };
    const total = rccpData.resources.length;
    const critical = rccpData.resources.filter(r => r.maxLoadPercentage > 100).length;
    const warning = rccpData.resources.filter(r => r.maxLoadPercentage > 85 && r.maxLoadPercentage <= 100).length;
    return { total, critical, warning, ok: total - critical - warning };
  }, [rccpData]);

  if (isLoading) {
    return <div className="rccp-dashboard loading"><Activity className="animate-spin" /> 正在分析全廠產能...</div>;
  }

  return (
    <div className="rccp-dashboard">
      <div className="dashboard-header">
        <div className="title-area">
          <h1>全廠產能瓶頸儀錶板 <span className="premium-badge">RCCP Insights</span></h1>
          <p>彙總所有 MPS 計劃，預測未來 12 週資源負荷趨勢</p>
        </div>
        <div className="time-range">
          <Clock size={16} /> <span>{new Date(rccpData?.startDate || '').toLocaleDateString()} ~ {new Date(rccpData?.endDate || '').toLocaleDateString()}</span>
        </div>
      </div>

      <div className="stat-cards">
        <div className="stat-card critical">
          <div className="icon"><ShieldAlert /></div>
          <div className="content">
            <span className="label">超載資源</span>
            <span className="value">{summary.critical}</span>
          </div>
        </div>
        <div className="stat-card warning">
          <div className="icon"><AlertCircle /></div>
          <div className="content">
            <span className="label">高風險資源</span>
            <span className="value">{summary.warning}</span>
          </div>
        </div>
        <div className="stat-card success">
          <div className="icon"><CheckCircle2 /></div>
          <div className="content">
            <span className="label">健康資源</span>
            <span className="value">{summary.ok}</span>
          </div>
        </div>
        <div className="stat-card info">
          <div className="icon"><Factory /></div>
          <div className="content">
            <span className="label">監控總數</span>
            <span className="value">{summary.total}</span>
          </div>
        </div>
      </div>

      <div className="dashboard-grid">
        {/* 左側：熱力圖矩陣 */}
        <div className="chart-panel heatmap-panel">
          <div className="panel-header">
            <h3><Zap size={18} /> 產能負荷熱力圖 (未來 12 週)</h3>
            <div className="legend">
              <span className="l-item"><span className="dot red"></span> &gt;100%</span>
              <span className="l-item"><span className="dot yellow"></span> 85-100%</span>
              <span className="l-item"><span className="dot green"></span> &lt;85%</span>
            </div>
          </div>
          <div className="heatmap-container">
            <table className="heatmap-table">
              <thead>
                <tr>
                  <th className="res-th">資源名稱</th>
                  {rccpData?.resources[0]?.weeklyLoads.map(w => (
                    <th key={w.weekCode}>{w.weekCode}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rccpData?.resources.map(res => (
                  <tr key={res.resourceId}>
                    <td className="res-name-td">
                      <span className="code">{res.resourceCode}</span>
                      <span className="name">{res.resourceName}</span>
                    </td>
                    {res.weeklyLoads.map(w => {
                      const load = w.loadPercentage;
                      const level = load > 100 ? 'overload' : load > 85 ? 'warning' : 'ok';
                      return (
                        <td key={w.weekCode} className={`load-cell ${level}`}>
                          <div className="tooltip-content">
                            {load.toFixed(0)}%
                            <div className="detail">
                              需要: {w.required.toFixed(0)} {res.uom}<br/>
                              可用: {w.available.toFixed(0)} {res.uom}
                            </div>
                          </div>
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* 右側：瓶頸詳情 */}
        <div className="chart-panel bottleneck-list">
          <div className="panel-header">
            <h3><TrendingUp size={18} /> 關鍵瓶頸排行</h3>
          </div>
          <div className="list-container">
            {bottlenecks.length > 0 ? bottlenecks.map(b => (
              <div className="bottleneck-item" key={b.resourceId}>
                <div className="b-info">
                  <span className="b-name">{b.resourceName}</span>
                  <span className="b-type">{b.resourceType}</span>
                </div>
                <div className="b-bar-group">
                  <div className="b-bar-bg">
                    <div 
                      className={`b-bar-fill ${b.maxLoadPercentage > 100 ? 'bg-red' : 'bg-yellow'}`} 
                      style={{ width: `${Math.min(b.maxLoadPercentage, 100)}%` }}
                    ></div>
                  </div>
                  <span className="b-pct">{b.maxLoadPercentage.toFixed(0)}%</span>
                </div>
              </div>
            )) : (
              <div className="empty-bottleneck">
                <CheckCircle2 size={32} />
                <p>目前無明顯產能瓶頸</p>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="dashboard-footer">
        <div className="insight">
          <Activity size={16} /> 系統建議：{summary.critical > 0 ? `檢測到 ${summary.critical} 處嚴重超載，建議調整外協比例或增加加班班次。` : '產能狀況良好，可考慮提前排產以優化稼動。'}
        </div>
      </div>
    </div>
  );
};
