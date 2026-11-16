import React, { useState } from 'react';
import {
  LayoutDashboard, ShoppingCart, Truck, Package, Factory,
  ShieldCheck, Users, Landmark, Building2, PieChart,
  FlaskConical, Leaf, BrainCircuit, MessageSquare,
  HelpCircle,
  Scale, Network, DollarSign, GitBranch, CalendarDays,
  ClipboardList, CalendarClock, Map, Wrench, Scan,
  Link, Clock, FileOutput, Lightbulb, Moon, Sun, Activity,
  type LucideIcon
} from 'lucide-react';
import type { ModuleId } from '../types';
import { useTheme } from '../theme/themeContext';
import { useRole } from '../contexts/useRole';
import { useSidebarBadges } from '../hooks/useApi';
import './Sidebar.css';

const iconMap: Record<string, LucideIcon> = {
  LayoutDashboard, ShoppingCart, Truck, Package, Factory,
  ShieldCheck, Users, Landmark, Building2, PieChart,
  FlaskConical, Leaf, BrainCircuit, MessageSquare, Scale, Network, DollarSign, GitBranch, CalendarDays,
  ClipboardList, CalendarClock, Map, Wrench, Scan,
  Link, Clock, FileOutput, Lightbulb, Activity,
};

interface NavGroup {
  label: string;
  items: {
    id: ModuleId;
    label: string;
    icon: string;
    badge?: number;
    group: string;
  }[];
}

interface SidebarProps {
  activeModule: ModuleId;
  onModuleChange: (id: ModuleId) => void;
  collapsed: boolean;
  onCollapseToggle: () => void;
}

const navGroups: NavGroup[] = [
  {
    label: '0. 總覽與監控',
    items: [
      { id: 'dashboard', label: '全廠戰情室', icon: 'LayoutDashboard', group: 'main' },
      { id: 'kpi', label: 'KPI 指標看板', icon: 'PieChart', group: 'main' },
    ],
  },
  {
    label: '1. 主資料與基礎',
    items: [
      { id: 'systemsettings', label: '系統與權限', icon: 'Settings', group: 'master' },
      { id: 'uom', label: '單位量測', icon: 'Scale', group: 'master' },
      { id: 'engineering', label: '工藝與 BOM', icon: 'Wrench', group: 'master' },
      { id: 'org', label: '組織架構', icon: 'Network', group: 'master' },
    ],
  },
  {
    label: '2. 銷售管理',
    items: [
      { id: 'sales', label: '銷售與 CRM', icon: 'ShoppingCart', badge: 3, group: 'sales' },
    ],
  },
  {
    label: '客商管理',
    items: [
      { id: 'customers', label: '客戶管理', icon: 'Building2', group: 'sales' },
      { id: 'suppliers', label: '供應商管理', icon: 'Users', group: 'procurement' },
    ],
  },
  {
    label: '3. 生產計劃 (MPS)',
    items: [
      { id: 'mps', label: 'MPS 主排程', icon: 'Map', group: 'planning' },
      { id: 'rccp', label: 'RCCP 粗產能', icon: 'Activity', group: 'planning' },
    ],
  },
  {
    label: '4. 物料計畫 (MRP)',
    items: [
      { id: 'mrp', label: 'MRP 需求規劃', icon: 'ClipboardList', group: 'planning' },
    ],
  },
  {
    label: '5. 採購供應鏈',
    items: [
      { id: 'procurement', label: '採購管理', icon: 'Truck', badge: 5, group: 'procurement' },
      { id: 'supplierportal', label: '供應商入口', icon: 'Link', group: 'procurement' },
    ],
  },
  {
    label: '6. 庫存物流',
    items: [
      { id: 'inventory', label: '庫存管理', icon: 'Package', group: 'inventory' },
      { id: 'barcode', label: '條碼系統', icon: 'Scan', group: 'inventory' },
    ],
  },
  {
    label: '7. 生產執行',
    items: [
      { id: 'manufacturing', label: '生產工單', icon: 'Factory', group: 'manufacturing' },
      { id: 'aps', label: 'APS 細排程', icon: 'CalendarClock', group: 'manufacturing' },
      { id: 'tpm', label: '設備維護 TPM', icon: 'Wrench', group: 'manufacturing' },
    ],
  },
  {
    label: '8. 財務與預算',
    items: [
      { id: 'finance', label: '財務會計', icon: 'Landmark', group: 'finance' },
      { id: 'budget', label: '預算規劃', icon: 'PieChart', group: 'finance' },
      { id: 'actual_costing', label: '實際成本結轉', icon: 'FileOutput', group: 'finance' },
      { id: 'costcenter', label: '成本中心', icon: 'DollarSign', group: 'finance' },
    ],
  },
  {
    label: '9. 人力資源',
    items: [
      { id: 'hr', label: '人事薪資', icon: 'Users', badge: 7, group: 'hr' },
    ],
  },
  {
    label: '10. 品質管理',
    items: [
      { id: 'quality', label: '品質與稽核', icon: 'ShieldCheck', group: 'quality' },
    ],
  },
  {
    label: '11. AI 智能應用',
    items: [
      { id: 'ai', label: 'AI 監控中心', icon: 'BrainCircuit', group: 'digital' },
      { id: 'agentflows', label: 'Agent Flow', icon: 'GitBranch', group: 'digital' },
      { id: 'aidecision', label: 'AI 決策助手', icon: 'Lightbulb', group: 'digital' },
      { id: 'aischeduling', label: 'AI 智能排班', icon: 'Clock', group: 'digital' },
    ],
  },
];

export const Sidebar: React.FC<SidebarProps> = ({
  activeModule,
  onModuleChange,
  collapsed,
}) => {
  const [, setHoveredItem] = useState<string | null>(null);
  const { themeName, setTheme } = useTheme();
  const { hasModule, isLoading: roleLoading } = useRole();
  const { data: badgesData } = useSidebarBadges();
  const badges = badgesData?.data || {
    salesCount: 0,
    procurementCount: 0,
    hrCount: 0,
    inventoryCount: 0,
    manufacturingCount: 0,
    qualityCount: 0
  };

  const getBadgeValue = (id: string, staticBadge?: number) => {
    switch (id) {
      case 'sales': return badges.salesCount;
      case 'procurement': return badges.procurementCount;
      case 'hr': return badges.hrCount;
      case 'inventory': return badges.inventoryCount;
      case 'manufacturing': return badges.manufacturingCount;
      case 'quality': return badges.qualityCount;
      default: return staticBadge;
    }
  };

  const themeOptions = [
    { id: 'modern-dark', name: '現代深色', icon: Moon },
    { id: 'professional-light', name: '淺色專業', icon: Sun },
    { id: 'high-contrast', name: '高對比', icon: Moon }
  ] as const;

  const handleThemeSelect = (themeId: Parameters<typeof setTheme>[0]) => {
    setTheme(themeId);
  };

  const isModuleAccessible = (moduleId: string): boolean => {
    if (roleLoading) return false;
    if (moduleId === 'customers') return hasModule('sales');
    if (moduleId === 'suppliers') return hasModule('suppliers');
    return hasModule(moduleId);
  };

  return (
    <aside
      className="sidebar"
      style={{
        width: collapsed ? 'var(--sidebar-collapsed)' : 'var(--sidebar-width)',
      }}
    >
      {/* Logo 區塊 */}
      <div className="sidebar-logo">
        <div className="sidebar-logo-icon">
          <svg width="22" height="22" viewBox="0 0 22 22" fill="none">
            <polygon
              points="11,2 20,7 20,15 11,20 2,15 2,7"
              fill="none"
              stroke="#d4af37"
              strokeWidth="1.5"
            />
            <polygon
              points="11,6 16,9 16,13 11,16 6,13 6,9"
              fill="#d4af37"
              opacity="0.6"
            />
            <circle cx="11" cy="11" r="2" fill="#f3cf65" />
          </svg>
        </div>
        {!collapsed && (
          <div className="sidebar-logo-text">
            <div className="sidebar-logo-brand">ProMES</div>
            <div className="sidebar-logo-sub">製造業ERP系統</div>
          </div>
        )}
      </div>

      {/* 導覽清單 */}
      <nav className="sidebar-nav">
        {navGroups.map((group) => {
          const accessibleItems = group.items.filter(item => isModuleAccessible(item.id));
          if (accessibleItems.length === 0) return null;
          
          return (
            <div key={group.label} className="sidebar-nav-group">
              {!collapsed && (
                <div className="sidebar-group-label">{group.label}</div>
              )}

              {accessibleItems.map((item) => {
              const Icon = iconMap[item.icon];
              const isActive = activeModule === item.id;
              return (
                <button
                  key={item.id}
                  className={`sidebar-nav-item ${isActive ? 'active' : ''}`}
                  onClick={() => onModuleChange(item.id)}
                  onMouseEnter={() => setHoveredItem(item.id)}
                  onMouseLeave={() => setHoveredItem(null)}
                  title={collapsed ? item.label : undefined}
                >
                  <span className="sidebar-nav-icon">
                    {Icon && <Icon size={16} />}
                  </span>
                  {!collapsed && (
                    <span className="sidebar-nav-label">{item.label}</span>
                  )}
                  {getBadgeValue(item.id, item.badge) !== undefined && (getBadgeValue(item.id, item.badge) ?? 0) > 0 && (
                    <span className={`sidebar-badge ${collapsed ? 'collapsed' : ''}`}>
                      {getBadgeValue(item.id, item.badge)}
                    </span>
                  )}
                  {isActive && <span className="sidebar-active-indicator" />}
                </button>
              );
            })}
            </div>
          );
        })}
        
        {/* 主題選項 */}
        {!collapsed && (
          <div className="sidebar-nav-group">
            <div className="sidebar-group-label">顯示設定</div>
            {themeOptions.map(option => {
              const isActive = themeName === option.id;
              return (
                <button
                  key={`theme-${option.id}`}
                  className={`sidebar-nav-item ${isActive ? 'active' : ''}`}
                  onClick={() => handleThemeSelect(option.id)}
                  onMouseEnter={() => setHoveredItem(`theme-${option.id}`)}
                  onMouseLeave={() => setHoveredItem(null)}
                >
                  <span className="sidebar-nav-icon">
                    <option.icon size={16} />
                  </span>
                  <span className="sidebar-nav-label">{option.name}</span>
                  {isActive && <span className="sidebar-active-indicator" />}
                </button>
              );
            })}
          </div>
        )}
      </nav>

{/* 底部工具 */}
      <div className="sidebar-footer">
        {!collapsed && (
          <>
            <button className="sidebar-footer-btn" disabled title="功能開發中" style={{ opacity: 0.5, cursor: 'not-allowed' }}>
              <HelpCircle size={15} />
              <span>說明文件</span>
            </button>
          </>
        )}
      </div>
    </aside>
  );
};
         
