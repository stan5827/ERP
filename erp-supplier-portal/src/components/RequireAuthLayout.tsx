import { useEffect, useState } from 'react';
import { Building2, CircleHelp, ClipboardCheck, LayoutDashboard, LogOut, PackageCheck, ReceiptText, WalletCards } from 'lucide-react';
import { Link, NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { portalApi } from '../api/client';
import type { SupplierPortalSession } from '../api/types';
import { ErrorState, LoadingScreen } from './Feedback';

const navItems = [
  { to: '/dashboard', label: '總覽', icon: LayoutDashboard },
  { to: '/rfqs', label: '詢價與報價', icon: ReceiptText },
  { to: '/purchase-orders', label: '採購訂單', icon: PackageCheck },
  { to: '/ap-submissions', label: '請款送審', icon: WalletCards },
  { to: '/capa', label: '客訴與 CAPA', icon: ClipboardCheck },
];

export function RequireAuthLayout() {
  const { signOut } = useAuth();
  const [session, setSession] = useState<SupplierPortalSession | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function loadSession() {
    setLoading(true);
    setError(null);
    try {
      await portalApi.completeInvitation();
      const currentSession = await portalApi.getSession();
      setSession(currentSession);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : '無法載入供應商帳號資訊。');
    } finally {
      setLoading(false);
    }
  }

  async function handleSignOut() {
    try {
      await signOut();
    } catch {
      setError('無法完成 Keycloak 登出，請關閉此分頁並聯絡系統管理員。');
    }
  }

  useEffect(() => {
    void loadSession();
  }, []);

  if (loading)
    return <LoadingScreen title="正在安全地載入供應商入口" />;

  if (error || !session) {
    return (
      <main className="feedback-page">
        <div className="feedback-card">
          <ErrorState message={error || '目前無法取得供應商帳號資訊。'} onRetry={() => void loadSession()} />
          <button className="button button-primary full-width" onClick={() => void handleSignOut()}>
            返回登入
          </button>
        </div>
      </main>
    );
  }

  return (
    <div className="portal-shell">
      <aside className="sidebar">
        <Link className="brand" to="/dashboard" aria-label="供應商入口首頁">
          <span className="brand-mark"><Building2 size={20} /></span>
          <span><strong>ProMES</strong><small>供應商協作入口</small></span>
        </Link>
        <div className="supplier-identity">
          <span className="identity-label">目前登入供應商</span>
          <strong>{session.supplierName}</strong>
          <span>{session.supplierCode}</span>
        </div>
        <nav className="primary-nav" aria-label="主要導覽">
          {navItems.map(({ to, label, icon: Icon }) => (
            <NavLink key={to} to={to} className={({ isActive }) => `nav-link${isActive ? ' is-active' : ''}`}>
              <Icon size={18} aria-hidden="true" />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="help-note"><CircleHelp size={17} /><span>需要協助？<small>請聯絡您的採購窗口</small></span></div>
          <button
            className="nav-link signout-link"
            onClick={() => void handleSignOut()}
          >
            <LogOut size={18} aria-hidden="true" /><span>登出</span>
          </button>
        </div>
      </aside>
      <div className="portal-main">
        <header className="topbar">
          <div className="topbar-mobile-brand"><span className="brand-mark"><Building2 size={18} /></span>供應商入口</div>
          <span className="secure-label"><span className="secure-dot" />安全連線</span>
          <div className="account-chip">
            <span className="avatar">{(session.contactName || session.username).slice(0, 1)}</span>
            <span><strong>{session.contactName || session.username}</strong><small>{session.role}</small></span>
          </div>
        </header>
        <main className="page-content"><Outlet context={{ session }} /></main>
      </div>
    </div>
  );
}
