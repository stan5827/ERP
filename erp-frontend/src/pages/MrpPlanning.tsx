import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Play, 
  Settings, 
  History, 
  CheckCircle2, 
  Package, 
  ShoppingCart, 
  Factory, 
  Search, 
  Filter, 
  ArrowRightLeft, 
  Calendar, 
  Clock, 
  Loader2, 
  FileText,
  LineChart,
  BarChart2,
  ChevronRight
} from 'lucide-react';
import { toast } from 'react-hot-toast';
import { mrpApi, MrpRunDto, MrpPlannedOrderDto } from '../api/api';
import { FormModal } from '../components/FormModal';
import './MrpPlanning.css';
import './PremiumDesign.css';

interface MrpPlanningProps {
  onNavigate?: (module: string) => void;
}

export const MrpPlanning: React.FC<MrpPlanningProps> = ({ onNavigate }) => {
  const [activeTab, setActiveTab] = useState<'orders' | 'history'>('orders');
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [isRunModalOpen, setIsRunModalOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const queryClient = useQueryClient();

  // Queries
  const { data: summary } = useQuery({
    queryKey: ['mrp-summary'],
    queryFn: () => mrpApi.getSummary().then(res => res.data?.data)
  });

  const { data: runs } = useQuery({
    queryKey: ['mrp-runs'],
    queryFn: () => mrpApi.getRuns().then(res => res.data?.data)
  });

  const { data: orders, isLoading: ordersLoading } = useQuery({
    queryKey: ['mrp-orders', selectedRunId || summary?.latestRunId],
    queryFn: () => {
      const runId = selectedRunId || summary?.latestRunId;
      if (!runId) return Promise.resolve([]);
      return mrpApi.getOrders(runId).then(res => res.data?.data || []);
    },
    enabled: !!(selectedRunId || summary?.latestRunId)
  });

  // Mutations
  const runMrpMutation = useMutation({
    mutationFn: (data: { horizonStart: string; horizonEnd: string; runType: string }) => 
      mrpApi.runMrp(data),
    onSuccess: () => {
      toast.success('MRP 計算已啟動');
      queryClient.invalidateQueries({ queryKey: ['mrp-summary'] });
      queryClient.invalidateQueries({ queryKey: ['mrp-runs'] });
      setIsRunModalOpen(false);
    },
    onError: (err: any) => toast.error(`啟動失敗: ${err.message}`)
  });

  const convertMutation = useMutation({
    mutationFn: (orderId: string) => mrpApi.convertOrder(orderId),
    onSuccess: (res) => {
      const result = res.data?.data;
      toast.success(`${result?.message || '轉換成功'}: ${result?.documentNumber || ''}`);
      queryClient.invalidateQueries({ queryKey: ['mrp-orders'] });
      queryClient.invalidateQueries({ queryKey: ['mrp-summary'] });
    },
    onError: (err: any) => toast.error(`轉換失敗: ${err.message}`)
  });

  const handleRunMrp = async (formData: any) => {
    await runMrpMutation.mutateAsync(formData);
  };

  // 修復 FE-001: 使用 server-side 搜尋，而非 client 端過濾
  const { data: filteredOrders, isLoading: filteredLoading } = useQuery({
    queryKey: ['mrp-orders-filtered', selectedRunId || summary?.latestRunId, searchTerm],
    queryFn: () => {
      const runId = selectedRunId || summary?.latestRunId;
      if (!runId) return Promise.resolve([]);
      return mrpApi.getOrders(runId, searchTerm).then(res => res.data?.data || []);
    },
    enabled: !!(selectedRunId || summary?.latestRunId)
  });

  return (
    <div className="mrp-page animate-up">
      {/* Header */}
      <div className="mrp-header">
        <div className="mrp-title">
          <h1>物料需求規劃 (MRP)</h1>
          <div className="last-run-info">
            <Clock size={16} />
            <span>最後執行回報: {summary?.completedAt ? new Date(summary.completedAt).toLocaleString() : '系統就緒，等待首執行'}</span>
          </div>
        </div>
        <div className="flex gap-4">
          <button 
            className="btn btn-outline flex items-center gap-2"
            onClick={() => setActiveTab(activeTab === 'orders' ? 'history' : 'orders')}
          >
            {activeTab === 'orders' ? <History size={18} /> : <FileText size={18} />}
            <span>{activeTab === 'orders' ? '查看執行歷史' : '返回建議清單'}</span>
          </button>
          <button 
            className="btn btn-primary flex items-center gap-2 px-8"
            onClick={() => setIsRunModalOpen(true)}
          >
            <Play size={18} fill="currentColor" />
            <span>啟動 MRP 運算</span>
          </button>
        </div>
      </div>

      {/* Summary Grid */}
      <div className="summary-grid">
        <div className="summary-card">
          <div className="card-icon icon-blue">
            <Factory size={24} />
          </div>
          <div className="card-value">
            {summary?.plannedOrders?.find((p: any) => p.type === 'work_order')?.count || 0}
          </div>
          <div className="card-label">建議生產工單</div>
          <div className="card-trend"><LineChart size={40} /></div>
        </div>
        
        <div className="summary-card">
          <div className="card-icon icon-emerald">
            <ShoppingCart size={24} />
          </div>
          <div className="card-value">
            {summary?.plannedOrders?.find((p: any) => p.type === 'purchase_order')?.count || 0}
          </div>
          <div className="card-label">建議採購申請</div>
          <div className="card-trend"><BarChart2 size={40} /></div>
        </div>

        <div className="summary-card">
          <div className="card-icon icon-amber">
            <Settings size={24} />
          </div>
          <div className="card-value">
            {summary?.latestRunNo || '--'}
          </div>
          <div className="card-label">當前計畫版本</div>
        </div>

        <div className="summary-card">
          <div className="card-icon icon-purple">
            <ArrowRightLeft size={24} />
          </div>
          <div className="card-value">
            {summary?.plannedOrders?.length || 0}
          </div>
          <div className="card-label">關聯排程總數</div>
          <button className="flex items-center text-xs text-purple-400 hover:text-purple-300 mt-4 cursor-pointer" onClick={() => onNavigate?.('aps')}>
            前往生產排程 <ChevronRight size={12} className="ml-1" />
          </button>
        </div>
      </div>

      {activeTab === 'orders' ? (
        <div className="space-y-6">
          {/* Controls Bar */}
          <div className="mrp-controls">
            <div className="search-box">
              <Search className="search-icon" size={18} />
              <input 
                type="text" 
                placeholder="搜尋料號、品名或規格..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
            <div className="flex items-center gap-4">
              <select 
                className="bg-slate-900/80 border border-slate-700/50 rounded-lg py-2 px-4 text-slate-200 outline-none focus:border-purple-500"
                onChange={(e) => setSelectedRunId(e.target.value)}
                value={selectedRunId || ''}
              >
                <option value="">最新計畫結果</option>
                {runs?.map((run: MrpRunDto) => (
                  <option key={run.id} value={run.id}>{run.runNumber} ({new Date(run.completedAt || '').toLocaleDateString()})</option>
                ))}
              </select>
              <button className="btn btn-outline btn-sm">
                <Filter size={16} />
                <span>進階篩選</span>
              </button>
            </div>
          </div>

          {/* Orders Table */}
          <div className="mrp-table-container">
            <table className="mrp-table">
              <thead>
                <tr>
                  <th>需求類型</th>
                  <th>產品資訊</th>
                  <th>建議量</th>
                  <th>計畫開工</th>
                  <th>計畫交期</th>
                  <th>處理狀態</th>
                  <th className="text-right">系統操作</th>
                </tr>
              </thead>
              <tbody>
                {ordersLoading ? (
                  <tr>
                    <td colSpan={7} className="text-center py-24 text-slate-500">
                      <div className="flex flex-col items-center gap-4">
                        <Loader2 className="animate-spin text-purple-500" size={40} />
                        <span className="text-lg font-medium">引擎正在運算物料需求數據...</span>
                      </div>
                    </td>
                  </tr>
                ) : filteredOrders?.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="text-center py-24 text-slate-500">
                      <div className="flex flex-col items-center gap-4">
                        <Package size={64} className="text-slate-800" />
                        <div className="text-xl font-semibold text-slate-600">目前無任何建議計畫</div>
                        <p className="text-slate-500">請確認是否已匯入 MPS 或手動啟動 MRP 運算</p>
                      </div>
                    </td>
                  </tr>
                ) : filteredOrders?.map((order: MrpPlannedOrderDto) => (
                  <tr key={order.id} className="group">
                    <td>
                      <span className={`type-badge ${order.orderType === 'work_order' ? 'type-wo' : 'type-po'}`}>
                        {order.orderType === 'work_order' ? '生產工單' : '採購申請'}
                      </span>
                    </td>
                    <td>
                      <div className="sku-info-cell">
                        <span className="sku-code">{order.skuNumber}</span>
                        <span className="sku-name">{order.skuName}</span>
                      </div>
                    </td>
                    <td><span className="qty-badge">{order.orderQty?.toLocaleString()}</span></td>
                    <td>
                      <div className="date-cell">
                        <span className="date-label">START</span>
                        <span>{new Date(order.plannedStartDate).toLocaleDateString()}</span>
                      </div>
                    </td>
                    <td>
                      <div className="date-cell">
                        <span className="date-label">END</span>
                        <span className="font-bold">{new Date(order.plannedEndDate).toLocaleDateString()}</span>
                      </div>
                    </td>
                    <td>
                      <span className={`status-pill ${order.status === 'confirmed' ? 'pill-emerald' : 'pill-amber'}`}>
                        {order.status === 'confirmed' ? '已執行轉單' : '建議中 (Draft)'}
                      </span>
                    </td>
                    <td className="text-right">
                      {order.status === 'suggested' ? (
                        <button 
                          className="action-btn opacity-0 group-hover:opacity-100"
                          onClick={() => convertMutation.mutate(order.id)}
                        >
                          核准並轉發
                        </button>
                      ) : (
                        <div className="converted-doc">
                          <CheckCircle2 size={16} />
                          <span>{order.convertedDocumentNumber}</span>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        <div className="mrp-table-container animate-up">
          <table className="mrp-table">
            <thead>
              <tr>
                <th>計畫執行編號</th>
                <th>運算類型</th>
                <th>規劃起點</th>
                <th>規劃終點</th>
                <th>執行狀態</th>
                <th>完成時間時間</th>
                <th>執行人</th>
              </tr>
            </thead>
            <tbody>
              {runs?.map((run: MrpRunDto) => (
                <tr key={run.id} className="cursor-pointer hover:bg-white/5 transition-colors" onClick={() => {
                  setSelectedRunId(run.id);
                  setActiveTab('orders');
                }}>
                  <td className="sku-code">{run.runNumber}</td>
                  <td>{run.runType === 'regenerative' ? '全重算 (Regen)' : '淨變算 (Net Change)'}</td>
                  <td>{new Date(run.planningHorizonStart).toLocaleDateString()}</td>
                  <td>{new Date(run.planningHorizonEnd).toLocaleDateString()}</td>
                  <td><span className="status-pill pill-emerald">Success</span></td>
                  <td>{new Date(run.completedAt || '').toLocaleString()}</td>
                  <td className="font-medium text-slate-400">{run.createdBy || 'AI Agent'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* MRP Run Modal */}
      <FormModal
        open={isRunModalOpen}
        onClose={() => setIsRunModalOpen(false)}
        title="啟動 MRP 運算核心"
        fields={[
          { name: 'runType', label: '運算邏輯模式', type: 'select', options: [
            { value: 'regenerative', label: '全重算模式 (Regenerative) - 完整清理並重新排成' },
            { value: 'net_change', label: '淨變模式 (Net Change) - 僅針對變動需求增量計算' }
          ], defaultValue: 'regenerative', required: true },
          { name: 'horizonStart', label: '需求分析起點', type: 'date', defaultValue: new Date().toISOString().split('T')[0], required: true },
          { name: 'horizonEnd', label: '需求分析終點', type: 'date', defaultValue: new Date(Date.now() + 90*24*60*60*1000).toISOString().split('T')[0], required: true },
          { name: 'notes', label: '執行備註', type: 'text', placeholder: '例如：因應 Q3 訂單突增進行緊急重算' }
        ]}
        onSubmit={handleRunMrp}
      />
    </div>
  );
};
