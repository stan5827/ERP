import React from 'react';
import { Truck } from 'lucide-react';

const ShippingPanel: React.FC = () => (
  <div className="glass-card no-lift" style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>
    <Truck size={48} style={{ marginBottom: 16, opacity: 0.2 }} />
    <h3>出貨作業模組 (施工中)</h3>
    <p>未來將整合銷售訂單 (SO) 一鍵轉換為出庫單</p>
  </div>
);

export default ShippingPanel;
