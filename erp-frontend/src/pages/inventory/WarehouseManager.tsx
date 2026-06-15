import React, { useState, useEffect } from 'react';
import { Search, Plus, X, Warehouse, Edit2, Trash2 } from 'lucide-react';
import { warehousesApi, warehouseTypesApi, WarehouseDto, WarehouseTypeDto } from '../../api/api';
import { FormModal } from '../../components/FormModal';
import { useConfirmDialog } from '../../components/ConfirmDialog';

interface WarehouseManagerProps {
  open: boolean;
  onClose: () => void;
  onUpdate: () => void;
}

const WarehouseManager: React.FC<WarehouseManagerProps> = ({ open, onClose, onUpdate }) => {
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingWh, setEditingWh] = useState<WarehouseDto | null>(null);
  const [search, setSearch] = useState("");
  const [warehouseTypes, setWarehouseTypes] = useState<WarehouseTypeDto[]>([]);
  const { confirm, close } = useConfirmDialog();

  const loadWarehouses = async () => {
    const res = await warehousesApi.list({ includeInactive: true });
    setWarehouses(res.data?.data || []);
  };

  const loadTypes = async () => {
    const res = await warehouseTypesApi.list();
    setWarehouseTypes(res.data?.data || []);
  };

  useEffect(() => {
    if (!open) return;
    (async () => {
      const r1 = await warehousesApi.list({ includeInactive: true });
      setWarehouses(r1.data?.data || []);
      const r2 = await warehouseTypesApi.list();
      setWarehouseTypes(r2.data?.data || []);
      setSearch("");
    })();
  }, [open]);

  const handleDelete = (id: string) => {
    confirm(
      "刪除確認",
      "確定要刪除此倉庫嗎？\n注意：若倉庫內已有存貨或儲位，可能導致刪除失敗。",
      async () => {
        try {
          await warehousesApi.delete(id);
          loadWarehouses();
          onUpdate();
          close();
        } catch (err: any) {
          alert("刪除失敗: " + err.message);
          close();
        }
      },
      { confirmVariant: 'danger', confirmText: '確定刪除' }
    );
  };

  const filteredWhs = warehouses.filter(w =>
    w.warehouseName?.toLowerCase().includes(search.toLowerCase()) ||
    w.warehouseCode?.toLowerCase().includes(search.toLowerCase())
  );

  if (!open) return null;

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-card" style={{ maxWidth: 720, width: '95%', maxHeight: '85vh', display: 'flex', flexDirection: 'column' }}>
        <div className="modal-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px', borderBottom: '1px solid var(--glass-border)' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <Warehouse size={18} style={{ color: 'var(--cobalt-400)' }} />
            <h3 style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-100)' }}>倉庫清單管理</h3>
          </div>
          <button className="icon-action-btn" onClick={onClose}><X size={18} /></button>
        </div>
        
        <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--glass-border)', display: 'flex', gap: 12, alignItems: 'center' }}>
          <div className="filter-search" style={{ flex: 1 }}>
            <Search size={14} style={{ color: "var(--slate-500)" }} />
            <input className="search-input" placeholder="搜尋倉庫名稱或代碼..." value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={14} /> 新增倉庫</button>
        </div>
        
        <div className="modal-body" style={{ padding: '12px 20px', overflowY: 'auto', flex: 1 }}>
          <table className="data-table">
            <thead>
              <tr><th>代碼</th><th>名稱</th><th>類型</th><th>狀態</th><th style={{ textAlign: "right" }}>操作</th></tr>
            </thead>
            <tbody>
              {filteredWhs.map(w => (
                <tr key={w.id}>
                  <td><code style={{ color: 'var(--cobalt-400)', fontWeight: 600 }}>{w.warehouseCode}</code></td>
                  <td style={{ color: 'var(--slate-200)', fontWeight: 500 }}>{w.warehouseName}</td>
                  <td>
                    <span style={{ fontSize: 11, color: 'var(--amber-400)', background: 'rgba(251,191,36,0.1)', padding: '2px 6px', borderRadius: 4 }}>
                      {(() => {
                        const typeMap: Record<string, string> = {
                          'raw_materials': '原料倉',
                          'finished_goods': '成品倉',
                          'semi_finished': '半成品倉',
                          'wip': '在製品倉',
                          'scrap': '廢品倉',
                          'return': '退貨倉',
                          'virtual': '虛擬倉',
                          'raw': '原料倉',
                          'finished': '成品倉'
                        };
                        const typeObj = warehouseTypes.find(t => t.typeCode === w.type);
                        const displayName = typeObj?.typeName || w.type;
                        return typeMap[displayName] || typeMap[w.type] || displayName;
                      })()}
                    </span>
                  </td>
                  <td><span className={`status-badge ${w.isActive ? "status-active" : "status-neutral"}`}>{w.isActive ? "啟用" : "停用"}</span></td>
                  <td style={{ textAlign: "right" }}>
                    <div className="flex justify-end gap-2">
                      <button className="btn btn-ghost btn-sm" onClick={() => setEditingWh(w)}><Edit2 size={13} /> 編輯</button>
                      <button className="btn btn-ghost btn-sm text-red-400" onClick={() => handleDelete(w.id)}><Trash2 size={13} /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="modal-footer" style={{ padding: '12px 20px', borderTop: '1px solid var(--glass-border)', display: 'flex', justifyContent: 'flex-end' }}>
           <button className="btn btn-ghost btn-sm" onClick={onClose}>關閉</button>
        </div>

        {(showAdd || editingWh) && (
          <FormModal
            open={!!(showAdd || editingWh)}
            title={editingWh ? "編輯倉庫" : "新增倉庫"}
            initialValues={editingWh || { isActive: true, type: 'raw' }}
            fields={[
              { name: "warehouseCode", label: "倉庫代碼 *", type: "text", required: true },
              { name: "warehouseName", label: "倉庫名稱 *", type: "text", required: true },
              { 
                name: "type", 
                label: "類型", 
                type: "select", 
                options: warehouseTypes.map(t => {
                  const typeMap: Record<string, string> = {
                    'raw_materials': '原料倉',
                    'finished_goods': '成品倉',
                    'semi_finished': '半成品倉',
                    'wip': '在製品倉',
                    'scrap': '廢品倉',
                    'return': '退貨倉',
                    'virtual': '虛擬倉',
                    'raw': '原料倉',
                    'finished': '成品倉'
                  };
                  return { value: t.typeCode, label: typeMap[t.typeName] || typeMap[t.typeCode] || t.typeName };
                }), 
                required: true 
              },
              { name: "address", label: "地址", type: "text" },
              { name: "isActive", label: "啟用狀態", type: "checkbox" }
            ]}
            onClose={() => { setShowAdd(false); setEditingWh(null); }}
            onSubmit={async (data) => {
              if (editingWh) {
                await warehousesApi.update(editingWh.id, data);
              } else {
                await warehousesApi.create(data);
              }
              loadWarehouses();
              onUpdate();
              setShowAdd(false);
              setEditingWh(null);
            }}
          />
        )}
      </div>
    </div>
  );
};

export default WarehouseManager;
