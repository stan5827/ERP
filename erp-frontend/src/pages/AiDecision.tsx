import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, AreaChart, Area } from 'recharts';
import { 
  TrendingUp, TrendingDown, AlertTriangle, Package, ShoppingCart, 
  Zap, ArrowRight, Brain, Target, Cpu, Sparkles
} from 'lucide-react';

interface Forecast {
  skuId: string;
  skuName: string;
  period: string;
  forecastQty: number;
  confidence: number;
  trend: string;
}

interface Recommendation {
  skuId: string;
  skuName: string;
  currentStock: number;
  safetyStock: number;
  reorderPoint: number;
  recommendedQty: number;
  urgency: string;
  reason: string;
  estimatedDeliveryDays: number;
  estimatedCost: number;
}

interface ForecastChartDatum {
  period: string;
  [skuName: string]: string | number;
}

const containerVariants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { staggerChildren: 0.1 } }
};

const itemVariants = {
  hidden: { y: 20, opacity: 0 },
  visible: { y: 0, opacity: 1 }
};

export function AiDecision() {
  const [activeTab, setActiveTab] = useState<'forecast' | 'procurement' | 'orders' | 'insights'>('forecast');

  const forecasts: Forecast[] = [
    { skuId: 'SKU-001', skuName: '產品A', period: '2026-04', forecastQty: 1200, confidence: 0.85, trend: 'up' },
    { skuId: 'SKU-002', skuName: '產品B', period: '2026-04', forecastQty: 800, confidence: 0.78, trend: 'stable' },
    { skuId: 'SKU-003', skuName: '產品C', period: '2026-04', forecastQty: 650, confidence: 0.72, trend: 'down' },
    { skuId: 'SKU-001', skuName: '產品A', period: '2026-05', forecastQty: 1350, confidence: 0.82, trend: 'up' },
    { skuId: 'SKU-002', skuName: '產品B', period: '2026-05', forecastQty: 820, confidence: 0.75, trend: 'stable' },
    { skuId: 'SKU-003', skuName: '產品C', period: '2026-05', forecastQty: 620, confidence: 0.68, trend: 'down' },
    { skuId: 'SKU-001', skuName: '產品A', period: '2026-06', forecastQty: 1400, confidence: 0.78, trend: 'up' },
    { skuId: 'SKU-002', skuName: '產品B', period: '2026-06', forecastQty: 850, confidence: 0.71, trend: 'stable' },
    { skuId: 'SKU-003', skuName: '產品C', period: '2026-06', forecastQty: 580, confidence: 0.65, trend: 'down' },
  ];

  const recommendations: Recommendation[] = [
    { skuId: 'RAW-001', skuName: '原料A', currentStock: 150, safetyStock: 200, reorderPoint: 300, recommendedQty: 450, urgency: 'high', reason: '庫存低於安全庫存，建議立即補貨', estimatedDeliveryDays: 7, estimatedCost: 45000 },
    { skuId: 'RAW-002', skuName: '原料B', currentStock: 800, safetyStock: 500, reorderPoint: 600, recommendedQty: 200, urgency: 'medium', reason: '庫存接近再訂點，建議適量補貨', estimatedDeliveryDays: 14, estimatedCost: 28000 },
    { skuId: 'RAW-003', skuName: '原料C', currentStock: 1200, safetyStock: 400, reorderPoint: 500, recommendedQty: 0, urgency: 'low', reason: '庫存充足，無需補貨', estimatedDeliveryDays: 0, estimatedCost: 0 },
  ];

  const chartData = forecasts.reduce<ForecastChartDatum[]>((acc, f) => {
    const existing = acc.find(a => a.period === f.period);
    if (existing) {
      existing[f.skuName] = f.forecastQty;
    } else {
      acc.push({ period: f.period, [f.skuName]: f.forecastQty });
    }
    return acc;
  }, []);

  const tabs = [
    { id: 'forecast', label: '需求預測', icon: <TrendingUp size={16} /> },
    { id: 'procurement', label: '採購建議', icon: <ShoppingCart size={16} /> },
    { id: 'orders', label: '訂單分析', icon: <Target size={16} /> },
    { id: 'insights', label: '工廠洞察', icon: <Zap size={16} /> },
  ] as const;

  return (
    <motion.div 
      initial="hidden"
      animate="visible"
      variants={containerVariants}
      className="dashboard page-enter"
      style={{ maxWidth: 1400, margin: '0 auto', padding: '0 24px 40px' }}
    >
      <div className="mesh-bg" />
      
      {/* 頁面標題 */}
      <motion.div variants={itemVariants} className="dashboard-header" style={{ marginBottom: 32 }}>
        <div style={{ position: 'relative', zIndex: 1 }}>
          <div className="dashboard-greeting" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Sparkles size={14} /> AI POWERED DECISIONS
          </div>
          <h1 className="dashboard-title">智能決策指揮中心</h1>
          <div className="dashboard-subtitle">
            <span>使用深度學習模型進行需求與供應鏈優化分析</span>
          </div>
        </div>
        <div className="dashboard-actions">
          <div className="premium-tag" style={{ fontSize: 12, padding: '8px 16px' }}>
            <Cpu size={14} style={{ marginRight: 6 }} /> NEURAL ENGINE ACTIVE
          </div>
        </div>
      </motion.div>

      {/* 標籤切換 */}
      <motion.div variants={itemVariants} className="tab-list" style={{ maxWidth: 600, marginBottom: 32, padding: 6, gap: 8, border: '1px solid var(--premium-border)' }}>
        {tabs.map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`tab-item ${activeTab === tab.id ? 'active' : ''}`}
            style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 8, height: 40 }}
          >
            {tab.icon} {tab.label}
          </button>
        ))}
      </motion.div>

      <AnimatePresence mode="wait">
        <motion.div
          key={activeTab}
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -10 }}
          transition={{ duration: 0.2 }}
        >
          {activeTab === 'forecast' && (
            <div className="dashboard-main-grid">
              <div className="dashboard-left-col">
                <div className="premium-card" style={{ padding: 32, height: 500 }}>
                  <div className="section-header-premium">
                    <div>
                      <h3 className="section-title">未來 90 天需求核心預測</h3>
                      <div className="section-subtitle">模型信心度 92.4% | 已整合市場趨勢因子</div>
                    </div>
                  </div>
                  <ResponsiveContainer width="100%" height={380}>
                    <AreaChart data={chartData}>
                      <defs>
                        <linearGradient id="colorA" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="var(--cobalt-400)" stopOpacity={0.3}/>
                          <stop offset="95%" stopColor="var(--cobalt-400)" stopOpacity={0}/>
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
                      <XAxis dataKey="period" axisLine={false} tickLine={false} tick={{ fill: 'var(--slate-500)', fontSize: 11 }} dy={10} />
                      <YAxis axisLine={false} tickLine={false} tick={{ fill: 'var(--slate-500)', fontSize: 11 }} dx={-10} />
                      <Tooltip 
                        contentStyle={{ background: 'rgba(13, 24, 41, 0.9)', borderRadius: 12, border: '1px solid var(--cobalt-500)', color: '#fff' }}
                        itemStyle={{ color: '#fff' }}
                      />
                      <Area type="monotone" dataKey="產品A" stroke="var(--cobalt-400)" strokeWidth={3} fillOpacity={1} fill="url(#colorA)" />
                      <Area type="monotone" dataKey="產品B" stroke="var(--emerald-400)" strokeWidth={3} fillOpacity={1} fill="rgba(16, 185, 122, 0.1)" />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>
              <div className="dashboard-sidebar">
                <div className="premium-card" style={{ padding: 24, height: '100%' }}>
                  <h3 className="section-title" style={{ marginBottom: 20 }}>預測摘要</h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                    {forecasts.slice(0, 3).map((f) => (
                      <div key={f.skuId} className="wo-item-premium" style={{ border: 'none', background: 'rgba(255,255,255,0.03)', borderRadius: 12, padding: 16 }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <span style={{ fontWeight: 600, color: 'var(--slate-100)' }}>{f.skuName}</span>
                          <span style={{ color: f.trend === 'up' ? 'var(--emerald-400)' : 'var(--crimson-400)' }}>
                            {f.trend === 'up' ? <TrendingUp size={16} /> : <TrendingDown size={16} />}
                          </span>
                        </div>
                        <div style={{ fontSize: 24, fontWeight: 700, margin: '8px 0', fontFamily: 'var(--font-mono)', color: 'var(--cobalt-300)' }}>{f.forecastQty}</div>
                        <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>信心度 {Math.round(f.confidence * 100)}%</div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {activeTab === 'procurement' && (
            <div className="premium-card" style={{ padding: 0, overflow: 'hidden' }}>
              <div style={{ padding: 24, borderBottom: '1px solid var(--premium-border)' }}>
                <h3 className="section-title">智能補貨清單</h3>
                <div className="section-subtitle">根據預測需求與庫存水位自動生成</div>
              </div>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>物料編號</th>
                    <th>庫存明細</th>
                    <th>再訂點</th>
                    <th>建議採購</th>
                    <th>緊急程度</th>
                    <th>預估成本</th>
                    <th style={{ textAlign: 'right' }}>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {recommendations.filter(r => r.recommendedQty > 0).map(r => (
                    <tr key={r.skuId}>
                      <td>
                        <div style={{ fontWeight: 600, color: 'var(--slate-100)' }}>{r.skuName}</div>
                        <div style={{ fontSize: 11, color: 'var(--slate-500)', fontFamily: 'var(--font-mono)' }}>{r.skuId}</div>
                      </td>
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                          <span style={{ fontSize: 16, fontWeight: 700, color: r.currentStock < r.safetyStock ? 'var(--crimson-400)' : 'var(--emerald-400)' }}>{r.currentStock}</span>
                          <span style={{ fontSize: 11, color: 'var(--slate-500)' }}>/ 安全 {r.safetyStock}</span>
                        </div>
                      </td>
                      <td style={{ fontFamily: 'var(--font-mono)', color: 'var(--slate-400)' }}>{r.reorderPoint}</td>
                      <td>
                        <div style={{ display: 'flex', alignItems: 'center', gap: 6, color: 'var(--cobalt-300)', fontWeight: 700 }}>
                          <Package size={14} /> +{r.recommendedQty}
                        </div>
                      </td>
                      <td>
                        <span className={`status-badge ${r.urgency === 'high' ? 'status-danger' : 'status-warning'}`}>
                          {r.urgency === 'high' ? '立即處理' : '建議關注'}
                        </span>
                      </td>
                      <td style={{ color: 'var(--emerald-400)', fontWeight: 600 }}>NT$ {r.estimatedCost.toLocaleString()}</td>
                      <td style={{ textAlign: 'right' }}>
                        <button className="btn btn-primary btn-sm">啟動採購</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {activeTab === 'insights' && (
            <div className="dashboard-main-grid">
              <div className="dashboard-left-col">
                <div style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
                  {[
                    { title: '關鍵原料短缺風險', desc: '原料 RAW-001 庫存預計於 72 小時內耗盡，將影響核心產線運作。', severity: 'critical', action: '執行緊急調度' },
                    { title: '產線效率偏差預警', desc: 'Line-B 目前效能低於基準線 15%，分析顯示為設備微停頻次增加。', severity: 'warning', action: '排查設備狀態' }
                  ].map((insight, idx) => (
                    <div key={idx} className="premium-card" style={{ padding: 24, borderLeft: `4px solid ${insight.severity === 'critical' ? 'var(--crimson-400)' : 'var(--amber-400)'}` }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                        <div style={{ flex: 1 }}>
                          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
                            {insight.severity === 'critical' ? <AlertTriangle size={20} color="var(--crimson-400)" /> : <Zap size={20} color="var(--amber-400)" />}
                            <h4 style={{ fontSize: 18, color: '#fff' }}>{insight.title}</h4>
                          </div>
                          <p style={{ color: 'var(--slate-400)', fontSize: 14, lineHeight: 1.6 }}>{insight.desc}</p>
                          <div style={{ marginTop: 24, padding: 16, background: 'rgba(255,255,255,0.03)', borderRadius: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <div style={{ fontSize: 13, color: 'var(--slate-200)', display: 'flex', alignItems: 'center', gap: 8 }}>
                              <Sparkles size={14} color="var(--cobalt-300)" /> 建議行動：{insight.action}
                            </div>
                            <button className="btn btn-primary btn-sm">立即執行</button>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
              <div className="dashboard-sidebar">
                <div className="premium-card" style={{ padding: 24, height: 400 }}>
                  <h3 className="section-title" style={{ marginBottom: 20 }}>資源受影響分析</h3>
                  <div style={{ height: 300, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                    <div style={{ textAlign: 'center' }}>
                      <Brain size={48} color="var(--cobalt-400)" style={{ marginBottom: 16, opacity: 0.5 }} />
                      <div style={{ fontSize: 12, color: 'var(--slate-500)' }}>正在生成多維度影響矩陣...</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
          
          {activeTab === 'orders' && (
             <div className="premium-card" style={{ padding: 32 }}>
                <div className="section-header-premium">
                  <div>
                    <h3 className="section-title">基於交期與價值的訂單優先序</h3>
                    <div className="section-subtitle">分析 152 筆進行中訂單 | 自動調整排程建議</div>
                  </div>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                  {[1, 2, 3].map(i => (
                    <div key={i} className="wo-item-premium" style={{ display: 'flex', alignItems: 'center', gap: 20, padding: 20, background: 'rgba(255,255,255,0.02)', border: '1px solid var(--premium-border)', borderRadius: 16 }}>
                      <div style={{ width: 40, height: 40, borderRadius: '50%', background: 'var(--cobalt-500)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 800, color: '#fff' }}>{i}</div>
                      <div style={{ flex: 1 }}>
                        <div style={{ fontWeight: 600, fontSize: 16, color: 'var(--slate-100)' }}>SO-2026-00{i} <span className="premium-tag" style={{ marginLeft: 8 }}>TOP PRIORITY</span></div>
                        <div style={{ fontSize: 13, color: 'var(--slate-400)', marginTop: 4 }}>客戶 A | 全球電子合作夥伴</div>
                      </div>
                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontSize: 12, color: 'var(--slate-500)' }}>交期 2026-04-15</div>
                        <div style={{ fontSize: 14, color: 'var(--emerald-400)', fontWeight: 600, marginTop: 4 }}>風險性偵測：低</div>
                      </div>
                      <button className="btn btn-ghost btn-sm"><ArrowRight size={14} /></button>
                    </div>
                  ))}
                </div>
             </div>
          )}
        </motion.div>
      </AnimatePresence>
    </motion.div>
  );
}

export default AiDecision;
