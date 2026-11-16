import React, { useState, useEffect } from 'react';
import { Search, Plus, Edit2, Trash2, ArrowRight, Calculator, Layers, Settings, Loader2 } from 'lucide-react';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { uomsApi, skusApi } from '../api/api';
import type { UomDto, UomConversionDto } from '../api/api';
import { useQueryClient } from '@tanstack/react-query';

function toPascalCase(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const key in obj) {
    const pascal = key.charAt(0).toUpperCase() + key.slice(1);
    const value = obj[key];
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      result[pascal] = toPascalCase(value as Record<string, unknown>);
    } else if (Array.isArray(value)) {
      result[pascal] = value.map(item => 
        (item && typeof item === 'object') ? toPascalCase(item as Record<string, unknown>) : item
      );
    } else {
      result[pascal] = value;
    }
  }
  return result;
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

const uomTypeConfig: Record<string, { label: string; color: string }> = {
  數量: { label: '數量', color: 'var(--cobalt-400)' },
  重量: { label: '重量', color: 'var(--emerald-400)' },
  長度: { label: '長度', color: 'var(--amber-400)' },
  面積: { label: '面積', color: 'var(--violet-400)' },
  體積: { label: '體積', color: 'var(--cyan-400)' },
  時間: { label: '時間', color: 'var(--orange-400)' },
  其他: { label: '其他', color: 'var(--slate-400)' },
};

export const UomManagement: React.FC = () => {
  const [activeView, setActiveView] = useState<'units' | 'conversions' | 'calculator'>('units');
  const [search, setSearch] = useState('');
  const [showAddUnit, setShowAddUnit] = useState(false);
  const [showAddConversion, setShowAddConversion] = useState(false);
  const [editingUnit, setEditingUnit] = useState<UomDto | null>(null);
  const [editingConversion, setEditingConversion] = useState<UomConversionDto | null>(null);
  const [uoms, setUoms] = useState<UomDto[]>([]);
  const [conversions, setConversions] = useState<UomConversionDto[]>([]);
  const [skus, setSkus] = useState<{ id: string; skuNumber: string; skuName: string }[]>([]);
  const [loading, setLoading] = useState(true);

  const [calcSkuId, setCalcSkuId] = useState('');
  const [calcQty, setCalcQty] = useState<number>(1);
  const [calcFromUom, setCalcFromUom] = useState('');
  const [calcToUom, setCalcToUom] = useState('');
  const [calcResult, setCalcResult] = useState<{ convertedQty: number; formula: string } | null>(null);

  const queryClient = useQueryClient();

  useEffect(() => {
    loadData();
  }, [activeView]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [uomsRes, skusRes] = await Promise.all([
        uomsApi.list({ activeOnly: 'true' }),
        skusApi.list({ pageSize: '100' }),
      ]);
      setUoms(uomsRes.data?.data ?? []);
      setSkus(skusRes.data?.data ?? []);
      try {
        const conversionsRes = await uomsApi.getConversions({});
        setConversions(conversionsRes.data?.data ?? []);
      } catch (convErr) {
        console.warn('Failed to load conversions:', convErr);
        setConversions([]);
      }
    } catch (err) {
      console.error('Failed to load UOM data:', err);
    } finally {
      setLoading(false);
    }
  };

  const unitFields: FormField[] = [
    { name: 'uomCode', label: '單位代碼', type: 'text', required: true, placeholder: '如: BOX, KG, PCS', description: '系統內唯一識別碼，建議使用國際標準代碼。' },
    { name: 'uomName', label: '單位名稱', type: 'text', required: true, placeholder: '如: 箱、公斤、個', description: '顯示給使用者的中文名稱。' },
    { name: 'uomNameEn', label: '英文名稱', type: 'text', placeholder: '如: Box, Kilogram, Piece', description: '選填，英文名稱。' },
    { name: 'uomType', label: '單位類型', type: 'select', required: true, options: [
      { value: '數量', label: '數量' },
      { value: '重量', label: '重量' },
      { value: '長度', label: '長度' },
      { value: '面積', label: '面積' },
      { value: '體積', label: '體積' },
      { value: '時間', label: '時間' },
      { value: '其他', label: '其他' },
    ], defaultValue: '數量', description: '用於分類單位的類型，如：數量(箱/個)、重量(公斤/克)、長度(米/公分)等。' },
    { name: 'decimalPlaces', label: '小數位數', type: 'number', placeholder: '2', min: 0, max: 6, defaultValue: 2, description: '計算時保留的小數位數。' },
    { name: 'allowDecimal', label: '允許小數', type: 'checkbox', defaultValue: true, description: '此單位是否允許輸入小數量值。' },
    { name: 'displayOrder', label: '排序', type: 'number', placeholder: '0', min: 0, defaultValue: 0, description: '在下拉選單中的排序順序。' },
    { name: 'description', label: '說明', type: 'textarea', placeholder: '補充說明...' },
  ];

  const conversionFields: FormField[] = [
    { name: 'fromUomId', label: '來源單位', type: 'select', required: true, options: uoms.map(u => ({ value: u.id, label: `${u.uomCode} - ${u.uomName}` })), description: '要轉換的來源單位。' },
    { name: 'toUomId', label: '目標單位', type: 'select', required: true, options: uoms.map(u => ({ value: u.id, label: `${u.uomCode} - ${u.uomName}` })), description: '轉換後的目標單位。' },
    { name: 'factor', label: '換算係數', type: 'number', required: true, placeholder: '1', min: 0.000001, step: 0.000001, defaultValue: 1, description: '例如: 1 箱 = 12 個，係數為 12。' },
    { name: 'operator', label: '運算方式', type: 'select', required: true, options: [
      { value: '*', label: '乘法 (*)' },
      { value: '/', label: '除法 (/)' },
    ], defaultValue: '*', description: '選擇乘法或除法進行換算。' },
    { name: 'description', label: '說明', type: 'textarea', placeholder: '如: 1 箱 = 12 個' },
  ];

  const filteredUoms = uoms.filter(u =>
    u.uomCode.toLowerCase().includes(search.toLowerCase()) ||
    u.uomName.toLowerCase().includes(search.toLowerCase())
  );

  const filteredConversions = conversions.filter(c =>
    c.fromUomCode.toLowerCase().includes(search.toLowerCase()) ||
    c.toUomCode.toLowerCase().includes(search.toLowerCase()) ||
    (c.skuCode ?? '').toLowerCase().includes(search.toLowerCase())
  );

  const handleConvert = async () => {
    if (!calcSkuId || !calcFromUom || !calcToUom || calcQty <= 0) {
      alert('請選擇 SKU、來源單位、目標單位並輸入數量');
      return;
    }
    try {
      const res = await uomsApi.convert({ skuId: calcSkuId, qty: calcQty, fromUom: calcFromUom, toUom: calcToUom });
      if (res.data?.success && res.data.data) {
        setCalcResult({
          convertedQty: res.data.data.convertedQty,
          formula: res.data.data.formula,
        });
      } else {
        alert('換算失敗: ' + (res.data?.message || '未知錯誤'));
      }
    } catch (err: unknown) {
      console.error('Conversion failed:', err);
      alert('換算失敗: ' + getErrorMessage(err));
    }
  };

  const handleDeleteUnit = async (id: string) => {
    if (!confirm('確定要刪除此單位嗎？')) return;
    try {
      await uomsApi.delete(id);
      queryClient.invalidateQueries({ queryKey: ['uoms'] });
      loadData();
    } catch (err: unknown) {
      alert('刪除失敗: ' + getErrorMessage(err));
    }
  };

  const handleDeleteConversion = async (id: string) => {
    if (!confirm('確定要刪除此換算關係嗎？')) return;
    try {
      await uomsApi.deleteConversion(id);
      loadData();
    } catch (err: unknown) {
      alert('刪除失敗: ' + getErrorMessage(err));
    }
  };

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">單位量測管理</h2>
          <p className="module-page-desc">UOM 單位維護 · 換算關係設定 · 即時換算計算機</p>
        </div>
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        <button
          className={`rd-tab ${activeView === 'units' ? 'active' : ''}`}
          onClick={() => setActiveView('units')}
        >
          <Layers size={14} />單位主檔
        </button>
        <button
          className={`rd-tab ${activeView === 'conversions' ? 'active' : ''}`}
          onClick={() => setActiveView('conversions')}
        >
          <ArrowRight size={14} />換算關係
        </button>
        <button
          className={`rd-tab ${activeView === 'calculator' ? 'active' : ''}`}
          onClick={() => setActiveView('calculator')}
        >
          <Calculator size={14} />換算計算機
        </button>
      </div>

      <div className="glass-card" style={{ padding: '14px 18px', display: 'flex', gap: 12, alignItems: 'center' }}>
        <div className="filter-search" style={{ flex: 1, maxWidth: 300 }}>
          <Search size={13} style={{ color: 'var(--slate-500)' }} />
          <input
            className="search-input"
            placeholder={activeView === 'units' ? '搜尋單位代碼、名稱...' : '搜尋來源單位、目標單位...'}
            value={search}
            onChange={e => setSearch(e.target.value)}
          />
        </div>
        {activeView === 'units' && (
          <button className="btn btn-primary btn-sm" onClick={() => { setEditingUnit(null); setShowAddUnit(true); }}>
            <Plus size={13} />新增單位
          </button>
        )}
        {activeView === 'conversions' && (
          <button className="btn btn-primary btn-sm" onClick={() => { setEditingConversion(null); setShowAddConversion(true); }}>
            <Plus size={13} />新增換算
          </button>
        )}
      </div>

      {loading ? (
        <div className="glass-card" style={{ textAlign: 'center', padding: 40 }}>
          <Loader2 size={24} style={{ animation: 'spin 1s linear infinite', color: 'var(--slate-500)' }} />
          <div style={{ color: 'var(--slate-500)', marginTop: 8 }}>載入中...</div>
        </div>
      ) : (
        <>
          {activeView === 'units' && (
            <div className="glass-card" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>單位代碼</th>
                    <th>單位名稱</th>
                    <th>英文名稱</th>
                    <th>類型</th>
                    <th>小數位</th>
                    <th>允許小數</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUoms.length === 0 ? (
                    <tr>
                      <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>
                        無符合的單位資料
                      </td>
                    </tr>
                  ) : (
                    filteredUoms.map(u => {
                      const typeConfig = uomTypeConfig[u.uomType] ?? { label: u.uomType, color: 'var(--slate-400)' };
                      return (
                        <tr key={u.id}>
                          <td className="mono" style={{ color: 'var(--cobalt-300)', fontWeight: 600 }}>{u.uomCode}</td>
                          <td style={{ fontWeight: 500 }}>{u.uomName}</td>
                          <td style={{ color: 'var(--slate-400)', fontSize: 12 }}>{u.uomNameEn ?? '—'}</td>
                          <td>
                            <span style={{ fontSize: 11, color: typeConfig.color, background: `${typeConfig.color}15`, padding: '2px 8px', borderRadius: 4 }}>
                              {typeConfig.label}
                            </span>
                          </td>
                          <td className="mono" style={{ color: 'var(--slate-400)' }}>{u.decimalPlaces}</td>
                          <td>
                            <span style={{ fontSize: 11, color: u.allowDecimal ? 'var(--emerald-400)' : 'var(--slate-500)' }}>
                              {u.allowDecimal ? '是' : '否'}
                            </span>
                          </td>
                          <td>
                            <div style={{ display: 'flex', gap: 4 }}>
                              <button className="icon-action-btn" title="編輯" onClick={() => { setEditingUnit(u); setShowAddUnit(true); }}>
                                <Edit2 size={13} />
                              </button>
                              <button className="icon-action-btn" title="刪除" style={{ color: 'var(--crimson-400)' }} onClick={() => handleDeleteUnit(u.id)}>
                                <Trash2 size={13} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          )}

          {activeView === 'conversions' && (
            <div className="glass-card" style={{ overflow: 'hidden' }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>來源單位</th>
                    <th style={{ width: 40 }}></th>
                    <th>目標單位</th>
                    <th>換算係數</th>
                    <th>說明</th>
                    <th>適用範圍</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredConversions.length === 0 ? (
                    <tr>
                      <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>
                        無換算關係
                      </td>
                    </tr>
                  ) : (
                    filteredConversions.map(c => (
                      <tr key={c.id}>
                        <td>
                          <span className="mono" style={{ color: 'var(--cobalt-300)' }}>{c.fromUomCode}</span>
                          <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{c.fromUomName}</div>
                        </td>
                        <td style={{ textAlign: 'center', color: 'var(--slate-500)' }}>
                          <ArrowRight size={14} />
                        </td>
                        <td>
                          <span className="mono" style={{ color: 'var(--emerald-300)' }}>{c.toUomCode}</span>
                          <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>{c.toUomName}</div>
                        </td>
                        <td>
                          <span className="mono" style={{ color: 'var(--amber-300)', fontWeight: 600 }}>
                            {c.factor} {c.operator}
                          </span>
                          <div style={{ fontSize: 10, color: 'var(--slate-500)' }}>
                            1 {c.fromUomCode} = {c.operator === '*' ? c.factor : 1/c.factor} {c.toUomCode}
                          </div>
                        </td>
                        <td style={{ fontSize: 12, color: 'var(--slate-400)', maxWidth: 200, overflow: 'hidden', textOverflow: 'ellipsis' }}>
                          {c.description ?? '—'}
                        </td>
                        <td>
                          {c.isGlobal ? (
                            <span style={{ fontSize: 10, color: 'var(--emerald-400)', background: 'rgba(16,185,129,0.1)', padding: '2px 6px', borderRadius: 3 }}>
                              全域通用
                            </span>
                          ) : (
                            <span style={{ fontSize: 10, color: 'var(--cobalt-400)', background: 'rgba(45,110,245,0.1)', padding: '2px 6px', borderRadius: 3 }}>
                              {c.skuCode}
                            </span>
                          )}
                        </td>
                        <td>
                          <div style={{ display: 'flex', gap: 4 }}>
                            <button className="icon-action-btn" title="編輯" onClick={() => { setEditingConversion(c); setShowAddConversion(true); }}>
                              <Edit2 size={13} />
                            </button>
                            <button className="icon-action-btn" title="刪除" style={{ color: 'var(--crimson-400)' }} onClick={() => handleDeleteConversion(c.id)}>
                              <Trash2 size={13} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}

          {activeView === 'calculator' && (
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
              <div className="glass-card" style={{ padding: 24 }}>
                <h3 style={{ fontSize: 15, fontWeight: 700, color: 'var(--slate-100)', marginBottom: 20, display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Calculator size={18} style={{ color: 'var(--cobalt-400)' }} />
                  單位換算計算機
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                  <div>
                    <label style={{ fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>選擇商品 (SKU)</label>
                    <select
                      className="form-input"
                      value={calcSkuId}
                      onChange={e => { setCalcSkuId(e.target.value); setCalcResult(null); }}
                      style={{ width: '100%' }}
                    >
                      <option value="">請選擇商品</option>
                      {skus.map(s => <option key={s.id} value={s.id}>{s.skuNumber} - {s.skuName}</option>)}
                    </select>
                  </div>
                  <div>
                    <label style={{ fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>選擇單位 (從)</label>
                    <select
                      className="form-input"
                      value={calcFromUom}
                      onChange={e => { setCalcFromUom(e.target.value); setCalcResult(null); }}
                      style={{ width: '100%' }}
                    >
                      <option value="">請選擇來源單位</option>
                      {uoms.map(u => <option key={u.id} value={u.uomCode}>{u.uomCode} - {u.uomName}</option>)}
                    </select>
                  </div>
                  <div>
                    <label style={{ fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>數量</label>
                    <input
                      type="number"
                      className="form-input"
                      value={calcQty}
                      onChange={e => { setCalcQty(parseFloat(e.target.value) || 0); setCalcResult(null); }}
                      min={0}
                      step={0.001}
                      style={{ width: '100%' }}
                    />
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--slate-500)' }}>
                    <ArrowRight size={20} />
                  </div>
                  <div>
                    <label style={{ fontSize: 11, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, display: 'block' }}>選擇單位 (到)</label>
                    <select
                      className="form-input"
                      value={calcToUom}
                      onChange={e => { setCalcToUom(e.target.value); setCalcResult(null); }}
                      style={{ width: '100%' }}
                    >
                      <option value="">請選擇目標單位</option>
                      {uoms.map(u => <option key={u.id} value={u.uomCode}>{u.uomCode} - {u.uomName}</option>)}
                    </select>
                  </div>
                  <button className="btn btn-primary" onClick={handleConvert} style={{ marginTop: 8 }}>
                    <Calculator size={14} />執行換算
                  </button>
                  {calcResult && (
                    <div style={{ marginTop: 16, padding: 16, background: 'rgba(45,110,245,0.1)', borderRadius: 8, border: '1px solid rgba(45,110,245,0.2)' }}>
                      <div style={{ fontSize: 11, color: 'var(--slate-400)', marginBottom: 4 }}>換算結果</div>
                      <div className="mono" style={{ fontSize: 24, color: 'var(--cobalt-300)', fontWeight: 700 }}>
                        {calcResult.convertedQty.toLocaleString('zh-TW', { maximumFractionDigits: 6 })} {calcToUom}
                      </div>
                      <div style={{ fontSize: 11, color: 'var(--slate-500)', marginTop: 4 }}>{calcResult.formula}</div>
                    </div>
                  )}
                </div>
              </div>
              <div className="glass-card" style={{ padding: 24 }}>
                <h3 style={{ fontSize: 15, fontWeight: 700, color: 'var(--slate-100)', marginBottom: 20, display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Settings size={18} style={{ color: 'var(--amber-400)' }} />
                  常用換算速查表
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  {[
                    { from: 'BOX', to: 'PCS', factor: '12' },
                    { from: 'CARTON', to: 'BOX', factor: '2' },
                    { from: 'TON', to: 'KG', factor: '1,000' },
                    { from: 'KG', to: 'G', factor: '1,000' },
                    { from: 'M', to: 'CM', factor: '100' },
                    { from: 'L', to: 'ML', factor: '1,000' },
                    { from: 'BAG', to: 'KG', factor: '25' },
                    { from: 'ROLL', to: 'M', factor: '100' },
                  ].map((item, i) => (
                    <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 12px', background: 'var(--slate-800)', borderRadius: 6, fontSize: 12 }}>
                      <span className="mono" style={{ color: 'var(--slate-300)' }}>
                        1 {item.from}
                      </span>
                      <ArrowRight size={12} style={{ color: 'var(--slate-500)' }} />
                      <span className="mono" style={{ color: 'var(--emerald-300)', fontWeight: 600 }}>
                        {item.factor} {item.to}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </>
      )}

      <FormModal
        open={showAddUnit}
        title={editingUnit ? '編輯單位' : '新增單位'}
        fields={unitFields}
        initialValues={editingUnit ? {
          uomCode: editingUnit.uomCode,
          uomName: editingUnit.uomName,
          uomNameEn: editingUnit.uomNameEn,
          uomType: editingUnit.uomType,
          decimalPlaces: editingUnit.decimalPlaces,
          allowDecimal: editingUnit.allowDecimal,
          displayOrder: editingUnit.displayOrder,
          description: editingUnit.description,
        } : { uomType: 'base', decimalPlaces: 2, allowDecimal: true, isActive: true, displayOrder: 0 }}
        onClose={() => { setShowAddUnit(false); setEditingUnit(null); }}
        onSubmit={async (data) => {
          if (!data.uomCode || String(data.uomCode).length === 0) {
            alert('請填寫單位代碼');
            return;
          }
          if (editingUnit) {
            await uomsApi.update(editingUnit.id, data);
          } else {
            await uomsApi.create(data);
          }
          setShowAddUnit(false);
          setEditingUnit(null);
          loadData();
        }}
      />

      <FormModal
        open={showAddConversion}
        title={editingConversion ? '編輯換算關係' : '新增換算關係'}
        fields={conversionFields}
        initialValues={editingConversion ? {
          fromUomId: editingConversion.fromUomId,
          toUomId: editingConversion.toUomId,
          factor: editingConversion.factor,
          operator: editingConversion.operator,
          description: editingConversion.description,
        } : { factor: 1, operator: '*' }}
        onClose={() => { setShowAddConversion(false); setEditingConversion(null); }}
        onSubmit={async (data) => {
          try {
            if (data.fromUomId === data.toUomId) {
              alert('來源單位與目標單位不可相同');
              return;
            }
            // Clean up data - convert empty values to null
            const cleanedData: Record<string, unknown> = {};
            for (const key in data) {
              const value = data[key];
              if (value === '' || value === undefined) {
                cleanedData[key] = null;
              } else {
                // Ensure numeric values are properly converted
                if (key === 'factor' && typeof value === 'number') {
                  cleanedData[key] = parseFloat(String(value));
                } else {
                  cleanedData[key] = value;
                }
              }
            }
            const payload = toPascalCase(cleanedData);
            if (!editingConversion) {
              payload.IsGlobal = true;
            }
            console.log('Saving conversion:', payload);
            if (editingConversion) {
              await uomsApi.updateConversion(editingConversion.id, payload);
            } else {
              await uomsApi.createConversion(payload);
            }
            setShowAddConversion(false);
            setEditingConversion(null);
            loadData();
          } catch (err: unknown) {
            alert('儲存失敗: ' + getErrorMessage(err));
          }
        }}
      />

      <style>{`
        .rd-tab { display: flex; align-items: center; gap: 6px; padding: 8px 16px; font-size: 13px; font-weight: 600; border: none; background: transparent; color: var(--slate-500); border-radius: var(--radius-md); cursor: pointer; transition: all var(--transition-fast); }
        .rd-tab:hover { color: var(--slate-300); background: rgba(45, 110, 245, 0.06); }
        .rd-tab.active { color: var(--cobalt-300); background: rgba(45, 110, 245, 0.1); }
        .filter-search { display: flex; align-items: center; gap: 8px; min-width: 220px; padding: 6px 12px; background: var(--slate-800); border: 1px solid var(--slate-700); border-radius: 6px; }
        .search-input { background: transparent; border: none; outline: none; color: var(--slate-200); font-family: var(--font-body); font-size: 13px; flex: 1; }
        .search-input::placeholder { color: var(--slate-600); }
        @keyframes spin { to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
};
