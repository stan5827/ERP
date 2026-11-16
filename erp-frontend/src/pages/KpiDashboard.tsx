import React, { useCallback, useState, useEffect } from 'react';
import { kpiApi } from '../api/api';
import type { KpiDashboardDto } from '../api/api';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  LineChart, Line, PieChart, Pie, Cell, AreaChart, Area
} from 'recharts';
import { Activity, Target, AlertTriangle, TrendingUp, Package } from 'lucide-react';
import './KpiDashboard.css';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D'];

export const KpiDashboard: React.FC = () => {
  const [dashboard, setDashboard] = useState<KpiDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPeriod, setSelectedPeriod] = useState('month');

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    try {
      const now = new Date();
      let fromDate: Date;
      
      switch (selectedPeriod) {
        case 'week':
          fromDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
          break;
        case 'month':
          fromDate = new Date(now.getFullYear(), now.getMonth(), 1);
          break;
        case 'quarter':
          fromDate = new Date(now.getFullYear(), Math.floor(now.getMonth() / 3) * 3, 1);
          break;
        default:
          fromDate = new Date(now.getFullYear(), now.getMonth(), 1);
      }
      
      const response = await kpiApi.dashboard({
        fromDate: fromDate.toISOString(),
        toDate: now.toISOString()
      });
      
      if (response.data?.success) {
        setDashboard(response.data.data);
      }
    } catch (error) {
      console.error('Failed to load KPI dashboard:', error);
    } finally {
      setLoading(false);
    }
  }, [selectedPeriod]);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  if (loading) {
    return (
      <div className="kpi-dashboard-state">
        <div className="spinner"></div>
        <p>載入中...</p>
      </div>
    );
  }

  if (!dashboard) {
    return <div className="kpi-dashboard-state kpi-dashboard-state--error">無法載入 KPI 數據，請重新整理。</div>;
  }

  const getOeeColor = (oee: number) => {
    if (oee >= 85) return '#10B981';
    if (oee >= 70) return '#F59E0B';
    if (oee >= 50) return '#F97316';
    return '#EF4444';
  };

  const getStatusLabel = (status: string) => {
    switch (status) {
      case 'excellent': return '優秀';
      case 'good': return '良好';
      case 'warning': return '警告';
      case 'critical': return '嚴重';
      default: return status;
    }
  };

  return (
    <div className="kpi-dashboard">
      <header className="kpi-dashboard__header">
        <div className="kpi-dashboard__heading">
          <span className="kpi-dashboard__eyebrow">營運績效總覽</span>
          <h1><Activity size={22} />KPI 績效看板</h1>
          <p>掌握設備效率、交期表現與生產損失</p>
        </div>
        <div className="kpi-dashboard__actions">
          <label className="kpi-dashboard__period">
            <span>統計期間</span>
            <select className="form-select" value={selectedPeriod} onChange={(e) => setSelectedPeriod(e.target.value)}>
            <option value="week">本週</option>
            <option value="month">本月</option>
            <option value="quarter">本季</option>
            </select>
          </label>
          <button onClick={loadDashboard} className="kpi-dashboard__refresh">重新整理</button>
        </div>
      </header>

      {/* KPI 卡片 */}
      <section className="kpi-dashboard__summary" aria-label="績效摘要">
        <div className="kpi-dashboard__metric">
          <div className="kpi-dashboard__metric-icon" style={{ '--metric-color': getOeeColor(dashboard.averageOee) } as React.CSSProperties}>
            <Activity size={24} />
          </div>
          <div className="kpi-dashboard__metric-content">
            <span className="kpi-dashboard__metric-label">平均 OEE</span>
            <span className="kpi-dashboard__metric-value">{dashboard.averageOee?.toFixed(1)}<small>%</small></span>
            <span className="kpi-dashboard__metric-detail">設備綜合效率</span>
          </div>
        </div>

        <div className="kpi-dashboard__metric">
          <div className="kpi-dashboard__metric-icon" style={{ '--metric-color': dashboard.onTimeDeliveryRate >= 90 ? '#10B981' : '#F59E0B' } as React.CSSProperties}>
            <Target size={24} />
          </div>
          <div className="kpi-dashboard__metric-content">
            <span className="kpi-dashboard__metric-label">準時交貨率</span>
            <span className="kpi-dashboard__metric-value">{dashboard.onTimeDeliveryRate?.toFixed(1)}<small>%</small></span>
            <span className="kpi-dashboard__metric-detail">{dashboard.onTimeOrders}/{dashboard.totalOrders} 張訂單準時</span>
          </div>
        </div>

        <div className="kpi-dashboard__metric">
          <div className="kpi-dashboard__metric-icon" style={{ '--metric-color': '#3B82F6' } as React.CSSProperties}>
            <Package size={24} />
          </div>
          <div className="kpi-dashboard__metric-content">
            <span className="kpi-dashboard__metric-label">在製品 (WIP)</span>
            <span className="kpi-dashboard__metric-value">{dashboard.wipQuantity?.toLocaleString()}</span>
            <span className="kpi-dashboard__metric-detail">件</span>
          </div>
        </div>

        <div className="kpi-dashboard__metric">
          <div className="kpi-dashboard__metric-icon" style={{ '--metric-color': dashboard.scrapAmount > 100000 ? '#EF4444' : '#F59E0B' } as React.CSSProperties}>
            <AlertTriangle size={24} />
          </div>
          <div className="kpi-dashboard__metric-content">
            <span className="kpi-dashboard__metric-label">報廢損失</span>
            <span className="kpi-dashboard__metric-value"><small>$</small>{dashboard.scrapAmount?.toLocaleString()}</span>
            <span className="kpi-dashboard__metric-detail">{dashboard.scrapQuantity?.toLocaleString()} 件</span>
          </div>
        </div>

        <div className="kpi-dashboard__metric">
          <div className="kpi-dashboard__metric-icon" style={{ '--metric-color': '#8B5CF6' } as React.CSSProperties}>
            <TrendingUp size={24} />
          </div>
          <div className="kpi-dashboard__metric-content">
            <span className="kpi-dashboard__metric-label">產能利用率</span>
            <span className="kpi-dashboard__metric-value">{dashboard.capacityUtilization?.toFixed(1)}<small>%</small></span>
            <span className="kpi-dashboard__metric-detail">整體產能</span>
          </div>
        </div>
      </section>

      {/* 圖表區域 */}
      <section className="kpi-dashboard__section">
        <div className="kpi-dashboard__section-heading">
          <div>
            <span className="kpi-dashboard__eyebrow">績效分析</span>
            <h2>營運趨勢與分佈</h2>
          </div>
        </div>
      <div className="kpi-dashboard__charts">
        {/* OEE by Work Center */}
        <article className="kpi-dashboard__chart kpi-dashboard__chart--wide">
          <div className="kpi-dashboard__chart-heading">
            <div><h3>各工作中心 OEE</h3><p>比較各中心綜合效率與組成</p></div>
          </div>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={dashboard.oeeByWorkCenter}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(160,176,204,0.12)" />
              <XAxis dataKey="workCenterName" tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={{ stroke: 'rgba(160,176,204,0.2)' }} tickLine={false} />
              <YAxis domain={[0, 100]} tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ background: '#0d1829', border: '1px solid rgba(212,175,55,0.2)', borderRadius: 10 }} formatter={(value) => `${Number(value).toFixed(1)}%`} />
              <Legend wrapperStyle={{ color: '#a0b0cc', fontSize: 12, paddingTop: 12 }} />
              <Bar dataKey="oeeValue" name="OEE" fill="#3B82F6">
                {dashboard.oeeByWorkCenter?.map((entry, index) => (
                  <Cell key={`cell-${index}`} fill={getOeeColor(entry.oeeValue)} />
                ))}
              </Bar>
              <Bar dataKey="availability" name="稼動率" fill="#10B981" />
              <Bar dataKey="performance" name="性能效率" fill="#F59E0B" />
              <Bar dataKey="quality" name="品質率" fill="#8B5CF6" />
            </BarChart>
          </ResponsiveContainer>
        </article>

        {/* OEE Trend */}
        <article className="kpi-dashboard__chart">
          <div className="kpi-dashboard__chart-heading"><div><h3>OEE 趨勢</h3><p>綜合效率與目標值</p></div></div>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={dashboard.oeeTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(160,176,204,0.12)" />
              <XAxis dataKey="periodStart" tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={{ stroke: 'rgba(160,176,204,0.2)' }} tickLine={false} tickFormatter={(date) => new Date(date as string).toLocaleDateString()} />
              <YAxis domain={[0, 100]} tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ background: '#0d1829', border: '1px solid rgba(212,175,55,0.2)', borderRadius: 10 }}
                labelFormatter={(date) => new Date(date as string).toLocaleDateString()}
                formatter={(value) => `${Number(value).toFixed(1)}%`}
              />
              <Legend wrapperStyle={{ color: '#a0b0cc', fontSize: 12, paddingTop: 12 }} />
              <Line type="monotone" dataKey="value" name="OEE" stroke="#3B82F6" strokeWidth={2} dot={{ r: 4 }} />
              <Line type="monotone" dataKey="targetValue" name="目標" stroke="#EF4444" strokeDasharray="5 5" />
            </LineChart>
          </ResponsiveContainer>
        </article>

        {/* Scrap by Reason */}
        <article className="kpi-dashboard__chart">
          <div className="kpi-dashboard__chart-heading"><div><h3>報廢原因分析</h3><p>依原因查看報廢數量占比</p></div></div>
          <ResponsiveContainer width="100%" height={300}>
            <PieChart>
              <Pie
                data={Object.entries(dashboard.scrapByReason || {}).map(([name, value]) => ({ name, value }))}
                cx="50%"
                cy="50%"
                labelLine={false}
                label={({ name, percent }) => `${name}: ${((percent ?? 0) * 100).toFixed(0)}%`}
                outerRadius={100}
                fill="#8884d8"
                dataKey="value"
              >
                {Object.entries(dashboard.scrapByReason || {}).map(([,], index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip contentStyle={{ background: '#0d1829', border: '1px solid rgba(212,175,55,0.2)', borderRadius: 10 }} formatter={(value) => Number(value).toLocaleString()} />
            </PieChart>
          </ResponsiveContainer>
        </article>

        {/* Top Scrap Items */}
        <article className="kpi-dashboard__chart kpi-dashboard__chart--table">
          <div className="kpi-dashboard__chart-heading"><div><h3>前 5 大報廢料號</h3><p>依報廢金額排序</p></div></div>
          <div className="kpi-dashboard__table-wrap">
            <table className="kpi-dashboard__table">
              <thead>
                <tr>
                  <th>料號</th>
                  <th>品名</th>
                  <th>數量</th>
                  <th>金額</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.topScrapItems?.map((item, index) => (
                  <tr key={index}>
                    <td>{item.skuNumber}</td>
                    <td>{item.skuName}</td>
                    <td>{item.scrapQuantity?.toLocaleString()}</td>
                    <td>${item.scrapAmount?.toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </article>

        {/* OTD Trend */}
        <article className="kpi-dashboard__chart">
          <div className="kpi-dashboard__chart-heading"><div><h3>準時交貨率趨勢</h3><p>交期達成率與目標比較</p></div></div>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={dashboard.otdTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(160,176,204,0.12)" />
              <XAxis dataKey="periodStart" tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={{ stroke: 'rgba(160,176,204,0.2)' }} tickLine={false} tickFormatter={(date) => new Date(date as string).toLocaleDateString()} />
              <YAxis domain={[0, 100]} tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ background: '#0d1829', border: '1px solid rgba(212,175,55,0.2)', borderRadius: 10 }}
                labelFormatter={(date) => new Date(date as string).toLocaleDateString()}
                formatter={(value) => `${Number(value).toFixed(1)}%`}
              />
              <Legend wrapperStyle={{ color: '#a0b0cc', fontSize: 12, paddingTop: 12 }} />
              <Area type="monotone" dataKey="value" name="OTD" stroke="#10B981" fill="#10B981" fillOpacity={0.3} />
              <Line type="monotone" dataKey="targetValue" name="目標" stroke="#EF4444" strokeDasharray="5 5" />
            </AreaChart>
          </ResponsiveContainer>
        </article>

        {/* WIP by Work Center */}
        <article className="kpi-dashboard__chart">
          <div className="kpi-dashboard__chart-heading"><div><h3>各工作中心 WIP 分佈</h3><p>在製品數量分布</p></div></div>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart 
              data={Object.entries(dashboard.wipByWorkCenter || {}).map(([id, qty]) => ({ 
                workCenterId: id, 
                quantity: qty 
              }))}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(160,176,204,0.12)" />
              <XAxis dataKey="workCenterId" tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={{ stroke: 'rgba(160,176,204,0.2)' }} tickLine={false} />
              <YAxis tick={{ fill: '#a0b0cc', fontSize: 11 }} axisLine={false} tickLine={false} />
              <Tooltip contentStyle={{ background: '#0d1829', border: '1px solid rgba(212,175,55,0.2)', borderRadius: 10 }} formatter={(value) => Number(value).toLocaleString()} />
              <Legend wrapperStyle={{ color: '#a0b0cc', fontSize: 12, paddingTop: 12 }} />
              <Bar dataKey="quantity" name="WIP 數量" fill="#3B82F6" />
            </BarChart>
          </ResponsiveContainer>
        </article>
      </div>
      </section>

      {/* 工作中心 OEE 明細 */}
      <section className="kpi-dashboard__section">
        <div className="kpi-dashboard__section-heading">
          <div><span className="kpi-dashboard__eyebrow">設備效率</span><h2>工作中心 OEE 明細</h2></div>
        </div>
        <div className="kpi-dashboard__oee-grid">
          {dashboard.oeeByWorkCenter?.map((wc, index) => (
            <article key={index} className="kpi-dashboard__oee-card">
              <div className="kpi-dashboard__oee-header">
                <h4>{wc.workCenterName}</h4>
                <span 
                  className="kpi-dashboard__status"
                  style={{ '--status-color': getOeeColor(wc.oeeValue) } as React.CSSProperties}
                >
                  {getStatusLabel(wc.status)}
                </span>
              </div>
              <div className="kpi-dashboard__oee-score">
                <div className="kpi-dashboard__oee-value" style={{ color: getOeeColor(wc.oeeValue) }}>
                  {wc.oeeValue?.toFixed(1)}%
                </div>
                <div className="kpi-dashboard__oee-label">OEE 綜合效率</div>
              </div>
              <div className="kpi-dashboard__components">
                <div className="kpi-dashboard__component">
                  <span className="kpi-dashboard__component-label">稼動率</span>
                  <div className="kpi-dashboard__progress">
                    <div 
                      className="kpi-dashboard__progress-value"
                      style={{ width: `${Math.min(100, Math.max(0, wc.availability ?? 0))}%`, backgroundColor: '#10B981' }}
                    />
                  </div>
                  <span className="kpi-dashboard__component-value">{wc.availability?.toFixed(1)}%</span>
                </div>
                <div className="kpi-dashboard__component">
                  <span className="kpi-dashboard__component-label">性能效率</span>
                  <div className="kpi-dashboard__progress">
                    <div 
                      className="kpi-dashboard__progress-value"
                      style={{ width: `${Math.min(100, Math.max(0, wc.performance ?? 0))}%`, backgroundColor: '#F59E0B' }}
                    />
                  </div>
                  <span className="kpi-dashboard__component-value">{wc.performance?.toFixed(1)}%</span>
                </div>
                <div className="kpi-dashboard__component">
                  <span className="kpi-dashboard__component-label">品質率</span>
                  <div className="kpi-dashboard__progress">
                    <div 
                      className="kpi-dashboard__progress-value"
                      style={{ width: `${Math.min(100, Math.max(0, wc.quality ?? 0))}%`, backgroundColor: '#8B5CF6' }}
                    />
                  </div>
                  <span className="kpi-dashboard__component-value">{wc.quality?.toFixed(1)}%</span>
                </div>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
};