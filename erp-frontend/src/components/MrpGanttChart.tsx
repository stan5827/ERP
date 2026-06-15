import React, { useMemo } from 'react';
import { format, differenceInDays, startOfDay } from 'date-fns';
import { Calendar, Package, ShoppingCart, Factory } from 'lucide-react';
import './MrpGantt.css';

interface MrpPlannedOrder {
  id: string;
  skuNumber: string;
  skuName: string;
  orderType: 'work_order' | 'purchase_order';
  orderQty: number;
  plannedStartDate: string;
  plannedEndDate: string;
  status: string;
}

interface MrpGanttChartProps {
  orders: MrpPlannedOrder[];
  isLoading?: boolean;
}

const MrpGanttChart: React.FC<MrpGanttChartProps> = ({ orders, isLoading }) => {
  const { startDate, endDate, days } = useMemo(() => {
    if (!orders.length) return { startDate: new Date(), endDate: new Date(), days: [] };

    const dates = orders.flatMap(o => [new Date(o.plannedStartDate), new Date(o.plannedEndDate)]);
    const minDate = startOfDay(new Date(Math.min(...dates.map(d => d.getTime()))));
    const maxDate = new Date(Math.max(...dates.map(d => d.getTime())));
    
    // Extend range a bit
    maxDate.setDate(maxDate.getDate() + 7);
    
    const totalDays = differenceInDays(maxDate, minDate) + 1;
    const dayArray = Array.from({ length: totalDays }).map((_, i) => {
      const d = new Date(minDate);
      d.setDate(d.getDate() + i);
      return d;
    });

    return { startDate: minDate, endDate: maxDate, days: dayArray };
  }, [orders]);

  if (isLoading) {
    return <div className="gantt-loading">載入排程圖表中...</div>;
  }

  if (!orders.length) {
    return <div className="gantt-empty">暫無建議計畫資料</div>;
  }

  return (
    <div className="mrp-gantt-root">
      <div className="gantt-header-row">
        <div className="gantt-info-col">計畫項目 (Planned Items)</div>
        <div className="gantt-time-col">
          {days.filter((_, i) => i % 5 === 0 || i === 0).map((day, i) => (
            <div key={i} className="time-tick" style={{ left: `${(differenceInDays(day, startDate) / days.length) * 100}%` }}>
              {format(day, 'MM/dd')}
            </div>
          ))}
        </div>
      </div>

      <div className="gantt-scroll-area">
        {orders.map((order) => {
          const start = new Date(order.plannedStartDate);
          const end = new Date(order.plannedEndDate);
          const left = (differenceInDays(start, startDate) / days.length) * 100;
          const width = (Math.max(1, differenceInDays(end, start)) / days.length) * 100;

          return (
            <div key={order.id} className="gantt-item-row">
              <div className="gantt-info-col">
                <div className="item-meta">
                  {order.orderType === 'work_order' ? <Factory size={12} className="text-blue-400" /> : <ShoppingCart size={12} className="text-emerald-400" />}
                  <span className="sku-code">{order.skuNumber}</span>
                </div>
                <div className="sku-name text-xs text-slate-500 truncate">{order.skuName}</div>
              </div>
              <div className="gantt-time-col">
                <div 
                  className={`gantt-order-bar ${order.orderType}`}
                  style={{ left: `${left}%`, width: `${width}%` }}
                >
                  <span className="order-qty">{order.orderQty}</span>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default MrpGanttChart;
