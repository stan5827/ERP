import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { inventoryApi } from '../../api/api';

const TransactionsPanel: React.FC = () => {
  const [search, setSearch] = useState("");
  const { data: transData } = useQuery({
    queryKey: ["inventory-transactions", search],
    queryFn: () => inventoryApi.transactions(search ? { search } : undefined).then(res => res.data)
  });
  const transactions = Array.isArray(transData?.data) ? transData.data : [];

  return (
    <div className="glass-card no-lift">
      <div style={{ padding: 16, borderBottom: "1px solid var(--glass-border)", display: "flex", gap: 12 }}>
        <div className="filter-search" style={{ flex: 1 }}>
          <Search size={14} style={{ color: "var(--slate-500)" }} />
          <input className="search-input" placeholder="搜尋交易單號、品碼..." value={search} onChange={e => setSearch(e.target.value)} />
        </div>
      </div>
      <table className="data-table">
        <thead>
          <tr>
            <th>日期</th>
            <th>類型</th>
            <th>品碼 / 品名</th>
            <th>數量</th>
            <th>倉庫 / 儲位</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((t: any) => (
            <tr key={t.id}>
              <td>{new Date(t.createdAt).toLocaleDateString()}</td>
              <td>
                <span className={`status-badge ${t.type === "in" || t.transactionType === "IN" ? "status-active" : "status-neutral"}`}>
                  {t.type === "in" || t.transactionType === "IN" ? "入庫" : "出庫"}
                </span>
              </td>
              <td>
                <div style={{ fontWeight: 600 }}>{t.skuNumber}</div>
                <div style={{ fontSize: 10, opacity: 0.6 }}>{t.skuName}</div>
              </td>
              <td className="mono" style={{ fontWeight: 700, color: (t.quantity || 0) > 0 ? "var(--emerald-400)" : "var(--crimson-400)" }}>
                {(t.quantity || 0) > 0 ? "+" : ""}{t.quantity}
              </td>
              <td>
                <div style={{ fontSize: 13 }}>{t.warehouseName}</div>
                <div style={{ fontSize: 11, color: "var(--cobalt-400)", fontFamily: "var(--font-mono)" }}>{t.binLocationCode || "-"}</div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default TransactionsPanel;
