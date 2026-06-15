import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, RefreshCw } from 'lucide-react';
import { inventoryTransfersApi, warehousesApi, skusApi } from '../../api/api';
import { FormModal } from '../../components/FormModal';

const TransferPanel: React.FC = () => {
  const [showAdd, setShowAdd] = useState(false);
  const queryClient = useQueryClient();
  const { data: transData } = useQuery({
    queryKey: ["inventory-transfers"],
    queryFn: () => inventoryTransfersApi.list().then(res => res.data)
  });
  const transfers = Array.isArray(transData?.data) ? transData.data : [];

  const { data: warehouseData } = useQuery({
    queryKey: ["warehouses"],
    queryFn: () => warehousesApi.list().then(res => res.data)
  });
  const warehouses = (warehouseData?.data || []).map((w: any) => ({ value: w.id, label: w.warehouseName }));

  const { data: skuData } = useQuery({
    queryKey: ["skus-simple"],
    queryFn: () => skusApi.list({ pageSize: "100" }).then(res => res.data)
  });
  const skus = (skuData?.data || []).map((s: any) => ({ value: s.id, label: `${s.skuNumber} - ${s.skuName}` }));

  const executeTransfer = useMutation({
    mutationFn: (id: string) => inventoryTransfersApi.execute(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] })
  });

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div style={{ display: "flex", justifyContent: "flex-end" }}>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={14} /> 新增調撥單</button>
      </div>
      <div className="glass-card no-lift">
        <table className="data-table">
          <thead>
            <tr><th>單號</th><th>狀態</th><th>來源倉庫</th><th>目標倉庫</th><th>建立時間</th><th>操作</th></tr>
          </thead>
          <tbody>
            {transfers.map((t: any) => (
              <tr key={t.id}>
                <td className="mono" style={{ color: "var(--cobalt-300)" }}>{t.transferNumber}</td>
                <td>
                  <span className={`status-badge ${t.status === "executed" ? "status-active" : "status-neutral"}`}>
                    {t.status === "executed" ? "已執行" : "待處理"}
                  </span>
                </td>
                <td>{t.sourceWarehouseName || "-"}</td>
                <td>{t.targetWarehouseName || "-"}</td>
                <td>{new Date(t.createdAt).toLocaleString()}</td>
                <td>
                  {t.status !== "executed" && (
                    <button className="icon-action-btn" onClick={() => executeTransfer.mutate(t.id)} title="執行調撥">
                      <RefreshCw size={13} />
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showAdd && (
        <FormModal 
          open={showAdd}
          title="新增倉庫調撥"
          fields={[
            { name: "sourceWarehouseId", label: "來源倉庫 *", type: "select", options: warehouses, required: true },
            { name: "targetWarehouseId", label: "目標倉庫 *", type: "select", options: warehouses, required: true },
            { name: "skuId", label: "品碼 *", type: "select", options: skus, required: true },
            { name: "transferQty", label: "調撥數量 *", type: "number", required: true },
            { name: "lotNumber", label: "批號", type: "text" }
          ]}
          onClose={() => setShowAdd(false)}
          onSubmit={async (data) => {
            const payload = {
              sourceWarehouseId: data.sourceWarehouseId,
              targetWarehouseId: data.targetWarehouseId,
              items: [{
                skuId: data.skuId,
                transferQty: data.transferQty,
                lotNumber: data.lotNumber
              }]
            };
            await inventoryTransfersApi.create(payload);
            queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
            setShowAdd(false);
          }}
        />
      )}
    </div>
  );
};

export default TransferPanel;
