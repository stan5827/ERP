import React, { useEffect, useState } from 'react';
import { ClipboardCheck, Plus, Save, Trash2 } from 'lucide-react';
import {
  inspectionTemplatesApi,
  operationsApi,
  processInspectionsApi,
  skusApi,
  workOrdersApi,
} from '../api/api';
import type {
  InspectionTemplateDto,
  ProcessInspectionReportDto,
  WorkOrderInspectionOperationDto,
  WorkOrderDto,
} from '../api/api';
import { FqcWorkflowPanel } from './FqcWorkflowPanel';

type TemplateItemDraft = {
  itemCode: string;
  itemName: string;
  inputType: string;
  specification: string;
  unit: string;
  lowerLimit: string;
  upperLimit: string;
  options: string;
  inspectionMethod: string;
  instrument: string;
  samplingPlan: string;
  isRequired: boolean;
  isCritical: boolean;
};

type TemplateDraft = {
  templateCode: string;
  templateName: string;
  operationId: string;
  productSkuId: string;
  isActive: boolean;
  notes: string;
};

type InspectionValue = { value: string; result: string; notes: string };

const resultLabels: Record<string, string> = {
  pass: '合格',
  fail: '不合格',
  na: '不適用',
  incomplete: '未完成',
  pending: '待檢',
};

const emptyItem = (): TemplateItemDraft => ({
  itemCode: '',
  itemName: '',
  inputType: 'passFail',
  specification: '',
  unit: '',
  lowerLimit: '',
  upperLimit: '',
  options: '',
  inspectionMethod: '',
  instrument: '',
  samplingPlan: '',
  isRequired: true,
  isCritical: false,
});

const emptyTemplate = (): TemplateDraft => ({
  templateCode: '',
  templateName: '',
  operationId: '',
  productSkuId: '',
  isActive: true,
  notes: '',
});

const getErrorMessage = (error: unknown) =>
  (error as { response?: { data?: { message?: string } }; message?: string }).response?.data?.message ??
  (error as { message?: string }).message ??
  '操作失敗，請稍後再試';

export function ProcessQuality({ onBack }: { onBack: () => void }) {
  const [view, setView] = useState<'templates' | 'records' | 'fqc'>('templates');
  const [templates, setTemplates] = useState<InspectionTemplateDto[]>([]);
  const [operations, setOperations] = useState<Array<{ id: string; operationCode: string; operationName: string }>>([]);
  const [skus, setSkus] = useState<Array<{ id: string; skuNumber: string; skuName: string }>>([]);
  const [workOrders, setWorkOrders] = useState<WorkOrderDto[]>([]);
  const [editingTemplate, setEditingTemplate] = useState<InspectionTemplateDto | null>(null);
  const [templateDraft, setTemplateDraft] = useState<TemplateDraft>(emptyTemplate);
  const [itemDrafts, setItemDrafts] = useState<TemplateItemDraft[]>([]);
  const [workOrderId, setWorkOrderId] = useState('');
  const [operationOptions, setOperationOptions] = useState<WorkOrderInspectionOperationDto[]>([]);
  const [operationSeq, setOperationSeq] = useState('');
  const [templateId, setTemplateId] = useState('');
  const [lotNumber, setLotNumber] = useState('');
  const [inspectorName, setInspectorName] = useState('');
  const [sampleQuantity, setSampleQuantity] = useState('1');
  const [inspectionNotes, setInspectionNotes] = useState('');
  const [values, setValues] = useState<Record<string, InspectionValue>>({});
  const [reports, setReports] = useState<ProcessInspectionReportDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const [templateResponse, operationResponse, skuResponse, workOrderResponse] = await Promise.all([
          inspectionTemplatesApi.list(),
          operationsApi.list({ page: 1, pageSize: 200 }),
          skusApi.list({ pageSize: '200' }),
          workOrdersApi.list({ pageSize: '200' }),
        ]);
        setTemplates(templateResponse.data?.data ?? []);
        setOperations(operationResponse.data?.data ?? []);
        setSkus(skuResponse.data?.data ?? []);
        setWorkOrders(workOrderResponse.data?.data ?? []);
      } catch (loadError) {
        setError(getErrorMessage(loadError));
      } finally {
        setLoading(false);
      }
    };
    void load();
  }, []);

  useEffect(() => {
    if (!workOrderId) {
      setOperationOptions([]);
      setReports([]);
      setOperationSeq('');
      setTemplateId('');
      return;
    }
    const loadWorkOrderInspections = async () => {
      try {
        const [operationResponse, reportResponse] = await Promise.all([
          processInspectionsApi.workOrderOperations(workOrderId),
          processInspectionsApi.list({ workOrderId }),
        ]);
        const options = operationResponse.data?.data ?? [];
        setOperationOptions(options);
        setReports(reportResponse.data?.data ?? []);
        setOperationSeq(options[0]?.operationSeq.toString() ?? '');
      } catch (loadError) {
        setError(getErrorMessage(loadError));
      }
    };
    void loadWorkOrderInspections();
  }, [workOrderId]);

  const selectedOperation = operationOptions.find(item => item.operationSeq.toString() === operationSeq);
  const selectedTemplate = selectedOperation?.templates.find(item => item.id === templateId);

  useEffect(() => {
    setTemplateId(selectedOperation?.templates[0]?.id ?? '');
  }, [operationSeq, operationOptions, selectedOperation?.templates]);

  useEffect(() => {
    const next: Record<string, InspectionValue> = {};
    selectedTemplate?.items.forEach(item => {
      next[item.id] = { value: '', result: 'pass', notes: '' };
    });
    setValues(next);
  }, [templateId, templates, selectedTemplate?.items]);

  const refreshTemplates = async () => {
    const response = await inspectionTemplatesApi.list();
    setTemplates(response.data?.data ?? []);
  };

  const beginCreateTemplate = () => {
    setEditingTemplate(null);
    setTemplateDraft(emptyTemplate());
    setItemDrafts([emptyItem()]);
    setError('');
  };

  const beginEditTemplate = (template: InspectionTemplateDto) => {
    setEditingTemplate(template);
    setTemplateDraft({
      templateCode: template.templateCode,
      templateName: template.templateName,
      operationId: template.operationId,
      productSkuId: template.productSkuId ?? '',
      isActive: template.isActive,
      notes: template.notes ?? '',
    });
    setItemDrafts(template.items.map(item => ({
      itemCode: item.itemCode,
      itemName: item.itemName,
      inputType: item.inputType,
      specification: item.specification ?? '',
      unit: item.unit ?? '',
      lowerLimit: item.lowerLimit?.toString() ?? '',
      upperLimit: item.upperLimit?.toString() ?? '',
      options: item.options.join(', '),
      inspectionMethod: item.inspectionMethod ?? '',
      instrument: item.instrument ?? '',
      samplingPlan: item.samplingPlan ?? '',
      isRequired: item.isRequired,
      isCritical: item.isCritical,
    })));
    setError('');
  };

  const saveTemplate = async () => {
    if (!templateDraft.templateCode.trim() || !templateDraft.templateName.trim() ||
        !templateDraft.operationId || itemDrafts.length === 0 ||
        itemDrafts.some(item => !item.itemCode.trim() || !item.itemName.trim())) {
      setError('請填妥範本代碼、名稱、工藝與每個檢驗項目的代碼和名稱。');
      return;
    }
    const data = {
      ...templateDraft,
      productSkuId: templateDraft.productSkuId || null,
      items: itemDrafts.map((item, index) => ({
        ...item,
        lowerLimit: item.lowerLimit === '' ? null : Number(item.lowerLimit),
        upperLimit: item.upperLimit === '' ? null : Number(item.upperLimit),
        options: item.options.split(',').map(option => option.trim()).filter(Boolean),
        sortOrder: index,
      })),
    };

    setSaving(true);
    setError('');
    try {
      if (editingTemplate) await inspectionTemplatesApi.update(editingTemplate.id, data);
      else await inspectionTemplatesApi.create(data);
      await refreshTemplates();
      setEditingTemplate(null);
      setItemDrafts([]);
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const submitInspection = async () => {
    if (!workOrderId || !selectedOperation || !selectedTemplate || !inspectorName.trim() ||
        !Number.isFinite(Number(sampleQuantity)) || Number(sampleQuantity) <= 0) {
      setError('請選擇工單、適用範本，並填寫檢驗人員與有效的抽樣數量。');
      return;
    }
    setSaving(true);
    setError('');
    try {
      await processInspectionsApi.create({
        workOrderId,
        operationSeq: selectedOperation.operationSeq,
        templateId: selectedTemplate.id,
        lotNumber: lotNumber.trim() || null,
        inspectorName: inspectorName.trim(),
        sampleQuantity: Number(sampleQuantity),
        notes: inspectionNotes.trim() || null,
        items: selectedTemplate.items.map(item => ({
          templateItemId: item.id,
          value: values[item.id]?.value ?? '',
          result: values[item.id]?.result ?? 'pass',
          notes: values[item.id]?.notes ?? '',
        })),
      });
      const response = await processInspectionsApi.list({ workOrderId });
      setReports(response.data?.data ?? []);
      setLotNumber('');
      setInspectionNotes('');
      setError('');
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const updateValue = (id: string, patch: Partial<InspectionValue>) => {
    setValues(current => ({ ...current, [id]: { ...current[id], ...patch } }));
  };

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">工藝檢驗</h2>
          <p className="module-page-desc">依工藝自訂範本 · 記錄製程實測結果 · 出貨時彙整追溯</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-ghost btn-sm" onClick={onBack}>返回品質管理</button>
          {view === 'templates' && (
            <button className="btn btn-primary btn-sm" onClick={beginCreateTemplate}>
              <Plus size={13} />新增檢驗範本
            </button>
          )}
        </div>
      </div>

      <div className="module-filter-bar glass-card no-lift">
        <button className={`tab-item ${view === 'templates' ? 'active' : ''}`} onClick={() => setView('templates')}>
          檢驗範本
        </button>
        <button className={`tab-item ${view === 'records' ? 'active' : ''}`} onClick={() => setView('records')}>
          工藝檢驗紀錄
        </button>
        <button className={`tab-item ${view === 'fqc' ? 'active' : ''}`} onClick={() => setView('fqc')}>
          FQC 整批放行／完工入庫
        </button>
      </div>

      {error && <div className="glass-card" role="alert" style={{ padding: 12, color: 'var(--crimson-300)' }}>{error}</div>}

      {view === 'fqc' ? (
        <FqcWorkflowPanel workOrders={workOrders} />
      ) : view === 'templates' ? (
        <>
          {editingTemplate !== null || itemDrafts.length > 0 ? (
            <div className="glass-card" style={{ padding: 20, display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h3 style={{ color: 'var(--slate-100)', fontSize: 15, fontWeight: 700 }}>
                  {editingTemplate ? `編輯範本 · v${editingTemplate.version}` : '新增檢驗範本'}
                </h3>
                <button className="btn btn-ghost btn-sm" onClick={() => { setEditingTemplate(null); setItemDrafts([]); }}>
                  取消
                </button>
              </div>
              <div className="inspection-template-grid">
                <label>範本代碼<input className="form-input" value={templateDraft.templateCode} onChange={e => setTemplateDraft({ ...templateDraft, templateCode: e.target.value })} /></label>
                <label>範本名稱<input className="form-input" value={templateDraft.templateName} onChange={e => setTemplateDraft({ ...templateDraft, templateName: e.target.value })} /></label>
                <label>適用工藝<select className="form-input" value={templateDraft.operationId} onChange={e => setTemplateDraft({ ...templateDraft, operationId: e.target.value })}><option value="">選擇工藝</option>{operations.map(operation => <option key={operation.id} value={operation.id}>{operation.operationCode} - {operation.operationName}</option>)}</select></label>
                <label>適用產品（空白為所有產品）<select className="form-input" value={templateDraft.productSkuId} onChange={e => setTemplateDraft({ ...templateDraft, productSkuId: e.target.value })}><option value="">所有產品</option>{skus.map(sku => <option key={sku.id} value={sku.id}>{sku.skuNumber} - {sku.skuName}</option>)}</select></label>
              </div>
              <label>備註<textarea className="form-input" value={templateDraft.notes} onChange={e => setTemplateDraft({ ...templateDraft, notes: e.target.value })} /></label>

              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <h4 style={{ color: 'var(--slate-200)', fontSize: 13, fontWeight: 700 }}>檢驗項目</h4>
                <button className="btn btn-ghost btn-sm" onClick={() => setItemDrafts(items => [...items, emptyItem()])}><Plus size={13} />新增項目</button>
              </div>
              {itemDrafts.map((item, index) => (
                <div key={index} className="inspection-item-editor">
                  <div className="inspection-item-grid">
                    <label>項目代碼<input className="form-input" value={item.itemCode} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, itemCode: e.target.value } : row))} /></label>
                    <label>檢驗項目<input className="form-input" value={item.itemName} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, itemName: e.target.value } : row))} /></label>
                    <label>輸入類型<select className="form-input" value={item.inputType} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, inputType: e.target.value } : row))}><option value="number">數值量測</option><option value="text">文字紀錄</option><option value="boolean">是 / 否</option><option value="choice">選項</option><option value="passFail">合格判定</option></select></label>
                    <label>單位<input className="form-input" value={item.unit} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, unit: e.target.value } : row))} /></label>
                    <label>下限<input type="number" className="form-input" value={item.lowerLimit} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, lowerLimit: e.target.value } : row))} /></label>
                    <label>上限<input type="number" className="form-input" value={item.upperLimit} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, upperLimit: e.target.value } : row))} /></label>
                    <label className="inspection-item-wide">規格<input className="form-input" value={item.specification} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, specification: e.target.value } : row))} /></label>
                    <label className="inspection-item-wide">檢驗方法<input className="form-input" value={item.inspectionMethod} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, inspectionMethod: e.target.value } : row))} /></label>
                    <label>量具 / 設備<input className="form-input" value={item.instrument} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, instrument: e.target.value } : row))} /></label>
                    <label>抽樣計畫<input className="form-input" value={item.samplingPlan} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, samplingPlan: e.target.value } : row))} /></label>
                    {item.inputType === 'choice' && <label className="inspection-item-wide">選項（以逗號分隔）<input className="form-input" value={item.options} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, options: e.target.value } : row))} /></label>}
                  </div>
                  <div className="inspection-item-footer">
                    <label><input type="checkbox" checked={item.isRequired} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, isRequired: e.target.checked } : row))} /> 必填</label>
                    <label><input type="checkbox" checked={item.isCritical} onChange={e => setItemDrafts(rows => rows.map((row, i) => i === index ? { ...row, isCritical: e.target.checked } : row))} /> 重要品質特性</label>
                    <button className="icon-action-btn" title="移除此項目" disabled={itemDrafts.length === 1} onClick={() => setItemDrafts(rows => rows.filter((_, i) => i !== index))}><Trash2 size={14} /></button>
                  </div>
                </div>
              ))}
              <label className="inspection-item-footer"><input type="checkbox" checked={templateDraft.isActive} onChange={e => setTemplateDraft({ ...templateDraft, isActive: e.target.checked })} /> 啟用此範本</label>
              <div><button className="btn btn-primary btn-sm" disabled={saving} onClick={() => void saveTemplate()}><Save size={13} />{saving ? '儲存中…' : '儲存範本'}</button></div>
            </div>
          ) : loading ? (
            <div className="glass-card" style={{ padding: 32, textAlign: 'center', color: 'var(--slate-400)' }}>載入中…</div>
          ) : templates.length === 0 ? (
            <div className="glass-card" style={{ padding: 32, textAlign: 'center', color: 'var(--slate-400)' }}>尚未建立工藝檢驗範本。</div>
          ) : (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <table className="data-table">
                <thead><tr><th>範本代碼 / 版本</th><th>檢驗範本</th><th>適用工藝</th><th>適用產品</th><th>項目數</th><th>狀態</th><th>操作</th></tr></thead>
                <tbody>{templates.map(template => (
                  <tr key={template.id}>
                    <td className="mono">{template.templateCode} v{template.version}</td>
                    <td>{template.templateName}</td>
                    <td>{template.operationCode} - {template.operationName}</td>
                    <td>{template.productSkuNumber ? `${template.productSkuNumber} - ${template.productSkuName}` : '所有產品'}</td>
                    <td>{template.items.length}</td>
                    <td>{template.isActive ? '啟用' : '停用'}</td>
                    <td><button className="btn btn-ghost btn-sm" onClick={() => beginEditTemplate(template)}>編輯</button></td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
          )}
        </>
      ) : (
        <>
          <div className="glass-card" style={{ padding: 20, display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div className="inspection-template-grid">
              <label>工單<select className="form-input" value={workOrderId} onChange={e => setWorkOrderId(e.target.value)}><option value="">選擇工單</option>{workOrders.map(wo => <option key={wo.id} value={wo.id}>{wo.woNumber} - {wo.productSkuNumber} {wo.productSkuName}</option>)}</select></label>
              <label>工藝<select className="form-input" value={operationSeq} onChange={e => setOperationSeq(e.target.value)} disabled={!operationOptions.length}><option value="">選擇工藝</option>{operationOptions.map(operation => <option key={`${operation.operationSeq}-${operation.operationId}`} value={operation.operationSeq}>{operation.operationSeq}. {operation.operationName} ({operation.workCenterName ?? '未指定工作中心'})</option>)}</select></label>
              <label>檢驗範本<select className="form-input" value={templateId} onChange={e => setTemplateId(e.target.value)} disabled={!selectedOperation?.templates.length}><option value="">選擇範本</option>{selectedOperation?.templates.map(template => <option key={template.id} value={template.id}>{template.templateName} v{template.version}</option>)}</select></label>
              <label>批號<input className="form-input" value={lotNumber} onChange={e => setLotNumber(e.target.value)} placeholder="出貨明細相同批號用於追溯" /></label>
              <label>檢驗人員<input className="form-input" value={inspectorName} onChange={e => setInspectorName(e.target.value)} /></label>
              <label>抽樣數量<input type="number" min="0.01" step="0.01" className="form-input" value={sampleQuantity} onChange={e => setSampleQuantity(e.target.value)} /></label>
            </div>
            {selectedTemplate && (
              <>
                <h3 style={{ color: 'var(--slate-100)', fontSize: 14, fontWeight: 700 }}>檢驗項目 · {selectedTemplate.templateName} v{selectedTemplate.version}</h3>
                {selectedTemplate.items.map(item => {
                  const current = values[item.id] ?? { value: '', result: 'pass', notes: '' };
                  return (
                    <div key={item.id} className="inspection-record-item">
                      <div style={{ minWidth: 160 }}>
                        <strong>{item.itemName}</strong>{item.isRequired && <span style={{ color: 'var(--crimson-300)' }}> *</span>}
                        {item.isCritical && <span className="inspection-critical">重要特性</span>}
                        <small>{item.specification || '未設定規格'}{item.unit ? ` (${item.unit})` : ''}</small>
                        {(item.lowerLimit !== undefined || item.upperLimit !== undefined) && <small>允收範圍：{item.lowerLimit ?? '−∞'} ~ {item.upperLimit ?? '∞'}</small>}
                      </div>
                      {item.inputType === 'choice' ? (
                        <select className="form-input" value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })}>
                          <option value="">選擇結果</option>{item.options.map(option => <option key={option} value={option}>{option}</option>)}
                        </select>
                      ) : item.inputType === 'boolean' ? (
                        <select className="form-input" value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })}>
                          <option value="">選擇結果</option><option value="true">是</option><option value="false">否</option>
                        </select>
                      ) : item.inputType === 'passFail' ? (
                        <select className="form-input" value={current.result} onChange={e => updateValue(item.id, { value: e.target.value === 'pass' ? '合格' : '不合格', result: e.target.value })}>
                          <option value="pass">合格</option><option value="fail">不合格</option>
                        </select>
                      ) : (
                        <input className="form-input" type={item.inputType === 'number' ? 'number' : 'text'} step={item.inputType === 'number' ? 'any' : undefined} value={current.value} onChange={e => updateValue(item.id, { value: e.target.value })} placeholder={item.inputType === 'number' ? '輸入實測值' : '輸入檢驗結果'} />
                      )}
                      {item.inputType !== 'number' && item.inputType !== 'passFail' && (
                        <select className="form-input" aria-label={`${item.itemName}判定`} value={current.result} onChange={e => updateValue(item.id, { result: e.target.value })}>
                          <option value="pass">合格</option><option value="fail">不合格</option><option value="na">不適用</option>
                        </select>
                      )}
                      <input className="form-input" value={current.notes} onChange={e => updateValue(item.id, { notes: e.target.value })} placeholder="備註" />
                    </div>
                  );
                })}
                <label>檢驗備註<textarea className="form-input" value={inspectionNotes} onChange={e => setInspectionNotes(e.target.value)} /></label>
                <div><button className="btn btn-primary btn-sm" disabled={saving} onClick={() => void submitInspection()}><ClipboardCheck size={13} />{saving ? '提交中…' : '提交檢驗紀錄'}</button></div>
              </>
            )}
            {!selectedTemplate && workOrderId && selectedOperation && (
              <div style={{ color: 'var(--amber-300)', fontSize: 13 }}>此工藝尚無啟用的檢驗範本，請先到「檢驗範本」建立並啟用範本。</div>
            )}
          </div>

          {workOrderId && (
            <div className="glass-card" style={{ overflowX: 'auto' }}>
              <h3 style={{ padding: 16, color: 'var(--slate-100)', fontSize: 14, fontWeight: 700 }}>最近工藝檢驗紀錄</h3>
              <table className="data-table">
                <thead><tr><th>報告編號</th><th>工藝</th><th>範本</th><th>批號</th><th>檢驗人員</th><th>檢驗時間</th><th>判定</th></tr></thead>
                <tbody>{reports.map(report => (
                  <tr key={report.id}>
                    <td className="mono">{report.reportNumber}</td><td>{report.operationName ?? '—'}</td>
                    <td>{report.templateName} v{report.templateVersion}</td><td>{report.lotNumber || '—'}</td>
                    <td>{report.inspectorName}</td><td>{new Date(report.inspectedAt).toLocaleString()}</td>
                    <td>{resultLabels[report.result] ?? report.result}</td>
                  </tr>
                ))}{reports.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', padding: 24, color: 'var(--slate-500)' }}>尚無檢驗紀錄</td></tr>}</tbody>
              </table>
            </div>
          )}
        </>
      )}

      <style>{`
        .inspection-template-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(210px, 1fr)); gap: 12px; }
        .inspection-template-grid > label, .inspection-item-grid > label { display: flex; flex-direction: column; gap: 6px; color: var(--slate-400); font-size: 12px; }
        .inspection-item-editor { padding: 14px; background: var(--slate-850, var(--slate-800)); border: 1px solid var(--slate-700); border-radius: 8px; }
        .inspection-item-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(145px, 1fr)); gap: 10px; }
        .inspection-item-wide { grid-column: span 2; }
        .inspection-item-footer { display: flex; align-items: center; gap: 16px; margin-top: 10px; color: var(--slate-400); font-size: 12px; }
        .inspection-item-footer label { display: inline-flex; align-items: center; gap: 6px; }
        .inspection-item-footer button { margin-left: auto; }
        .inspection-record-item { display: grid; grid-template-columns: minmax(160px, 1.2fr) repeat(3, minmax(130px, 1fr)); gap: 10px; align-items: center; padding: 10px 0; border-top: 1px solid var(--slate-800); }
        .inspection-record-item strong { display: block; color: var(--slate-100); font-size: 13px; }
        .inspection-record-item small { display: block; margin-top: 3px; color: var(--slate-500); font-size: 11px; }
        .inspection-critical { margin-left: 6px; padding: 2px 5px; border-radius: 4px; color: var(--amber-300); background: rgba(245,158,11,.12); font-size: 10px; }
        @media (max-width: 760px) { .inspection-record-item { grid-template-columns: 1fr; } .inspection-item-wide { grid-column: auto; } }
      `}</style>
    </div>
  );
}
