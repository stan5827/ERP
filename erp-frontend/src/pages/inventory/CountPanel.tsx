import React from 'react';
import { ClipboardList } from 'lucide-react';

const CountPanel: React.FC = () => (
  <div className="glass-card no-lift" style={{ padding: 40, textAlign: 'center', color: 'var(--slate-500)' }}>
    <ClipboardList size={48} style={{ marginBottom: 16, opacity: 0.2 }} />
    <h3>庫存盤點模組 (施工中)</h3>
    <p>支援盤點計畫建立、盤點差異過帳與庫存調整</p>
  </div>
);

export default CountPanel;
