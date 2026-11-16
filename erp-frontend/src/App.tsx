import React, { useState, Suspense, lazy } from 'react';
import { Sidebar } from './components/Sidebar';
import { Topbar } from './components/Topbar';
import { useLogin, useLogout, useCurrentUser } from './hooks/useApi';
import type { ModuleId } from './types';
import { Lock, User, Eye, EyeOff } from 'lucide-react';
import { Toaster } from 'react-hot-toast';
import { authApi } from './api/auth';
import { useRole } from './contexts/useRole';
import { resolveAccessibleModule } from './utils/accessibleModule';

const Dashboard = lazy(() => import('./pages/Dashboard').then(m => ({ default: m.Dashboard })));
const Manufacturing = lazy(() => import('./pages/Manufacturing').then(m => ({ default: m.Manufacturing })));
const Inventory = lazy(() => import('./pages/Inventory').then(m => ({ default: m.Inventory })));
const Sales = lazy(() => import('./pages/Sales').then(m => ({ default: m.Sales })));
const CustomersManagement = lazy(() => import('./pages/CustomersManagement').then(m => ({ default: m.CustomersManagement })));
const Procurement = lazy(() => import('./pages/Procurement').then(m => ({ default: m.Procurement })));
const HR = lazy(() => import('./pages/HR').then(m => ({ default: m.HR })));
const Finance = lazy(() => import('./pages/Finance').then(m => ({ default: m.Finance })));
const Assets = lazy(() => import('./pages/Assets').then(m => ({ default: m.Assets })));
const Budget = lazy(() => import('./pages/Budget').then(m => ({ default: m.Budget })));
const Quality = lazy(() => import('./pages/Quality').then(m => ({ default: m.Quality })));
const RD = lazy(() => import('./pages/RD').then(m => ({ default: m.RD })));
const ESG = lazy(() => import('./pages/ESG').then(m => ({ default: m.ESG })));
const AI = lazy(() => import('./pages/AI').then(m => ({ default: m.AI })));
const AgentFlows = lazy(() => import('./pages/AgentFlows').then(m => ({ default: m.AgentFlows })));
const Collaboration = lazy(() => import('./pages/Collaboration').then(m => ({ default: m.Collaboration })));
const Calendar = lazy(() => import('./pages/Calendar').then(m => ({ default: m.Calendar })));
const UomManagement = lazy(() => import('./pages/UomManagement').then(m => ({ default: m.UomManagement })));
const OrganizationManagement = lazy(() => import('./pages/OrganizationManagement').then(m => ({ default: m.OrganizationManagement })));
const CostCenterManagement = lazy(() => import('./pages/CostCenterManagement').then(m => ({ default: m.CostCenterManagement })));
const SubstituteManagement = lazy(() => import('./pages/SubstituteManagement').then(m => ({ default: m.SubstituteManagement })));
const MrpPlanning = lazy(() => import('./pages/MrpPlanning').then(m => ({ default: m.MrpPlanning })));
const ApsScheduling = lazy(() => import('./pages/ApsScheduling').then(m => ({ default: m.ApsScheduling })));
const MpsPlanning = lazy(() => import('./pages/MpsPlanning').then(m => ({ default: m.MpsPlanning })));
const RccpDashboard = lazy(() => import('./pages/RccpDashboard').then(m => ({ default: m.RccpDashboard })));
const TpmMaintenance = lazy(() => import('./pages/TpmMaintenance').then(m => ({ default: m.TpmMaintenance })));
const BarcodeSystem = lazy(() => import('./pages/BarcodeSystem').then(m => ({ default: m.BarcodeSystem })));
const KpiDashboard = lazy(() => import('./pages/KpiDashboard').then(m => ({ default: m.KpiDashboard })));
const ManufacturingEngineering = lazy(() => import('./pages/ManufacturingEngineering').then(m => ({ default: m.ManufacturingEngineering })));
const SupplierPortal = lazy(() => import('./pages/SupplierPortal').then(m => ({ default: m.SupplierPortal })));
const AiScheduling = lazy(() => import('./pages/AiScheduling').then(m => ({ default: m.AiScheduling })));
const EdiExport = lazy(() => import('./pages/EdiExport').then(m => ({ default: m.EdiExport })));
const AiDecision = lazy(() => import('./pages/AiDecision').then(m => ({ default: m.AiDecision })));
const ProfileSettings = lazy(() => import('./pages/profile').then(m => ({ default: m.ProfileSettings })));
const SystemSettings = lazy(() => import('./pages/SystemSettings').then(m => ({ default: m.SystemSettings })));
const Suppliers = lazy(() => import('./pages/Suppliers').then(m => ({ default: m.Suppliers })));
const WorkOrderCostReport = lazy(() => import('./pages/WorkOrderCostReport').then(m => ({ default: m.WorkOrderCostReport })));

function PageLoader() {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: 400 }}>
      <div style={{ 
        width: 40, height: 40, 
        border: '3px solid var(--slate-800)', 
        borderTopColor: 'var(--cobalt-400)', 
        borderRadius: '50%', 
        animation: 'spin 1s linear infinite' 
      }} />
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
    </div>
  );
}

const moduleLabels: Record<ModuleId, string> = {
  dashboard: '系統儀表板',
  sales: '銷貨及收款',
  customers: '客戶管理',
  procurement: '採購及付款',
  inventory: '庫存管理',
  manufacturing: '生產製造 · MES',
  hr: '人力資源',
  finance: '財務管理',
  assets: '固定資產',
  budget: '預算規劃',
  quality: '品質管理',
  rd: '研發管理',
  esg: 'ESG 永續管理',
  ai: 'AI Agent 監控',
  agentflows: 'Agent Flow',
  collaboration: '協作平台',
  calendar: '公司行事曆',
  uom: '單位量測管理',
  org: '組織架構',
  costcenter: '成本中心管理',
  substitute: '替代料管理',
  mrp: 'MRP 物料需求計劃',
  aps: 'APS 有限產能排程',
  engineering: '工藝設計',
        profile: '個人設定',
  mps: 'MPS 主生產排程',
  rccp: '產能瓶頸儀表板',
  tpm: '設備維護 TPM',
  barcode: '條碼系統',
  kpi: 'KPI 績效看板',
  supplierportal: '供應商入口',
  aischeduling: 'AI 智能排班',
  ediexport: 'EDI 匯出',
  aidecision: 'AI 決策助手',
  suppliers: '供應商管理',
  systemsettings: '系統設定',
  actual_costing: '實際成本分析結轉',
};

function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState('');
  const login = useLogin();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const res = await login.mutateAsync({ username, password });
      if (!res.success) setError(res.message || '登入失敗');
    } catch (error) {
      setError(error instanceof Error ? error.message : '登入失敗，請稍後再試');
    }
  };

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'linear-gradient(135deg, #04080f 0%, #0a1628 50%, #0d1f3c 100%)',
    }}>
      <div className="glass-card" style={{ width: 420, padding: '48px 40px' }}>
        <div style={{ textAlign: 'center', marginBottom: 36 }}>
          <div style={{ fontSize: 32, fontWeight: 800, color: 'var(--cobalt-300)', fontFamily: 'var(--font-display)', letterSpacing: '-0.02em' }}>
            ProMES ERP
          </div>
          <div style={{ fontSize: 13, color: 'var(--slate-400)', marginTop: 8 }}>
            製造業資源規劃系統 v1.0
          </div>
        </div>
        <form onSubmit={handleSubmit}>
          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.05em' }}>帳號</label>
            <div style={{ position: 'relative' }}>
              <User size={16} style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
              <input className="form-input" style={{ paddingLeft: 36, width: '100%' }} value={username} onChange={e => setUsername(e.target.value)} placeholder="請輸入帳號" required />
            </div>
          </div>
          <div style={{ marginBottom: 28 }}>
            <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6, textTransform: 'uppercase', letterSpacing: '0.05em' }}>密碼</label>
            <div style={{ position: 'relative' }}>
              <Lock size={16} style={{ position: 'absolute', left: 12, top: '50%', transform: 'translateY(-50%)', color: 'var(--slate-500)' }} />
              <input className="form-input" style={{ paddingLeft: 36, paddingRight: 36, width: '100%' }} type={showPw ? 'text' : 'password'} value={password} onChange={e => setPassword(e.target.value)} placeholder="請輸入密碼" required />
              <button type="button" onClick={() => setShowPw(!showPw)} style={{ position: 'absolute', right: 8, top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--slate-500)' }}>
                {showPw ? <EyeOff size={16} /> : <Eye size={16} />}
              </button>
            </div>
          </div>
          {error && <div style={{ padding: '10px 14px', background: 'rgba(199,31,61,0.15)', border: '1px solid rgba(199,31,61,0.3)', borderRadius: 8, color: 'var(--crimson-300)', fontSize: 13, marginBottom: 20 }}>{error}</div>}
          <button type="submit" className="btn btn-primary" style={{ width: '100%', padding: '12px' }} disabled={login.isPending}>
            {login.isPending ? '登入中...' : '登入系統'}
          </button>
        </form>
        <div style={{ textAlign: 'center', marginTop: 24, fontSize: 12, color: 'var(--slate-500)' }}>
          管理員初始帳密由部署環境設定；請洽系統管理員。
        </div>
      </div>
    </div>
  );
}

function App() {
  const [activeModule, setActiveModule] = useState<ModuleId>('dashboard');
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const currentUser = useCurrentUser();
  const logout = useLogout();
  const { currentRole, moduleAccess, isLoading: roleAccessLoading } = useRole();

  if (!currentUser) return <LoginPage />;
  if (currentUser.mustChangePassword)
    return <ChangePasswordPage username={currentUser.username} onComplete={() => {
      localStorage.removeItem('erp_token');
      localStorage.removeItem('erp_user');
    }} onLogout={logout} />;

  const isAdmin = currentRole === 'admin' ||
    currentUser.roles.some(role => role.toLowerCase() === 'admin');
  const resolvedModule = roleAccessLoading
    ? null
    : resolveAccessibleModule(activeModule, moduleAccess, isAdmin);
  const displayedModule = resolvedModule ?? activeModule;

  const renderPage = () => {
    if (roleAccessLoading)
      return <PageLoader />;
    if (!resolvedModule)
      return <div role="status" style={{ padding: 24 }}>此帳號尚未設定可使用的系統模組，請聯絡系統管理員。</div>;

    const pageElement = (() => {
      switch (resolvedModule) {
        case 'dashboard': return <Dashboard onNavigate={setActiveModule} />;
        case 'manufacturing': return <Manufacturing onNavigate={setActiveModule} />;
        case 'inventory': return <Inventory />;
        case 'sales': return <Sales />;
        case 'customers': return <CustomersManagement />;
        case 'procurement': return <Procurement />;
        case 'hr': return <HR />;
        case 'finance': return <Finance />;
        case 'assets': return <Assets />;
        case 'budget': return <Budget />;
        case 'quality': return <Quality />;
        case 'rd': return <RD />;
        case 'esg': return <ESG />;
        case 'ai': return <AI />;
        case 'agentflows': return <AgentFlows />;
        case 'systemsettings': return <SystemSettings />;
        case 'collaboration': return <Collaboration />;
        case 'calendar': return <Calendar />;
        case 'uom': return <UomManagement />;
        case 'org': return <OrganizationManagement />;
        case 'costcenter': return <CostCenterManagement />;
        case 'substitute': return <SubstituteManagement />;
        case 'mrp': return <MrpPlanning onNavigate={(m) => setActiveModule(m as ModuleId)} />;
        case 'aps': return <ApsScheduling onNavigate={(m) => setActiveModule(m as ModuleId)} />;
        case 'engineering': return <ManufacturingEngineering />;
        case 'mps': return <MpsPlanning onNavigate={(m) => setActiveModule(m as ModuleId)} />;
        case 'rccp': return <RccpDashboard />;
        case 'tpm': return <TpmMaintenance />;
        case 'barcode': return <BarcodeSystem />;
        case 'kpi': return <KpiDashboard />;
        case 'supplierportal': return <SupplierPortal />;
        case 'aischeduling': return <AiScheduling />;
        case 'ediexport': return <EdiExport />;
        case 'aidecision': return <AiDecision />;
        case 'suppliers': return <Suppliers />;
        case 'profile': return <ProfileSettings userId={currentUser.userId} />;
        case 'actual_costing': return <WorkOrderCostReport />;
        default: return <Dashboard onNavigate={setActiveModule} />;
      }

    })();

    return <Suspense fallback={<PageLoader />}>{pageElement}</Suspense>;
  };

  return (
    <div className="app-layout">
      <Toaster position="top-right" toastOptions={{ duration: 3000, style: { background: '#1e293b', color: '#fff', border: '1px solid rgba(255,255,255,0.1)' } }} />
      <Sidebar activeModule={displayedModule} onModuleChange={setActiveModule} collapsed={sidebarCollapsed} onCollapseToggle={() => setSidebarCollapsed(!sidebarCollapsed)} />
      <div className={`main-content ${sidebarCollapsed ? 'sidebar-collapsed' : ''}`} style={{ marginLeft: sidebarCollapsed ? 'var(--sidebar-collapsed)' : 'var(--sidebar-width)' }}>
        <Topbar
          user={{ id: currentUser.userId, name: currentUser.username, role: currentUser.roles[0] || 'user', department: '', avatar: undefined, permissions: [] }}
          notifications={[]}
          pageTitle={resolvedModule ? moduleLabels[resolvedModule] : roleAccessLoading ? '載入權限中' : '無可用模組'}
          pageBreadcrumb={displayedModule !== 'dashboard' ? '首頁' : undefined}
          onLogout={logout}
        />
        <main className="page-content" style={{ paddingTop: 'calc(var(--topbar-height) + 24px)' }}>
          {renderPage()}
        </main>
      </div>
    </div>
  );
}

function ChangePasswordPage({
  username,
  onComplete,
  onLogout,
}: {
  username: string;
  onComplete: () => void;
  onLogout: () => void;
}) {
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [isComplete, setIsComplete] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError('');

    if (newPassword.length < 12) {
      setError('新密碼至少需要 12 個字元');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('新密碼與確認密碼不一致');
      return;
    }

    setIsSaving(true);
    try {
      const response = await authApi.changePassword({
        oldPassword,
        newPassword,
      });
      if (!response.data.success) {
        setError(response.data.message || '變更密碼失敗');
        return;
      }
      onComplete();
      setIsComplete(true);
    } catch (changeError) {
      setError(changeError instanceof Error ? changeError.message : '變更密碼失敗');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'linear-gradient(135deg, #04080f 0%, #0a1628 50%, #0d1f3c 100%)',
    }}>
      <div className="glass-card" style={{ width: 420, padding: '40px' }}>
        {isComplete ? (
          <div role="status">
            <h2 style={{ marginBottom: 8 }}>密碼已更新</h2>
            <p style={{ color: 'var(--slate-400)', marginBottom: 24 }}>
              為使其他登入中的工作階段立即失效，請使用新密碼重新登入。
            </p>
            <button className="btn btn-primary" type="button" style={{ width: '100%' }} onClick={onLogout}>
              重新登入
            </button>
          </div>
        ) : (
          <>
            <h2 style={{ marginBottom: 8 }}>首次登入請變更密碼</h2>
            <p style={{ color: 'var(--slate-400)', marginBottom: 24 }}>帳號：{username}。請設定至少 12 個字元的新密碼。</p>
            <form onSubmit={handleSubmit}>
              <label style={{ display: 'block', marginBottom: 16 }}>
                目前密碼
                <input className="form-input" style={{ width: '100%' }} type="password" autoComplete="current-password" value={oldPassword} onChange={event => setOldPassword(event.target.value)} required />
              </label>
              <label style={{ display: 'block', marginBottom: 16 }}>
                新密碼
                <input className="form-input" style={{ width: '100%' }} type="password" autoComplete="new-password" minLength={12} value={newPassword} onChange={event => setNewPassword(event.target.value)} required />
              </label>
              <label style={{ display: 'block', marginBottom: 20 }}>
                確認新密碼
                <input className="form-input" style={{ width: '100%' }} type="password" autoComplete="new-password" minLength={12} value={confirmPassword} onChange={event => setConfirmPassword(event.target.value)} required />
              </label>
              {error && <div role="alert" style={{ color: 'var(--crimson-300)', marginBottom: 16 }}>{error}</div>}
              <button className="btn btn-primary" type="submit" style={{ width: '100%' }} disabled={isSaving}>
                {isSaving ? '儲存中...' : '變更密碼並重新登入'}
              </button>
              <button className="btn btn-ghost" type="button" style={{ width: '100%', marginTop: 10 }} onClick={onLogout}>
                登出
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
}

export default App;
