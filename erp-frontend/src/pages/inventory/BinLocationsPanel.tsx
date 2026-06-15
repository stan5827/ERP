import React, { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Search, Plus, Edit2, Settings, Warehouse } from 'lucide-react';
import { binLocationsApi, warehousesApi } from '../../api/api';
import { FormModal } from '../../components/FormModal';

interface BinLocationsPanelProps {
  showAddBin: boolean;
  setShowAddBin: (v: boolean) => void;
  setShowAddWh: (v: boolean) => void;
  setShowWhTypes: (v: boolean) => void;
  setShowWhManager: (v: boolean) => void;
}

const BinLocationsPanel: React.FC<BinLocationsPanelProps> = ({ 
  showAddBin, 
  setShowAddBin, 
  setShowAddWh, 
  setShowWhTypes, 
  setShowWhManager 
}) => {
  const [selectedWh, setSelectedWh] = useState<string>("");
  const [search, setSearch] = useState("");
  const [editingBin, setEditingBin] = useState<any>(null);
  const queryClient = useQueryClient();

  const { data: warehouseData } = useQuery({
    queryKey: ["warehouses"],
    queryFn: () => warehousesApi.list().then(res => res.data)
  });

  const { data: binData, refetch } = useQuery({
    queryKey: ["bin-locations", selectedWh],
    queryFn: () => binLocationsApi.list(selectedWh ? { warehouseId: selectedWh } : undefined).then(res => res.data)
  });

  const warehouses = Array.isArray(warehouseData?.data) ? warehouseData.data.filter((w: any) => w.isActive === true) : [];
  
  useEffect(() => {
    if (selectedWh && warehouses.length > 0 && !warehouses.some(w => w.id === selectedWh)) {
      setSelectedWh("");
    }
  }, [warehouses, selectedWh]);

  const bins = Array.isArray(binData?.data) ? binData.data : [];

  const filteredBins = bins.filter(b => {
    const matchesSearch = b.locationCode.toLowerCase().includes(search.toLowerCase());
    const matchesWh = !selectedWh || b.warehouseId === selectedWh;
    return matchesSearch && matchesWh;
  });

  const createBin = useMutation({
    mutationFn: (data: any) => binLocationsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bin-locations"] });
      setShowAddBin(false);
      refetch();
    }
  });

  const updateBin = useMutation({
    mutationFn: (data: any) => binLocationsApi.update(editingBin.id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bin-locations"] });
      setEditingBin(null);
      refetch();
    }
  });

  const toggleActive = useMutation({
    mutationFn: (bin: any) => binLocationsApi.update(bin.id, { ...bin, isActive: !bin.isActive }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["bin-locations"] });
      refetch();
    }
  });

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
      <div style={{ display: "flex", justifyContent: "flex-start", alignItems: "center", flexWrap: "wrap", gap: 12 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
          <div className="filter-search" style={{ minWidth: 220 }}>
            <Search size={14} style={{ color: "var(--slate-400)" }} />
            <input className="search-input" placeholder="搜尋儲位代碼..." value={search} onChange={e => setSearch(e.target.value)} />
          </div>
          <select className="form-input" style={{ width: 160, background: "var(--slate-800)", border: "1px solid var(--slate-700)" }} value={selectedWh} onChange={e => setSelectedWh(e.target.value)}>
            <option value="">所有倉庫</option>
            {warehouses.map((w: any) => <option key={w.id} value={w.id}>{w.warehouseName}</option>)}
          </select>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginLeft: 'auto' }}>
          <button 
            onClick={() => setShowAddBin(true)} 
            className="btn btn-primary btn-sm"
            style={{ background: "linear-gradient(135deg, var(--emerald-500), var(--emerald-600))", border: 'none' }}
          >
            <Plus size={16} /> 新增儲位
          </button>
          <button 
            onClick={() => setShowAddWh(true)} 
            className="btn btn-primary btn-sm"
          >
            <Plus size={16} /> 新增倉庫
          </button>
          <button 
            onClick={() => setShowWhTypes(true)} 
            className="btn btn-ghost btn-sm"
            style={{ color: "var(--emerald-400)", background: "rgba(16,185,129,0.08)", border: "1px solid rgba(16,185,129,0.2)" }}
          >
            <Settings size={14} /> 類型維護
          </button>
          <button 
            onClick={() => setShowWhManager(true)} 
            className="btn btn-ghost btn-sm"
            style={{ color: "var(--cobalt-400)", background: "rgba(59,130,246,0.08)", border: "1px solid rgba(59,130,246,0.2)" }}
          >
            <Warehouse size={14} /> 倉庫管理
          </button>
        </div>
      </div>

      <div className="glass-card no-lift" style={{ overflow: "hidden" }}>
        <table className="data-table">
          <thead>
            <tr><th>倉庫</th><th>儲位代碼</th><th>區域</th><th>通道</th><th>貨架</th><th>層</th><th>位置</th><th>狀態</th><th>操作</th></tr>
          </thead>
          <tbody>
            {filteredBins.map((bin: any) => (
              <tr key={bin.id}>
                <td style={{ color: "var(--slate-300)" }}>{warehouses.find(w => w.id === bin.warehouseId)?.warehouseName || "-"}</td>
                <td className="mono" style={{ color: "var(--cobalt-300)", fontWeight: 600 }}>{bin.locationCode}</td>
                <td style={{ color: "var(--slate-400)" }}>{bin.zone || "-"}</td>
                <td style={{ color: "var(--slate-400)" }}>{bin.aisle || "-"}</td>
                <td style={{ color: "var(--slate-400)" }}>{bin.rack || "-"}</td>
                <td style={{ color: "var(--slate-400)" }}>{bin.level || "-"}</td>
                <td style={{ color: "var(--slate-400)" }}>{bin.position || "-"}</td>
                <td>
                  <button 
                    onClick={() => toggleActive.mutate(bin)}
                    className={`status-badge ${bin.isActive ? "status-active" : "status-neutral"}`}
                    style={{ cursor: "pointer", border: "none", outline: "none" }}
                    title={bin.isActive ? "點擊停用" : "點擊啟用"}
                  >
                    {bin.isActive ? "啟用" : "停用"}
                  </button>
                </td>
                <td>
                  <button 
                    className="icon-action-btn" 
                    onClick={() => setEditingBin(bin)}
                    title="編輯儲位"
                  >
                    <Edit2 size={13} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {(showAddBin || editingBin) && (
        <FormModal 
          open={!!(showAddBin || editingBin)} 
          title={editingBin ? `編輯儲位: ${editingBin.locationCode}` : "新增儲位"}
          initialValues={editingBin}
          fields={[
            { name: "warehouseId", label: "倉庫 *", type: "select", options: warehouses.filter((w: any) => w.isActive || w.id === editingBin?.warehouseId).map((w: any) => ({ value: w.id, label: w.warehouseName })), required: true },
            { name: "locationCode", label: "儲位代碼 *", type: "text", required: true },
            { name: "zone", label: "區域", type: "text" },
            { name: "aisle", label: "通道", type: "text" },
            { name: "rack", label: "貨架", type: "text" },
            { name: "level", label: "層", type: "text" },
            { name: "position", label: "位置", type: "text" },
            { name: "maxWeight", label: "最大負重 (kg)", type: "number" },
            { name: "isActive", label: "啟用狀態", type: "checkbox", placeholder: "啟用此儲位" }
          ]}
          onClose={() => { setShowAddBin(false); setEditingBin(null); }}
          onSubmit={async (data) => {
            if (editingBin) {
              await updateBin.mutateAsync(data);
            } else {
              await createBin.mutateAsync(data);
            }
          }}
        />
      )}
    </div>
  );
};

export default BinLocationsPanel;
