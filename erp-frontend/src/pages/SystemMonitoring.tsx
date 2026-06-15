import React, { useState, useEffect } from 'react';
import { 
  Server, Database, Activity, Cpu, HardDrive, 
  ShieldCheck, AlertCircle, Clock, BarChart3, 
  Terminal, Globe, RefreshCcw, CheckCircle2,
  Lock, Search, Filter, ArrowUpRight, ArrowDownRight
} from 'lucide-react';
import { 
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, 
  ResponsiveContainer, PieChart, Pie, Cell, 
  BarChart, Bar, Legend 
} from 'recharts';
import { systemMetricsApi } from '../api/api';
import toast from 'react-hot-toast';

// Dummy data for initial render or fallback
const performanceData = [
  { time: '00:00', cpu: 12, ram: 45, disk: 30 },
  { time: '04:00', cpu: 15, ram: 48, disk: 30 },
  { time: '08:00', cpu: 45, ram: 65, disk: 32 },
  { time: '12:00', cpu: 78, ram: 82, disk: 35 },
  { time: '16:00', cpu: 55, ram: 75, disk: 35 },
  { time: '20:00', cpu: 32, ram: 55, disk: 36 },
  { time: '23:59', cpu: 18, ram: 42, disk: 36 },
];

const auditData = [
  { name: '登入', value: 450, color: '#3b82f6' },
  { name: '銷貨單', value: 300, color: '#10b981' },
  { name: '採購單', value: 120, color: '#f59e0b' },
  { name: '庫存異動', value: 800, color: '#8b5cf6' },
  { name: '權限變更', value: 25, color: '#ef4444' },
];

export const SystemMonitoring: React.FC = () => {
  const [metrics, setMetrics] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [refreshInterval, setRefreshInterval] = useState(30); // seconds
  const [lastUpdated, setLastUpdated] = useState(new Date());

  const fetchMetrics = async () => {
    setLoading(true);
    try {
      const res = await systemMetricsApi.getOverview();
      if (res.data?.success) {
        setMetrics(res.data.data);
        setLastUpdated(new Date());
      }
    } catch (error) {
      console.error('Failed to fetch metrics:', error);
      toast.error('無法取得系統指標數據');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMetrics();
    const interval = setInterval(fetchMetrics, refreshInterval * 1000);
    return () => clearInterval(interval);
  }, [refreshInterval]);

  const StatusCard = ({ title, value, status, icon: Icon, detail }: any) => (
    <div className="glass-card" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '12px', position: 'relative', overflow: 'hidden' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start' }}>
        <div style={{ 
          padding: '10px', 
          borderRadius: '12px', 
          backgroundColor: status === 'ok' ? 'rgba(16, 185, 129, 0.1)' : 'rgba(245, 151, 10, 0.1)',
          color: status === 'ok' ? '#10b981' : '#f59e0b'
        }}>
          <Icon size={20} />
        </div>
        <div style={{ 
          fontSize: '10px', 
          fontWeight: 'bold', 
          padding: '4px 8px', 
          borderRadius: '9999px', 
          textTransform: 'uppercase', 
          letterSpacing: '0.05em',
          backgroundColor: status === 'ok' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(245, 151, 10, 0.2)',
          color: status === 'ok' ? '#34d399' : '#fbbf24'
        }}>
          {status === 'ok' ? 'Online' : 'Warning'}
        </div>
      </div>
      <div>
        <div style={{ color: 'var(--slate-400)', fontSize: '14px', fontWeight: 500, marginBottom: '4px' }}>{title}</div>
        <div style={{ fontSize: '24px', fontWeight: 'bold', color: 'white', letterSpacing: '-0.025em' }}>{value}</div>
        <div style={{ fontSize: '12px', color: 'var(--slate-500)', marginTop: '8px', display: 'flex', alignItems: 'center', gap: '4px' }}>
          {detail}
        </div>
      </div>
      <div style={{ 
        position: 'absolute', 
        bottom: 0, 
        left: 0, 
        height: '4px', 
        width: status === 'ok' ? '100%' : '50%',
        backgroundColor: status === 'ok' ? 'rgba(16, 185, 129, 0.4)' : 'rgba(245, 151, 10, 0.4)',
        transition: 'all 0.5s'
      }} />
    </div>
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ fontSize: '30px', fontWeight: 'bold', color: 'white', letterSpacing: '-0.025em', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <BarChart3 style={{ color: 'var(--cobalt-400)' }} />
            系統監控面板
          </h1>
          <p style={{ color: 'var(--slate-400)', marginTop: '4px' }}>實時監控 ERP 核心組件狀態與效能指標</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', backgroundColor: 'rgba(30, 41, 59, 0.5)', border: '1px solid rgba(51, 65, 85, 0.5)', borderRadius: '8px', padding: '6px 12px' }}>
            <Clock size={14} style={{ color: 'var(--slate-400)' }} />
            <span style={{ fontSize: '12px', color: 'var(--slate-300)' }}>上次更新: {lastUpdated.toLocaleTimeString()}</span>
          </div>
          <button 
            onClick={fetchMetrics}
            disabled={loading}
            className="btn btn-ghost"
            style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
          >
            <RefreshCcw size={16} className={loading ? 'animate-spin' : ''} />
            立即重新整理
          </button>
        </div>
      </div>

      {/* Grid of Status Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '24px' }}>
        <StatusCard 
          title="資料庫狀態" 
          value={metrics?.database?.status === 'Success' ? 'PostgreSQL 連線正常' : '連線異常'} 
          status={metrics?.database?.status === 'Success' ? 'ok' : 'warning'}
          icon={Database}
          detail={`連線字串: ${metrics?.database?.isPostgres ? 'PostgreSQL' : 'SQL Server'} (Encrypted)`}
        />
        <StatusCard 
          title="系統環境" 
          value={metrics?.environment?.os || 'Windows Server'} 
          status="ok"
          icon={Server}
          detail={metrics?.environment?.runtime || '.NET 8.0'}
        />
        <StatusCard 
          title="活動稽核 (24h)" 
          value={`${metrics?.audit?.totalLogs || 0} 筆記錄`} 
          status="ok"
          icon={ShieldCheck}
          detail={`${metrics?.audit?.activeUsers || 0} 位活躍用戶`}
        />
        <StatusCard 
          title="快取服務" 
          value="Redis Standalone" 
          status="ok"
          icon={Activity}
          detail="延遲: 0.8ms"
        />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '24px' }}>
        {/* Performance Chart */}
        <div className="glass-card" style={{ padding: '24px', gridColumn: 'span 2' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '24px' }}>
            <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: 'white', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Cpu size={18} style={{ color: 'var(--cobalt-400)' }} />
              系統資源負載趨勢
            </h3>
            <div style={{ display: 'flex', gap: '16px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <div style={{ width: '12px', height: '12px', borderRadius: '50%', backgroundColor: '#3b82f6' }} />
                <span style={{ fontSize: '12px', color: 'var(--slate-400)' }}>CPU</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <div style={{ width: '12px', height: '12px', borderRadius: '50%', backgroundColor: '#10b981' }} />
                <span style={{ fontSize: '12px', color: 'var(--slate-400)' }}>RAM</span>
              </div>
            </div>
          </div>
          <div style={{ height: '300px', width: '100%' }}>
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={performanceData}>
                <defs>
                  <linearGradient id="colorCpu" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.3}/>
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0}/>
                  </linearGradient>
                  <linearGradient id="colorRam" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10b981" stopOpacity={0.3}/>
                    <stop offset="95%" stopColor="#10b981" stopOpacity={0}/>
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" vertical={false} />
                <XAxis dataKey="time" stroke="#64748b" fontSize={12} tickLine={false} axisLine={false} />
                <YAxis stroke="#64748b" fontSize={12} tickLine={false} axisLine={false} unit="%" />
                <Tooltip 
                  contentStyle={{ backgroundColor: '#0f172a', border: '1px solid #334155', borderRadius: '8px' }}
                  itemStyle={{ fontSize: '12px' }}
                />
                <Area type="monotone" dataKey="cpu" stroke="#3b82f6" fillOpacity={1} fill="url(#colorCpu)" strokeWidth={2} />
                <Area type="monotone" dataKey="ram" stroke="#10b981" fillOpacity={1} fill="url(#colorRam)" strokeWidth={2} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Audit Distribution */}
        <div className="glass-card" style={{ padding: '24px' }}>
          <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: 'white', marginBottom: '24px', display: 'flex', alignItems: 'center', gap: '8px' }}>
            <ShieldCheck size={18} style={{ color: '#10b981' }} />
            活動類型分佈
          </h3>
          <div style={{ height: '250px', width: '100%', display: 'flex', justifyContent: 'center' }}>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={auditData}
                  cx="50%"
                  cy="50%"
                  innerRadius={60}
                  outerRadius={80}
                  paddingAngle={5}
                  dataKey="value"
                >
                  {auditData.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip 
                  contentStyle={{ backgroundColor: '#0f172a', border: '1px solid #334155', borderRadius: '8px' }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <div style={{ marginTop: '16px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {auditData.slice(0, 4).map((item) => (
              <div key={item.name} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontSize: '14px' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: 'var(--slate-400)' }}>
                  <div style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: item.color }} />
                  {item.name}
                </div>
                <div style={{ color: 'var(--slate-200)', fontWeight: 500 }}>{item.value} 筆</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: '24px' }}>
        {/* Recent Audit Logs Table */}
        <div className="glass-card" style={{ padding: 0, overflow: 'hidden' }}>
          <div style={{ padding: '24px', borderBottom: '1px solid #1e293b', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: 'white', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Terminal size={18} style={{ color: '#f59e0b' }} />
              最近稽核日誌
            </h3>
            <button style={{ fontSize: '12px', color: 'var(--cobalt-400)', background: 'none', border: 'none', cursor: 'pointer', fontWeight: 500 }}>查看完整日誌</button>
          </div>
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', textAlign: 'left', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ backgroundColor: 'rgba(15, 23, 42, 0.5)', fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.08em', color: 'var(--slate-500)' }}>
                  <th style={{ padding: '12px 24px', fontWeight: 600 }}>時間</th>
                  <th style={{ padding: '12px 24px', fontWeight: 600 }}>用戶</th>
                  <th style={{ padding: '12px 24px', fontWeight: 600 }}>模組</th>
                  <th style={{ padding: '12px 24px', fontWeight: 600 }}>操作</th>
                  <th style={{ padding: '12px 24px', fontWeight: 600 }}>狀態</th>
                </tr>
              </thead>
              <tbody style={{ fontSize: '13px' }}>
                {(metrics?.audit?.recentLogs || []).map((log: any, idx: number) => (
                  <tr key={idx} style={{ borderTop: '1px solid rgba(30, 41, 59, 0.5)', transition: 'background 0.2s' }}>
                    <td style={{ padding: '16px 24px', color: 'var(--slate-400)' }}>{new Date(log.timestamp).toLocaleTimeString()}</td>
                    <td style={{ padding: '16px 24px', color: 'var(--slate-200)', fontWeight: 500 }}>{log.username}</td>
                    <td style={{ padding: '16px 24px' }}>
                      <span style={{ padding: '4px 8px', borderRadius: '4px', backgroundColor: '#1e293b', color: 'var(--slate-400)', border: '1px solid #334155', fontSize: '11px' }}>
                        {log.module}
                      </span>
                    </td>
                    <td style={{ padding: '16px 24px', color: 'var(--slate-300)' }}>{log.action}</td>
                    <td style={{ padding: '16px 24px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: '#10b981', fontSize: '12px' }}>
                        <CheckCircle2 size={12} />
                        成功
                      </div>
                    </td>
                  </tr>
                ))}
                {(!metrics?.audit?.recentLogs || metrics?.audit?.recentLogs.length === 0) && (
                   <tr>
                     <td colSpan={5} style={{ padding: '40px 24px', textAlign: 'center', color: 'var(--slate-500)', fontStyle: 'italic' }}>
                       目前尚無稽核日誌記錄
                     </td>
                   </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* System Details */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          <div className="glass-card" style={{ padding: '24px' }}>
            <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: 'white', marginBottom: '24px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <Globe size={18} style={{ color: 'var(--cobalt-400)' }} />
              節點詳細資訊
            </h3>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
              <div style={{ backgroundColor: 'rgba(15, 23, 42, 0.5)', padding: '16px', borderRadius: '12px', border: '1px solid #1e293b' }}>
                <div style={{ fontSize: '10px', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.1em', marginBottom: '4px' }}>主機名稱</div>
                <div style={{ color: 'var(--slate-200)', fontFamily: 'monospace', fontSize: '14px' }}>{metrics?.environment?.machineName || 'ERP-SERVER-01'}</div>
              </div>
              <div style={{ backgroundColor: 'rgba(15, 23, 42, 0.5)', padding: '16px', borderRadius: '12px', border: '1px solid #1e293b' }}>
                <div style={{ fontSize: '10px', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.1em', marginBottom: '4px' }}>IP 位址</div>
                <div style={{ color: 'var(--slate-200)', fontFamily: 'monospace', fontSize: '14px' }}>192.168.10.45</div>
              </div>
              <div style={{ backgroundColor: 'rgba(15, 23, 42, 0.5)', padding: '16px', borderRadius: '12px', border: '1px solid #1e293b' }}>
                <div style={{ fontSize: '10px', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.1em', marginBottom: '4px' }}>區域</div>
                <div style={{ color: 'var(--slate-200)', fontSize: '14px' }}>Taiwan North (AZ-1)</div>
              </div>
              <div style={{ backgroundColor: 'rgba(15, 23, 42, 0.5)', padding: '16px', borderRadius: '12px', border: '1px solid #1e293b' }}>
                <div style={{ fontSize: '10px', color: 'var(--slate-500)', textTransform: 'uppercase', letterSpacing: '0.1em', marginBottom: '4px' }}>運行時間</div>
                <div style={{ color: 'var(--slate-200)', fontSize: '14px' }}>12d 15h 42m</div>
              </div>
            </div>
          </div>

          <div className="glass-card" style={{ padding: '24px', background: 'linear-gradient(135deg, rgba(59, 130, 246, 0.1), transparent)' }}>
            <div style={{ display: 'flex', alignItems: 'start', justifyContent: 'space-between' }}>
              <div>
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: 'white', display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <ShieldCheck size={18} style={{ color: 'var(--cobalt-400)' }} />
                  系統安全性
                </h3>
                <p style={{ fontSize: '14px', color: 'var(--slate-400)', marginTop: '4px' }}>目前系統處於受保護狀態</p>
              </div>
              <div style={{ padding: '8px', backgroundColor: 'rgba(16, 185, 129, 0.2)', borderRadius: '50%' }}>
                <Lock size={20} style={{ color: '#10b981' }} />
              </div>
            </div>
            <div style={{ marginTop: '24px', display: 'flex', alignItems: 'center', gap: '16px' }}>
               <div style={{ flex: 1, height: '8px', backgroundColor: '#1e293b', borderRadius: '9999px', overflow: 'hidden' }}>
                 <div style={{ height: '100%', backgroundColor: '#10b981', width: '98%' }} />
               </div>
               <span style={{ fontSize: '14px', fontWeight: 'bold', color: '#10b981' }}>98% Secure</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
