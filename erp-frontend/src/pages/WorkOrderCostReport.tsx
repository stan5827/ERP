import React from 'react';
import { motion } from 'framer-motion';
import { 
  X, BarChart3, PieChart as PieChartIcon, 
  TrendingDown, TrendingUp, DollarSign, 
  Target, Calculator, Activity, Cpu
} from 'lucide-react';
import { 
  PieChart, Pie, Cell, ResponsiveContainer, 
  BarChart, Bar, XAxis, YAxis, CartesianGrid, 
  Tooltip, Legend 
} from 'recharts';
import { useWorkOrderCosting, useWorkOrderCostingSummary, useSettleCosting, useBatchSettleCosting } from '../hooks/useApi';
import { api, ApiResponse } from '../api/api';
import './styles/CostReport.css';

export const WorkOrderCostReport: React.FC<{ workOrderId?: string; onClose?: () => void }> = ({ workOrderId: propId, onClose }) => {
  const [selectedOrderId, setSelectedOrderId] = React.useState<string | null>(propId || null);
  
  const { data: listData, isLoading: listLoading } = useWorkOrderCostingSummary();
  const settleMutation = useSettleCosting();
  const batchSettleMutation = useBatchSettleCosting();

  const { data: detailData, isLoading: detailLoading, error: detailError } = useWorkOrderCosting(selectedOrderId || '');

  if (selectedOrderId) {
    const costData = detailData?.data;
    if (detailLoading) return (
      <div style={{ padding: '80px', textAlign: 'center', color: 'var(--lux-gold)' }}>
        <Activity className="animate-spin" style={{ margin: '0 auto 16px' }} size={32} />
        <div className="mono">ANALYZING COST MATRIX...</div>
      </div>
    );

    if (detailError || !costData) return (
      <div style={{ padding: '40px', color: 'var(--crimson-400)', textAlign: 'center' }}>
        <button onClick={() => setSelectedOrderId(null)} style={{ marginBottom: '1rem', color: 'var(--lux-gold)', background: 'none', border: 'none', cursor: 'pointer' }}>← 返回列表</button>
        <div>無法讀取成本分析數據，請稍後再試。</div>
      </div>
    );

    const pieData = [
      { name: '直接材料', value: costData.actualMaterialCost, color: '#d4af37' },
      { name: '直接人工', value: costData.actualLaborCost, color: '#38bdf8' },
      { name: '製造費用', value: costData.actualOverheadCost, color: '#a78bfa' },
    ];

    const barData = [
      { name: '材料', 標準: costData.standardMaterialCost, 實際: costData.actualMaterialCost },
      { name: '人工', 標準: costData.standardLaborCost, 實際: costData.actualLaborCost },
    ];

    const isPositiveVariance = costData.varianceAmount > 0;

    return (
      <div className="cost-report-container">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '2rem' }}>
          <div>
            <button onClick={() => setSelectedOrderId(null)} style={{ marginBottom: '1rem', color: 'var(--lux-gold)', background: 'none', border: 'none', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
               返回列表
            </button>
            <div style={{ fontSize: '10px', letterSpacing: '0.2em', color: 'var(--lux-gold)', fontWeight: 700, textTransform: 'uppercase', marginBottom: '0.25rem' }}>
              Work Order Cost Intelligence
            </div>
            <h2 style={{ fontSize: '1.5rem', fontWeight: 600, color: '#fff', display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
              <Calculator style={{ color: 'var(--lux-gold)' }} />
              成本分析報告: {costData.woNumber}
            </h2>
          </div>
          <div style={{ display: 'flex', gap: '0.75rem' }}>
            <motion.button 
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              onClick={async () => {
                if (selectedOrderId) {
                  const res = await settleMutation.mutateAsync(selectedOrderId);
                  if (res.success) alert('成本結轉成功！');
                }
              }}
              style={{ background: 'var(--lux-gold)', color: '#000', border: 'none', padding: '0.5rem 1rem', borderRadius: '4px', fontWeight: 600, cursor: 'pointer' }}
            >
              <Activity size={14} /> 結轉成本 (Settle)
            </motion.button>
            {onClose && <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--slate-400)', cursor: 'pointer' }}><X size={24} /></button>}
          </div>
        </div>

        {/* KPI Stats */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem', marginBottom: '2rem' }}>
          <div className="kpi-card">
            <div className="kpi-label"><DollarSign size={14} /> 實際總成本</div>
            <div className="kpi-value">${costData.totalActualCost.toLocaleString()}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label"><Target size={14} /> 標準總成本</div>
            <div className="kpi-value" style={{ color: 'var(--slate-400)' }}>${costData.totalStandardCost.toLocaleString()}</div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label">
              {isPositiveVariance ? <TrendingUp size={14} color="#f87171" /> : <TrendingDown size={14} color="#34d399" />}
              成本變異率
            </div>
            <div className="kpi-value" style={{ color: isPositiveVariance ? '#f87171' : '#34d399' }}>
              {isPositiveVariance ? '+' : ''}{costData.variancePercentage}%
            </div>
          </div>
          <div className="kpi-card">
            <div className="kpi-label"><BarChart3 size={14} /> 單位實際成本</div>
            <div className="kpi-value" style={{ color: 'var(--lux-gold)' }}>${costData.unitCost.toLocaleString()}</div>
            <div style={{ fontSize: '10px', color: 'var(--slate-500)', marginTop: '0.25rem' }}>基於完工數: {costData.actualQty}</div>
          </div>
        </div>

        {/* Charts Row */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: '1.5rem', marginBottom: '2.5rem' }}>
          <div className="chart-wrapper">
            <h3 className="chart-title"><PieChartIcon size={16} /> 實際成本構成 (Actual Breakdown)</h3>
            <div style={{ width: '100%', height: 260 }}>
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={pieData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={80}
                    paddingAngle={8}
                    dataKey="value"
                  >
                    {pieData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip 
                    contentStyle={{ background: 'var(--navy-800)', border: '1px solid var(--lux-border)', borderRadius: '8px' }}
                    itemStyle={{ color: '#fff', fontSize: '12px' }}
                  />
                  <Legend verticalAlign="bottom" height={36} wrapperStyle={{ fontSize: '12px', color: 'var(--slate-400)' }} />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="chart-wrapper">
            <h3 className="chart-title"><BarChart3 size={16} /> 標準 vs 實際對比 (Variance Analysis)</h3>
            <div style={{ width: '100%', height: 260 }}>
              <ResponsiveContainer>
                <BarChart data={barData} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
                  <XAxis dataKey="name" stroke="var(--slate-500)" fontSize={11} axisLine={false} tickLine={false} />
                  <YAxis stroke="var(--slate-500)" fontSize={11} axisLine={false} tickLine={false} />
                  <Tooltip 
                    cursor={{ fill: 'rgba(255,255,255,0.02)' }}
                    contentStyle={{ background: 'var(--navy-800)', border: '1px solid var(--lux-border)', borderRadius: '8px' }}
                  />
                  <Legend wrapperStyle={{ fontSize: '12px' }} />
                  <Bar name="標準" dataKey="標準" fill="rgba(255,255,255,0.15)" radius={[4, 4, 0, 0]} barSize={30} />
                  <Bar name="實際" dataKey="實際" fill="var(--lux-gold)" radius={[4, 4, 0, 0]} barSize={30} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>

        {/* Details Table */}
        <table className="cost-table">
          <thead>
            <tr>
              <th>成本科目</th>
              <th>標準 (Standard)</th>
              <th>實際 (Actual)</th>
              <th style={{ textAlign: 'right' }}>變異百分比</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td style={{ fontWeight: 600 }}>直接材料 (Material)</td>
              <td>${costData.standardMaterialCost.toLocaleString()}</td>
              <td>${costData.actualMaterialCost.toLocaleString()}</td>
              <td style={{ textAlign: 'right' }}>
                <span className={`variance-badge ${costData.actualMaterialCost > costData.standardMaterialCost ? 'variance-positive' : 'variance-negative'}`}>
                  {costData.standardMaterialCost > 0 ? ((costData.actualMaterialCost - costData.standardMaterialCost)/costData.standardMaterialCost*100).toFixed(1) : '0'}%
                </span>
              </td>
            </tr>
            <tr>
              <td style={{ fontWeight: 600 }}>直接人工 (Labor)</td>
              <td>${costData.standardLaborCost.toLocaleString()}</td>
              <td>${costData.actualLaborCost.toLocaleString()}</td>
              <td style={{ textAlign: 'right' }}>
                <span className={`variance-badge ${costData.actualLaborCost > costData.standardLaborCost ? 'variance-positive' : 'variance-negative'}`}>
                  {costData.standardLaborCost > 0 ? ((costData.actualLaborCost - costData.standardLaborCost)/costData.standardLaborCost*100).toFixed(1) : '0'}%
                </span>
              </td>
            </tr>
            <tr>
              <td style={{ fontWeight: 600 }}>製造費用 (Overhead)</td>
              <td style={{ color: 'var(--slate-600)' }}>---</td>
              <td>${costData.actualOverheadCost.toLocaleString()}</td>
              <td style={{ textAlign: 'right', color: 'var(--slate-600)' }}>---</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td>合計成本</td>
              <td>${costData.totalStandardCost.toLocaleString()}</td>
              <td style={{ color: 'var(--lux-gold-light)' }}>${costData.totalActualCost.toLocaleString()}</td>
              <td style={{ textAlign: 'right' }}>
                <span className={`variance-badge ${isPositiveVariance ? 'variance-positive' : 'variance-negative'}`}>
                  {costData.variancePercentage}%
                </span>
              </td>
            </tr>
          </tfoot>
        </table>

        <div className="ai-insight-box" style={{ marginTop: '2rem', background: 'rgba(56, 189, 248, 0.05)', border: '1px solid rgba(56, 189, 248, 0.2)' }}>
          <DollarSign size={18} style={{ flexShrink: 0, color: 'var(--sky-400)' }} />
          <div style={{ width: '100%' }}>
            <div style={{ fontWeight: 700, marginBottom: '0.5rem', color: 'var(--sky-400)' }}>會計分錄預覽 (FINANCE VOUCHER PREVIEW)</div>
            <div style={{ fontSize: '12px', color: 'var(--slate-400)', borderTop: '1px solid rgba(255,255,255,0.05)', paddingTop: '0.5rem' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
                <span>1200 在製品 - 直接材料</span>
                <span style={{ color: 'var(--sky-400)' }}>Dr. ${costData.actualMaterialCost.toLocaleString()}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
                <span>1210 在製品 - 直接人工</span>
                <span style={{ color: 'var(--sky-400)' }}>Dr. ${costData.actualLaborCost.toLocaleString()}</span>
              </div>
               <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0' }}>
                <span>1220 在製品 - 製造費用</span>
                <span style={{ color: 'var(--sky-400)' }}>Dr. ${costData.actualOverheadCost.toLocaleString()}</span>
              </div>
              <div style={{ display: 'flex', justifyContent: 'space-between', padding: '0.25rem 0', borderTop: '1px dashed rgba(255,255,255,0.1)', marginTop: '0.25rem' }}>
                <span>1120 原物料 / 應付工資 / 製造費用分攤</span>
                <span style={{ color: '#f87171' }}>Cr. ${costData.totalActualCost.toLocaleString()}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  // List View
  return (
    <div className="cost-list-container" style={{ padding: '2rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <div>
          <h1 style={{ fontSize: '2rem', fontWeight: 700, color: '#fff' }}>實際成本分析結轉</h1>
          <p style={{ color: 'var(--slate-400)' }}>分析已完工工單的實際投入與產出成本差異</p>
        </div>
        <button 
          onClick={async () => {
            if (confirm('確定要針對所有待結轉工單進行批次分析與結轉嗎？')) {
               await batchSettleMutation.mutateAsync();
               alert('批次結轉要求已送出');
            }
          }}
          style={{ background: 'var(--lux-gold)', color: '#000', border: 'none', padding: '0.75rem 1.5rem', borderRadius: '6px', fontWeight: 700, cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '0.5rem' }}
        >
          <Activity size={16} /> 分析並結轉 (Batch Settle)
        </button>
      </div>

      <div style={{ background: 'rgba(15, 23, 42, 0.6)', border: '1px solid var(--lux-border)', borderRadius: '8px', overflow: 'hidden' }}>
        <table className="cost-table" style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ background: 'rgba(255,255,255,0.05)', textAlign: 'left' }}>
              <th style={{ padding: '1rem' }}>工單號碼</th>
              <th style={{ padding: '1rem' }}>成品品項</th>
              <th style={{ padding: '1rem' }}>生產數量</th>
              <th style={{ padding: '1rem' }}>標準成本 (Total)</th>
              <th style={{ padding: '1rem' }}>已累計實際</th>
              <th style={{ padding: '1rem' }}>結轉狀態</th>
              <th style={{ padding: '1rem' }}>操作</th>
            </tr>
          </thead>
          <tbody>
            {listLoading ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: '2rem', color: 'var(--slate-400)' }}>載入中...</td></tr>
            ) : listData?.data && listData.data.length > 0 ? (
              listData.data.map(item => (
                <tr key={item.id} style={{ borderTop: '1px solid rgba(255,255,255,0.05)' }}>
                  <td style={{ padding: '1rem', color: 'var(--lux-gold)', fontFamily: 'monospace' }}>{item.woNumber}</td>
                  <td style={{ padding: '1rem' }}>{item.productSkuName || item.productSkuNumber}</td>
                  <td style={{ padding: '1rem' }}>{item.actualQty}</td>
                  <td style={{ padding: '1rem' }}>${item.totalStandardCost?.toLocaleString()}</td>
                  <td style={{ padding: '1rem' }}>${(item.actualMaterialCost + item.actualLaborCost + item.actualOverheadCost).toLocaleString()}</td>
                  <td style={{ padding: '1rem' }}>
                    <span style={{ padding: '0.25rem 0.5rem', borderRadius: '4px', fontSize: '11px', background: item.status === 'Completed' ? 'rgba(52, 211, 153, 0.1)' : 'rgba(245, 158, 11, 0.1)', color: item.status === 'Completed' ? '#34d399' : '#f59e0b' }}>
                      {item.status}
                    </span>
                  </td>
                  <td style={{ padding: '1rem' }}>
                    <button onClick={() => setSelectedOrderId(item.id)} style={{ background: 'none', border: '1px solid var(--lux-gold)', color: 'var(--lux-gold)', padding: '0.25rem 0.75rem', borderRadius: '4px', cursor: 'pointer', fontSize: '12px' }}>
                      詳細分析
                    </button>
                  </td>
                </tr>
              ))
            ) : (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: '2rem', color: 'var(--slate-500)' }}>目前無待結轉工單資料</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};
