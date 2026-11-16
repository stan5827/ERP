import React, { useCallback, useState, useEffect } from 'react';
import { Search, Plus, Edit2, Trash2, Package, Settings, Loader2, X, Factory, Clock, Route, ArrowRight, ChevronUp, ChevronDown, Layers, Zap, DollarSign, ExternalLink, CheckCircle2, XCircle } from 'lucide-react';
import { workCentersApi, operationsApi, routingsApi, skusApi, rccpApi, bomsApi } from '../api/api';
import type { SkuDto } from '../api/api';
import './ManufacturingEngineering.css';

interface EngineeringRecord {
  id: string;
  items?: BomItem[];
  operations?: RoutingOp[];
}

interface WorkCenter extends EngineeringRecord {
  id: string;
  workCenterCode: string;
  workCenterName: string;
  capacity: number;
  efficiency: number;
  isActive: boolean;
}

interface Operation extends EngineeringRecord {
  id: string;
  operationCode: string;
  operationName: string;
  setupTime: number;
  runTime: number;
  costPerHour: number;
  isOutsourced: boolean;
  isActive: boolean;
}

interface RoutingOp {
  id?: string;
  operationSeq: number;
  operationId: string;
  operationName?: string;
  workCenterId: string;
  workCenterName?: string;
  transferTime: number;
  waitTime: number;
}

interface Routing extends EngineeringRecord {
  id: string;
  routingCode: string;
  routingName: string;
  productSkuNumber?: string;
  productSkuName?: string;
  productSkuId: string;
  version: string;
  isActive: boolean;
  operations: RoutingOp[];
}

interface BomItem {
  id?: string;
  childSkuId: string;
  childSkuNumber?: string;
  childSkuName?: string;
  quantity: number;
  unit?: string;
  unitName?: string;
  scrapRate: number;
  operationSeq: number;
  isPhantom: boolean;
  effectiveDate?: string;
}

interface Bom extends EngineeringRecord {
  id: string;
  bomCode: string;
  productSkuId: string;
  productSkuNumber?: string;
  productSkuName?: string;
  version: string;
  status: string;
  effectiveDate: string;
  expiryDate?: string;
  notes?: string;
  items: BomItem[];
}

interface RccpResource extends EngineeringRecord {
  resourceCode: string;
  resourceName: string;
  resourceType?: string;
  workCenter?: { workCenterName?: string };
  capacityPerBucket?: number;
  uom?: string;
  isActive?: boolean;
}

type TabType = 'boms' | 'workcenters' | 'operations' | 'routings' | 'rccp';

function isRccpResource(value: unknown): value is RccpResource {
  return typeof value === 'object'
    && value !== null
    && 'resourceCode' in value
    && typeof value.resourceCode === 'string'
    && 'resourceName' in value
    && typeof value.resourceName === 'string';
}

function getErrorMessage(error: unknown): string {
  if (typeof error !== 'object' || error === null)
    return error instanceof Error ? error.message : String(error);
  const response = 'response' in error ? error.response : undefined;
  const responseData = typeof response === 'object' && response !== null && 'data' in response
    ? response.data
    : undefined;
  const responseMessage = typeof responseData === 'object' && responseData !== null && 'message' in responseData
    ? responseData.message
    : undefined;
  return typeof responseMessage === 'string'
    ? responseMessage
    : error instanceof Error ? error.message : String(error);
}

const TAB_CONFIG = {
  boms: {
    label: 'BOM 管理',
    icon: Package,
    description: '管理產品結構與物料清單 (Bill of Materials)',
    color: 'bom',
  },
  workcenters: {
    label: '工作中心',
    icon: Factory,
    description: '管理生產線與工作站配置',
    color: 'workcenter',
  },
  operations: {
    label: '工序管理',
    icon: Settings,
    description: '定義工序參數與加工標準',
    color: 'operation',
  },
  routings: {
    label: '工藝管理',
    icon: Route,
    description: '規劃產品製造流程與工序順序',
    color: 'routing',
  },
  rccp: {
    label: '產能資源',
    icon: Zap,
    description: '定義 MPS/RCCP 使用的關鍵產能資源',
    color: 'rccp',
  },
};

export const ManufacturingEngineering: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>('boms');
  const [boms, setBoms] = useState<Bom[]>([]);
  const [workCenters, setWorkCenters] = useState<WorkCenter[]>([]);
  const [operations, setOperations] = useState<Operation[]>([]);
  const [routings, setRoutings] = useState<Routing[]>([]);
  const [rccpResources, setRccpResources] = useState<RccpResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const [showModal, setShowModal] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [editingItem, setEditingItem] = useState<EngineeringRecord | null>(null);
  const [formData, setFormData] = useState<Record<string, unknown>>({});
  const [routingOps, setRoutingOps] = useState<RoutingOp[]>([]);
  const [bomItems, setBomItems] = useState<BomItem[]>([]);
  const [skus, setSkus] = useState<SkuDto[]>([]);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (showModal && activeTab === 'routings') {
      skusApi.all().then(r => setSkus((r.data?.data || []).filter(s => s.status === 'active')));
    }
  }, [showModal, activeTab]);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      if (activeTab === 'boms') {
        const res = await bomsApi.list();
        setBoms((res.data?.data ?? []) as Bom[]);
      } else if (activeTab === 'workcenters') {
        const res = await workCentersApi.list();
        setWorkCenters((res.data?.data ?? []) as WorkCenter[]);
      } else if (activeTab === 'operations') {
        const res = await operationsApi.list();
        setOperations((res.data?.data ?? []) as Operation[]);
      } else if (activeTab === 'routings') {
        const res = await routingsApi.list();
        setRoutings((res.data?.data ?? []) as Routing[]);
      } else if (activeTab === 'rccp') {
        const res = await rccpApi.listResources();
        setRccpResources((res.data?.data ?? []).filter(isRccpResource));
      }
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  }, [activeTab]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const openCreate = async () => {
    setModalMode('create');
    setEditingItem(null);
    setFormData({});
    setRoutingOps([]);
    setBomItems([]);
    if (activeTab === 'boms') {
      setFormData({ version: '1.0', status: 'active', effectiveDate: new Date().toISOString().split('T')[0] });
      if (skus.length === 0) {
        const skusRes = await skusApi.all();
        setSkus((skusRes.data?.data || []).filter(s => s.status === 'active'));
      }
    }
    if (activeTab === 'workcenters') setFormData({ isActive: true, capacity: 0, efficiency: 100 });
    if (activeTab === 'operations') setFormData({ isActive: true, setupTime: 0, runTime: 0, costPerHour: 0, isOutsourced: false });
    if (activeTab === 'routings') {
      setFormData({ version: '1.0', isActive: true });
      // 載入產品、工序和工作中心
      if (skus.length === 0) {
        const skusRes = await skusApi.all();
        setSkus((skusRes.data?.data || []).filter(s => s.status === 'active'));
      }
      if (operations.length === 0) {
        const opsRes = await operationsApi.list();
        setOperations((opsRes.data?.data ?? []) as Operation[]);
      }
      if (workCenters.length === 0) {
        const wcRes = await workCentersApi.list();
        setWorkCenters((wcRes.data?.data ?? []) as WorkCenter[]);
      }
    }
    setShowModal(true);
  };

  const openEdit = async (item: EngineeringRecord) => {
    setModalMode('edit');
    setEditingItem(item);
    setFormData({ ...item });
    if (activeTab === 'boms') {
      let availableSkus = skus;
      try {
        if (availableSkus.length === 0) {
          const response = await skusApi.all();
          availableSkus = (response.data?.data || []).filter(sku => sku.status === 'active');
          setSkus(availableSkus);
        }
      } catch (error: unknown) {
        alert(`載入產品與元件 SKU 失敗：${getErrorMessage(error)}`);
        return;
      }
      setBomItems((item.items || []).map((bomItem: BomItem) => ({
        ...bomItem,
        unit: availableSkus.find(sku => sku.id === bomItem.childSkuId)?.productionUomCode || bomItem.unit || '',
        unitName: availableSkus.find(sku => sku.id === bomItem.childSkuId)?.productionUomName || ''
      })));
    }
    if (activeTab === 'routings') {
      setRoutingOps([...(item.operations || [])].sort((a, b) => a.operationSeq - b.operationSeq));
      try {
        const [skuRes, opsRes, wcRes] = await Promise.all([
          skusApi.all(),
          operationsApi.list(),
          workCentersApi.list(),
        ]);
        setSkus((skuRes.data?.data || []).filter(sku => sku.status === 'active'));
        setOperations((opsRes.data?.data ?? []) as Operation[]);
        setWorkCenters((wcRes.data?.data ?? []) as WorkCenter[]);
      } catch (error: unknown) {
        alert(`載入產品、工序與工作中心失敗：${getErrorMessage(error)}`);
        return;
      }
    }
    setShowModal(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('確定要刪除此資料嗎？')) return;
    try {
      if (activeTab === 'boms') await bomsApi.delete(id);
      else if (activeTab === 'workcenters') await workCentersApi.delete(id);
      else if (activeTab === 'operations') await operationsApi.delete(id);
      else if (activeTab === 'routings') await routingsApi.delete(id);
      // NOTE: rccpApi delete endpoint not yet implemented in backend, skipping for now
      await loadData();
    } catch (err: unknown) {
      alert('刪除失敗：' + getErrorMessage(err));
    }
  };

  const handleSubmit = async () => {
    setSubmitting(true);
    try {
      if (activeTab === 'boms') {
        const items = bomItems.map(it => ({
          childSkuId: it.childSkuId,
          quantity: it.quantity,
          unit: skus.find(sku => sku.id === it.childSkuId)?.productionUomCode || it.unit || '',
          scrapRate: it.scrapRate,
          operationSeq: it.operationSeq,
          isPhantom: it.isPhantom
        }));
        const itemWithoutUnit = items.find(item => !item.childSkuId || !item.unit || item.quantity <= 0);
        if (itemWithoutUnit) {
          alert('每個 BOM 元件都必須選擇有效的 SKU，且數量須大於 0；元件 SKU 也必須設定生產單位。');
          return;
        }
        const bomData = {
          ...formData,
          items
        };
        if (modalMode === 'create') await bomsApi.create(bomData);
        else await bomsApi.update(editingItem.id, bomData);
      } else if (activeTab === 'workcenters') {
        if (modalMode === 'create') await workCentersApi.create(formData);
        else await workCentersApi.update(editingItem.id, formData);
      } else if (activeTab === 'operations') {
        if (modalMode === 'create') await operationsApi.create(formData);
        else await operationsApi.update(editingItem.id, formData);
      } else if (activeTab === 'routings') {
        const routingData: Record<string, unknown> = {
          ...formData,
          operations: routingOps.map((op, idx) => ({
            operationSeq: idx + 1,
            operationId: op.operationId,
            workCenterId: op.workCenterId,
            transferTime: op.transferTime,
            waitTime: op.waitTime,
          })),
        };
        if (modalMode === 'create') await routingsApi.create(routingData);
        else await routingsApi.update(editingItem.id, routingData);
      } else if (activeTab === 'rccp') {
        if (modalMode === 'create') await rccpApi.createResource(formData);
        else await rccpApi.updateResource(editingItem.id, formData);
      }
      setShowModal(false);
      await loadData();
    } catch (err: unknown) {
      alert('儲存失敗：' + getErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const filteredBoms = boms.filter(b =>
    b.bomCode.toLowerCase().includes(search.toLowerCase()) ||
    (b.productSkuNumber ?? '').toLowerCase().includes(search.toLowerCase()) ||
    (b.productSkuName ?? '').toLowerCase().includes(search.toLowerCase())
  );
  const filteredWorkCenters = workCenters.filter(wc =>
    wc.workCenterCode.toLowerCase().includes(search.toLowerCase()) ||
    wc.workCenterName.toLowerCase().includes(search.toLowerCase())
  );
  const filteredOperations = operations.filter(op =>
    op.operationCode.toLowerCase().includes(search.toLowerCase()) ||
    op.operationName.toLowerCase().includes(search.toLowerCase())
  );
  const filteredRoutings = routings.filter(r =>
    r.routingCode.toLowerCase().includes(search.toLowerCase()) ||
    (r.routingName ?? '').toLowerCase().includes(search.toLowerCase()) ||
    (r.productSkuNumber ?? '').toLowerCase().includes(search.toLowerCase())
  );
  const filteredRccp = rccpResources.filter(r =>
    r.resourceCode.toLowerCase().includes(search.toLowerCase()) ||
    r.resourceName.toLowerCase().includes(search.toLowerCase())
  );

  const activeConfig = TAB_CONFIG[activeTab];
  const ActiveIcon = activeConfig.icon;

  const getFilteredData = () => {
    if (activeTab === 'boms') return filteredBoms;
    if (activeTab === 'workcenters') return filteredWorkCenters;
    if (activeTab === 'operations') return filteredOperations;
    if (activeTab === 'rccp') return filteredRccp;
    return filteredRoutings;
  };

  if (loading) {
    return (
      <div className="manufacturing-engineering">
        <div className="glass-card loading-card">
          <div className="loading-spinner">
            <Loader2 size={32} className="spinner-icon" />
            <div className="loading-text">載入資料中...</div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="manufacturing-engineering">
      <div className="page-header">
        <div className="page-title-section">
          <div className="page-title-icon">
            <Layers size={24} />
          </div>
          <div className="page-title-text">
            <h1>工藝設計</h1>
            <p className="page-subtitle">管理工作中心、製程工序與工藝路線配置</p>
          </div>
        </div>
        <button className="btn btn-primary btn-lg" onClick={openCreate}>
          <Plus size={18} />
          <span>新增資料</span>
        </button>
      </div>

      <div className="tab-navigation">
        {(Object.keys(TAB_CONFIG) as TabType[]).map((tabKey) => {
          const config = TAB_CONFIG[tabKey];
          const Icon = config.icon;
          const isActive = activeTab === tabKey;
          return (
            <button
              key={tabKey}
              className={`tab-card ${isActive ? 'active' : ''}`}
              onClick={() => { setActiveTab(tabKey); setSearch(''); }}
            >
              <div className="tab-card-icon">
                <Icon size={20} />
              </div>
              <div className="tab-card-content">
                <span className="tab-card-label">{config.label}</span>
                <span className="tab-card-desc">{config.description}</span>
              </div>
              {isActive && <div className="tab-card-indicator" />}
            </button>
          );
        })}
      </div>

      <div className="toolbar">
        <div className="search-bar">
          <Search size={16} />
          <input
            type="text"
            placeholder={`搜尋${activeConfig.label}...`}
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
          {search && (
            <button className="search-clear" onClick={() => setSearch('')}>
              <X size={14} />
            </button>
          )}
        </div>
        <div className="toolbar-stats">
          <span className="stat-badge">
            <span className="stat-dot" />
            共 {getFilteredData().length} 筆資料
          </span>
        </div>
      </div>

      <div className="data-table-container glass-card">
        {getFilteredData().length === 0 ? (
          <div className="empty-state">
            <div className="empty-state-icon">
              <ActiveIcon size={48} />
            </div>
            <h3>尚無{activeConfig.label}資料</h3>
            <p>點擊上方「新增資料」按鈕建立第一筆{activeConfig.label}</p>
            <button className="btn btn-primary" onClick={openCreate}>
              <Plus size={16} />
              <span>立即建立</span>
            </button>
          </div>
        ) : (
          <div className="table-wrapper">
            {activeTab === 'boms' && (
              <table className="data-table">
                <thead>
                  <tr>
                    <th className="col-code">BOM 代碼</th>
                    <th className="col-product">產品料號</th>
                    <th className="col-product">產品名稱</th>
                    <th className="col-version">版本</th>
                    <th className="col-status">狀態</th>
                    <th className="col-date">生效日期</th>
                    <th className="col-ops">元件數</th>
                    <th className="col-actions">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredBoms.map(b => (
                    <tr key={b.id}>
                      <td className="col-code">
                        <span className="code-badge">{b.bomCode}</span>
                      </td>
                      <td className="col-product">
                        <span className="product-sku">{b.productSkuNumber ?? '—'}</span>
                      </td>
                      <td className="col-product">
                        <span className="product-name">{b.productSkuName ?? '—'}</span>
                      </td>
                      <td className="col-version">
                        <span className="version-badge">v{b.version}</span>
                      </td>
                      <td className="col-status">
                        <span className={`status-pill ${b.status === 'active' ? 'active' : 'inactive'}`}>
                          {b.status === 'active' ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {b.status === 'active' ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td className="col-date">
                        <span className="date-text">{b.effectiveDate ? new Date(b.effectiveDate).toLocaleDateString() : '—'}</span>
                      </td>
                      <td className="col-ops">
                        <span className="ops-count">{b.items?.length ?? 0}</span>
                      </td>
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button className="btn-action btn-action-edit" onClick={() => openEdit(b)} title="編輯">
                            <Edit2 size={14} />
                          </button>
                          <button className="btn-action btn-action-delete" onClick={() => handleDelete(b.id)} title="刪除">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'workcenters' && (
              <table className="data-table">
                <thead>
                  <tr>
                    <th className="col-code">代碼</th>
                    <th className="col-name">名稱</th>
                    <th className="col-capacity">每小時產能</th>
                    <th className="col-efficiency">效率</th>
                    <th className="col-status">狀態</th>
                    <th className="col-actions">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredWorkCenters.map(wc => (
                    <tr key={wc.id}>
                      <td className="col-code">
                        <span className="code-badge">{wc.workCenterCode}</span>
                      </td>
                      <td className="col-name">
                        <div className="name-cell">
                          <Factory size={14} className="name-icon" />
                          {wc.workCenterName}
                        </div>
                      </td>
                      <td className="col-capacity">
                        <span className="metric-value">{wc.capacity.toLocaleString()}</span>
                        <span className="metric-unit">件/小時</span>
                      </td>
                      <td className="col-efficiency">
                        <div className="efficiency-bar">
                          <div
                            className="efficiency-fill"
                            style={{ width: `${Math.min(wc.efficiency, 100)}%` }}
                          />
                          <span className="efficiency-text">{wc.efficiency}%</span>
                        </div>
                      </td>
                      <td className="col-status">
                        <span className={`status-pill ${wc.isActive ? 'active' : 'inactive'}`}>
                          {wc.isActive ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {wc.isActive ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button className="btn-action btn-action-edit" onClick={() => openEdit(wc)} title="編輯">
                            <Edit2 size={14} />
                          </button>
                          <button className="btn-action btn-action-delete" onClick={() => handleDelete(wc.id)} title="刪除">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'operations' && (
              <table className="data-table">
                <thead>
                  <tr>
                    <th className="col-code">工序代碼</th>
                    <th className="col-name">工序名稱</th>
                    <th className="col-time">Setup (分)</th>
                    <th className="col-time">Run (分)</th>
                    <th className="col-cost">時薪</th>
                    <th className="col-type">類型</th>
                    <th className="col-status">狀態</th>
                    <th className="col-actions">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredOperations.map(op => (
                    <tr key={op.id}>
                      <td className="col-code">
                        <span className="code-badge">{op.operationCode}</span>
                      </td>
                      <td className="col-name">
                        <div className="name-cell">
                          <Settings size={14} className="name-icon" />
                          {op.operationName}
                        </div>
                      </td>
                      <td className="col-time">
                        <span className="time-badge">
                          <Clock size={12} />
                          {op.setupTime}
                        </span>
                      </td>
                      <td className="col-time">
                        <span className="time-badge">
                          <Clock size={12} />
                          {op.runTime}
                        </span>
                      </td>
                      <td className="col-cost">
                        <span className="cost-value">
                          <DollarSign size={12} />
                          {op.costPerHour.toLocaleString()}
                        </span>
                      </td>
                      <td className="col-type">
                        <span className={`type-badge ${op.isOutsourced ? 'outsourced' : 'internal'}`}>
                          {op.isOutsourced ? <ExternalLink size={12} /> : <Zap size={12} />}
                          {op.isOutsourced ? '委外' : '自製'}
                        </span>
                      </td>
                      <td className="col-status">
                        <span className={`status-pill ${op.isActive ? 'active' : 'inactive'}`}>
                          {op.isActive ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {op.isActive ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button className="btn-action btn-action-edit" onClick={() => openEdit(op)} title="編輯">
                            <Edit2 size={14} />
                          </button>
                          <button className="btn-action btn-action-delete" onClick={() => handleDelete(op.id)} title="刪除">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'routings' && (
              <table className="data-table routing-table">
                <thead>
                  <tr>
                    <th className="col-code">路線代碼</th>
                    <th className="col-name">路線名稱</th>
                    <th className="col-product">產品料號</th>
                    <th className="col-product">產品名稱</th>
                    <th className="col-version">版本</th>
                    <th className="col-ops">工序數</th>
                    <th className="col-status">狀態</th>
                    <th className="col-actions">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredRoutings.map(r => (
                    <tr key={r.id}>
                      <td className="col-code">
                        <span className="code-badge">{r.routingCode}</span>
                      </td>
                      <td className="col-name">
                        <div className="name-cell">
                          <Route size={14} className="name-icon" />
                          {r.routingName}
                        </div>
                      </td>
                      <td className="col-product">
                        <span className="product-sku">{r.productSkuNumber ?? '—'}</span>
                      </td>
                      <td className="col-product">
                        <span className="product-name">{r.productSkuName ?? '—'}</span>
                      </td>
                      <td className="col-version">
                        <span className="version-badge">v{r.version}</span>
                      </td>
                      <td className="col-ops">
                        <span className="ops-count">{r.operations?.length ?? 0}</span>
                      </td>
                      <td className="col-status">
                        <span className={`status-pill ${r.isActive ? 'active' : 'inactive'}`}>
                          {r.isActive ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {r.isActive ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button className="btn-action btn-action-edit" onClick={() => openEdit(r)} title="編輯">
                            <Edit2 size={14} />
                          </button>
                          <button className="btn-action btn-action-delete" onClick={() => handleDelete(r.id)} title="刪除">
                            <Trash2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'rccp' && (
              <table className="data-table">
                <thead>
                  <tr>
                    <th className="col-code">資源代碼</th>
                    <th className="col-name">資源名稱</th>
                    <th className="col-type">類型</th>
                    <th className="col-wc">所屬工作中心</th>
                    <th className="col-capacity">時格產能</th>
                    <th className="col-status">狀態</th>
                    <th className="col-actions">操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredRccp.map(r => (
                    <tr key={r.id}>
                      <td className="col-code">
                        <span className="code-badge">{r.resourceCode}</span>
                      </td>
                      <td className="col-name">
                        <div className="name-cell">
                          <Zap size={14} className="name-icon" />
                          {r.resourceName}
                        </div>
                      </td>
                      <td className="col-type">{r.resourceType}</td>
                      <td className="col-wc">{r.workCenter?.workCenterName ?? '—'}</td>
                      <td className="col-capacity">
                        <span className="metric-value">{r.capacityPerBucket}</span>
                        <span className="metric-unit">{r.uom}</span>
                      </td>
                      <td className="col-status">
                        <span className={`status-pill ${r.isActive ? 'active' : 'inactive'}`}>
                          {r.isActive ? <CheckCircle2 size={12} /> : <XCircle size={12} />}
                          {r.isActive ? '啟用' : '停用'}
                        </span>
                      </td>
                      <td className="col-actions">
                        <div className="action-buttons">
                          <button className="btn-action btn-action-edit" onClick={() => openEdit(r)} title="編輯">
                            <Edit2 size={14} />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>

      {/* Modal */}
      {showModal && (
        <div className="modal-overlay" onClick={() => !submitting && setShowModal(false)}>
          <div className="modal-backdrop" />
          <div 
            className={`modal-container ${activeTab === 'routings' ? 'modal-wide' : ''}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="modal-header">
              <div className="modal-title">
                <div className="modal-title-icon">
                  <ActiveIcon size={20} />
                </div>
                <div>
                  <h3>{modalMode === 'create' ? '新增' : '編輯'}{activeConfig.label}</h3>
                  <p className="modal-subtitle">
                    {modalMode === 'create' ? '建立新的' : '修改現有的'}{activeConfig.label.toLowerCase()}資料
                  </p>
                </div>
              </div>
              <button className="modal-close" onClick={() => setShowModal(false)} disabled={submitting}>
                <X size={18} />
              </button>
            </div>

            <div className="modal-body custom-scrollbar">
              {activeTab === 'boms' && (
                <div className="form-content">
                  <div className="form-section">
                    <h4 className="section-title">
                      <Package size={16} />
                      基本資訊
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="BOM 代碼" required value={String(formData.bomCode || '')} onChange={v => setFormData(p => ({ ...p, bomCode: v }))} placeholder="BOM-PROD-001" />
                      <div className="form-field">
                        <label className="field-label">產品 (SKU)</label>
                        <select 
                          className="form-select" 
                          value={(formData.productSkuId as string) || ''} 
                          onChange={e => setFormData(p => ({ ...p, productSkuId: e.target.value }))}
                        >
                          <option value="">選擇產品</option>
                          {skus.map(s => <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>)}
                        </select>
                      </div>
                    </div>
                    <div className="form-grid form-grid-3">
                      <FormField label="版本" required value={String(formData.version || '1.0')} onChange={v => setFormData(p => ({ ...p, version: v }))} />
                      <FormField label="生效日期" type="date" value={String(formData.effectiveDate || '').split('T')[0]} onChange={v => setFormData(p => ({ ...p, effectiveDate: v }))} />
                      <div className="form-field">
                        <label className="field-label">狀態</label>
                        <select 
                          className="form-select" 
                          value={(formData.status as string) || 'active'} 
                          onChange={e => setFormData(p => ({ ...p, status: e.target.value }))}
                        >
                          <option value="active">啟用</option>
                          <option value="inactive">停用</option>
                          <option value="draft">草稿</option>
                        </select>
                      </div>
                    </div>
                    <FormField label="備註" value={String(formData.notes || '')} onChange={v => setFormData(p => ({ ...p, notes: v }))} />
                  </div>

                  <div className="form-section">
                    <div className="section-header">
                      <h4 className="section-title">
                        <Layers size={16} />
                        BOM 元件
                      </h4>
                      <button 
                        className="btn btn-ghost btn-sm" 
                        onClick={() => setBomItems([...bomItems, { childSkuId: '', quantity: 1, scrapRate: 0, operationSeq: 10, isPhantom: false }])}
                      >
                        <Plus size={14} />
                        <span>新增元件</span>
                      </button>
                    </div>

                    {bomItems.length === 0 ? (
                      <div className="empty-items">尚未添加任何元件</div>
                    ) : (
                      <div className="bom-items-list">
                        {bomItems.map((it, idx) => (
                          <div key={idx} className="bom-item-row">
                            <div className="item-row-header">
                              <span className="item-index">#{idx + 1}</span>
                              <button className="btn-remove" onClick={() => setBomItems(bomItems.filter((_, i) => i !== idx))}>
                                <Trash2 size={14} />
                              </button>
                            </div>
                            <div className="form-grid form-grid-2">
                              <div className="form-field">
                                <label className="field-label">元件 (SKU)</label>
                                <select 
                                  className="form-select" 
                                  value={it.childSkuId} 
                                  onChange={e => {
                                    const newItems = [...bomItems];
                                    const selectedSku = skus.find(sku => sku.id === e.target.value);
                                    newItems[idx] = {
                                      ...newItems[idx],
                                      childSkuId: e.target.value,
                                      unit: selectedSku?.productionUomCode || '',
                                      unitName: selectedSku?.productionUomName || ''
                                    };
                                    setBomItems(newItems);
                                  }}
                                >
                                  <option value="">選擇元件</option>
                                  {skus.map(s => <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>)}
                                </select>
                              </div>
                              <div className="form-grid form-grid-2">
                                <FormField 
                                  label={`數量 (${it.unit ? `${it.unit}${it.unitName ? ` - ${it.unitName}` : ''}` : '單位未設定'})`}
                                  type="number" 
                                  value={it.quantity} 
                                  onChange={v => {
                                    const newItems = [...bomItems];
                                    newItems[idx] = { ...newItems[idx], quantity: parseFloat(v) || 0 };
                                    setBomItems(newItems);
                                  }} 
                                />
                                <FormField 
                                  label="損耗率 (%)" 
                                  type="number" 
                                  value={it.scrapRate} 
                                  onChange={v => {
                                    const newItems = [...bomItems];
                                    newItems[idx] = { ...newItems[idx], scrapRate: parseFloat(v) || 0 };
                                    setBomItems(newItems);
                                  }} 
                                />
                              </div>
                            </div>
                            <div className="form-grid form-grid-2">
                              <FormField 
                                label="工序順序" 
                                type="number" 
                                value={it.operationSeq} 
                                onChange={v => {
                                  const newItems = [...bomItems];
                                  newItems[idx] = { ...newItems[idx], operationSeq: parseInt(v) || 10 };
                                  setBomItems(newItems);
                                }} 
                              />
                              <div className="form-field" style={{ alignSelf: 'center', paddingTop: '20px' }}>
                                <label className="checkbox-container">
                                  <input 
                                    type="checkbox" 
                                    checked={it.isPhantom} 
                                    onChange={e => {
                                      const newItems = [...bomItems];
                                      newItems[idx] = { ...newItems[idx], isPhantom: e.target.checked };
                                      setBomItems(newItems);
                                    }}
                                  />
                                  <span className="checkbox-label">虛擬件 (Phantom)</span>
                                </label>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              )}

              {activeTab === 'workcenters' && (
                <div className="form-content">
                  <div className="form-section">
                    <h4 className="section-title">
                      <Factory size={16} />
                      基本資訊
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="代碼" required value={String(formData.workCenterCode || '')} onChange={v => setFormData(p => ({ ...p, workCenterCode: v }))} placeholder="WC-001" />
                      <FormField label="名稱" required value={String(formData.workCenterName || '')} onChange={v => setFormData(p => ({ ...p, workCenterName: v }))} placeholder="CNC 加工區" />
                    </div>
                  </div>
                  <div className="form-section">
                    <h4 className="section-title">
                      <Zap size={16} />
                      產能參數
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="每小時產能 (件/小時)" type="number" value={Number(formData.capacity) || 0} onChange={v => setFormData(p => ({ ...p, capacity: parseFloat(v) || 0 }))} />
                      <FormField label="效率 (%)" type="number" value={Number(formData.efficiency) || 100} onChange={v => setFormData(p => ({ ...p, efficiency: parseFloat(v) || 0 }))} />
                    </div>
                  </div>
                  <div className="form-section">
                    <ToggleField
                      label="啟用此工作中心"
                      description="停用後將無法在工藝路線中使用"
                      checked={!!formData.isActive}
                      onChange={v => setFormData(p => ({ ...p, isActive: v }))}
                    />
                  </div>
                </div>
              )}

              {activeTab === 'operations' && (
                <div className="form-content">
                  <div className="form-section">
                    <h4 className="section-title">
                      <Settings size={16} />
                      基本資訊
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="工序代碼" required value={String(formData.operationCode || '')} onChange={v => setFormData(p => ({ ...p, operationCode: v }))} placeholder="OP-001" />
                      <FormField label="工序名稱" required value={String(formData.operationName || '')} onChange={v => setFormData(p => ({ ...p, operationName: v }))} placeholder="車削加工" />
                    </div>
                  </div>
                  <div className="form-section">
                    <h4 className="section-title">
                      <Clock size={16} />
                      時間參數
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="Setup 時間 (分)" type="number" value={Number(formData.setupTime) || 0} onChange={v => setFormData(p => ({ ...p, setupTime: parseFloat(v) || 0 }))} />
                      <FormField label="Run 時間 (分)" type="number" value={Number(formData.runTime) || 0} onChange={v => setFormData(p => ({ ...p, runTime: parseFloat(v) || 0 }))} />
                    </div>
                  </div>
                  <div className="form-section">
                    <h4 className="section-title">
                      <DollarSign size={16} />
                      成本設定
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="時薪" type="number" value={Number(formData.costPerHour) || 0} onChange={v => setFormData(p => ({ ...p, costPerHour: parseFloat(v) || 0 }))} />
                      <ToggleField
                        label="委外加工"
                        description="此工序是否委外處理"
                        checked={!!formData.isOutsourced}
                        onChange={v => setFormData(p => ({ ...p, isOutsourced: v }))}
                      />
                    </div>
                  </div>
                  <div className="form-section">
                    <ToggleField
                      label="啟用此工序"
                      description="停用後將無法在工藝路線中使用"
                      checked={!!formData.isActive}
                      onChange={v => setFormData(p => ({ ...p, isActive: v }))}
                    />
                  </div>
                </div>
              )}

              {activeTab === 'routings' && (
                <div className="form-content">
                  <div className="form-section">
                    <h4 className="section-title">
                      <Route size={16} />
                      基本資訊
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="路線代碼" required value={String(formData.routingCode || '')} onChange={v => setFormData(p => ({ ...p, routingCode: v }))} placeholder="RT-001" />
                      <FormField label="路線名稱" required value={String(formData.routingName || '')} onChange={v => setFormData(p => ({ ...p, routingName: v }))} placeholder="馬達組裝路線" />
                    </div>
                    <div className="form-grid form-grid-2">
                      <div className="form-field">
                        <label className="field-label">產品 SKU <span className="required-mark">*</span></label>
                        <select
                          className="form-select"
                          value={(formData.productSkuId as string) || ''}
                          onChange={e => setFormData(p => ({ ...p, productSkuId: e.target.value }))}
                        >
                          <option value="">請選擇產品</option>
                          {skus.map(s => (
                            <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>
                          ))}
                        </select>
                      </div>
                      <FormField label="版本" value={String(formData.version) || '1.0'} onChange={v => setFormData(p => ({ ...p, version: v }))} />
                    </div>
                  </div>

                  <div className="form-section">
                    <div className="section-header">
                      <h4 className="section-title">
                        <Layers size={16} />
                        工序配置
                      </h4>
                      <button className="btn btn-sm btn-ghost" onClick={() => setRoutingOps(p => [...p, { operationSeq: p.length + 1, operationId: '', workCenterId: '', transferTime: 0, waitTime: 0 } as RoutingOp])}>
                        <Plus size={14} />
                        <span>添加工序</span>
                      </button>
                    </div>

                    {routingOps.length === 0 ? (
                      <div className="empty-subsection">
                        <Layers size={24} />
                        <p>尚未添加工序，請點擊上方按鈕添加</p>
                      </div>
                    ) : (
                      <div className="routing-ops-list">
                        {routingOps.map((op, idx) => (
                          <div key={idx} className="routing-op-card">
                            <div className="routing-op-header">
                              <span className="routing-op-seq">
                                <span className="seq-number">{idx + 1}</span>
                                <ArrowRight size={14} className="seq-arrow" />
                                <span className="seq-label">工序 {idx + 1}</span>
                              </span>
                              <div className="routing-op-actions">
                                <button
                                  className="btn-icon-sm routing-op-move"
                                  onClick={() => setRoutingOps(current => {
                                    const next = [...current];
                                    [next[idx - 1], next[idx]] = [next[idx], next[idx - 1]];
                                    return next;
                                  })}
                                  disabled={idx === 0}
                                  aria-label={`工序 ${idx + 1} 上移`}
                                  title="上移工序"
                                >
                                  <ChevronUp size={14} />
                                </button>
                                <button
                                  className="btn-icon-sm routing-op-move"
                                  onClick={() => setRoutingOps(current => {
                                    const next = [...current];
                                    [next[idx], next[idx + 1]] = [next[idx + 1], next[idx]];
                                    return next;
                                  })}
                                  disabled={idx === routingOps.length - 1}
                                  aria-label={`工序 ${idx + 1} 下移`}
                                  title="下移工序"
                                >
                                  <ChevronDown size={14} />
                                </button>
                                <button
                                  className="btn-icon-sm btn-icon-danger"
                                  onClick={() => setRoutingOps(p => p.filter((_, i) => i !== idx))}
                                  title="刪除此工序"
                                  aria-label={`刪除工序 ${idx + 1}`}
                                >
                                  <Trash2 size={12} />
                                </button>
                              </div>
                            </div>
                            <div className="routing-op-fields">
                              <div className="form-field">
                                <label className="field-label">工序</label>
                                <select
                                  className="form-select form-select-sm"
                                  value={op.operationId}
                                  onChange={e => {
                                    const newOps = [...routingOps];
                                    newOps[idx] = { ...newOps[idx], operationId: e.target.value };
                                    setRoutingOps(newOps);
                                  }}
                                >
                                  <option value="">選擇工序</option>
                                  {operations.map(o => (
                                    <option key={o.id} value={o.id}>{o.operationCode} - {o.operationName}</option>
                                  ))}
                                </select>
                              </div>
                              <div className="form-field">
                                <label className="field-label">工作中心</label>
                                <select
                                  className="form-select form-select-sm"
                                  value={op.workCenterId}
                                  onChange={e => {
                                    const newOps = [...routingOps];
                                    newOps[idx] = { ...newOps[idx], workCenterId: e.target.value };
                                    setRoutingOps(newOps);
                                  }}
                                >
                                  <option value="">選擇工作中心</option>
                                  {workCenters.map(w => (
                                    <option key={w.id} value={w.id}>{w.workCenterCode} - {w.workCenterName}</option>
                                  ))}
                                </select>
                              </div>
                              <div className="form-field form-field-sm">
                                <label className="field-label">轉移 (分)</label>
                                <input
                                  className="form-input form-input-sm"
                                  type="number"
                                  value={op.transferTime}
                                  onChange={e => {
                                    const newOps = [...routingOps];
                                    newOps[idx] = { ...newOps[idx], transferTime: parseFloat(e.target.value) || 0 };
                                    setRoutingOps(newOps);
                                  }}
                                />
                              </div>
                              <div className="form-field form-field-sm">
                                <label className="field-label">等待 (分)</label>
                                <input
                                  className="form-input form-input-sm"
                                  type="number"
                                  value={op.waitTime}
                                  onChange={e => {
                                    const newOps = [...routingOps];
                                    newOps[idx] = { ...newOps[idx], waitTime: parseFloat(e.target.value) || 0 };
                                    setRoutingOps(newOps);
                                  }}
                                />
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="form-section">
                    <ToggleField
                      label="啟用此工藝路線"
                      description="停用後將無法用於生產訂單"
                      checked={!!formData.isActive}
                      onChange={v => setFormData(p => ({ ...p, isActive: v }))}
                    />
                  </div>
                </div>
              )}

              {activeTab === 'rccp' && (
                <div className="form-content">
                  <div className="form-section">
                    <h4 className="section-title">
                      <Zap size={16} />
                      資源定義
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="資源代碼" required value={String(formData.resourceCode || '')} onChange={v => setFormData(p => ({ ...p, resourceCode: v }))} placeholder="RCCP-MACHINE-01" />
                      <FormField label="資源名稱" required value={String(formData.resourceName || '')} onChange={v => setFormData(p => ({ ...p, resourceName: v }))} placeholder="五軸加工中心機組" />
                    </div>
                    <div className="form-grid form-grid-2">
                      <div className="form-field">
                        <label className="field-label">所屬工作中心</label>
                        <select
                          className="form-select"
                          value={(formData.workCenterId as string) || ''}
                          onChange={e => setFormData(p => ({ ...p, workCenterId: e.target.value }))}
                        >
                          <option value="">選擇工作中心 (可選)</option>
                          {workCenters.map(wc => (
                            <option key={wc.id} value={wc.id}>{wc.workCenterName}</option>
                          ))}
                        </select>
                      </div>
                      <div className="form-field">
                        <label className="field-label">資源類型</label>
                        <select
                          className="form-select"
                          value={(formData.resourceType as string) || 'Machine'}
                          onChange={e => setFormData(p => ({ ...p, resourceType: e.target.value }))}
                        >
                          <option value="Machine">設備 (Machine)</option>
                          <option value="Labor">人力 (LaborPool)</option>
                          <option value="Tooling">模具/治具 (Tooling)</option>
                        </select>
                      </div>
                    </div>
                  </div>
                  <div className="form-section">
                    <h4 className="section-title">
                      <Clock size={16} />
                      產能參數
                    </h4>
                    <div className="form-grid form-grid-2">
                      <FormField label="時格時數 (每週/月可供)" type="number" value={Number(formData.capacityPerBucket) || 0} onChange={v => setFormData(p => ({ ...p, capacityPerBucket: parseFloat(v) || 0 }))} />
                      <FormField label="單位" value={String(formData.uom || 'Hrs')} onChange={v => setFormData(p => ({ ...p, uom: v }))} />
                    </div>
                  </div>
                  <div className="form-section">
                    <ToggleField
                      label="啟用此資源"
                      description="停用後將不會計算產能負荷"
                      checked={!!formData.isActive}
                      onChange={v => setFormData(p => ({ ...p, isActive: v }))}
                    />
                  </div>
                </div>
              )}
            </div>

            <div className="modal-footer">
              <button className="btn btn-ghost" onClick={() => setShowModal(false)} disabled={submitting}>
                取消
              </button>
              <button className="btn btn-primary btn-lg" onClick={handleSubmit} disabled={submitting}>
                {submitting ? (
                  <>
                    <Loader2 size={16} className="btn-spinner" />
                    <span>儲存中...</span>
                  </>
                ) : (
                  <>
                    <CheckCircle2 size={16} />
                    <span>{modalMode === 'create' ? '建立' : '更新'}</span>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

function FormField({ label, required, value, onChange, type = 'text', placeholder }: { label: string; required?: boolean; value: string | number; onChange: (v: string) => void; type?: string; placeholder?: string }) {
  return (
    <div className="form-field">
      <label className="field-label">
        {label}
        {required && <span className="required-mark">*</span>}
      </label>
      <input
        className="form-input"
        type={type}
        value={value}
        placeholder={placeholder}
        onChange={e => onChange(e.target.value)}
      />
    </div>
  );
}

function ToggleField({ label, description, checked, onChange }: { label: string; description?: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="toggle-field">
      <div className="toggle-info">
        <span className="toggle-label">{label}</span>
        {description && <span className="toggle-desc">{description}</span>}
      </div>
      <button
        className={`toggle-switch ${checked ? 'on' : 'off'}`}
        onClick={() => onChange(!checked)}
        type="button"
      >
        <span className="toggle-knob" />
      </button>
    </div>
  );
}
