import React, { useEffect, useMemo, useState } from 'react';
import { CheckCircle, Clock, ExternalLink, RefreshCw, ShoppingCart } from 'lucide-react';
import type { PurchaseOrderDto } from '../api/api';
import { supplierPortalProcurementApi } from '../api/procurementApi';

interface SupplierPoListProps {
  supplierId: string;
}

export function SupplierPoList({ supplierId }: SupplierPoListProps) {
  const [pos, setPos] = useState<PurchaseOrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [error, setError] = useState('');

  const loadPurchaseOrders = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await supplierPortalProcurementApi.purchaseOrders();
      if (res.data.success) {
        setPos(res.data.data || []);
      } else {
        setError(res.data.message || '載入採購訂單失敗');
      }
    } catch (err) {
      console.error('Failed to load purchase orders', err);
      setError('載入採購訂單失敗');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadPurchaseOrders();
  }, [supplierId]);

  const recentPos = useMemo(() => {
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - 30);
    return pos.filter(po => new Date(po.poDate) >= cutoff);
  }, [pos]);

  const handleConfirm = async (id: string) => {
    setConfirmingId(id);
    try {
      const res = await supplierPortalProcurementApi.confirmPurchaseOrder(id);
      if (res.data.success) {
        await loadPurchaseOrders();
      } else {
        setError(res.data.message || '確認採購訂單失敗');
      }
    } catch (err) {
      console.error('Failed to confirm purchase order', err);
      setError('確認採購訂單失敗');
    } finally {
      setConfirmingId(null);
    }
  };

  return (
    <div className="glass-card page-enter" style={{ padding: 20 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h3 style={{ fontWeight: 600 }}>採購訂單 (PO)</h3>
        <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span style={{ fontSize: 12, color: 'var(--slate-500)' }}>顯示最近 30 天內訂單</span>
          <button className="btn btn-ghost btn-xs" onClick={loadPurchaseOrders} disabled={loading}>
            <RefreshCw size={12} /> 重新整理
          </button>
        </div>
      </div>

      {error && (
        <div style={{ marginBottom: 16, padding: 12, borderRadius: 8, background: 'rgba(239, 68, 68, 0.08)', color: 'var(--red-400)', border: '1px solid rgba(239, 68, 68, 0.18)' }}>
          {error}
        </div>
      )}

      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--navy-700)' }}>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>PO 編號</th>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>日期</th>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>總金額</th>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>狀態</th>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>品項</th>
              <th style={{ textAlign: 'left', padding: '12px', fontSize: 12, color: 'var(--slate-400)' }}>操作</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={6} style={{ padding: '32px 12px', textAlign: 'center', color: 'var(--slate-500)' }}>
                  載入中...
                </td>
              </tr>
            ) : recentPos.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ padding: '48px 12px', textAlign: 'center', color: 'var(--slate-500)' }}>
                  <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10 }}>
                    <ShoppingCart size={36} style={{ opacity: 0.3 }} />
                    <span>最近 30 天沒有採購訂單</span>
                  </div>
                </td>
              </tr>
            ) : recentPos.map(po => {
              const isConfirmed = po.status?.toLowerCase() === 'confirmed';
              const itemCount = po.items?.length || 0;
              return (
                <tr key={po.id} style={{ borderBottom: '1px solid var(--navy-800)' }}>
                  <td style={{ padding: '12px', fontWeight: 600, color: 'var(--cobalt-300)' }}>{po.poNumber}</td>
                  <td style={{ padding: '12px', fontSize: 13 }}>{new Date(po.poDate).toLocaleDateString('zh-TW')}</td>
                  <td style={{ padding: '12px', fontSize: 13 }}>${Number(po.totalAmount || 0).toLocaleString()}</td>
                  <td style={{ padding: '12px' }}>
                    <span style={{ 
                      padding: '2px 8px', 
                      borderRadius: 4, 
                      fontSize: 11, 
                      background: isConfirmed ? 'rgba(16, 185, 129, 0.1)' : 'rgba(245, 158, 11, 0.1)',
                      color: isConfirmed ? 'var(--emerald-400)' : 'var(--amber-400)',
                      border: `1px solid ${isConfirmed ? 'rgba(16, 185, 129, 0.2)' : 'rgba(245, 158, 11, 0.2)'}`
                    }}>
                      {isConfirmed ? '已確認' : '待確認'}
                    </span>
                  </td>
                  <td style={{ padding: '12px', fontSize: 13 }}>{itemCount}</td>
                  <td style={{ padding: '12px' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      {!isConfirmed && (
                        <button
                          className="btn btn-primary btn-xs"
                          onClick={() => handleConfirm(po.id)}
                          disabled={confirmingId === po.id}
                        >
                          <CheckCircle size={12} /> {confirmingId === po.id ? '確認中...' : '確認'}
                        </button>
                      )}
                      <button className="btn btn-ghost btn-xs" style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
                        <ExternalLink size={12} /> 查看明細
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
