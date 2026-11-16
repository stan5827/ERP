import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from '../auth/oidc';
import { ErrorState, LoadingScreen } from '../components/Feedback';

export function LogoutCallbackPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    if (!userManager) {
      navigate('/login', { replace: true });
      return;
    }

    void userManager.signoutRedirectCallback()
      .then(() => {
        if (active)
          navigate('/login', { replace: true });
      })
      .catch(() => {
        if (active)
          setError('已離開登入流程，但未能完成 Keycloak 登出確認。請關閉此視窗並重新登入。');
      });

    return () => {
      active = false;
    };
  }, [navigate]);

  if (!error)
    return <LoadingScreen title="正在完成安全登出" />;

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
