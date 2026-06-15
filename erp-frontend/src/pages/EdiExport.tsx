import React, { useState } from 'react';
import { Download, FileText, DollarSign, Truck, Calendar } from 'lucide-react';
import { api } from '../api/api';

export const EdiExport: React.FC = () => {
  const [format, setFormat] = useState<'csv' | 'xml'>('csv');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [exporting, setExporting] = useState('');
  const [result, setResult] = useState<string | null>(null);

  const handleExport = async (type: 'invoices' | 'receipts' | 'asn') => {
    setExporting(type);
    setResult(null);
    try {
      let path: string;
      switch (type) {
        case 'invoices':
          path = `/api/edi/invoices/${format}`;
          break;
        case 'receipts':
          path = `/api/edi/receipts/${format}`;
          break;
        case 'asn':
          path = `/api/edi/asn/${format}`;
          break;
      }

      const params: Record<string, string> = {};
      if (fromDate) params.fromDate = fromDate;
      if (toDate) params.toDate = toDate;

      const res = await api.get(path, { params });
      if (res.data.success && res.data.data) {
        setResult(res.data.data || '匯出成功');
        
        const blob = new Blob([res.data.data || ''], { type: format === 'csv' ? 'text/csv' : 'application/xml' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${type}_${new Date().toISOString().split('T')[0]}.${format}`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
      }
    } catch (e) {
      setResult('匯出失敗');
    } finally {
      setExporting('');
    }
  };

  return (
    <div style={{ padding: 24 }}>
      <div style={{ marginBottom: 24 }}>
        <h2 style={{ fontSize: 20, fontWeight: 600, marginBottom: 4 }}>EDI 資料匯出</h2>
        <p style={{ color: 'var(--slate-400)', fontSize: 13 }}>匯出發票、收款、ASN 資料供 EDI 系統使用</p>
      </div>

      <div style={{ display: 'flex', gap: 16, marginBottom: 24 }}>
        <div>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>格式</label>
          <select 
            className="form-input" 
            value={format} 
            onChange={e => setFormat(e.target.value as 'csv' | 'xml')}
            style={{ width: 120 }}
          >
            <option value="csv">CSV</option>
            <option value="xml">XML</option>
          </select>
        </div>
        <div>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>起始日期</label>
          <input
            type="date"
            className="form-input"
            value={fromDate}
            onChange={e => setFromDate(e.target.value)}
            style={{ width: 160 }}
          />
        </div>
        <div>
          <label style={{ display: 'block', fontSize: 12, fontWeight: 600, color: 'var(--slate-400)', marginBottom: 6 }}>結束日期</label>
          <input
            type="date"
            className="form-input"
            value={toDate}
            onChange={e => setToDate(e.target.value)}
            style={{ width: 160 }}
          />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16 }}>
        <div className="glass-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <FileText size={24} style={{ color: 'var(--cobalt-400)' }} />
            <h3 style={{ fontWeight: 600 }}>發票 (Invoice)</h3>
          </div>
          <p style={{ fontSize: 13, color: 'var(--slate-400)', marginBottom: 16 }}>EDI 810 發票格式</p>
          <button 
            onClick={() => handleExport('invoices')} 
            className="btn btn-primary"
            disabled={exporting === 'invoices'}
            style={{ width: '100%' }}
          >
            <Download size={16} /> {exporting === 'invoices' ? '匯出中...' : '匯出發票'}
          </button>
        </div>

        <div className="glass-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <DollarSign size={24} style={{ color: 'var(--emerald-400)' }} />
            <h3 style={{ fontWeight: 600 }}>收款 (Receipt)</h3>
          </div>
          <p style={{ fontSize: 13, color: 'var(--slate-400)', marginBottom: 16 }}>EDI 820 付款通知格式</p>
          <button 
            onClick={() => handleExport('receipts')} 
            className="btn btn-primary"
            disabled={exporting === 'receipts'}
            style={{ width: '100%' }}
          >
            <Download size={16} /> {exporting === 'receipts' ? '匯出中...' : '匯出收款'}
          </button>
        </div>

        <div className="glass-card" style={{ padding: 24 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <Truck size={24} style={{ color: 'var(--amber-400)' }} />
            <h3 style={{ fontWeight: 600 }}>送貨通知 (ASN)</h3>
          </div>
          <p style={{ fontSize: 13, color: 'var(--slate-400)', marginBottom: 16 }}>EDI 856 提前通知格式</p>
          <button 
            onClick={() => handleExport('asn')} 
            className="btn btn-primary"
            disabled={exporting === 'asn'}
            style={{ width: '100%' }}
          >
            <Download size={16} /> {exporting === 'asn' ? '匯出中...' : '匯出ASN'}
          </button>
        </div>
      </div>

      {result && (
        <div style={{ marginTop: 24, padding: 16, background: 'var(--slate-800)', borderRadius: 8 }}>
          <div style={{ fontWeight: 600, marginBottom: 8 }}>匯出結果</div>
          <pre style={{ fontSize: 12, overflow: 'auto', maxHeight: 300, whiteSpace: 'pre-wrap' }}>
            {result.substring(0, 2000)}...
          </pre>
        </div>
      )}
    </div>
  );
};

export default EdiExport;
