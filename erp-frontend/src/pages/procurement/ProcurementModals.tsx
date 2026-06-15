import React from 'react';
import { Trash2, Plus } from 'lucide-react';
import { FormModal } from '../../components/FormModal';
import { procurementPurchaseOrdersApi, procurementSuppliersApi, purchaseRequisitionsApi } from '../../api/procurementApi';
import { getPoFields, getPrFields, supplierFields } from './procurementUtils';
import { purchaseOrderSchema, supplierSchema } from '../../schemas/procurementSchema';
import type { PurchaseOrderDto, PurchaseRequisitionDto, PurchaseOrderItemDto, PurchaseRequisitionItemDto, SupplierDto } from '../../api/api';

type PurchaseOrderEditItem = Partial<PurchaseOrderItemDto> & {
  skuId: string;
  quantity: number;
  unitPrice: number;
};

type PurchaseRequisitionEditItem = Partial<PurchaseRequisitionItemDto> & {
  skuId: string;
  quantity: number;
  unit: string;
  requiredDate: string;
  estimatedUnitPrice: number;
  leadTime?: number;
};

interface ProcurementModalsProps {
  showAdd: boolean;
  showEditPo: boolean;
  selectedPo: PurchaseOrderDto | null;
  selectedPr: PurchaseRequisitionDto | null;
  poItems: PurchaseOrderEditItem[];
  setPoItems: (items: PurchaseOrderEditItem[]) => void;
  setShowAdd: (b: boolean) => void;
  setShowEditPo: (b: boolean) => void;
  setSelectedPr: (pr: PurchaseRequisitionDto | null) => void;
  setSelectedPo: (po: PurchaseOrderDto | null) => void;
  suppliersMapped: { value: string; label: string; }[];
  skusMapped: { value: string; label: string; price?: number; unit?: string }[];
  queryClient: import('@tanstack/react-query').QueryClient;
  
  showAddPr: boolean;
  showEditPr: boolean;
  prItems: PurchaseRequisitionEditItem[];
  setPrItems: (items: PurchaseRequisitionEditItem[]) => void;
  setShowAddPr: (b: boolean) => void;
  setShowEditPr: (b: boolean) => void;
  departmentsMapped: { value: string; label: string }[];

  showAddSupplier: boolean;
  showEditSupplier: boolean;
  editingSupplierId: string | null;
  supplierData?: { data?: SupplierDto[] };
  setShowAddSupplier: (b: boolean) => void;
  setShowEditSupplier: (b: boolean) => void;
  setEditingSupplierId: (id: string | null) => void;
}

const ProcurementModals: React.FC<ProcurementModalsProps> = ({
  showAdd,
  showEditPo,
  selectedPo,
  selectedPr,
  poItems,
  setPoItems,
  setShowAdd,
  setShowEditPo,
  setSelectedPr,
  setSelectedPo,
  suppliersMapped,
  skusMapped,
  queryClient,
  
  showAddPr,
  showEditPr,
  prItems,
  setPrItems,
  setShowAddPr,
  setShowEditPr,
  departmentsMapped,

  showAddSupplier,
  showEditSupplier,
  editingSupplierId,
  supplierData,
  setShowAddSupplier,
  setShowEditSupplier,
  setEditingSupplierId
}) => {
  return (
    <>
      <FormModal 
        open={showAdd || showEditPo} 
        title={selectedPo?.id ? `編輯採購單 ${selectedPo.poNumber}` : (selectedPr ? "從 PR 轉化採購指令" : "智動化下達 PO")}
        width={1000}
        schema={purchaseOrderSchema}
        initialValues={selectedPo || undefined}
        onClose={() => { setShowAdd(false); setShowEditPo(false); setSelectedPr(null); setPoItems([]); setSelectedPo(null); }}
        fields={getPoFields(suppliersMapped)}
        onSubmit={async (v) => {
           if (selectedPo?.id) {
             await procurementPurchaseOrdersApi.update(selectedPo.id, { ...v, items: poItems });
           } else {
             await procurementPurchaseOrdersApi.create({ ...v, items: poItems });
           }
           queryClient.invalidateQueries({ queryKey:['purchase-orders'] });
           setShowAdd(false);
           setShowEditPo(false);
        }}
        customRenderers={{
          _poItems: (
            <div className="items-editor">
              <div className="editor-header">
                <h4>採購品項明細</h4>
                <button type="button" className="btn-sm btn-outline" onClick={() => setPoItems([...poItems, { skuId: '', quantity: 1, unitPrice: 0 }])}>新增物料</button>
              </div>
              <div className="items-grid">
                <div className="item-row" style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', marginBottom: 12, paddingBottom: 8 }}>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>物料品號 / 規格描述</div>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>採購數量</div>
                  <div style={{ fontSize: 11, fontWeight: 700, color: 'var(--slate-500)' }}>建議單價 (TWD)</div>
                  <div></div>
                </div>
                {poItems.map((item, i) => (
                  <div key={i} className="item-row">
                    <select value={item.skuId} onChange={e => {
                      const next = [...poItems];
                      next[i].skuId = e.target.value;
                      const sku = skusMapped.find(s => s.value === e.target.value);
                      if (sku) next[i].unitPrice = sku.price;
                      setPoItems(next);
                    }}>
                      <option value="">選擇物料</option>
                      {skusMapped.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                    </select>
                    <input type="number" value={item.quantity} onChange={e => {
                      const next = [...poItems];
                      next[i].quantity = Number(e.target.value);
                      setPoItems(next);
                    }} placeholder="數量" />
                    <input type="number" value={item.unitPrice} onChange={e => {
                      const next = [...poItems];
                      next[i].unitPrice = Number(e.target.value);
                      setPoItems(next);
                    }} placeholder="單價" />
                    <button type="button" className="btn-icon danger" onClick={() => setPoItems(poItems.filter((_, idx) => idx !== i))}><Trash2 size={14} /></button>
                  </div>
                ))}
              </div>
            </div>
          )
        }}
      />

      <FormModal
        open={showAddPr || showEditPr}
        title={showEditPr ? `編輯請購單 ${selectedPr?.prNumber}` : "建立請購單 (PR)"}
        width={1240}
        fields={getPrFields(departmentsMapped)}
        initialValues={showEditPr ? selectedPr : undefined}
        onClose={() => { setShowAddPr(false); setShowEditPr(false); setSelectedPr(null); setPrItems([]); }}
        onSubmit={async (values) => {
          if (showEditPr && selectedPr) {
            await purchaseRequisitionsApi.update(selectedPr.id, { ...values, items: prItems });
          } else {
            await purchaseRequisitionsApi.create({ ...values, items: prItems });
          }
          queryClient.invalidateQueries({ queryKey: ['purchase-requisitions'] });
          setShowAddPr(false);
          setShowEditPr(false);
        }}
        customRenderers={{
          _prItems: (
            <div className="pr-items-container">
               <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                 <h4 style={{ margin: 0, fontSize: 13, color: 'var(--gold-400)', fontWeight: 700 }}>精確請購明細</h4>
                 <button type="button" className="btn btn-outline btn-sm" style={{ borderColor: 'var(--gold-400)', color: 'var(--gold-400)' }} onClick={() => setPrItems([...prItems, { skuId: '', quantity: 1, unit: 'PCS', requiredDate: new Date().toISOString().slice(0,10), estimatedUnitPrice: 0, leadTime: 7 }])}>
                   <Plus size={14} style={{ marginRight: 6 }} /> 增加品項
                 </button>
               </div>
               <div style={{ overflowX: 'auto', borderRadius: 8, border: '1px solid rgba(255,255,255,0.05)' }}>
                 <table className="pr-edit-table">
                   <thead>
                     <tr>
                       <th style={{ width: 420 }}>品號 / 規格 *</th>
                       <th style={{ width: 100 }}>數量 *</th>
                       <th style={{ width: 80 }}>單位</th>
                       <th style={{ width: 160 }}>需求交期 *</th>
                       <th style={{ width: 120 }}>預估單價</th>
                       <th style={{ width: 90 }}>前置(天)</th>
                       <th style={{ width: 50 }}>操作</th>
                     </tr>
                   </thead>
                   <tbody>
                     {prItems.map((item, i) => (
                       <tr key={i}>
                         <td>
                           <select className="form-input mandatory" style={{ width: '100%' }} value={item.skuId} onChange={e => {
                             const next = [...prItems];
                             next[i].skuId = e.target.value;
                             const sku = skusMapped.find(s => s.value === e.target.value);
                             if (sku) {
                               next[i].unit = sku.unit;
                               next[i].estimatedUnitPrice = sku.price;
                             }
                             setPrItems(next);
                           }}>
                             <option value="">搜尋採購品項...</option>
                             {skusMapped.map(s => <option key={s.value} value={s.value}>{s.label}</option>)}
                           </select>
                         </td>
                         <td><input type="number" className="form-input mandatory" value={item.quantity} onChange={e => {
                           const next = [...prItems];
                           next[i].quantity = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td><input type="text" className="form-input secondary" value={item.unit} readOnly /></td>
                         <td><input type="date" className="form-input mandatory" value={item.requiredDate} onChange={e => {
                           const next = [...prItems];
                           next[i].requiredDate = e.target.value;
                           setPrItems(next);
                         }} /></td>
                         <td><input type="number" className="form-input" value={item.estimatedUnitPrice} onChange={e => {
                           const next = [...prItems];
                           next[i].estimatedUnitPrice = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td><input type="number" className="form-input" value={item.leadTime} onChange={e => {
                           const next = [...prItems];
                           next[i].leadTime = Number(e.target.value);
                           setPrItems(next);
                         }} /></td>
                         <td style={{ textAlign: 'center' }}>
                           <button type="button" className="btn-icon danger" onClick={() => setPrItems(prItems.filter((_, idx) => idx !== i))}><Trash2 size={15} /></button>
                         </td>
                       </tr>
                     ))}
                   </tbody>
                 </table>
               </div>
            </div>
          )
        }}
      />

      <FormModal
        open={showAddSupplier || showEditSupplier}
        title={showEditSupplier ? '編輯供應商' : '新增供應商'}
        schema={supplierSchema}
        fields={supplierFields}
        initialValues={showEditSupplier ? (supplierData?.data || []).find((s:any) => s.id === editingSupplierId) : undefined}
        onClose={() => { setShowAddSupplier(false); setShowEditSupplier(false); setEditingSupplierId(null); }}
        onSubmit={async (values) => {
          if (showEditSupplier && editingSupplierId) {
            await procurementSuppliersApi.update(editingSupplierId, values);
          } else {
            await procurementSuppliersApi.create(values);
          }
          queryClient.invalidateQueries({ queryKey: ['suppliers'] });
          setShowAddSupplier(false);
          setShowEditSupplier(false);
          setEditingSupplierId(null);
        }}
      />
    </>
  );
};

export default ProcurementModals;
