import { ArrowRight, ShieldCheck } from 'lucide-react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';
import { ErrorState } from '../components/Feedback';
import { useState } from 'react';

export function LoginPage() {
  const { user, signIn, isLoading, configurationError } = useAuth();
  const [error, setError] = useState<string | null>(null);
  const [isRedirecting, setIsRedirecting] = useState(false);

  if (user && !isLoading)
    return <Navigate to="/dashboard" replace />;

  async function beginSignIn() {
    setError(null);
    setIsRedirecting(true);
    try {
      await signIn();
    } catch (signInError) {
      setError(signInError instanceof Error ? signInError.message : '目前無法連線至身分驗證服務。');
      setIsRedirecting(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-layout">
        <div className="login-story">
          <div className="brand login-brand"><span className="brand-mark"><ShieldCheck size={21} /></span><span><strong>ProMES</strong><small>供應商協作入口</small></span></div>
          <div className="login-story-copy">
            <span className="eyebrow">SUPPLIER COLLABORATION</span>
            <h1>讓合作，<br />從清楚開始。</h1>
            <p>在安全、專屬的工作空間中，掌握詢價與訂單進度，讓每一次協作都有跡可循。</p>
          </div>
          <div className="login-trust"><ShieldCheck size={18} /><span>由企業身分服務保護您的帳號</span></div>
        </div>
        <div className="login-panel">
          <span className="eyebrow">WELCOME BACK</span>
          <h2>登入供應商入口</h2>
          <p>使用您收到邀請時設定的帳號登入。</p>
          {configurationError && <ErrorState message={configurationError} />}
          {error && <ErrorState message={error} />}
          <button
            className="button button-primary login-button"
            onClick={() => void beginSignIn()}
            disabled={Boolean(configurationError) || isLoading || isRedirecting}
          >
            {isRedirecting ? '正在前往安全登入…' : '使用公司帳號登入'}
            {!isRedirecting && <ArrowRight size={18} />}
          </button>
          <div className="login-security"><ShieldCheck size={16} /><span>登入將由公司安全驗證服務處理，Portal 不會保存您的密碼。</span></div>
          <div className="login-help">尚未收到邀請或無法登入？<br /><span>請聯絡貴公司採購窗口。</span></div>
        </div>
      </section>
    </main>
  );
}
