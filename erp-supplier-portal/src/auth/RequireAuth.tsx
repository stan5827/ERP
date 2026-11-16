import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';
import { LoadingScreen } from '../components/Feedback';

export function RequireAuth() {
  const { user, isLoading, configurationError } = useAuth();
  const location = useLocation();

  if (configurationError)
    return <LoadingScreen title="Portal 尚未完成設定" detail={configurationError} />;

  if (isLoading)
    return <LoadingScreen title="正在確認登入狀態" />;

  if (!user)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;

  return <Outlet />;
}
