import React, { useEffect, useState } from 'react';
import { Truck, Calendar, Box, Hash, Save, X } from 'lucide-react';
import { api, type PurchaseOrderDto } from '../api/api';
import { supplierPortalProcurementApi } from '../api/procurementApi';

interface SupplierAsnCreateProps {
  supplierId: string;
  onSuccess: () => void;
  onCancel: () => void;
}

export function SupplierAsnCreate({ supplierId, onSuccess, onCancel }: SupplierAsnCreateProps) {
  const [formData, setFormData] = useState({
    purchaseOrderId: '',
    expectedArrivalDate: '',
    carrierName: '',
    trackingNumber: '',
    remarks: ''
  });
  const [items, setItems] = useState([{ skuNumber: '', skuName: '', shippedQty: 1, batchNo: '' }]);
  const [purchaseOrders, setPurchaseOrders] = useState<PurchaseOrderDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const loadPurchaseOrders = async () => {
      try {
        const res = await supplierPortalProcurementApi.purchaseOrders();
        if (res.data.success) {
          setPurchaseOrders(res.data.data || []);
        }
      } catch (error) {
        console.error('Failed to load supplier purchase orders', error);
      }
    };

    loadPurchaseOrders();
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      const payload = {
        supplierId,
        purchaseOrderId: formData.purchaseOrderId || undefined,
        expectedArrivalDate: formData.expectedArrivalDate,
        carrierName: formData.carrierName || undefined,
        trackingNumber: formData.trackingNumber || undefined,
        remarks: formData.remarks || undefined,
        notes: formData.remarks || undefined,
        items: items
          .filter(item => item.skuNumber.trim() && item.skuName.trim())
          .map(item => ({
            skuNumber: item.skuNumber.trim(),
            skuName: item.skuName.trim(),
            shippedQty: Number(item.shippedQty) || 0,
            batchNo: item.batchNo.trim() || undefined,
          })),
      };

      const res = await supplierPortalProcurementApi.createAsnLegacy(payload);
      if (res.data.success) {
        onSuccess();
      } else {
        alert(res.data.message || '建立失敗');
      }
    } catch (err) {
      alert('建立 ASN 失敗');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="glass-card page-enter" style={{ padding: 24, maxWidth: 600, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <Truck size={20} className="text-cobalt-400" />
          <h3 style={{ fontWeight: 700, margin: 0 }}>建立送貨通知 (ASN)</h3>
        </div>
        <button onClick={onCancel} className="icon-action-btn"><X size={18} /></button>
      </div>

      <form onSubmit={handleSubmit}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>採購單編號</label>
            <div style={{ position: 'relative' }}>
              <Hash size={14} style={{ position: 'absolute', left: 10, top: 10, color: 'var(--slate-500)' }} />
              <select
                className="form-input"
                style={{ width: '100%', paddingLeft: 32 }}
                value={formData.purchaseOrderId}
                onChange={e => setFormData({ ...formData, purchaseOrderId: e.target.value })}
              >
                <option value="">請選擇採購單</option>
                {purchaseOrders.map(po => (
                  <option key={po.id} value={po.id}>
                    {po.poNumber}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>預計到貨日</label>
            <div style={{ position: 'relative' }}>
              <Calendar size={14} style={{ position: 'absolute', left: 10, top: 10, color: 'var(--slate-500)' }} />
              <input
                type="date"
                className="form-input"
                style={{ width: '100%', paddingLeft: 32 }}
                value={formData.expectedArrivalDate}
                onChange={e => setFormData({ ...formData, expectedArrivalDate: e.target.value })}
                required
              />
            </div>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 16 }}>
          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>物流商名稱</label>
            <div style={{ position: 'relative' }}>
              <Truck size={14} style={{ position: 'absolute', left: 10, top: 10, color: 'var(--slate-500)' }} />
              <input
                className="form-input"
                style={{ width: '100%', paddingLeft: 32 }}
                placeholder="例如: 順豐, 敦豪..."
                value={formData.carrierName}
                onChange={e => setFormData({ ...formData, carrierName: e.target.value })}
              />
            </div>
          </div>
          <div>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>物流追蹤單號</label>
            <div style={{ position: 'relative' }}>
              <Box size={14} style={{ position: 'absolute', left: 10, top: 10, color: 'var(--slate-500)' }} />
              <input
                className="form-input"
                style={{ width: '100%', paddingLeft: 32 }}
                placeholder="Tracking Number"
                value={formData.trackingNumber}
                onChange={e => setFormData({ ...formData, trackingNumber: e.target.value })}
              />
            </div>
          </div>
        </div>

        <div style={{ marginBottom: 16 }}>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>出貨品項</label>
          <div style={{ display: 'grid', gap: 12 }}>
            {items.map((item, index) => (
              <div key={index} style={{ display: 'grid', gridTemplateColumns: '1.1fr 1.2fr 0.6fr 0.8fr', gap: 12 }}>
                <input
                  className="form-input"
                  placeholder="SKU 編號"
                  value={item.skuNumber}
                  onChange={e => {
                    const next = [...items];
                    next[index].skuNumber = e.target.value;
                    setItems(next);
                  }}
                />
                <input
                  className="form-input"
                  placeholder="品名"
                  value={item.skuName}
                  onChange={e => {
                    const next = [...items];
                    next[index].skuName = e.target.value;
                    setItems(next);
                  }}
                />
                <input
                  className="form-input"
                  type="number"
                  min={1}
                  placeholder="數量"
                  value={item.shippedQty}
                  onChange={e => {
                    const next = [...items];
                    next[index].shippedQty = Number(e.target.value) || 0;
                    setItems(next);
                  }}
                />
                <input
                  className="form-input"
                  placeholder="批號"
                  value={item.batchNo}
                  onChange={e => {
                    const next = [...items];
                    next[index].batchNo = e.target.value;
                    setItems(next);
                  }}
                />
              </div>
            ))}
          </div>
          <div style={{ marginTop: 10 }}>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setItems(prev => [...prev, { skuNumber: '', skuName: '', shippedQty: 1, batchNo: '' }])}
            >
              新增品項
            </button>
          </div>
        </div>

        <div style={{ marginBottom: 24 }}>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>備註</label>
          <textarea
            className="form-input"
            style={{ width: '100%', minHeight: 80, padding: 12 }}
            placeholder="其他送貨資訊..."
            value={formData.remarks}
            onChange={e => setFormData({ ...formData, remarks: e.target.value })}
          />
        </div>

        <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
          <button type="button" onClick={onCancel} className="btn btn-secondary">取消</button>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            <Save size={16} /> {loading ? '處理中...' : '提交送貨通知'}
          </button>
        </div>
      </form>
    </div>
  );
}
