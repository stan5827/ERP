import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { completionOrdersApi } from '../../api/api';

const CompletionPanel: React.FC = () => {
  const [showAdd, setShowAdd] = useState(false);
  const queryClient = useQueryClient();
  const { data: compData } = useQuery({
    queryKey: ["completion-orders"],
    queryFn: () => completionOrdersApi.list().then(res => res.data)
  });
  const completions = Array.isArray(compData?.data) ? compData.data : [];

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
      <div style={{ display: "flex", justifyContent: "flex-end" }}>
        <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={14} /> 新增完工入庫</button>
      </div>
      <div className="glass-card no-lift">
        <table className="data-table">
          <thead>
            <tr><th>入庫單號</th><th>工單號碼</th><th>狀態</th><th>入庫倉庫</th><th>建立時間</th></tr>
          </thead>
          <tbody>
            {completions.map((c: any) => (
              <tr key={c.id}>
                <td className="mono" style={{ color: "var(--emerald-300)" }}>{c.completionNumber}</td>
                <td>{c.workOrderNumber || "-"}</td>
                <td>
                  <span className={`status-badge ${c.status === "completed" ? "status-active" : "status-neutral"}`}>
                    {c.status === "completed" ? "已完成" : "待收貨"}
                  </span>
                </td>
                <td>{c.warehouseName || "-"}</td>
                <td>{new Date(c.createdAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default CompletionPanel;
