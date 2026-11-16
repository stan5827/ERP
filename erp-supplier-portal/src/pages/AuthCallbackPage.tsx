import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from '../auth/oidc';
import { ErrorState, LoadingScreen } from '../components/Feedback';

export function AuthCallbackPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const callbackStarted = useRef(false);

  useEffect(() => {
    if (callbackStarted.current)
      return;
    callbackStarted.current = true;

    if (!userManager) {
      setError('Keycloak 尚未完成設定，請聯絡系統管理員。');
      return;
    }

    void userManager.signinRedirectCallback()
      .then(() => {
        navigate('/dashboard', { replace: true });
      })
      .catch((callbackError: unknown) => {
        console.error('OIDC sign-in callback failed.', callbackError);
        setError('登入驗證未完成或連結已失效，請返回登入頁重新操作。');
      });
  }, [navigate]);

  if (!error)
    return <LoadingScreen title="正在完成安全登入" />;

  return (
    <main className="feedback-page">
      <div className="feedback-card">
        <ErrorState message={error} />
        <button className="button button-primary full-width" onClick={() => navigate('/login', { replace: true })}>
          返回登入
        </button>
      </div>
    </main>
  );
}
