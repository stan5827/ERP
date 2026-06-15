import React, { useState, useEffect, useMemo } from 'react';
import { X, Download, Maximize2, GitBranch, Share2, Layers } from 'lucide-react';

interface BomNode {
  skuId: string;
  skuNumber: string;
  skuName: string;
  quantity: number;
  unit: string;
  children?: BomNode[];
  isPhantom: boolean;
  scrapRate: number;
}

interface BomMermaidViewerProps {
  open: boolean;
  onClose: () => void;
  bom: any;
  boms: any[];
  skus: any[];
}

export function BomMermaidViewer({ open, onClose, bom, boms, skus }: BomMermaidViewerProps) {
  const [viewMode, setViewMode] = useState<'mermaid' | 'tree'>('mermaid');
  const [expandedNodes, setExpandedNodes] = useState<Set<string>>(new Set());
  const [mermaidSvg, setMermaidSvg] = useState<string>('');
  const [loading, setLoading] = useState(false);

  const buildBomTree = (bomData: any, level: number = 0): BomNode[] => {
    const items = bomData.items || [];
    return items.map((item: any) => {
      const childBom = boms.find((b: any) => b.productSkuId === item.childSkuId);
      const sku = skus.find((s: any) => s.id === item.childSkuId);
      return {
        skuId: item.childSkuId,
        skuNumber: item.childSkuNumber || sku?.skuNumber || 'Unknown',
        skuName: item.childSkuName || sku?.skuName || 'Unknown',
        quantity: item.quantity,
        unit: item.unit || 'PCS',
        isPhantom: item.isPhantom || false,
        scrapRate: item.scrapRate || 0,
        children: childBom ? buildBomTree(childBom, level + 1) : undefined
      };
    });
  };

  const bomTree = useMemo(() => {
    if (!bom) return null;
    return {
      skuId: bom.productSkuId,
      skuNumber: bom.productSkuNumber || 'Unknown',
      skuName: bom.productSkuName || 'Unknown',
      quantity: 1,
      unit: 'PCS',
      isPhantom: false,
      scrapRate: 0,
      children: buildBomTree(bom, 0)
    };
  }, [bom, boms, skus]);

  const generateMermaid = (node: BomNode, parentId: string | null = null, depth: number = 0): string => {
    const nodeId = `node_${node.skuId.replace(/-/g, '')}`;
    const nodeLabel = `${node.skuNumber}\n${node.quantity} ${node.unit}${node.isPhantom ? ' [P]' : ''}${node.scrapRate > 0 ? ` (${node.scrapRate}%)` : ''}`;
    
    let mermaid = '';
    
    if (parentId) {
      const edgeStyle = node.isPhantom ? '-->|"phantom"' : '-->';
      mermaid += `    ${parentId} ${edgeStyle} ${nodeId}\n`;
    }

    if (node.children && node.children.length > 0) {
      mermaid += `    ${nodeId}{{"${nodeLabel}"}}\n`;
      mermaid += `    subgraph ${nodeId}_subs["${node.skuNumber}"]\n`;
      node.children.forEach((child, idx) => {
        mermaid += generateMermaid(child, nodeId, depth + 1);
      });
      mermaid += `    end\n`;
    } else {
      const color = node.isPhantom ? '#94a3b8' : '#10b981';
      mermaid += `    ${nodeId}["${nodeLabel}"]\n`;
    }

    return mermaid;
  };

  const mermaidChart = useMemo(() => {
    if (!bomTree) return '';
    let chart = `flowchart TD\n`;
    chart += `    %% BOM Structure: ${bom.productSkuNumber || 'Product'}\n`;
    chart += `    subgraph root["<b>${bomTree.skuNumber}</b><br/>${bomTree.skuName}"]\n`;
    chart += `        direction TB\n`;
    chart += generateMermaid(bomTree);
    chart += `    end\n`;
    chart += `    classDef phantom fill:#64748b,stroke:#94a3b8,color:#fff\n`;
    chart += `    classDef component fill:#10b981,stroke:#34d399,color:#fff\n`;
    chart += `    classDef topLevel fill:#3b82f6,stroke:#60a5fa,color:#fff\n`;
    return chart;
  }, [bomTree, bom]);

  useEffect(() => {
    if (open && viewMode === 'mermaid') {
      renderMermaid();
    }
  }, [open, viewMode, mermaidChart]);

  const renderMermaid = async () => {
    setLoading(true);
    try {
      if (typeof window !== 'undefined') {
        const mermaid = (await import('mermaid')).default;
        mermaid.initialize({
          startOnLoad: false,
          theme: 'dark',
          flowchart: {
            useMaxWidth: true,
            htmlLabels: true,
            curve: 'basis',
            nodeSpacing: 50,
            rankSpacing: 80
          },
          themeVariables: {
            darkMode: true,
            background: '#0f172a',
            mainBkg: '#1e293b',
            primaryColor: '#3b82f6',
            primaryTextColor: '#f1f5f9',
            primaryBorderColor: '#60a5fa',
            lineColor: '#475569',
            secondaryColor: '#10b981',
            tertiaryColor: '#1e293b'
          }
        });
        
        const id = `mermaid-${Date.now()}`;
        const { svg } = await mermaid.render(id, mermaidChart);
        setMermaidSvg(svg);
      }
    } catch (err) {
      console.error('Mermaid render error:', err);
      setMermaidSvg('');
    } finally {
      setLoading(false);
    }
  };

  const toggleNode = (skuId: string) => {
    setExpandedNodes(prev => {
      const next = new Set(prev);
      if (next.has(skuId)) {
        next.delete(skuId);
      } else {
        next.add(skuId);
      }
      return next;
    });
  };

  const renderTreeView = (node: BomNode, depth: number = 0): React.ReactNode => {
    const hasChildren = node.children && node.children.length > 0;
    const isExpanded = expandedNodes.has(node.skuId);
    const indent = depth * 24;

    return (
      <div key={node.skuId}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            padding: '8px 16px',
            paddingLeft: `${indent + 16}px`,
            borderBottom: '1px solid rgba(255,255,255,0.03)',
            background: depth === 0 ? 'rgba(59,130,246,0.1)' : 'transparent',
            fontSize: 13,
            cursor: hasChildren ? 'pointer' : 'default',
            transition: 'background 0.2s'
          }}
          onClick={() => hasChildren && toggleNode(node.skuId)}
        >
          {hasChildren && (
            <span style={{ marginRight: 8, color: 'var(--slate-400)' }}>
              {isExpanded ? '▼' : '▶'}
            </span>
          )}
          {!hasChildren && <span style={{ width: 24 }} />}
          
          <span style={{
            width: 24, height: 24, borderRadius: 6,
            background: node.isPhantom ? 'rgba(148,163,184,0.2)' : 'rgba(16,185,129,0.2)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            marginRight: 12,
            fontSize: 11, fontWeight: 700,
            color: node.isPhantom ? '#94a3b8' : '#10b981'
          }}>
            {depth === 0 ? 'P' : 'C'}
          </span>

          <div style={{ flex: 1 }}>
            <span style={{ fontWeight: depth === 0 ? 700 : 500 }}>{node.skuNumber}</span>
            <span style={{ marginLeft: 8, color: 'var(--slate-500)', fontSize: 12 }}>{node.skuName}</span>
          </div>

          <span style={{
            background: 'rgba(255,255,255,0.05)',
            padding: '4px 10px',
            borderRadius: 6,
            fontFamily: 'monospace',
            fontSize: 12,
            color: 'var(--slate-300)'
          }}>
            ×{node.quantity} {node.unit}
            {node.scrapRate > 0 && <span style={{ color: 'var(--amber-400)' }}> ({node.scrapRate}%)</span>}
            {node.isPhantom && <span style={{ color: '#94a3b8' }}> [Phantom]</span>}
          </span>
        </div>
        {hasChildren && isExpanded && node.children?.map(child => renderTreeView(child, depth + 1))}
      </div>
    );
  };

  const downloadSvg = () => {
    if (mermaidSvg) {
      const blob = new Blob([mermaidSvg], { type: 'image/svg+xml' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `BOM_${bom?.productSkuNumber || 'chart'}.svg`;
      a.click();
      URL.revokeObjectURL(url);
    }
  };

  if (!open) return null;

  const countNodes = (node: BomNode): number => {
    let count = 1;
    if (node.children) {
      node.children.forEach(child => {
        count += countNodes(child);
      });
    }
    return count;
  };

  const totalNodes = bomTree ? countNodes(bomTree) : 0;

  return (
    <div style={{
      position: 'fixed', inset: 0, zIndex: 1000,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'rgba(0,0,0,0.9)',
      backdropFilter: 'blur(12px)'
    }} onClick={onClose}>
      <div style={{
        background: 'linear-gradient(135deg, #1e293b 0%, #0f172a 100%)',
        borderRadius: 20,
        width: '90vw',
        maxWidth: 1400,
        height: '85vh',
        border: '1px solid rgba(255,255,255,0.1)',
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
          <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <GitBranch size={20} style={{ color: 'var(--cobalt-400)' }} />
              <h3 style={{ margin: 0, fontSize: 18, color: '#fff', fontWeight: 700 }}>
                BOM 結構視覺化分析
              </h3>
            </div>
            <div style={{
              display: 'flex',
              background: 'rgba(255,255,255,0.05)',
              borderRadius: 10,
              padding: 4,
              gap: 4
            }}>
              <button
                style={{
                  padding: '6px 14px',
                  borderRadius: 8,
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: 12,
                  fontWeight: 600,
                  transition: 'all 0.2s',
                  background: viewMode === 'mermaid' ? 'var(--cobalt-600)' : 'transparent',
                  color: viewMode === 'mermaid' ? '#fff' : 'var(--slate-400)'
                }}
                onClick={() => setViewMode('mermaid')}
              >
                流程圖
              </button>
              <button
                style={{
                  padding: '6px 14px',
                  borderRadius: 8,
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: 12,
                  fontWeight: 600,
                  transition: 'all 0.2s',
                  background: viewMode === 'tree' ? 'var(--cobalt-600)' : 'transparent',
                  color: viewMode === 'tree' ? '#fff' : 'var(--slate-400)'
                }}
                onClick={() => setViewMode('tree')}
              >
                樹狀圖
              </button>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <div style={{ fontSize: 13, color: 'var(--slate-400)' }}>
              <span style={{ color: 'var(--cobalt-300)', fontWeight: 600 }}>{bom?.productSkuNumber}</span>
              <span style={{ marginLeft: 8 }}>•</span>
              <span style={{ marginLeft: 8 }}>{totalNodes} ��節點</span>
            </div>
            {viewMode === 'mermaid' && (
              <button
                onClick={downloadSvg}
                style={{
                  padding: '8px 14px',
                  borderRadius: 8,
                  border: '1px solid rgba(255,255,255,0.1)',
                  background: 'rgba(255,255,255,0.05)',
                  color: 'var(--slate-300)',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: 6,
                  fontSize: 12,
                  fontWeight: 600,
                  transition: 'all 0.2s'
                }}
              >
                <Download size={14} /> 匯出 SVG
              </button>
            )}
            <button
              onClick={onClose}
              style={{
                width: 36, height: 36,
                borderRadius: 10,
                border: 'none',
                cursor: 'pointer',
                background: 'rgba(255,255,255,0.05)',
                color: 'var(--slate-400)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                transition: 'all 0.2s'
              }}
            >
              <X size={20} />
            </button>
          </div>
        </div>

        <div style={{ flex: 1, overflow: 'auto', padding: 24 }}>
          {viewMode === 'mermaid' ? (
            <div style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              justifyContent: 'center',
              minHeight: '100%'
            }}>
              {loading ? (
                <div style={{ textAlign: 'center', color: 'var(--slate-400)' }}>
                  <div className="animate-spin" style={{ marginBottom: 16 }}>⟳</div>
                  正在渲染流程圖...
                </div>
              ) : mermaidSvg ? (
                <div
                  dangerouslySetInnerHTML={{ __html: mermaidSvg }}
                  style={{
                    width: '100%',
                    overflow: 'auto',
                    display: 'flex',
                    justifyContent: 'center'
                  }}
                />
              ) : (
                <div style={{ textAlign: 'center', color: 'var(--slate-400)' }}>
                  無法渲染 Mermaid 圖表
                </div>
              )}
            </div>
          ) : (
            <div style={{ maxWidth: 1000, margin: '0 auto' }}>
              {bomTree && renderTreeView(bomTree)}
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
          <div style={{ display: 'flex', gap: 16, fontSize: 11, color: 'var(--slate-500)' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <div style={{ width: 8, height: 8, borderRadius: 2, background: 'var(--cobalt-500)' }} />
              目標成品
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <div style={{ width: 8, height: 8, borderRadius: 2, background: 'var(--emerald-500)' }} />
              組成零件
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <div style={{ width: 8, height: 8, borderRadius: 2, background: 'var(--slate-500)' }} />
              虛擬件
            </div>
          </div>
          <button
            className="btn-cy"
            onClick={onClose}
            style={{ padding: '6px 20px', fontSize: 12 }}
          >
            關閉預覽
          </button>
        </div>
      </div>
    </div>
  );
}