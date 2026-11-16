import React, { useState } from 'react';
import { Users, FileText, Truck } from 'lucide-react';
import { api } from '../api/api';

interface SupplierUser {
  userId: string;
  supplierId: string;
  username: string;
  contactName: string;
  email: string;
  role: string;
  supplierCode: string;
  supplierName: string;
}

interface Rfq {
  rfqId: string;
  rfqNumber: string;
  rfqDate: string;
  deadline: string | null;
  itemCount: number;
  status: string;
}

interface Asn {
  asnId: string;
  asnNumber: string;
  purchaseOrderId: string | null;
  expectedArrivalDate: string;
  carrierName: string | null;
  trackingNumber: string | null;
  status: string;
  createdAt: string;
}

export const SupplierPortal: React.FC = () => {
  const [view, setView] = useState<'login' | 'quotes' | 'asn' | 'complaints'>('login');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [supplierUser, setSupplierUser] = useState<SupplierUser | null>(null);
  const [rfqs, setRfqs] = useState<Rfq[]>([]);
  const [asns, setAsns] = useState<Asn[]>([]);
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await api.post('/supplier-portal/login', { username, password });
      if (res.data.success && res.data.data) {
        setSupplierUser(res.data.data);
        setView('quotes');
        loadData(res.data.data.supplierId);
      } else {
        setError(res.data.message || '登入失敗');
      }
    } catch {
      setError('登入失敗');
    } finally {
      setLoading(false);
    }
  };

  const loadData = async (supplierId: string) => {
    try {
      const rfqsRes = await api.get('/supplier-portal/quotes/rfqs', { params: { supplierId } });
      if (rfqsRes.data.success) {
        setRfqs(rfqsRes.data.data || []);
      }
      const asnsRes = await api.get('/supplier-portal/asn/my-asns', { params: { supplierId } });
      if (asnsRes.data.success) {
        setAsns(asnsRes.data.data || []);
      }
    } catch (e) {
      console.error('Load data error:', e);
    }
  };

  const handleLogout = () => {
    setSupplierUser(null);
    setView('login');
    setRfqs([]);
    setAsns([]);
  };

  if (!supplierUser) {
    return (
      <div style={{ padding: 24, maxWidth: 480, margin: '0 auto' }}>
        <div className="glass-card" style={{ padding: 32 }}>
          <div style={{ textAlign: 'center', marginBottom: 24 }}>
            <Users size={48} style={{ color: 'var(--cobalt-400)' }} />
            <h2 style={{ marginTop: 12, fontSize: 20, fontWeight: 600 }}>供應商入口</h2>
            <p style={{ color: 'var(--slate-400)', fontSize: 13 }}>供應商登入系統</p>
          </div>
          <form onSubmit={handleLogin}>
            <div style={{ marginBottom: 16 }}>
              <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>帳號</label>
              <input
                className="form-input"
                value={username}
                onChange={e => setUsername(e.target.value)}
                placeholder="請輸入帳號"
                required
                style={{ width: '100%' }}
              />
            </div>
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>密碼</label>
              <input
                className="form-input"
                type="password"
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="請輸入密碼"
                required
                style={{ width: '100%' }}
              />
            </div>
            {error && <div style={{ padding: 10, background: 'rgba(199,31,61,0.15)', borderRadius: 8, color: 'var(--crimson-300)', fontSize: 13, marginBottom: 16 }}>{error}</div>}
            <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
              {loading ? '登入中...' : '登入'}
            </button>
          </form>
        </div>
      </div>
    );
  }

  return (
    <div style={{ padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h2 style={{ fontSize: 20, fontWeight: 600 }}>供應商入口</h2>
          <p style={{ color: 'var(--slate-400)', fontSize: 13 }}>{supplierUser.supplierName} ({supplierUser.supplierCode})</p>
        </div>
        <button onClick={handleLogout} className="btn btn-secondary">登出</button>
      </div>

      <div style={{ display: 'flex', gap: 12, marginBottom: 24 }}>
        <button onClick={() => setView('quotes')} className={`btn ${view === 'quotes' ? 'btn-primary' : 'btn-secondary'}`}>
          <FileText size={16} /> 詢報價
        </button>
        <button onClick={() => setView('asn')} className={`btn ${view === 'asn' ? 'btn-primary' : 'btn-secondary'}`}>
          <Truck size={16} /> 送貨通知
        </button>
      </div>

      {view === 'quotes' && (
        <div className="glass-card" style={{ padding: 20 }}>
          <h3 style={{ marginBottom: 16, fontWeight: 600 }}>RFQ 詢價單</h3>
          {rfqs.length === 0 ? (
            <p style={{ color: 'var(--slate-400)' }}>目前無待報價的 RFQ</p>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--slate-700)' }}>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>RFQ 編號</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>日期</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>截止日</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>品項數</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>狀態</th>
                </tr>
              </thead>
              <tbody>
                {rfqs.map(rfq => (
                  <tr key={rfq.rfqId} style={{ borderBottom: '1px solid var(--slate-800)' }}>
                    <td style={{ padding: '12px' }}>{rfq.rfqNumber}</td>
                    <td style={{ padding: '12px' }}>{new Date(rfq.rfqDate).toLocaleDateString('zh-TW')}</td>
                    <td style={{ padding: '12px' }}>{rfq.deadline ? new Date(rfq.deadline).toLocaleDateString('zh-TW') : '-'}</td>
                    <td style={{ padding: '12px' }}>{rfq.itemCount}</td>
                    <td style={{ padding: '12px' }}>
                      <span style={{ padding: '2px 8px', borderRadius: 4, fontSize: 11, background: rfq.status === 'open' ? 'var(--emerald-400)' : 'var(--slate-600)', color: '#fff' }}>
                        {rfq.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {view === 'asn' && (
        <div className="glass-card" style={{ padding: 20 }}>
          <h3 style={{ marginBottom: 16, fontWeight: 600 }}>ASN 送貨通知</h3>
          {asns.length === 0 ? (
            <p style={{ color: 'var(--slate-400)' }}>尚無送貨記錄</p>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--slate-700)' }}>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>ASN 編號</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>預計到貨日</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>物流商</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>追蹤號</th>
                  <th style={{ textAlign: 'left', padding: '8px 12px', fontSize: 12, color: 'var(--slate-400)' }}>狀態</th>
                </tr>
              </thead>
              <tbody>
                {asns.map(asn => (
                  <tr key={asn.asnId} style={{ borderBottom: '1px solid var(--slate-800)' }}>
                    <td style={{ padding: '12px' }}>{asn.asnNumber}</td>
                    <td style={{ padding: '12px' }}>{new Date(asn.expectedArrivalDate).toLocaleDateString('zh-TW')}</td>
                    <td style={{ padding: '12px' }}>{asn.carrierName || '-'}</td>
                    <td style={{ padding: '12px' }}>{asn.trackingNumber || '-'}</td>
                    <td style={{ padding: '12px' }}>
                      <span style={{ padding: '2px 8px', borderRadius: 4, fontSize: 11, background: asn.status === 'pending' ? 'var(--amber-400)' : 'var(--emerald-400)', color: '#fff' }}>
                        {asn.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
};

export default SupplierPortal;
