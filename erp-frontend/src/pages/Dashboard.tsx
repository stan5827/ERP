import React from 'react';
import { motion } from 'framer-motion';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { 
  TrendingUp, TrendingDown, AlertTriangle, Megaphone,
  UserPlus, ClipboardCheck, FileDown,
  ChevronRight,
  ShieldCheck, Users, Briefcase, Activity, Sparkles, Package
} from 'lucide-react';
import { useDashboard } from '../hooks/useApi';
import type { DashboardKpi } from '../api/api';
import type { ModuleId } from '../types/index.ts';
import './styles/Dashboard-Luxury.css';

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: { staggerChildren: 0.1 }
  }
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1, transition: { type: 'spring', stiffness: 100, damping: 20 } }
} as const;

function PremiumKpiCard({ kpi }: { kpi: DashboardKpi }) {
  const isPositive = kpi.changeDirection === 'up';
  const color = isPositive ? 'var(--lux-emerald)' : '#ef4444';
  
  const iconMap: Record<string, React.ReactNode> = {
    'Total Employees': <Users size={20} />,
    'Today Attendance': <Activity size={20} />,
    'Pending Approvals': <ClipboardCheck size={20} />,
    'Low Stock Alerts': <Package size={20} />,
    '員工總數': <Users size={20} />,
    '今日出勤': <Activity size={20} />,
    '待審核項目': <ClipboardCheck size={20} />,
    '低庫存警示': <Package size={20} />,
    'Active Alerts': <AlertTriangle size={20} />,
  };
  const titleMap: Record<string, string> = {
    'total employees': '員工總數',
    'today attendance': '今日出勤',
    'pending approvals': '待審核項目',
    'low stock alerts': '低庫存警示',
  };
  const title = titleMap[kpi.title.trim().toLocaleLowerCase()] ?? kpi.title;

  return (
    <motion.div 
      variants={itemVariants}
      className="lux-stat-card"
      whileHover={{ y: -5, transition: { duration: 0.2 } }}
    >
      <div className="lux-stat-icon" style={{ color: 'var(--lux-gold)' }}>
        {iconMap[kpi.title] || <TrendingUp size={20} />}
      </div>
      <div className="lux-stat-label">{title}</div>
      <div className="lux-stat-value-container">
        <span className="lux-stat-value">{kpi.value}</span>
        {kpi.unit && <span className="lux-stat-unit">{kpi.unit}</span>}
      </div>
      {kpi.changePercent !== undefined && (
        <div className="lux-stat-trend" style={{ color: color }}>
          {isPositive ? <TrendingUp size={14} /> : <TrendingDown size={14} />}
          <span>{isPositive ? '+' : ''}{kpi.changePercent}%</span>
          <span style={{ color: '#64748b', fontSize: 11, fontWeight: 400, marginLeft: 6 }}>vs 歷史均值</span>
        </div>
      )}
    </motion.div>
  );
}

interface DashboardTooltipProps {
  active?: boolean;
  payload?: Array<{ color?: string; value?: string | number; name?: string | number }>;
  label?: string | number;
}

const CustomTooltip = ({ active, payload, label }: DashboardTooltipProps) => {
  if (!active || !payload?.length) return null;
  return (
    <div className="lux-tooltip">
      <div className="lux-tooltip-label">{label}</div>
      {payload.map((p, i) => (
        <div key={i} className="lux-tooltip-item">
          <div className="lux-tooltip-dot" style={{ background: p.color }} />
          <span className="lux-tooltip-value">{p.value}</span>
          <span className="lux-tooltip-name">{p.name}</span>
        </div>
      ))}
    </div>
  );
};

export function Dashboard({ onNavigate }: { onNavigate?: (id: ModuleId) => void }) {
  const { data: resp, isLoading, isFetching, refetch } = useDashboard();
  const data = resp?.data;
  const navigate = onNavigate || (() => {});

  if (isLoading) return <div className="dashboard-loading">
    <motion.div 
      animate={{ rotate: 360 }} 
      transition={{ repeat: Infinity, duration: 1, ease: 'linear' }}
      className="loading-spinner" 
    />
  </div>;

  if (!data) return (
    <div className="dashboard-error">
      無法載入系統資料
      <button
        type="button"
        disabled={isFetching}
        onClick={() => void refetch()}
        style={{ marginLeft: 12 }}
      >
        {isFetching ? '載入中...' : '重新載入'}
      </button>
    </div>
  );

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="dashboard-container"
    >
      <header className="lux-header">
        <div className="lux-title-group">
          <div className="lux-subtitle">Global Enterprise Intelligence</div>
          <h1 className="lux-main-title">企業核心智能監控</h1>
          <div className="lux-header-meta">
            <span><Users size={14} /> {data.totalEmployees} 位員工</span>
            <span><ShieldCheck size={14} /> {data.pendingApprovals} 項待核</span>
            <span><AlertTriangle size={14} color="var(--lux-gold)" /> {data.activeAlerts} 項系統警示</span>
          </div>
        </div>
        <div className="lux-header-actions">
          <button className="lux-btn-primary" onClick={() => navigate('ai')}>
            <Sparkles size={18} /> AI 決策中心
          </button>
        </div>
      </header>

      {/* KPI 卡片區 */}
      <div className="lux-stats-grid">
        {data.kpiCards.map((kpi, i) => (
          <PremiumKpiCard key={i} kpi={kpi} />
        ))}
      </div>

      {/* 主內容區：圖表 + 工單 */}
      <div className="dashboard-main-grid">
        <div className="dashboard-left-col">
          {/* 生產效率圖表 */}
          <motion.div variants={itemVariants} className="lux-card chart-card">
            <div className="lux-card-header">
              <div className="lux-card-title-group">
                <div className="lux-tag">實時數據分析</div>
                <h3 className="lux-card-title">全域生產效能趨勢</h3>
              </div>
              <div className="lux-chart-legend">
                <div className="legend-item">
                  <div className="legend-dot" style={{ background: 'var(--lux-gold)' }} /> 計畫
                </div>
                <div className="legend-item">
                  <div className="legend-dot" style={{ background: 'var(--lux-emerald)' }} /> 實際
                </div>
              </div>
            </div>
            <div className="lux-chart-container">
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={data.productionTrend}>
                  <defs>
                    <linearGradient id="colorPlanned" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="var(--lux-gold)" stopOpacity={0.2}/>
                      <stop offset="95%" stopColor="var(--lux-gold)" stopOpacity={0}/>
                    </linearGradient>
                    <linearGradient id="colorActual" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="var(--lux-emerald)" stopOpacity={0.2}/>
                      <stop offset="95%" stopColor="var(--lux-emerald)" stopOpacity={0}/>
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(212, 175, 55, 0.05)" vertical={false} />
                  <XAxis dataKey="date" axisLine={false} tickLine={false} tick={{ fill: '#64748b', fontSize: 11 }} dy={10} />
                  <YAxis axisLine={false} tickLine={false} tick={{ fill: '#64748b', fontSize: 11 }} dx={-10} />
                  <Tooltip content={<CustomTooltip />} cursor={{ stroke: 'var(--lux-gold)', strokeWidth: 1 }} />
                  <Area type="monotone" dataKey="planned" name="計畫目標" stroke="var(--lux-gold)" strokeWidth={3} fillOpacity={1} fill="url(#colorPlanned)" />
                  <Area type="monotone" dataKey="actual" name="實際產量" stroke="var(--lux-emerald)" strokeWidth={3} fillOpacity={1} fill="url(#colorActual)" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </motion.div>

          {/* 生產工單列表 */}
          <motion.div variants={itemVariants} className="lux-card wo-card">
            <div className="lux-card-header">
              <h3 className="lux-card-title">優先核心工單</h3>
              <button className="lux-tab-btn" onClick={() => navigate('manufacturing')}>
                管理全域工單 <ChevronRight size={14} />
              </button>
            </div>
            <div className="wo-list-luxury">
              {data.recentWorkOrders.slice(0, 4).map((wo, i) => (
                <div key={i} className="wo-item-luxury">
                   <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
                      <span style={{ fontSize: '0.75rem', color: 'var(--lux-gold)', fontWeight: 800, fontFamily: 'monospace' }}>#{wo.woNumber}</span>
                      <span className="lux-badge" style={{ 
                        background: wo.status === 'completed' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(212, 175, 55, 0.1)',
                        color: wo.status === 'completed' ? 'var(--lux-emerald)' : 'var(--lux-gold)'
                      }}>
                        {wo.status === 'completed' ? '生產完成' : '生產中'}
                      </span>
                   </div>
                   <div style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: 16 }}>{wo.productName}</div>
                   <div className="lux-progress-group">
                      <div className="lux-progress-bar">
                         <motion.div 
                           className="lux-progress-fill"
                           initial={{ width: 0 }}
                           animate={{ width: `${(wo.completedQty / wo.plannedQty) * 100}%` }}
                           transition={{ duration: 1, delay: 0.2 }}
                         />
                      </div>
                      <span className="lux-progress-text">{Math.round((wo.completedQty / wo.plannedQty) * 100)}%</span>
                   </div>
                </div>
              ))}
            </div>
          </motion.div>
        </div>

        <div className="dashboard-sidebar">
          {/* 快捷工具箱 */}
          <motion.div variants={itemVariants} className="lux-card sidebar-widget">
            <div className="lux-card-header" style={{ marginBottom: 20 }}>
              <h3 className="lux-card-title">核心捷徑</h3>
            </div>
            <div className="lux-action-grid">
              {[
                { icon: <Megaphone size={18} />, label: '全域公告', id: 'collaboration' },
                { icon: <UserPlus size={18} />, label: '人才招募', id: 'hr' },
                { icon: <Briefcase size={18} />, label: '產能排程', id: 'aps' },
                { icon: <FileDown size={18} />, label: '結算中心', id: 'actual_costing' },
              ].map((item, i) => (
              <button key={i} className="lux-action-btn" onClick={() => navigate(item.id as ModuleId)}>
                  <div className="lux-action-icon">{item.icon}</div>
                  <span className="lux-action-label">{item.label}</span>
                </button>
              ))}
            </div>
          </motion.div>

          {/* 系統消息 */}
          <motion.div variants={itemVariants} className="lux-card sidebar-widget" style={{ flex: 1 }}>
            <div className="lux-card-header">
              <h3 className="lux-card-title">智能警訊通知</h3>
              <div className="lux-dot-pulse red" />
            </div>
            <div className="lux-alert-list">
              {data.alerts.slice(0, 5).map((alert, i) => (
                <div key={i} className="lux-alert-item">
                  <div className="lux-alert-icon">
                    {alert.severity === 'danger' ? <AlertTriangle size={16} color="#ef4444" /> : <Activity size={16} color="var(--lux-gold)" />}
                  </div>
                  <div className="lux-alert-content">
                    <div className="alert-title">{alert.title}</div>
                    <div className="alert-time">{new Date(alert.createdAt).toLocaleTimeString()} · 已校閱</div>
                  </div>
                </div>
              ))}
            </div>
          </motion.div>
        </div>
      </div>
    </motion.div>
  );
}
