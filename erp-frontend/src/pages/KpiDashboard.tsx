import React, { useState, useEffect } from 'react';
import { kpiApi } from '../api/api';
import { 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  LineChart, Line, PieChart, Pie, Cell, AreaChart, Area
} from 'recharts';
import { Activity, Target, AlertTriangle, TrendingUp, Package } from 'lucide-react';

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#82CA9D'];

export const KpiDashboard: React.FC = () => {
  const [dashboard, setDashboard] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPeriod, setSelectedPeriod] = useState('month');

  useEffect(() => {
    loadDashboard();
  }, [selectedPeriod]);

  const loadDashboard = async () => {
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
  };

  if (loading) {
    return (
      <div className="loading-container">
        <div className="spinner"></div>
        <p>載入中...</p>
      </div>
    );
  }

  if (!dashboard) {
    return <div className="error">無法載入KPI數據</div>;
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
      <div className="page-header">
        <h1>
          <Activity size={24} />
          KPI 績效看板
        </h1>
        <div className="period-selector">
          <select value={selectedPeriod} onChange={(e) => setSelectedPeriod(e.target.value)}>
            <option value="week">本週</option>
            <option value="month">本月</option>
            <option value="quarter">本季</option>
          </select>
          <button onClick={loadDashboard} className="refresh-btn">重新整理</button>
        </div>
      </div>

      {/* KPI 卡片 */}
      <div className="kpi-cards">
        <div className="kpi-card">
          <div className="kpi-icon" style={{ backgroundColor: getOeeColor(dashboard.averageOee) }}>
            <Activity size={24} />
          </div>
          <div className="kpi-content">
            <span className="kpi-label">平均 OEE</span>
            <span className="kpi-value">{dashboard.averageOee?.toFixed(1)}%</span>
            <span className="kpi-unit">設備綜合效率</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon" style={{ backgroundColor: dashboard.onTimeDeliveryRate >= 90 ? '#10B981' : '#F59E0B' }}>
            <Target size={24} />
          </div>
          <div className="kpi-content">
            <span className="kpi-label">準時交貨率</span>
            <span className="kpi-value">{dashboard.onTimeDeliveryRate?.toFixed(1)}%</span>
            <span className="kpi-unit">{dashboard.onTimeOrders}/{dashboard.totalOrders} 訂單</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon" style={{ backgroundColor: '#3B82F6' }}>
            <Package size={24} />
          </div>
          <div className="kpi-content">
            <span className="kpi-label">在製品 (WIP)</span>
            <span className="kpi-value">{dashboard.wipQuantity?.toLocaleString()}</span>
            <span className="kpi-unit">件</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon" style={{ backgroundColor: dashboard.scrapAmount > 100000 ? '#EF4444' : '#F59E0B' }}>
            <AlertTriangle size={24} />
          </div>
          <div className="kpi-content">
            <span className="kpi-label">報廢損失</span>
            <span className="kpi-value">${dashboard.scrapAmount?.toLocaleString()}</span>
            <span className="kpi-unit">{dashboard.scrapQuantity?.toLocaleString()} 件</span>
          </div>
        </div>

        <div className="kpi-card">
          <div className="kpi-icon" style={{ backgroundColor: '#8B5CF6' }}>
            <TrendingUp size={24} />
          </div>
          <div className="kpi-content">
            <span className="kpi-label">產能利用率</span>
            <span className="kpi-value">{dashboard.capacityUtilization?.toFixed(1)}%</span>
            <span className="kpi-unit">整體產能</span>
          </div>
        </div>
      </div>

      {/* 圖表區域 */}
      <div className="charts-grid">
        {/* OEE by Work Center */}
        <div className="chart-card">
          <h3>各工作中心 OEE</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={dashboard.oeeByWorkCenter}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="workCenterName" />
              <YAxis domain={[0, 100]} />
              <Tooltip formatter={(value) => `${Number(value).toFixed(1)}%`} />
              <Legend />
              <Bar dataKey="oeeValue" name="OEE" fill="#3B82F6">
                {dashboard.oeeByWorkCenter?.map((entry: any, index: number) => (
                  <Cell key={`cell-${index}`} fill={getOeeColor(entry.oeeValue)} />
                ))}
              </Bar>
              <Bar dataKey="availability" name="稼動率" fill="#10B981" />
              <Bar dataKey="performance" name="性能效率" fill="#F59E0B" />
              <Bar dataKey="quality" name="品質率" fill="#8B5CF6" />
            </BarChart>
          </ResponsiveContainer>
        </div>

        {/* OEE Trend */}
        <div className="chart-card">
          <h3>OEE 趨勢</h3>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={dashboard.oeeTrend}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="periodStart" tickFormatter={(date) => new Date(date).toLocaleDateString()} />
              <YAxis domain={[0, 100]} />
              <Tooltip 
                labelFormatter={(date) => new Date(date).toLocaleDateString()}
                formatter={(value) => `${Number(value).toFixed(1)}%`}
              />
              <Legend />
              <Line type="monotone" dataKey="value" name="OEE" stroke="#3B82F6" strokeWidth={2} dot={{ r: 4 }} />
              <Line type="monotone" dataKey="targetValue" name="目標" stroke="#EF4444" strokeDasharray="5 5" />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Scrap by Reason */}
        <div className="chart-card">
          <h3>報廢原因分析</h3>
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
                {Object.entries(dashboard.scrapByReason || {}).map(([name], index) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip formatter={(value) => Number(value).toLocaleString()} />
            </PieChart>
          </ResponsiveContainer>
        </div>

        {/* Top Scrap Items */}
        <div className="chart-card">
          <h3>前5大報廢料號</h3>
          <div className="table-container">
            <table>
              <thead>
                <tr>
                  <th>料號</th>
                  <th>品名</th>
                  <th>數量</th>
                  <th>金額</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.topScrapItems?.map((item: any, index: number) => (
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
        </div>

        {/* OTD Trend */}
        <div className="chart-card">
          <h3>準時交貨率趨勢</h3>
          <ResponsiveContainer width="100%" height={300}>
            <AreaChart data={dashboard.otdTrend}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="periodStart" tickFormatter={(date) => new Date(date).toLocaleDateString()} />
              <YAxis domain={[0, 100]} />
              <Tooltip 
                labelFormatter={(date) => new Date(date).toLocaleDateString()}
                formatter={(value) => `${Number(value).toFixed(1)}%`}
              />
              <Legend />
              <Area type="monotone" dataKey="value" name="OTD" stroke="#10B981" fill="#10B981" fillOpacity={0.3} />
              <Line type="monotone" dataKey="targetValue" name="目標" stroke="#EF4444" strokeDasharray="5 5" />
            </AreaChart>
          </ResponsiveContainer>
        </div>

        {/* WIP by Work Center */}
        <div className="chart-card">
          <h3>各工作中心 WIP 分佈</h3>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart 
              data={Object.entries(dashboard.wipByWorkCenter || {}).map(([id, qty]) => ({ 
                workCenterId: id, 
                quantity: qty 
              }))}
            >
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="workCenterId" />
              <YAxis />
              <Tooltip formatter={(value) => Number(value).toLocaleString()} />
              <Legend />
              <Bar dataKey="quantity" name="WIP 數量" fill="#3B82F6" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* 工作中心 OEE 明細 */}
      <div className="detail-section">
        <h3>工作中心 OEE 明細</h3>
        <div className="oee-grid">
          {dashboard.oeeByWorkCenter?.map((wc: any, index: number) => (
            <div key={index} className="oee-card">
              <div className="oee-header">
                <h4>{wc.workCenterName}</h4>
                <span 
                  className={`status-badge ${wc.status}`}
                  style={{ backgroundColor: getOeeColor(wc.oeeValue) }}
                >
                  {getStatusLabel(wc.status)}
                </span>
              </div>
              <div className="oee-gauge">
                <div className="gauge-value" style={{ color: getOeeColor(wc.oeeValue) }}>
                  {wc.oeeValue?.toFixed(1)}%
                </div>
                <div className="gauge-label">OEE</div>
              </div>
              <div className="oee-components">
                <div className="component">
                  <span className="label">稼動率</span>
                  <div className="progress-bar">
                    <div 
                      className="progress" 
                      style={{ width: `${wc.availability}%`, backgroundColor: '#10B981' }}
                    />
                  </div>
                  <span className="value">{wc.availability?.toFixed(1)}%</span>
                </div>
                <div className="component">
                  <span className="label">性能效率</span>
                  <div className="progress-bar">
                    <div 
                      className="progress" 
                      style={{ width: `${wc.performance}%`, backgroundColor: '#F59E0B' }}
                    />
                  </div>
                  <span className="value">{wc.performance?.toFixed(1)}%</span>
                </div>
                <div className="component">
                  <span className="label">品質率</span>
                  <div className="progress-bar">
                    <div 
                      className="progress" 
                      style={{ width: `${wc.quality}%`, backgroundColor: '#8B5CF6' }}
                    />
                  </div>
                  <span className="value">{wc.quality?.toFixed(1)}%</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};