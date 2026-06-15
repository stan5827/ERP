import React, { useState, useEffect } from 'react';
import { Database, Plus, Edit2, Trash2 } from 'lucide-react';
import { uomsApi } from '../../api/api';
import { FormModal } from '../../components/FormModal';
import { ConfirmDialog, useConfirmDialog } from '../../components/ConfirmDialog';

interface UomManagerProps {
  isOpen: boolean;
  onClose: () => void;
  onUpdate: () => void;
}

const UomManager: React.FC<UomManagerProps> = ({ isOpen, onClose, onUpdate }) => {
  const [uoms, setUoms] = useState<any[]>([]);
  const [showAdd, setShowAdd] = useState(false);
  const [editingUom, setEditingUom] = useState<any>(null);
  const { isOpen: isConfirmOpen, title: confirmTitle, message: confirmMsg, onConfirm, confirmVariant, confirm: openConfirm, close: closeConfirm, confirmText, cancelText } = useConfirmDialog();

  const loadUoms = async () => {
    const res = await uomsApi.list();
    setUoms(res.data?.data || []);
  };

  useEffect(() => {
    if (isOpen) loadUoms();
  }, [isOpen]);

  const handleDelete = (id: string) => {
    openConfirm("刪除確認", "確定要刪除此單位嗎？", async () => {
      try {
        await uomsApi.delete(id);
        loadUoms();
        onUpdate();
        closeConfirm();
      } catch (err: any) {
        alert("刪除失敗: " + err.message);
        closeConfirm();
      }
    }, { confirmVariant: 'danger', confirmText: '確定刪除' });
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" style={{ zIndex: 1100 }}>
      <div className="modal-content" style={{ width: '600px', maxHeight: '80vh', overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        <div className="modal-header" style={{ padding: '16px 20px', borderBottom: '1px solid var(--glass-border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <h3 style={{ margin: 0, fontSize: 18, fontWeight: 700, color: 'var(--slate-100)', display: 'flex', alignItems: 'center', gap: 10 }}>
             <Database size={20} className="text-cobalt-400" /> 計量單位管理
          </h3>
          <button className="btn-action-primary btn-sm" onClick={() => setShowAdd(true)}>
             <Plus size={14} /> 新增單位
          </button>
        </div>
        
        <div style={{ flex: 1, overflowY: 'auto', padding: '0' }}>
          <table className="inv-table">
            <thead>
              <tr>
                <th>代碼</th>
                <th>名稱</th>
                <th>類型</th>
                <th>啟用</th>
                <th style={{ textAlign: "right" }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {uoms.map(u => (
                <tr key={u.id}>
                  <td className="mono">{u.uomCode}</td>
                  <td>{u.uomName}</td>
                  <td>
                     <span className="status-badge status-neutral" style={{ fontSize: 10 }}>
                        {u.uomType === 'base' ? '基本單位' : '轉換單位'}
                     </span>
                  </td>
                  <td><span className={`status-badge ${u.isActive ? "status-active" : "status-neutral"}`}>{u.isActive ? "啟用" : "停用"}</span></td>
                  <td style={{ textAlign: "right" }}>
                    <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
                      <button className="icon-action-btn" onClick={() => setEditingUom(u)}><Edit2 size={13} /></button>
                      <button className="icon-action-btn text-red-400" onClick={() => handleDelete(u.id)}><Trash2 size={13} /></button>
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

        {(showAdd || editingUom) && (
          <FormModal
            open={!!(showAdd || editingUom)}
            title={editingUom ? "編輯單位" : "新增單位"}
            initialValues={editingUom || { isActive: true, uomType: 'base', decimalPlaces: 2 }}
            fields={[
              { name: "uomCode", label: "單位代碼 *", type: "text", required: true, placeholder: '例: PCS, KG, BOX' },
              { name: "uomName", label: "單位名稱 *", type: "text", required: true, placeholder: '例: 個, 公斤, 箱' },
              { 
                name: "uomType", 
                label: "單位類型", 
                type: "select", 
                options: [
                  { value: 'base', label: '基本單位' },
                  { value: 'convert', label: '轉換單位' }
                ],
                required: true 
              },
              { name: "decimalPlaces", label: "小數位數", type: "number" },
              { name: "isActive", label: "啟用狀態", type: "checkbox" }
            ]}
            onClose={() => { setShowAdd(false); setEditingUom(null); }}
            onSubmit={async (data) => {
              if (editingUom) {
                await uomsApi.update(editingUom.id, data);
              } else {
                await uomsApi.create(data);
              }
              loadUoms();
              onUpdate();
              setShowAdd(false);
              setEditingUom(null);
            }}
          />
        )}

        <ConfirmDialog
          isOpen={isConfirmOpen}
          title={confirmTitle}
          message={confirmMsg}
          confirmText={confirmText}
          cancelText={cancelText}
          confirmVariant={confirmVariant}
          onConfirm={onConfirm || (() => {})}
          onCancel={closeConfirm}
        />
      </div>
    </div>
  );
};

export default UomManager;
