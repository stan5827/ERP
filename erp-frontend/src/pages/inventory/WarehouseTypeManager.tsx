import React, { useState, useEffect } from 'react';
import { Search, Plus, X } from 'lucide-react';
import { warehouseTypesApi, WarehouseTypeDto } from '../../api/api';
import { FormModal } from '../../components/FormModal';

interface WarehouseTypeManagerProps {
  open: boolean;
  onClose: () => void;
  onUpdate: () => void;
}

const WarehouseTypeManager: React.FC<WarehouseTypeManagerProps> = ({ open, onClose, onUpdate }) => {
  const [types, setTypes] = useState<WarehouseTypeDto[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingType, setEditingType] = useState<WarehouseTypeDto | null>(null);
  const [search, setSearch] = useState("");
  const loadTypes = async () => {
    const res = await warehouseTypesApi.list();
    setTypes(res.data?.data || []);
  };

  useEffect(() => {
    if (!open) return;
    (async () => {
      const r = await warehouseTypesApi.list();
      setTypes(r.data?.data || []);
      setSearch("");
    })();
  }, [open]);

  const handleDelete = async (id: string) => {
    if (window.confirm("確定要刪除此類型嗎？")) {
      await warehouseTypesApi.delete(id);
      loadTypes();
      onUpdate();
    }
  };

  const filteredTypes = types.filter(t => t.typeName?.toLowerCase().includes(search.toLowerCase()) || t.typeCode?.toLowerCase().includes(search.toLowerCase()));

  if (!open) return null;

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-card" style={{ maxWidth: 560, width: '90%', maxHeight: '80vh', display: 'flex', flexDirection: 'column' }}>
        <div className="modal-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '16px 20px', borderBottom: '1px solid var(--glass-border)' }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, color: 'var(--slate-100)' }}>倉庫類型維護</h3>
          <button className="icon-action-btn" onClick={onClose}><X size={18} /></button>
        </div>
        
        <div style={{ padding: '12px 20px', borderBottom: '1px solid var(--glass-border)', display: 'flex', gap: 12, alignItems: 'center' }}>
          <div className="filter-search" style={{ flex: 1 }}>
            <Search size={14} style={{ color: "var(--slate-500)" }} />
            <input className="search-input" placeholder="搜尋類型..." value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <button className="btn btn-primary btn-sm" onClick={() => { 
            setShowAdd(true); 
          }}>
            <Plus size={16} /> 新增
          </button>
        </div>
        
        <div className="modal-body" style={{ padding: '12px 20px', overflowY: 'auto', flex: 1 }}>
          <table className="data-table">
            <thead>
              <tr><th>代碼</th><th>名稱</th><th>排序</th><th>狀態</th><th style={{ textAlign: "right" }}>操作</th></tr>
            </thead>
            <tbody>
              {filteredTypes.map(t => (
                <tr key={t.id}>
                  <td><code style={{ background: 'rgba(59,130,246,0.1)', color: 'var(--cobalt-400)', padding: '2px 6px', borderRadius: '4px', fontSize: 12 }}>{t.typeCode}</code></td>
                  <td style={{ color: 'var(--slate-200)' }}>{t.typeName}</td>
                  <td style={{ color: 'var(--slate-400)' }}>{t.sortOrder}</td>
                  <td><span className={`status-badge ${t.isActive ? "status-active" : "status-neutral"}`}>{t.isActive ? "啟用" : "停用"}</span></td>
                  <td style={{ textAlign: "right" }}>
                    <div className="flex justify-end gap-2">
                      <button className="btn btn-ghost btn-sm" onClick={() => setEditingType(t)}>編輯</button>
                      <button className="btn btn-ghost btn-sm text-red-400" onClick={() => handleDelete(t.id)}>刪除</button>
                    </div>
                  </td>
                </tr>
              ))}
              {filteredTypes.length === 0 && (
                <tr>
                  <td colSpan={5} style={{ textAlign: "center", padding: "32px", color: "var(--slate-500)" }}>
                    暫無資料
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        <div className="modal-footer" style={{ padding: '12px 20px', borderTop: '1px solid var(--glass-border)', display: 'flex', justifyContent: 'flex-end' }}>
           <button className="btn btn-ghost btn-sm" onClick={onClose}>關閉</button>
        </div>

        {(showAdd || editingType) && (
          <FormModal
            open={!!(showAdd || editingType)}
            title={editingType ? "編輯類型" : "新增類型"}
            initialValues={editingType || { isActive: true, sortOrder: 0 }}
            fields={[
              { name: "typeCode", label: "類型代碼 *", type: "text", required: true },
              { name: "typeName", label: "類型名稱 *", type: "text", required: true },
              { name: "description", label: "描述", type: "text" },
              { name: "sortOrder", label: "排序", type: "number" },
              { name: "isActive", label: "啟用狀態", type: "checkbox" }
            ]}
            onClose={() => { setShowAdd(false); setEditingType(null); }}
            onSubmit={async (data) => {
              if (editingType) {
                await warehouseTypesApi.update(editingType.id, data);
              } else {
                await warehouseTypesApi.create(data);
              }
              loadTypes();
              onUpdate();
              setShowAdd(false);
              setEditingType(null);
            }}
          />
        )}
      </div>
    </div>
  );
};

export default WarehouseTypeManager;
