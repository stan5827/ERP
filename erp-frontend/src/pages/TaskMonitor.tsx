import React, { useState, useEffect } from 'react';
import { 
  Activity, Clock, CheckCircle2, XCircle, AlertCircle, 
  RotateCcw, Play, BarChart3, Search, Filter, 
  ArrowRight, MoreVertical, Layers, RefreshCw
} from 'lucide-react';
import { systemJobsApi, type SystemJobDto } from '../api/api';
import toast from 'react-hot-toast';

interface BackgroundTask {
  id: string;
  type: 'MRP' | 'Costing' | 'InventorySync';
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  progress: number;
  startTime: string;
  endTime?: string;
  message?: string;
}

export const TaskMonitor: React.FC = () => {
  const [tasks, setTasks] = useState<BackgroundTask[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('all');

  const fetchTasks = async () => {
    setLoading(true);
    try {
      // Use the new universal system jobs API
      const res = await systemJobsApi.list({ pageSize: 50 });
      if (res.data?.success) {
        const mappedTasks: BackgroundTask[] = res.data.data!.map((job: SystemJobDto) => ({
          id: job.id,
          type: job.jobType as any,
          status: job.status as any,
          progress: job.progress,
          startTime: job.startedAt,
          endTime: job.completedAt,
          message: job.message || job.error
        }));
        setTasks(mappedTasks);
      }
    } catch (error) {
      console.error('Failed to fetch tasks:', error);
      // toast.error('無法更新任務列表');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTasks();
    const interval = setInterval(() => {
      // Only poll if there are active tasks
      if (tasks.some(t => t.status === 'Processing' || t.status === 'Pending')) {
        fetchTasks();
      }
    }, 3000);
    return () => clearInterval(interval);
  }, [tasks]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Completed': return '#10b981';
      case 'Processing': return '#3b82f6';
      case 'Failed': return '#ef4444';
      case 'Pending': return '#f59e0b';
      default: return '#64748b';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Completed': return <CheckCircle2 size={16} />;
      case 'Processing': return <RefreshCw size={16} className="animate-spin" />;
      case 'Failed': return <XCircle size={16} />;
      case 'Pending': return <Clock size={16} />;
      default: return <Activity size={16} />;
    }
  };

  const filteredTasks = tasks.filter(t => {
    if (filter === 'all') return true;
    return t.status.toLowerCase() === filter.toLowerCase();
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <h1 style={{ fontSize: '30px', fontWeight: 'bold', color: 'white', letterSpacing: '-0.025em', display: 'flex', alignItems: 'center', gap: '12px' }}>
            <Layers style={{ color: 'var(--cobalt-400)' }} />
            背景任務監控
          </h1>
          <p style={{ color: 'var(--slate-400)', marginTop: '4px' }}>監控系統異步作業執行狀態與進度</p>
        </div>
        <div style={{ display: 'flex', gap: '12px' }}>
          <button className="btn btn-ghost" onClick={fetchTasks} disabled={loading}>
            <RotateCcw size={16} style={{ marginRight: '8px' }} className={loading ? 'animate-spin' : ''} />
            重新整理
          </button>
          <button className="btn btn-primary">
            <Play size={16} style={{ marginRight: '8px' }} />
            啟動新任務
          </button>
        </div>
      </div>

      {/* Stats */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '24px' }}>
        {[
          { label: '全部任務', value: tasks.length, icon: Layers, color: '#6366f1' },
          { label: '執行中', value: tasks.filter(t => t.status === 'Processing').length, icon: RefreshCw, color: '#3b82f6' },
          { label: '已完成', value: tasks.filter(t => t.status === 'Completed').length, icon: CheckCircle2, color: '#10b981' },
          { label: '失敗', value: tasks.filter(t => t.status === 'Failed').length, icon: XCircle, color: '#ef4444' },
        ].map((stat, i) => (
          <div key={i} className="glass-card" style={{ padding: '20px', display: 'flex', alignItems: 'center', gap: '16px' }}>
            <div style={{ padding: '12px', borderRadius: '12px', backgroundColor: `${stat.color}15`, color: stat.color }}>
              <stat.icon size={24} />
            </div>
            <div>
              <div style={{ fontSize: '12px', color: 'var(--slate-400)', fontWeight: 500 }}>{stat.label}</div>
              <div style={{ fontSize: '24px', fontWeight: 'bold', color: 'white' }}>{stat.value}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Main List */}
      <div className="glass-card" style={{ padding: 0, overflow: 'hidden' }}>
        <div style={{ padding: '20px 24px', borderBottom: '1px solid #1e293b', display: 'flex', justifyContent: 'space-between', alignItems: 'center', backgroundColor: 'rgba(15, 23, 42, 0.3)' }}>
          <div style={{ display: 'flex', gap: '12px' }}>
            <div style={{ position: 'relative' }}>
              <Search size={16} style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
              <input 
                className="form-input" 
                placeholder="搜尋任務..." 
                style={{ paddingLeft: '36px', fontSize: '13px', width: '240px' }}
              />
            </div>
            <div style={{ display: 'flex', backgroundColor: '#0f172a', borderRadius: '8px', padding: '4px', border: '1px solid #1e293b' }}>
              {['All', 'Processing', 'Completed', 'Failed'].map(f => (
                <button 
                  key={f}
                  onClick={() => setFilter(f.toLowerCase())}
                  style={{ 
                    padding: '6px 12px', 
                    fontSize: '12px', 
                    borderRadius: '6px', 
                    border: 'none',
                    cursor: 'pointer',
                    backgroundColor: filter === f.toLowerCase() ? '#1e293b' : 'transparent',
                    color: filter === f.toLowerCase() ? 'white' : 'var(--slate-400)',
                    fontWeight: 500,
                    transition: 'all 0.2s'
                  }}
                >
                  {f === 'All' ? '全部' : f}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', textAlign: 'left', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ fontSize: '11px', textTransform: 'uppercase', letterSpacing: '0.05em', color: 'var(--slate-500)', borderBottom: '1px solid #1e293b' }}>
                <th style={{ padding: '16px 24px' }}>任務類型</th>
                <th style={{ padding: '16px 24px' }}>執行進度</th>
                <th style={{ padding: '16px 24px' }}>狀態</th>
                <th style={{ padding: '16px 24px' }}>啟動時間</th>
                <th style={{ padding: '16px 24px' }}>備註</th>
                <th style={{ padding: '16px 24px' }}></th>
              </tr>
            </thead>
            <tbody>
              {filteredTasks.length > 0 ? (
                filteredTasks.map((task) => (
                  <tr key={task.id} style={{ borderBottom: '1px solid rgba(30, 41, 59, 0.5)', transition: 'background 0.2s' }}>
                    <td style={{ padding: '20px 24px' }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        <div style={{ width: '32px', height: '32px', borderRadius: '8px', backgroundColor: '#1e293b', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--cobalt-400)' }}>
                          {task.type === 'MRP' ? <BarChart3 size={18} /> : 
                           task.type === 'Costing' ? <Activity size={18} /> :
                           <Layers size={18} />}
                        </div>
                        <div>
                          <div style={{ color: 'white', fontWeight: 600, fontSize: '14px' }}>{task.type} 運算</div>
                          <div style={{ color: 'var(--slate-500)', fontSize: '11px' }}>ID: {task.id.slice(0, 8)}...</div>
                        </div>
                      </div>
                    </td>
                    <td style={{ padding: '20px 24px', width: '250px' }}>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px' }}>
                          <span style={{ color: 'var(--slate-400)' }}>Progress</span>
                          <span style={{ color: 'white', fontWeight: 600 }}>{task.progress}%</span>
                        </div>
                        <div style={{ width: '100%', height: '6px', backgroundColor: '#0f172a', borderRadius: '999px', overflow: 'hidden', border: '1px solid #1e293b' }}>
                          <div 
                            style={{ 
                              height: '100%', 
                              backgroundColor: getStatusColor(task.status), 
                              width: `${task.progress}%`,
                              transition: 'width 0.5s cubic-bezier(0.4, 0, 0.2, 1)',
                              boxShadow: `0 0 10px ${getStatusColor(task.status)}40`
                            }} 
                          />
                        </div>
                      </div>
                    </td>
                    <td style={{ padding: '20px 24px' }}>
                      <div style={{ 
                        display: 'inline-flex', 
                        alignItems: 'center', 
                        gap: '6px', 
                        padding: '4px 10px', 
                        borderRadius: '999px', 
                        fontSize: '12px', 
                        fontWeight: 600,
                        backgroundColor: `${getStatusColor(task.status)}15`,
                        color: getStatusColor(task.status),
                        border: `1px solid ${getStatusColor(task.status)}30`
                      }}>
                        {getStatusIcon(task.status)}
                        {task.status}
                      </div>
                    </td>
                    <td style={{ padding: '20px 24px', color: 'var(--slate-400)', fontSize: '13px' }}>
                      {new Date(task.startTime).toLocaleString()}
                    </td>
                    <td style={{ padding: '20px 24px', color: 'var(--slate-500)', fontSize: '13px', maxWidth: '200px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                      {task.message}
                    </td>
                    <td style={{ padding: '20px 24px' }}>
                      <button style={{ color: 'var(--slate-500)', background: 'none', border: 'none', cursor: 'pointer' }}>
                        <MoreVertical size={18} />
                      </button>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={6} style={{ padding: '60px 24px', textAlign: 'center', color: 'var(--slate-500)' }}>
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                      <Layers size={40} style={{ opacity: 0.2 }} />
                      <span>尚無符合條件的背景任務</span>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Recent Alerts */}
      <div className="glass-card" style={{ padding: '20px', borderLeft: '4px solid #ef4444' }}>
        <div style={{ display: 'flex', gap: '16px' }}>
          <div style={{ color: '#ef4444' }}>
            <AlertCircle size={24} />
          </div>
          <div>
            <h4 style={{ color: 'white', fontWeight: 600, fontSize: '16px' }}>異常提醒</h4>
            <p style={{ color: 'var(--slate-400)', fontSize: '14px', marginTop: '4px' }}>
              今日有 0 個任務執行失敗。若發現任務長時間卡在 Processing 狀態，請檢查資料庫連線或重啟 Hangfire 服務。
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
