import { Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth } from './auth/RequireAuth';
import { RequireAuthLayout } from './components/RequireAuthLayout';
import { AuthCallbackPage } from './pages/AuthCallbackPage';
import { DashboardPage } from './pages/DashboardPage';
import { CapaDetailPage, CapaPage } from './pages/CapaPage';
import { ApSubmissionDetailPage } from './pages/ApSubmissionDetailPage';
import { ApSubmissionsPage } from './pages/ApSubmissionsPage';
import { LoginPage } from './pages/LoginPage';
import { LogoutCallbackPage } from './pages/LogoutCallbackPage';
import { NewApSubmissionPage } from './pages/NewApSubmissionPage';
import { PurchaseOrderDetailPage } from './pages/PurchaseOrderDetailPage';
import { PurchaseOrdersPage } from './pages/PurchaseOrdersPage';
import { RfqDetailPage } from './pages/RfqDetailPage';
import { RfqsPage } from './pages/RfqsPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/auth/logout-callback" element={<LogoutCallbackPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<RequireAuthLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/rfqs" element={<RfqsPage />} />
          <Route path="/rfqs/:rfqId" element={<RfqDetailPage />} />
          <Route path="/purchase-orders" element={<PurchaseOrdersPage />} />
          <Route path="/purchase-orders/:purchaseOrderId" element={<PurchaseOrderDetailPage />} />
          <Route path="/ap-submissions" element={<ApSubmissionsPage />} />
          <Route path="/ap-submissions/new" element={<NewApSubmissionPage />} />
          <Route path="/ap-submissions/:submissionId" element={<ApSubmissionDetailPage />} />
          <Route path="/capa" element={<CapaPage />} />
          <Route path="/capa/:complaintId" element={<CapaDetailPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
