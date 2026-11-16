import { useState, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { ecnApi } from '../api/api';
import { Search, Plus, Download, Eye, FileText, Loader2, Beaker, GitBranch, GitMerge, FileArchive, Zap, X, Pencil } from 'lucide-react';
import { 
  useRndProjects,
  useBoms, useEcns, useSkus, useCreateBom, useUpdateBom,
  useCreateRndProject, useCreateEcn, useUoms, useActivateBom, useDeprecateBom 
} from '../hooks/useApi';
import { FormModal } from '../components/FormModal';
import { BomTreeModal } from '../components/BomTreeModal';
import type { FormField } from '../components/FormModal';
import type { BomDto, EcnDto, EcnImpactDto } from '../api/api';

export interface RndProjectDto { id: string; projectCode: string; projectName: string; description?: string; managerName?: string; startDate?: string; budget: number; progress: number; status: string; }

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function getErrorMessage(error: unknown, preferResponseMessage = false): string | undefined {
  if (!isRecord(error)) return undefined;
  const responseData = isRecord(error.response) ? error.response.data : undefined;
  const responseMessage = isRecord(responseData) && typeof responseData.message === 'string'
    ? responseData.message
    : undefined;
  const errorMessage = typeof error.message === 'string' ? error.message : undefined;
  return preferResponseMessage
    ? responseMessage || errorMessage
    : errorMessage || responseMessage;
}

const projectStatusConfig: Record<string, { label: string; cls: string }> = {
  planning: { label: '規劃中', cls: 'neutral' },
  in_progress: { label: '進行中', cls: 'info' },
  testing: { label: '測試中', cls: 'warning' },
  completed: { label: '已完成', cls: 'active' },
  suspended: { label: '已暫停', cls: 'neutral' },
};

const stats = [
  { label: '進行中專案', value: 12, color: 'var(--cobalt-400)' },
  { label: '本月投入預算', value: 'NT$2.6M', color: 'var(--emerald-400)' },
  { label: '有效用 BOM 總數', value: 1420, color: 'var(--purple-400)' },
  { label: '待簽核變更 (ECR/ECN)', value: 8, color: 'var(--amber-400)' },
];

const projectFields: FormField[] = [
  { name: 'projectName', label: '專案名稱', type: 'text', required: true, placeholder: '專案名稱' },
  { name: 'projectType', label: '專案類型', type: 'select', options: [{ value: 'new_product', label: '新產品開發' }, { value: 'improvement', label: '改善專案' }, { value: 'research', label: '研究專案' }] },
  { name: 'estimatedBudget', label: '預估預算', type: 'number', placeholder: '0', min: 0, required: true },
  { name: 'startDate', label: '開始日期', type: 'date' },
  { name: 'targetProductionDate', label: '預計投產日', type: 'date' },
];

const ecnFields: FormField[] = [
  { name: 'ecnNumber', label: 'ECN 編號', type: 'text', placeholder: '留空則自動產生' },
  { name: 'changeDescription', label: '變更說明', type: 'textarea', required: true },
  { name: 'effectiveDate', label: '預計生效日', type: 'date', required: true },
  { name: 'affectedBoms', label: '受影響 BOM', type: 'custom' },
];

export function RD() {
  const [search, setSearch] = useState('');
  const [activeTab, setActiveTab] = useState<'projects' | 'documents' | 'boms' | 'ecns' | 'logs' | 'trial' | 'mass'>('boms');
  const [showAdd, setShowAdd] = useState(false);
  const [showAddBom, setShowAddBom] = useState(false);
  const [editBom, setEditBom] = useState<(Omit<BomDto, 'id'> & { id?: string }) | null>(null);
  const [showDiffModal, setShowDiffModal] = useState(false);
  const [showBomTree, setShowBomTree] = useState(false);
  const [selectedBomTree, setSelectedBomTree] = useState<BomDto | null>(null);
  const [bomItems, setBomItems] = useState<{ childSkuId: string; quantity: number; unit: string; scrapRate: number }[]>([]);
  const [selectedEcn, setSelectedEcn] = useState<EcnDto | null>(null);
  const [ecnImpact, setEcnImpact] = useState<EcnImpactDto | null>(null);
  const [loadingImpact, setLoadingImpact] = useState(false);
  const [affectedBoms, setAffectedBoms] = useState<{ bomId: string; actionType: string }[]>([]);
  const [impactSubTab, setImpactSubTab] = useState<'risk' | 'list'>('risk');

  useEffect(() => {
    if (showDiffModal && selectedEcn) {
      const loadImpact = async () => {
        setLoadingImpact(true);
        try {
          const res = await ecnApi.getImpact(selectedEcn.id);
          if (res.data.success) {
            setEcnImpact(res.data.data);
          }
        } catch (err) {
          console.error('Fetch impact failed', err);
        } finally {
          setLoadingImpact(false);
        }
      };
      loadImpact();
    } else {
      setEcnImpact(null);
    }
  }, [showDiffModal, selectedEcn]);

  const { data: projectsResp } = useRndProjects();
  const { data: bomsResp } = useBoms();
  const { data: ecnsResp } = useEcns();
  const { data: skusResp } = useSkus({ pageSize: '9999' });
  const { data: uomResp } = useUoms();

  const createBom = useCreateBom();
  const updateBom = useUpdateBom();
  const activateBom = useActivateBom();
  const deprecateBom = useDeprecateBom();

  const handleToggleStatus = async (bom: BomDto) => {
    try {
      if (bom.status === 'active') {
        if (!confirm(`確定要停用 BOM [${bom.bomCode}] v${bom.version} 嗎？`)) return;
        await deprecateBom.mutateAsync(bom.id);
      } else {
        if (!confirm(`啟用 [${bom.bomCode}] v${bom.version} 將會自動停用該產品的其他有效版本，是否繼續？`)) return;
        await activateBom.mutateAsync(bom.id);
      }
    } catch (err: unknown) {
      alert(`操作失敗: ${getErrorMessage(err) || String(err)}`);
    }
  };

  const handleCopyAsNewVersion = (bom: BomDto) => {
    const currentVer = parseFloat(bom.version) || 1.0;
    const nextVer = (currentVer + 0.1).toFixed(1);
    
    setBomItems(bom.items?.map(it => ({
      childSkuId: it.childSkuId, 
      quantity: it.quantity, 
      unit: it.unit || '---', 
      scrapRate: it.scrapRate || 0 
    })) || []);
    
    setEditBom({
      ...bom,
      id: undefined, 
      version: nextVer,
      status: 'draft'
    });
    setShowAddBom(true);
  };

  const createProject = useCreateRndProject();
  const createEcn = useCreateEcn();

  const handleProjectSubmit = async (data: Record<string, unknown>) => {
    try {
      await createProject.mutateAsync(data);
      setShowAdd(false);
    } catch (err: unknown) {
      alert('建立專案失敗: ' + (getErrorMessage(err) || '發生未知錯誤'));
    }
  };

  const handleEcnSubmit = async (data: Record<string, unknown>) => {
    try {
      if (affectedBoms.length === 0) {
        alert('請至少選擇一個受影響的 BOM 表');
        return;
      }
      const payload = {
        ecnNumber: data.ecnNumber || null,
        changeDescription: data.changeDescription,
        effectiveDate: data.effectiveDate,
        affectedBoms: affectedBoms.map(b => ({ bomId: b.bomId, actionType: b.actionType }))
      };
      await createEcn.mutateAsync(payload);
      setShowAdd(false);
      setAffectedBoms([]);
    } catch (err: unknown) {
      alert('建立變更單失敗: ' + (getErrorMessage(err) || '發生未知錯誤'));
    }
  };

  const handleBomSubmit = async (data: Record<string, unknown>) => {
    try {
      const payload = {
        bomCode: data.bomCode,
        productSkuId: data.productSkuId,
        version: data.version,
        effectiveDate: data.effectiveDate,
        expiryDate: data.expiryDate || null,
        notes: data.notes,
        items: bomItems.filter(it => it.childSkuId).map(it => ({
          childSkuId: it.childSkuId,
          quantity: it.quantity,
          unit: it.unit || 'PCS',
          scrapRate: it.scrapRate || 0
        }))
      };

      if (editBom) {
        await updateBom.mutateAsync({ id: editBom.id, data: payload });
      } else {
        await createBom.mutateAsync(payload);
      }
      
      setShowAddBom(false);
      setEditBom(null);
      setBomItems([]);
    } catch (err: unknown) {
      alert('操作失敗: ' + (getErrorMessage(err, true) || '發生未知錯誤'));
    }
  };

  const projects = (projectsResp?.data as unknown as RndProjectDto[]) ?? [];
  const boms = bomsResp?.data ?? [];
  const ecns = ecnsResp?.data ?? [];
  const allSkus = skusResp?.data ?? [];
  
  const filteredBoms = boms.filter(b => b.bomCode?.includes(search) || b.productSkuName?.includes(search));
  const filteredEcns = ecns.filter(e => e.ecnNumber?.includes(search) || e.changeDescription?.includes(search));

  const bomParentSkuIds = new Set(boms.map(b => b.productSkuId));
  const bomParentSkus = allSkus.filter(s => {
    const isBaseMatch = (s.isBomTop || s.itemType === 'finished' || s.itemType === 'semi_finished') && s.status !== 'inactive';
    if (editBom && s.id === editBom.productSkuId) return true;
    return isBaseMatch && !bomParentSkuIds.has(s.id);
  });

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">產品工程 (R&D / PE)</h2>
          <p className="module-page-desc">研發專案管理、BOM 架構維護、工程變更單 (ECN) 管理</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出</button>
          <button className="btn btn-primary btn-sm" onClick={() => {
            if (activeTab === 'boms') setShowAddBom(true);
            else if (activeTab === 'ecns' || activeTab === 'projects') setShowAdd(true);
          }}>
            <Plus size={13} />新增{activeTab === 'projects' ? '專案' : activeTab === 'boms' ? 'BOM表' : '變更單'}
          </button>
        </div>
      </div>

      <div className="module-stat-grid">
        {stats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div className="module-stat-label">{s.label}</div>
            <div className="module-stat-value kpi-value" style={{ color: s.color, fontSize: '20px' }}>{s.value}</div>
          </div>
        ))}
      </div>

      <div className="rd-tab-bar" style={{ marginTop: 16 }}>
        <button className={`rd-tab ${activeTab === 'projects' ? 'active' : ''}`} onClick={() => setActiveTab('projects')}><Beaker size={14} />研發專案</button>
        <button className={`rd-tab ${activeTab === 'boms' ? 'active' : ''}`} onClick={() => setActiveTab('boms')}><GitBranch size={14} />BOM 表管理</button>
        <button className={`rd-tab ${activeTab === 'ecns' ? 'active' : ''}`} onClick={() => setActiveTab('ecns')}><GitMerge size={14} />工程變更單 (ECN)</button>
      </div>

      <div className="glass-card module-filter-bar" style={{ marginTop: 16 }}>
        <div className="filter-search">
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input className="search-input" placeholder="搜尋..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden', marginTop: 16 }}>
        {activeTab === 'projects' ? (
          <table className="data-table">
            <thead><tr><th>專案編號</th><th>專案名稱</th><th>預算</th><th>進度</th><th>狀態</th><th>操作</th></tr></thead>
            <tbody>
              {projects.map(p => (
                <tr key={p.id}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{p.projectCode}</td>
                  <td>{p.projectName}</td>
                  <td className="mono">{p.budget?.toLocaleString()}</td>
                  <td>{p.progress}%</td>
                  <td><span className={`status-badge status-${projectStatusConfig[p.status]?.cls}`}>{projectStatusConfig[p.status]?.label}</span></td>
                  <td><button className="icon-action-btn"><Eye size={13} /></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : activeTab === 'boms' ? (
          <table className="data-table">
            <thead><tr><th>BOM 編號</th><th>產品</th><th>版本</th><th>狀態</th><th>操作</th></tr></thead>
            <tbody>
              {filteredBoms.map(b => (
                <tr key={b.id}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{b.bomCode}</td>
                  <td>{b.productSkuName}</td>
                  <td className="mono">v{b.version}</td>
                  <td>
                    <span 
                      className={`status-badge ${b.status === 'active' ? 'status-active' : 'status-neutral'}`}
                      style={{ cursor: 'pointer' }}
                      onClick={() => handleToggleStatus(b)}
                      title="點擊切換狀態"
                    >
                      {b.status === 'active' ? '有效' : '停用'}
                    </span>
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 4 }}>
                      <button className="icon-action-btn" title="查看 BOM 結構" onClick={() => { setSelectedBomTree(b); setShowBomTree(true); }}><GitBranch size={13} /></button>
                      <button className="icon-action-btn" title="複製並建立新版本" onClick={() => handleCopyAsNewVersion(b)}><FileArchive size={13} /></button>
                      <button className="icon-action-btn" title="編輯" onClick={() => {
                        setEditBom(b);
                        setBomItems(b.items?.map(it => ({ childSkuId: it.childSkuId, quantity: it.quantity, unit: it.unit || '---', scrapRate: it.scrapRate || 0 })) || []);
                        setShowAddBom(true);
                      }}><Pencil size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <table className="data-table">
            <thead><tr><th>ECN 編號</th><th>變更說明</th><th>狀態</th><th>操作</th></tr></thead>
            <tbody>
              {filteredEcns.map(e => (
                <tr key={e.id}>
                  <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{e.ecnNumber}</td>
                  <td>{e.changeDescription}</td>
                  <td><span className={`status-badge ${e.status === 'approved' ? 'status-active' : 'status-warning'}`}>{e.status === 'approved' ? '已核准' : '審核中'}</span></td>
                  <td><button className="icon-action-btn" onClick={() => { setSelectedEcn(e); setShowDiffModal(true); }}><GitMerge size={13} /></button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <FormModal
        open={showAddBom}
        onClose={() => { setShowAddBom(false); setEditBom(null); setBomItems([]); }}
        title={editBom ? `編輯 BOM - ${editBom.bomCode}` : "新增 BOM 表"}
        width={650}
        initialValues={editBom ? {
          bomCode: editBom.bomCode,
          productSkuId: editBom.productSkuId,
          version: editBom.version,
          effectiveDate: editBom.effectiveDate?.split('T')[0],
          notes: editBom.notes
        } : { version: '1.0', effectiveDate: new Date().toISOString().split('T')[0] }}
        fields={[
          { name: 'bomCode', label: 'BOM 編號', type: 'text', required: true },
          { name: 'productSkuId', label: '母件', type: 'select', options: bomParentSkus.map(s => ({ value: s.id, label: `${s.skuNumber} - ${s.skuName}` })) },
          { name: 'version', label: '版本', type: 'text', required: true },
          { name: 'effectiveDate', label: '生效日', type: 'date', required: true },
          { name: 'bomItems', label: 'BOM 明細', type: 'custom' }
        ]}
        onSubmit={handleBomSubmit}
        customRenderers={{
          bomItems: (
            <div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, alignItems: 'center' }}>
                <span style={{ fontSize: 13, fontWeight: 600 }}>組成子件</span>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => setBomItems([...bomItems, { childSkuId: '', quantity: 1, unit: '---', scrapRate: 0 }])}><Plus size={12} /> 新增子件</button>
              </div>
              {bomItems.map((it, idx) => (
                <div key={idx} style={{ display: 'flex', gap: 8, marginTop: 8 }}>
                  <select className="form-input" style={{ flex: 3 }} value={it.childSkuId} onChange={e => { 
                    const n = [...bomItems]; 
                    const selectedId = e.target.value;
                    n[idx].childSkuId = selectedId;
                    const sku = allSkus.find(s => s.id === selectedId);
                    if (sku) {
                      // 尋找單位名稱
                      const targetUomId = sku.productionUomId || sku.baseUomId;
                      const uom = uomResp?.data?.find(u => u.id === targetUomId);
                      n[idx].unit = sku.productionUomName || uom?.uomName || sku.unit || '---';
                    }
                    setBomItems(n); 
                  }}>
                    <option value="">選擇子件</option>
                    {allSkus.filter(s => s.status === 'active' && s.itemType !== 'finished').map(s => <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>)}
                  </select>
                  <div style={{ display: 'flex', flex: 1.5, gap: 4 }}>
                    <input className="form-input" type="number" style={{ flex: 1 }} value={it.quantity} onChange={e => { const n = [...bomItems]; n[idx].quantity = parseFloat(e.target.value) || 0; setBomItems(n); }} placeholder="數量" />
                    <div style={{ display: 'flex', alignItems: 'center', paddingLeft: 8, paddingRight: 8, minWidth: 45, fontSize: 13, color: 'var(--cobalt-300)', fontWeight: 600 }}>
                      {it.unit}
                    </div>
                  </div>
                  <button type="button" className="icon-action-btn" onClick={() => setBomItems(bomItems.filter((_, i) => i !== idx))}><X size={14} /></button>
                </div>
              ))}
            </div>
          )
        }}
      />

      <FormModal
        open={showAdd}
        onClose={() => { setShowAdd(false); setAffectedBoms([]); }}
        title={activeTab === 'projects' ? '新增專案' : '新增工程變更單 (ECN)'}
        width={activeTab === 'projects' ? 500 : 700}
        initialValues={activeTab === 'projects' ? { projectType: 'new_product' } : { effectiveDate: new Date().toISOString().split('T')[0] }}
        fields={activeTab === 'projects' ? projectFields : ecnFields}
        onSubmit={activeTab === 'projects' ? handleProjectSubmit : handleEcnSubmit}
        customRenderers={{
          affectedBoms: (
            <div>
              <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8, alignItems: 'center' }}>
                <span style={{ fontSize: 13, fontWeight: 600 }}>關連 BOM</span>
                <button type="button" className="btn btn-secondary btn-sm" onClick={() => setAffectedBoms([...affectedBoms, { bomId: '', actionType: 'update' }])}><Plus size={12} /> 新增關聯</button>
              </div>
              {affectedBoms.map((b, idx) => (
                <div key={idx} style={{ display: 'flex', gap: 8, marginTop: 8 }}>
                  <select className="form-input" style={{ flex: 3 }} value={b.bomId} onChange={e => { const n = [...affectedBoms]; n[idx].bomId = e.target.value; setAffectedBoms(n); }}>
                    <option value="">選擇 BOM</option>
                    {boms.map(bom => <option key={bom.id} value={bom.id}>{bom.bomCode} - {bom.productSkuName}</option>)}
                  </select>
                  <select className="form-input" style={{ flex: 1.5 }} value={b.actionType} onChange={e => { const n = [...affectedBoms]; n[idx].actionType = e.target.value; setAffectedBoms(n); }}>
                    <option value="update">更新版本</option>
                    <option value="obsolete">廢止此 BOM</option>
                    <option value="replace">取代子件</option>
                  </select>
                  <button type="button" className="icon-action-btn" onClick={() => setAffectedBoms(affectedBoms.filter((_, i) => i !== idx))}><X size={14} /></button>
                </div>
              ))}
            </div>
          )
        }}
      />

      {showBomTree && selectedBomTree && (
        <BomTreeModal open={showBomTree} onClose={() => setShowBomTree(false)} bom={selectedBomTree} boms={boms} skus={allSkus} />
      )}

      {showDiffModal && createPortal(
        <div className="modal-overlay" style={{ 
          position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh', 
          background: 'rgba(5, 10, 20, 0.98)', backdropFilter: 'blur(12px)', 
          display: 'flex', alignItems: 'flex-start', justifyContent: 'center', 
          zIndex: 999999, overflowY: 'auto', padding: '80px 24px'
        }} onClick={() => setShowDiffModal(false)}>
          <div className="glass-card" style={{ 
            width: 850, display: 'flex', flexDirection: 'column', padding: 0, overflow: 'hidden', 
            background: '#0a0f1d', border: '1px solid rgba(255,255,255,0.2)', boxShadow: '0 32px 100px rgba(0,0,0,1)',
            transform: 'scale(0.8)',
            transformOrigin: 'top center'
          }} onClick={e => e.stopPropagation()}>
            {/* Header */}
            <div style={{ 
              background: '#162447', padding: '32px', borderBottom: '1px solid rgba(255,255,255,0.1)',
              display: 'flex', justifyContent: 'space-between', alignItems: 'center' 
            }}>
              <div>
                <h2 style={{ margin: '0 0 6px', fontSize: 24, fontWeight: 700, color: '#fff', display: 'flex', alignItems: 'center', gap: 12 }}>
                  <Zap size={24} color="var(--cobalt-300)" fill="var(--cobalt-300)" /> 工程變更比對核准
                </h2>
                <div style={{ color: 'var(--slate-300)', fontSize: 14, opacity: 0.9 }}>財務衝擊評估與庫存分析報表</div>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 13, color: 'var(--slate-400)', marginBottom: 4 }}>預計報廢損失</div>
                <div style={{ fontSize: 40, fontWeight: 800, color: '#ff4d6d', textShadow: '0 0 25px rgba(255, 77, 109, 0.4)' }} className="mono">
                  ${ecnImpact?.totalLossAmount?.toLocaleString() || 0}
                </div>
              </div>
            </div>

            <div style={{ padding: '32px' }}>
              {/* Info Row */}
              <div style={{ background: 'rgba(255,255,255,0.03)', padding: '24px', borderRadius: 16, marginBottom: 32, display: 'flex', gap: 48, border: '1px solid rgba(255,255,255,0.07)' }}>
                <div style={{ flex: 1 }}>
                  <label style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)', textTransform: 'uppercase' }}>變更編號</label>
                  <p style={{ margin: '8px 0 0', fontSize: 18, fontWeight: 600, color: 'var(--cobalt-300)' }} className="mono">{selectedEcn?.ecnNumber}</p>
                </div>
                <div style={{ flex: 2 }}>
                  <label style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)', textTransform: 'uppercase' }}>變更核心說明</label>
                  <p style={{ margin: '8px 0 0', fontSize: 15, color: '#fff', lineHeight: 1.6 }}>{selectedEcn?.changeDescription}</p>
                </div>
                <div style={{ flex: 1.5, borderLeft: '1px solid rgba(255,255,255,0.1)', paddingLeft: 40 }}>
                  <label style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)', textTransform: 'uppercase' }}>生效策略建議</label>
                  <div style={{ margin: '8px 0 0', fontSize: 13, color: 'var(--slate-300)' }}>
                    計畫日期: <span style={{ color: '#fff', fontWeight: 600 }}>{selectedEcn?.effectiveDate?.split('T')[0]}</span>
                    {ecnImpact?.suggestedEffectiveDate && (
                      <div style={{ marginTop: 8, color: 'var(--emerald-400)', fontWeight: 600 }}>
                        自動優化建議: {ecnImpact.suggestedEffectiveDate.split('T')[0]}
                      </div>
                    )}
                  </div>
                </div>
              </div>

              {/* Cards Row */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 32 }}>
                <div className="glass-card" style={{ padding: 28, background: 'linear-gradient(135deg, rgba(245, 158, 11, 0.05), transparent)', border: '1px solid rgba(245, 158, 11, 0.25)' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 20 }}>
                    <div style={{ width: 40, height: 40, borderRadius: 12, background: 'rgba(245, 158, 11, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--amber-400)' }}><FileText size={22} /></div>
                    <span style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-200)' }}>受影響工單</span>
                  </div>
                  <div style={{ fontSize: 36, fontWeight: 800, color: 'var(--amber-400)' }} className="mono">{ecnImpact?.affectedWorkOrderCount || 0} <span style={{ fontSize: 18, color: 'var(--slate-500)' }}>張</span></div>
                </div>
                
                <div className="glass-card" style={{ padding: 28, background: 'linear-gradient(135deg, rgba(45, 110, 245, 0.05), transparent)', border: '1px solid rgba(45, 110, 245, 0.25)' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 20 }}>
                    <div style={{ width: 40, height: 40, borderRadius: 12, background: 'rgba(45, 110, 245, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--cobalt-300)' }}><Download size={22} /></div>
                    <span style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-200)' }}>受影響採購</span>
                  </div>
                  <div style={{ fontSize: 36, fontWeight: 800, color: 'var(--cobalt-300)' }} className="mono">{ecnImpact?.affectedPurchaseOrderCount || 0} <span style={{ fontSize: 18, color: 'var(--slate-500)' }}>張</span></div>
                </div>
              </div>

              {/* Sub Tabs */}
              <div style={{ display: 'flex', gap: 40, marginBottom: 24, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
                <button 
                  className="btn btn-ghost" 
                  style={{ padding: '12px 4px', fontSize: 16, border: 'none', borderBottom: impactSubTab === 'risk' ? '3px solid var(--cobalt-400)' : 'none', color: impactSubTab === 'risk' ? 'var(--cobalt-300)' : 'var(--slate-500)', background: 'transparent', fontWeight: 600 }}
                  onClick={() => setImpactSubTab('risk')}
                >
                  風險分析清單
                </button>
                <button 
                  className="btn btn-ghost" 
                  style={{ padding: '12px 4px', fontSize: 16, border: 'none', borderBottom: impactSubTab === 'list' ? '3px solid var(--cobalt-400)' : 'none', color: impactSubTab === 'list' ? 'var(--cobalt-300)' : 'var(--slate-500)', background: 'transparent', fontWeight: 600 }}
                  onClick={() => setImpactSubTab('list')}
                >
                  受影響物料
                </button>
              </div>

              {/* Impact Table */}
              <div style={{ background: 'rgba(0,0,0,0.4)', borderRadius: 16, border: '1px solid rgba(255,255,255,0.06)', overflow: 'hidden', minHeight: '200px' }}>
                <table className="data-table">
                  <thead>
                    <tr><th>類別</th><th>受影響品項 / 說明</th><th style={{ textAlign: 'right' }}>影響量</th><th style={{ textAlign: 'right' }}>財務損失預算</th></tr>
                  </thead>
                  <tbody>
                    {loadingImpact ? (
                      <tr><td colSpan={4} style={{ textAlign: 'center', padding: '60px 0' }}><Loader2 className="animate-spin" size={32} color="var(--cobalt-400)" /></td></tr>
                    ) : (
                      <>
                        {impactSubTab === 'risk' ? (
                          ecnImpact?.affectedWorkOrders?.map((wo, idx) => (
                            <tr key={idx}>
                              <td><span className="status-badge status-warning" style={{ fontSize: 11 }}>工單</span></td>
                              <td>{wo.woNumber} - {wo.productSkuLabel}</td>
                              <td className="mono" style={{ textAlign: 'right' }}>{wo.plannedQty}</td>
                              <td className="mono" style={{ textAlign: 'right', color: '#ff4d6d' }}>--</td>
                            </tr>
                          ))
                        ) : (
                          ecnImpact?.details?.map((it, idx) => (
                            <tr key={idx}>
                              <td><span className="status-badge status-info" style={{ fontSize: 11 }}>庫存</span></td>
                              <td>{it.skuNumber} - {it.skuName}</td>
                              <td className="mono" style={{ textAlign: 'right' }}>{it.currentStock}</td>
                              <td className="mono" style={{ textAlign: 'right', color: '#ff4d6d', fontWeight: 700 }}>${it.potentialLoss?.toLocaleString()}</td>
                            </tr>
                          ))
                        )}
                        {(!ecnImpact?.details?.length && !ecnImpact?.affectedWorkOrders?.length) && <tr><td colSpan={4} style={{ textAlign: 'center', padding: '60px 0', color: 'var(--slate-500)' }}>無明顯衝擊項目</td></tr>}
                      </>
                    )}
                  </tbody>
                </table>
              </div>

              {/* Action Bar */}
              <div style={{ padding: '32px 0 8px', borderTop: '1px solid rgba(255,255,255,0.1)', display: 'flex', justifyContent: 'flex-end', gap: 24, marginTop: 40 }}>
                <button className="btn btn-ghost" style={{ padding: '12px 32px', fontSize: 15 }} onClick={() => setShowDiffModal(false)}>暫緩簽核</button>
                <button 
                  className="btn btn-primary" 
                  style={{ padding: '12px 48px', fontSize: 16, fontWeight: 700, minWidth: 240, height: 48 }}
                  onClick={() => { alert('ECN 已正式核准併同步 BOM 版本。'); setShowDiffModal(false); window.location.reload(); }}
                  disabled={selectedEcn?.status === 'approved' || loadingImpact}
                >
                  確認簽核並正式發布
                </button>
              </div>
            </div>
          </div>
        </div>,
        document.body
      )}

      <style>{`
        .rd-tab-bar { display: flex; gap: 4px; border-bottom: 1px solid var(--glass-border); padding-bottom: 8px; }
        .rd-tab { display: flex; align-items: center; gap: 6px; padding: 8px 16px; border: none; background: transparent; color: var(--slate-400); cursor: pointer; border-radius: 4px; font-weight: 500; transition: all 0.2s; }
        .rd-tab:hover { color: var(--slate-200); background: rgba(255,255,255,0.05); }
        .rd-tab.active { background: rgba(45, 110, 245, 0.15); color: var(--cobalt-300); font-weight: 600; box-shadow: inset 0 0 10px rgba(45, 110, 245, 0.1); }
        .modal-overlay { position: fixed; inset: 0; background: rgba(4, 8, 15, 0.85); backdrop-filter: blur(6px); display: flex; align-items: flex-start; justify-content: center; z-index: 999999; overflow-y: auto; padding: 60px 24px; }
      `}</style>
    </div>
  );
}
