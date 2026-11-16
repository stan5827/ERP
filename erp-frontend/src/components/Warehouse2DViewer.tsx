import React, { useState } from 'react';

interface WarehouseZone {
  id: string;
  name: string;
  type: 'raw_material' | 'finished_goods' | 'production' | 'shipping';
  position: { x: number; y: number };
  size: { width: number; height: number };
  utilization: number;
  status: 'normal' | 'warning' | 'critical';
  currentStock: number;
  maxCapacity: number;
  items: number;
}

const initialZones: WarehouseZone[] = [
  { id: 'zone-1', name: '原料倉 A區', type: 'raw_material', position: { x: 0, y: 0 }, size: { width: 200, height: 150 }, utilization: 0.75, status: 'normal', currentStock: 1500, maxCapacity: 2000, items: 45 },
  { id: 'zone-2', name: '原料倉 B區', type: 'raw_material', position: { x: 220, y: 0 }, size: { width: 180, height: 150 }, utilization: 0.45, status: 'normal', currentStock: 900, maxCapacity: 2000, items: 32 },
  { id: 'zone-3', name: '成品倉 A區', type: 'finished_goods', position: { x: 420, y: 0 }, size: { width: 200, height: 150 }, utilization: 0.90, status: 'warning', currentStock: 1800, maxCapacity: 2000, items: 28 },
  { id: 'zone-4', name: '成品倉 B區', type: 'finished_goods', position: { x: 640, y: 0 }, size: { width: 180, height: 150 }, utilization: 0.30, status: 'normal', currentStock: 600, maxCapacity: 2000, items: 15 },
  { id: 'zone-5', name: '線邊倉', type: 'production', position: { x: 200, y: 170 }, size: { width: 150, height: 100 }, utilization: 0.65, status: 'normal', currentStock: 780, maxCapacity: 1200, items: 18 },
  { id: 'zone-6', name: '出貨區', type: 'shipping', position: { x: 420, y: 170 }, size: { width: 180, height: 100 }, utilization: 0.20, status: 'normal', currentStock: 200, maxCapacity: 1000, items: 5 },
];

export function Warehouse2DViewer() {
  const zones = initialZones;
  const [selectedZone, setSelectedZone] = useState<WarehouseZone | null>(null);
  const [viewMode, setViewMode] = useState<'layout' | 'utilization' | 'stock'>('layout');

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'critical': return '#ef4444';
      case 'warning': return '#f59e0b';
      default: return '#3b82f6';
    }
  };

  const getUtilizationColor = (utilization: number) => {
    if (utilization >= 0.9) return '#ef4444';
    if (utilization >= 0.7) return '#f59e0b';
    return '#10b981';
  };

  const totalCapacity = zones.reduce((sum, z) => sum + z.maxCapacity, 0);
  const totalStock = zones.reduce((sum, z) => sum + z.currentStock, 0);
  const avgUtilization = totalStock / totalCapacity;

  return (
    <div style={{ padding: 20, background: '#0f172a', minHeight: '100%' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h2 style={{ color: '#fff', margin: 0 }}>倉儲監控儀表板</h2>
        
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            onClick={() => setViewMode('layout')}
            style={{
              padding: '8px 16px',
              borderRadius: 6,
              border: 'none',
              background: viewMode === 'layout' ? '#3b82f6' : '#1e293b',
              color: '#fff',
              cursor: 'pointer'
            }}
          >
            配置圖
          </button>
          <button
            onClick={() => setViewMode('utilization')}
            style={{
              padding: '8px 16px',
              borderRadius: 6,
              border: 'none',
              background: viewMode === 'utilization' ? '#3b82f6' : '#1e293b',
              color: '#fff',
              cursor: 'pointer'
            }}
          >
            利用率
          </button>
          <button
            onClick={() => setViewMode('stock')}
            style={{
              padding: '8px 16px',
              borderRadius: 6,
              border: 'none',
              background: viewMode === 'stock' ? '#3b82f6' : '#1e293b',
              color: '#fff',
              cursor: 'pointer'
            }}
          >
            庫存
          </button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 300px', gap: 20 }}>
        <div style={{ position: 'relative', background: '#1e293b', borderRadius: 12, padding: 20, minHeight: 400 }}>
          <div style={{ position: 'relative', width: '100%', height: 300 }}>
            {zones.map(zone => (
              <div
                key={zone.id}
                onClick={() => setSelectedZone(zone)}
                style={{
                  position: 'absolute',
                  left: zone.position.x,
                  top: zone.position.y,
                  width: zone.size.width,
                  height: zone.size.height,
                  background: viewMode === 'utilization' 
                    ? getUtilizationColor(zone.utilization)
                    : viewMode === 'stock'
                      ? `${getStatusColor(zone.status)}33`
                      : '#334155',
                  border: `2px solid ${selectedZone?.id === zone.id ? '#fff' : getStatusColor(zone.status)}`,
                  borderRadius: 8,
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'center',
                  alignItems: 'center',
                  cursor: 'pointer',
                  transition: 'all 0.2s',
                  opacity: viewMode === 'stock' && zone.status === 'critical' ? 0.5 : 1
                }}
              >
                <div style={{ color: '#fff', fontWeight: 'bold', fontSize: 14, textAlign: 'center' }}>
                  {zone.name}
                </div>
                {viewMode === 'utilization' && (
                  <div style={{ color: '#fff', fontSize: 24, fontWeight: 'bold', marginTop: 8 }}>
                    {Math.round(zone.utilization * 100)}%
                  </div>
                )}
                {viewMode === 'stock' && (
                  <div style={{ color: '#94a3b8', fontSize: 12, marginTop: 4 }}>
                    {zone.currentStock} 件
                  </div>
                )}
                {viewMode === 'layout' && (
                  <div style={{ color: '#94a3b8', fontSize: 11, marginTop: 4 }}>
                    {zone.items} 種品號
                  </div>
                )}
              </div>
            ))}
          </div>

          <div style={{ display: 'flex', justifyContent: 'center', gap: 24, marginTop: 20 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <div style={{ width: 16, height: 16, background: '#10b981', borderRadius: 4 }} />
              <span style={{ color: '#94a3b8', fontSize: 12 }}>正常 (&lt;70%)</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <div style={{ width: 16, height: 16, background: '#f59e0b', borderRadius: 4 }} />
              <span style={{ color: '#94a3b8', fontSize: 12 }}>警告 (70-90%)</span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <div style={{ width: 16, height: 16, background: '#ef4444', borderRadius: 4 }} />
              <span style={{ color: '#94a3b8', fontSize: 12 }}>危急 (&gt;90%)</span>
            </div>
          </div>
        </div>

        <div style={{ background: '#1e293b', borderRadius: 12, padding: 20 }}>
          <h3 style={{ color: '#fff', marginTop: 0, marginBottom: 16 }}>總覽</h3>
          
          <div style={{ marginBottom: 20 }}>
            <div style={{ color: '#94a3b8', fontSize: 12, marginBottom: 4 }}>總庫存數</div>
            <div style={{ color: '#fff', fontSize: 28, fontWeight: 'bold' }}>
              {totalStock.toLocaleString()}
            </div>
            <div style={{ color: '#64748b', fontSize: 12 }}>件</div>
          </div>

          <div style={{ marginBottom: 20 }}>
            <div style={{ color: '#94a3b8', fontSize: 12, marginBottom: 4 }}>平均利用率</div>
            <div style={{ 
              height: 8, 
              background: '#334155', 
              borderRadius: 4, 
              overflow: 'hidden',
              marginBottom: 8
            }}>
              <div style={{ 
                width: `${avgUtilization * 100}%`, 
                height: '100%', 
                background: getUtilizationColor(avgUtilization)
              }} />
            </div>
            <div style={{ color: '#fff', fontSize: 20, fontWeight: 'bold' }}>
              {Math.round(avgUtilization * 100)}%
            </div>
          </div>

          <div style={{ marginBottom: 20 }}>
            <div style={{ color: '#94a3b8', fontSize: 12, marginBottom: 8 }}>各區狀態</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
              <div style={{ background: '#334155', padding: 12, borderRadius: 8 }}>
                <div style={{ color: '#94a3b8', fontSize: 11 }}>原料倉</div>
                <div style={{ color: '#10b981', fontSize: 18, fontWeight: 'bold' }}>正常</div>
              </div>
              <div style={{ background: '#334155', padding: 12, borderRadius: 8 }}>
                <div style={{ color: '#94a3b8', fontSize: 11 }}>成品倉</div>
                <div style={{ color: '#f59e0b', fontSize: 18, fontWeight: 'bold' }}>警告</div>
              </div>
              <div style={{ background: '#334155', padding: 12, borderRadius: 8 }}>
                <div style={{ color: '#94a3b8', fontSize: 11 }}>線邊倉</div>
                <div style={{ color: '#10b981', fontSize: 18, fontWeight: 'bold' }}>正常</div>
              </div>
              <div style={{ background: '#334155', padding: 12, borderRadius: 8 }}>
                <div style={{ color: '#94a3b8', fontSize: 11 }}>出貨區</div>
                <div style={{ color: '#10b981', fontSize: 18, fontWeight: 'bold' }}>正常</div>
              </div>
            </div>
          </div>

          {selectedZone && (
            <div style={{ borderTop: '1px solid #334155', paddingTop: 16 }}>
              <h4 style={{ color: '#fff', marginTop: 0 }}>{selectedZone.name}</h4>
              <div style={{ display: 'grid', gap: 8 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: '#94a3b8', fontSize: 12 }}>類型</span>
                  <span style={{ color: '#fff', fontSize: 12 }}>
                    {selectedZone.type === 'raw_material' ? '原料倉' : 
                     selectedZone.type === 'finished_goods' ? '成品倉' : 
                     selectedZone.type === 'production' ? '線邊倉' : '出貨區'}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: '#94a3b8', fontSize: 12 }}>品號數</span>
                  <span style={{ color: '#fff', fontSize: 12 }}>{selectedZone.items} 種</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: '#94a3b8', fontSize: 12 }}>庫存數</span>
                  <span style={{ color: '#fff', fontSize: 12 }}>{selectedZone.currentStock} 件</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: '#94a3b8', fontSize: 12 }}>利用率</span>
                  <span style={{ color: getUtilizationColor(selectedZone.utilization), fontSize: 12, fontWeight: 'bold' }}>
                    {Math.round(selectedZone.utilization * 100)}%
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default Warehouse2DViewer;