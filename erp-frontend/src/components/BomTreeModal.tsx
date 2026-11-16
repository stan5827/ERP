import React, { useMemo, useState } from 'react';
import { X, ChevronRight, ChevronDown, Package, GitBranch, Loader2 } from 'lucide-react';
import type { BomDto, SkuDto } from '../api/api';

interface BomTreeItem {
  skuId: string;
  skuNumber: string;
  skuName: string;
  quantity: number;
  unit: string;
  children?: BomTreeItem[];
  level: number;
}

interface BomTreeModalProps {
  open: boolean;
  onClose: () => void;
  bom: BomDto;
  boms: BomDto[];
  skus: SkuDto[];
}

export function BomTreeModal({ open, onClose, bom, boms, skus }: BomTreeModalProps) {
  const [expandedState, setExpandedState] = useState<{
    rootSkuId: string;
    nodeIds: Set<string>;
  } | null>(null);
  const expandedNodes = bom && expandedState?.rootSkuId === bom.productSkuId
    ? expandedState.nodeIds
    : new Set(bom ? [bom.productSkuId] : []);
  const treeData = useMemo(() => {
    if (!open) return [];

    const buildTree = (bomData: BomDto, level = 0): BomTreeItem[] =>
      bomData.items.map(item => {
        const childBom = boms.find(candidate => candidate.productSkuId === item.childSkuId);
        const sku = skus.find(candidate => candidate.id === item.childSkuId);

        return {
          skuId: item.childSkuId,
          skuNumber: item.childSkuNumber || sku?.skuNumber || 'Unknown',
          skuName: item.childSkuName || sku?.skuName || 'Unknown',
          quantity: item.quantity,
          unit: item.unit || 'PCS',
          level,
          children: childBom ? buildTree(childBom, level + 1) : undefined,
        };
      });

    return [{
      skuId: bom.productSkuId,
      skuNumber: bom.productSkuNumber || 'Unknown',
      skuName: bom.productSkuName || 'Unknown',
      quantity: 1,
      unit: 'PCS',
      level: -1,
      children: buildTree(bom),
    }];
  }, [open, bom, boms, skus]);

  const toggleNode = (skuId: string) => {
    setExpandedState(prev => {
      const rootSkuId = bom.productSkuId;
      const next = new Set(prev?.rootSkuId === rootSkuId ? prev.nodeIds : [rootSkuId]);
      if (next.has(skuId)) {
        next.delete(skuId);
      } else {
        next.add(skuId);
      }
      return { rootSkuId, nodeIds: next };
    });
  };

  const renderTree = (items: BomTreeItem[], isRoot: boolean = false) => {
    return items.map((item, idx) => {
      const hasChildren = item.children && item.children.length > 0;
      const isExpanded = expandedNodes.has(item.skuId);
      const indent = (item.level + 1) * 24;

      return (
        <div key={`${item.skuId}-${idx}`} style={{ position: 'relative' }}>
          <div 
            className="tree-row"
            style={{
              display: 'flex',
              alignItems: 'center',
              padding: '10px 16px',
              paddingLeft: `${indent + 16}px`,
              borderBottom: '1px solid rgba(255,255,255,0.03)',
              background: isRoot 
                ? 'linear-gradient(90deg, rgba(59, 130, 246, 0.15) 0%, transparent 100%)' 
                : item.level % 2 === 0 ? 'rgba(255,255,255,0.01)' : 'transparent',
              fontSize: 13,
              position: 'relative',
              transition: 'all 0.2s ease',
              cursor: hasChildren ? 'pointer' : 'default'
            }}
            onClick={() => hasChildren && toggleNode(item.skuId)}
          >
            {/* 垂直引導線 */}
            {item.level >= 0 && (
              <div style={{
                position: 'absolute',
                left: `${indent - 12}px`,
                top: 0,
                bottom: item.children && isExpanded ? '50%' : 0,
                width: 1,
                background: 'rgba(255,255,255,0.1)'
              }} />
            )}
            {/* 水平引導線 */}
            {item.level >= 0 && (
              <div style={{
                position: 'absolute',
                left: `${indent - 12}px`,
                top: '50%',
                width: 12,
                height: 1,
                background: 'rgba(255,255,255,0.1)'
              }} />
            )}

            <div style={{ display: 'flex', alignItems: 'center', width: 24 }}>
              {hasChildren && (
                <div style={{ color: 'var(--slate-400)', display: 'flex' }}>
                  {isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                </div>
              )}
            </div>
            
            <div style={{ 
              width: 28, height: 28, borderRadius: 6, 
              background: isRoot ? 'rgba(59, 130, 246, 0.2)' : 'rgba(255,255,255,0.05)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              marginRight: 12,
              border: `1px solid ${isRoot ? 'rgba(59, 130, 246, 0.3)' : 'rgba(255,255,255,0.1)'}`
            }}>
              <Package size={14} style={{ color: item.level === -1 ? 'var(--cobalt-400)' : 'var(--emerald-400)' }} />
            </div>

            <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
              <span style={{ fontWeight: item.level === -1 ? 700 : 500, color: item.level === -1 ? '#fff' : 'var(--slate-200)' }}>
                {item.skuNumber}
              </span>
              <span style={{ fontSize: 11, color: 'var(--slate-500)' }}>{item.skuName}</span>
            </div>

            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <div style={{ 
                background: 'rgba(255,255,255,0.03)', 
                padding: '4px 10px', 
                borderRadius: 6, 
                border: '1px solid rgba(255,255,255,0.05)',
                fontSize: 12,
                fontFamily: 'monospace',
                color: 'var(--slate-300)'
              }}>
                <span style={{ color: 'var(--slate-500)', marginRight: 4 }}>x</span>
                {item.quantity} <span style={{ color: 'var(--slate-500)', fontSize: 10 }}>{item.unit}</span>
              </div>

              {hasChildren && (
                <div style={{ 
                  fontSize: 10, 
                  color: '#fff', 
                  background: 'var(--purple-600)', 
                  padding: '2px 8px', 
                  borderRadius: 20,
                  fontWeight: 600,
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>
                  {item.children?.length} 子件
                </div>
              )}
            </div>
          </div>
          {hasChildren && isExpanded && item.children && renderTree(item.children)}
        </div>
      );
    });
  };

  if (!open) return null;

  const totalItems = treeData.reduce((acc, item) => {
    const countChildren = (node: BomTreeItem): number => {
      let count = 1;
      if (node.children) {
        node.children.forEach(child => {
          count += countChildren(child);
        });
      }
      return count;
    };
    return acc + countChildren(item);
  }, 0);

  return (
    <div style={{
      position: 'fixed', inset: 0, zIndex: 1000,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'rgba(0,0,0,0.85)',
      backdropFilter: 'blur(8px)'
    }} onClick={onClose}>
      <div style={{
        background: 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)', 
        borderRadius: 16, 
        width: 800, 
        maxHeight: '85vh',
        border: '1px solid rgba(255,255,255,0.08)', 
        boxShadow: '0 0 0 1px rgba(255,255,255,0.05), 0 25px 50px -12px rgba(0,0,0,0.8)',
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column'
      }} onClick={e => e.stopPropagation()}>
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '20px 24px', 
          background: 'rgba(255,255,255,0.02)',
          borderBottom: '1px solid rgba(255,255,255,0.05)'
        }}>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
              <GitBranch size={18} style={{ color: 'var(--cobalt-400)' }} />
              <h3 style={{ margin: 0, fontSize: 18, color: '#fff', fontWeight: 700, letterSpacing: '-0.02em' }}>
                BOM 樹狀結構分析
              </h3>
            </div>
            <p style={{ margin: 0, fontSize: 13, color: 'var(--slate-400)' }}>
              主要產品：<span style={{ color: 'var(--cobalt-300)', fontWeight: 600 }}>{bom?.productSkuNumber}</span> • 架構包含 {totalItems} 個組成節點
            </p>
          </div>
          <button onClick={onClose} style={{ 
            background: 'rgba(255,255,255,0.05)', 
            border: 'none', 
            cursor: 'pointer', 
            color: 'var(--slate-400)',
            width: 32, height: 32, borderRadius: 8,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            transition: 'all 0.2s'
          }}>
            <X size={20} />
          </button>
        </div>
        <div style={{ flex: 1, overflow: 'auto', padding: '12px 0' }}>
          {treeData.length > 0 ? renderTree(treeData, true) : (
            <div style={{ padding: 60, textAlign: 'center' }}>
              <Loader2 className="animate-spin" style={{ margin: '0 auto 16px', color: 'var(--slate-600)' }} />
              <div style={{ color: 'var(--slate-500)', fontSize: 14 }}>正在解構物料清單...</div>
            </div>
          )}
        </div>
        
        <div style={{ 
          padding: '12px 24px', 
          background: 'rgba(0,0,0,0.2)', 
          borderTop: '1px solid rgba(255,255,255,0.05)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center'
        }}>
          <div style={{ display: 'flex', gap: 16 }}>
             <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--slate-500)' }}>
                <div style={{ width: 8, height: 8, borderRadius: 2, background: 'var(--cobalt-500)' }} /> 目標成品
             </div>
             <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--slate-500)' }}>
                <div style={{ width: 8, height: 8, borderRadius: 2, background: 'var(--emerald-500)' }} /> 組成零件
             </div>
          </div>
          <button className="btn-cy" onClick={onClose} style={{ padding: '6px 20px', fontSize: 12 }}>關閉預覽</button>
        </div>
      </div>
    </div>
  );
}
