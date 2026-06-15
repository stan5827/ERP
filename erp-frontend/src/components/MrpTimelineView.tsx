import React, { useMemo } from 'react';
import { 
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer, 
  AreaChart, Area, ReferenceLine, ComposedChart, Bar
} from 'recharts';
import { 
  TrendingDown, TrendingUp, AlertTriangle, Package, 
  Calendar, ShoppingCart, Factory, History 
} from 'lucide-react';
import { format } from 'date-fns';
import { MrpTimelineDto, MrpTimelineEvent } from '../api/api';
import './MrpTimeline.css';

interface MrpTimelineViewProps {
  data: MrpTimelineDto;
  isLoading?: boolean;
}

const MrpTimelineView: React.FC<MrpTimelineViewProps> = ({ data, isLoading }) => {
  // 準備圖表資料
  const chartData = useMemo(() => {
    const points = [{
      date: '現在',
      displayDate: 'Initial',
      balance: data.initialStock,
      change: 0,
      timestamp: 0
    }];

    let runningBalance = data.initialStock;
    
    // 將事件按日期分組（同一天的事件合併顯示在圖表上，但保留原始數據供清單顯示）
    const sortedEvents = [...data.events].sort((a, b) => 
      new Date(a.date).getTime() - new Date(b.date).getTime()
    );

    sortedEvents.forEach(event => {
      runningBalance += event.changeQty;
      points.push({
        date: format(new Date(event.date), 'MM/dd'),
        displayDate: format(new Date(event.date), 'yyyy-MM-dd'),
        balance: runningBalance,
        change: event.changeQty,
        timestamp: new Date(event.date).getTime()
      });
    });

    return points;
  }, [data]);

  // 計算指標
  const stats = useMemo(() => {
    const balances = chartData.map(d => d.balance);
    const minBalance = Math.min(...balances);
    const maxShortage = minBalance < 0 ? Math.abs(minBalance) : 0;
    const finalBalance = balances[balances.length - 1];
    
    return {
      minBalance,
      maxShortage,
      finalBalance,
      isShortage: minBalance < 0
    };
  }, [chartData]);

  if (isLoading) {
    return <div className="loading-shimmer" style={{ height: '400px' }}>載入時間軸數據中...</div>;
  }

  return (
    <div className="mrp-timeline-container">
      {/* 1. 摘要卡片 */}
      <div className="mrp-stats-grid">
        <div className="stat-card">
          <div className="stat-icon initial"><Package size={20} /></div>
          <div className="stat-info">
            <span className="stat-label">當前庫存</span>
            <span className="stat-value">{data.initialStock} {data.unit}</span>
          </div>
        </div>
        
        <div className={`stat-card ${stats.isShortage ? 'danger' : 'success'}`}>
          <div className="stat-icon"><AlertTriangle size={20} /></div>
          <div className="stat-info">
            <span className="stat-label">預計最低庫存</span>
            <span className="stat-value">{stats.minBalance} {data.unit}</span>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-icon"><TrendingUp size={20} /></div>
          <div className="stat-info">
            <span className="stat-label">期末預計結存</span>
            <span className="stat-value">{stats.finalBalance} {data.unit}</span>
          </div>
        </div>
      </div>

      {/* 2. 趨勢圖表 */}
      <div className="mrp-chart-wrapper">
        <h3 className="section-title">
          <TrendingDown size={18} /> 庫存預計可用量 (PAB) 趨勢
        </h3>
        <div style={{ width: '100%', height: 350 }}>
          <ResponsiveContainer>
            <ComposedChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#eee" />
              <XAxis dataKey="date" />
              <YAxis />
              <Tooltip 
                contentStyle={{ borderRadius: '8px', border: 'none', boxShadow: '0 4px 12px rgba(0,0,0,0.1)' }}
                formatter={(value: any) => [value, '數量']}
              />
              <Legend />
              {/* 庫存水位線 */}
              <Area 
                type="stepAfter" 
                dataKey="balance" 
                name="預計庫存 (PAB)" 
                stroke="#6366f1" 
                fill="url(#colorBalance)" 
                strokeWidth={3}
              />
              <defs>
                <linearGradient id="colorBalance" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#6366f1" stopOpacity={0.3}/>
                  <stop offset="95%" stopColor="#6366f1" stopOpacity={0}/>
                </linearGradient>
              </defs>
              {/* 零水位參考線 */}
              <ReferenceLine y={0} stroke="#ef4444" strokeDasharray="5 5" label="缺料警戒線" />
            </ComposedChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* 3. 甘特時序圖 (Gantt View) */}
      <div className="mrp-gantt-wrapper">
        <h3 className="section-title">
          <Calendar size={18} /> 供需時序甘特圖 (Gantt View)
        </h3>
        <div className="gantt-container">
          <div className="gantt-header">
            <div className="gantt-label-col">來源</div>
            <div className="gantt-timeline-col">
              {/* 生成日期刻度 */}
              {Array.from({ length: 6 }).map((_, i) => {
                const date = new Date();
                date.setDate(date.getDate() + i * 15);
                return (
                  <div key={i} className="gantt-tick">
                    {format(date, 'MM/dd')}
                  </div>
                );
              })}
            </div>
          </div>
          
          <div className="gantt-body">
            {['demand', 'supply'].map(type => (
              <div key={type} className={`gantt-row-group ${type}`}>
                <div className="group-label">{type === 'demand' ? '需求 (Demand)' : '供給 (Supply)'}</div>
                {data.events
                  .filter(e => e.type === type)
                  .map((event, idx) => {
                    const startDate = new Date(data.events[0]?.date || new Date());
                    const endDate = new Date(data.events[data.events.length - 1]?.date || new Date());
                    const eventDate = new Date(event.date);
                    const totalDays = Math.max(1, (endDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24));
                    const elapsedDays = (eventDate.getTime() - startDate.getTime()) / (1000 * 60 * 60 * 24);
                    const leftPos = Math.max(0, Math.min(95, (elapsedDays / totalDays) * 100));

                    return (
                      <div key={idx} className="gantt-row">
                        <div className="gantt-label-col">
                          <span className="ref-tag">{event.source}</span>
                          <span className="ref-no">{event.reference}</span>
                        </div>
                        <div className="gantt-timeline-col">
                          <div 
                            className={`gantt-bar ${type}`}
                            style={{ left: `${leftPos}%` }}
                            title={`${event.description}: ${event.changeQty}`}
                          >
                            <span className="bar-value">{Math.abs(event.changeQty)}</span>
                          </div>
                        </div>
                      </div>
                    );
                  })}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* 4. 事件清單 */}
      <div className="mrp-events-wrapper">
        <h3 className="section-title">
          <History size={18} /> 供需異動明細
        </h3>
        <div className="timeline-list">
          <div className="timeline-item initial">
            <div className="time-marker">Now</div>
            <div className="event-content">
              <div className="event-header">
                <span className="event-source">期初庫存</span>
                <span className="event-qty">{data.initialStock}</span>
              </div>
              <div className="event-desc">當前系統庫存量</div>
            </div>
          </div>

          {data.events.map((event, idx) => (
            <div key={idx} className={`timeline-item ${event.type}`}>
              <div className="time-marker">{format(new Date(event.date), 'MM/dd')}</div>
              <div className="event-icon">
                {event.type === 'demand' ? <ShoppingCart size={14} /> : <Factory size={14} />}
              </div>
              <div className="event-content">
                <div className="event-header">
                  <span className="event-source">
                    {event.source.toUpperCase()} - {event.reference}
                  </span>
                  <span className={`event-qty ${event.changeQty < 0 ? 'neg' : 'pos'}`}>
                    {event.changeQty > 0 ? '+' : ''}{event.changeQty}
                  </span>
                </div>
                <div className="event-desc">{event.description}</div>
                <div className="event-balance">異動後預計: {event.balanceAfter}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default MrpTimelineView;
