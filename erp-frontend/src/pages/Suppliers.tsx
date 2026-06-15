import React, { useState, useEffect } from 'react';
import { Search, Plus, Download, Eye, Edit2, CheckCircle, Loader2, Users, ClipboardCheck, Key, Lock, Unlock, Mail, Phone, MapPin, Star, AlertCircle, Trash2, XCircle, Send } from 'lucide-react';
import { suppliersApi } from '../api/api';
import type { SupplierDto } from '../api/api';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { useQueryClient } from '@tanstack/react-query';

export const Suppliers: React.FC = () => {
  const [search, setSearch] = useState('');
  const [filterRating, setFilterRating] = useState('all');
  const [supplierList, setSupplierList] = useState<SupplierDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [showAdd, setShowAdd] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [editingSupplier, setEditingSupplier] = useState<SupplierDto | null>(null);
  const [showEval, setShowEval] = useState(false);
  const [selectedEval, setSelectedEval] = useState<SupplierDto | null>(null);
  const [portalUsers, setPortalUsers] = useState<Record<string, any[]>>({});
  const [showPortalUserModal, setShowPortalUserModal] = useState(false);
  const [selectedPortalUser, setSelectedPortalUser] = useState<any>(null);
  const [showApprovalModal, setShowApprovalModal] = useState(false);
  const [approvalSupplier, setApprovalSupplier] = useState<SupplierDto | null>(null);
  const [approvalAction, setApprovalAction] = useState<'approve' | 'reject' | 'deactivate'>('approve');
  const [approvalComments, setApprovalComments] = useState('');
  const [createError, setCreateError] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);
  const queryClient = useQueryClient();

  const fetchSuppliers = async () => {
    setLoading(true);
    try {
      const res = await suppliersApi.list();
      const list = res.data?.data || [];
      setSupplierList(list);
      
      // Fetch portal users for each supplier
      list.forEach(async (s: any) => {
        try {
          const uRes = await suppliersApi.getPortalUsers(s.id);
          setPortalUsers(prev => ({ ...prev, [s.id]: uRes.data?.data || [] }));
        } catch (e) {
          console.error(`Failed to fetch portal users for ${s.id}`, e);
        }
      });
    } catch (err) {
      console.error('Failed to fetch suppliers', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSuppliers();
  }, []);

  const stats = [
    { label: '註冊供應商', value: supplierList.length, color: 'var(--cobalt-400)', icon: <Users size={20} /> },
    { label: '優化評等 (A)', value: supplierList.filter(s => s.rating === 'A').length, color: 'var(--emerald-400)', icon: <Star size={20} /> },
    { label: '待審批', value: supplierList.filter(s => (s as any).approvalStatus === 'PENDING' || (s as any).approvalStatus === 'UNDER_REVIEW').length, color: 'var(--amber-400)', icon: <ClipboardCheck size={20} /> },
    { label: '已啟用', value: supplierList.filter(s => (s as any).status === 'ACTIVE').length, color: 'var(--emerald-400)', icon: <CheckCircle size={20} /> },
  ];

  const getStatusBadge = (s: SupplierDto) => {
    const approvalStatus = (s as any).approvalStatus;
    const status = (s as any).status;
    if (approvalStatus === 'REJECTED') return <span className="status-badge status-rejected">已駁回</span>;
    if (approvalStatus === 'UNDER_REVIEW') return <span className="status-badge" style={{background:'rgba(251,191,36,0.2)',color:'var(--amber-400)'}}>審核中</span>;
    if (status === 'ACTIVE') return <span className="status-badge status-active">啟用</span>;
    return <span className="status-badge status-neutral">停用</span>;
  };

  const basicFields: FormField[] = [
    { name: 'supplierCode', label: '供應商代碼 *', type: 'text', required: true, placeholder: '如: SUP-001', description: '用於系統唯一識別。' },
    { name: 'supplierName', label: '供應商全名 *', type: 'text', required: true, placeholder: '輸入正式公司全銜', description: '將顯示於採購與對帳單據。' },
    { name: 'taxId', label: '統一編號', type: 'text', placeholder: '8 位數字', description: '供應商開立發票之統一編號。' }
  ];

  const contactFields: FormField[] = [
    { name: 'contactPerson', label: '主要聯絡人 *', type: 'text', required: true, placeholder: '聯絡人姓名' },
    { name: 'phone', label: '聯繫電話', type: 'text', placeholder: '02-xxxx-xxxx' },
    { name: 'email', label: '電子郵件', type: 'text', placeholder: 'email@example.com' },
    { name: 'invoiceAddress', label: '發票寄送地址', type: 'text', placeholder: '輸入完整地址' },
    { name: 'deliveryAddress', label: '货物接收地址', type: 'text', placeholder: '若與發票地址相同可留空' }
  ];

  const paymentFields: FormField[] = [
    { name: 'paymentTerms', label: '付款條件', type: 'select', options: [
      { value: '月結30天', label: '月結 30 天' },
      { value: '月結60天', label: '月結 60 天' },
      { value: '貨到付款', label: '貨到付款' },
      { value: '預付', label: '預付款 (TT)' }
    ], description: '與供應商議定的信用結帳期限。' },
    { name: 'currency', label: '交易幣別', type: 'select', options: [
      { value: 'TWD', label: 'TWD - 新台幣' },
      { value: 'USD', label: 'USD - 美元' },
      { value: 'JPY', label: 'JPY - 日圓' },
      { value: 'EUR', label: 'EUR - 歐元' }
    ] }
  ];

  const filtered = supplierList.filter(s => {
    const matchSearch = s.supplierCode.includes(search) || (s.supplierName ?? '').includes(search);
    const matchRating = filterRating === 'all' || s.rating === filterRating;
    return matchSearch && matchRating;
  });

  return (
    <div className="module-page page-enter">
      {/* 頁首 */}
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">供應商主檔管理</h2>
          <p className="module-page-desc">採購對象資料維護 · 供應商評鑑 (AVL) · 入口網站權限控管</p>
        </div>
        <div className="module-page-actions">
          <button className="btn btn-ghost btn-sm"><Download size={13} />匯出資料</button>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAdd(true)}><Plus size={13} />新增供應商</button>
        </div>
      </div>

      {/* 看板 */}
      <div className="module-stat-grid">
        {stats.map(s => (
          <div key={s.label} className="glass-card module-stat-card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <div className="module-stat-label">{s.label}</div>
                <div className="module-stat-value kpi-value" style={{ color: s.color, fontSize: '24px' }}>{s.value}</div>
              </div>
              <div style={{ color: s.color, opacity: 0.6 }}>{s.icon}</div>
            </div>
          </div>
        ))}
      </div>

      {/* 篩選與工具 */}
      <div className="glass-card module-filter-bar">
        <div className="filter-search" style={{ flex: 1, maxWidth: 400 }}>
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input 
            className="search-input" 
            placeholder="搜尋供應商代號、名稱、聯絡人..." 
            value={search} 
            onChange={e => setSearch(e.target.value)} 
          />
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          <span style={{ fontSize: 13, color: 'var(--slate-500)' }}>評等篩選:</span>
          <select 
            className="form-input" 
            style={{ width: 120, height: 32, fontSize: 13, padding: '0 8px' }}
            value={filterRating}
            onChange={e => setFilterRating(e.target.value)}
          >
            <option value="all">全部評等</option>
            <option value="A">A 級優選</option>
            <option value="B">B 級良好</option>
            <option value="C">C 級觀察</option>
          </select>
        </div>
      </div>

      {/* 資料表格 */}
      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <table className="data-table">
          <thead>
            <tr>
              <th>供應商代號</th>
              <th>名稱 / 統編</th>
              <th>聯絡資訊</th>
              <th>交易條件</th>
              <th>評等狀態</th>
              <th>入口帳號</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} style={{ textAlign: 'center', padding: '60px 0' }}>
                  <Loader2 className="animate-spin" size={24} style={{ color: 'var(--cobalt-400)' }} />
                  <div style={{ marginTop: 12, fontSize: 13, color: 'var(--slate-500)' }}>載入中...</div>
                </td>
              </tr>
            ) : filtered.length === 0 ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px 0', color: 'var(--slate-500)' }}>尚無供應商資料</td></tr>
            ) : filtered.map(s => (
              <tr key={s.id}>
                <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{s.supplierCode}</td>
                <td>
                  <div style={{ fontWeight: 600, color: 'var(--slate-100)' }}>{s.supplierName}</div>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)' }}>統編: {s.taxId || '—'}</div>
                </td>
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12 }}>
                    <Mail size={12} color="var(--slate-500)" /> {s.email || '—'}
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, marginTop: 4 }}>
                    <Phone size={12} color="var(--slate-500)" /> {s.phone || '—'}
                  </div>
                </td>
                <td>
                  <div style={{ fontSize: 12, color: 'var(--slate-300)' }}>{s.paymentTerms}</div>
                  <div style={{ fontSize: 11, color: 'var(--amber-400)', fontWeight: 600 }}>{s.currency}</div>
                </td>
                <td>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                    <span style={{ 
                      fontSize: 18, 
                      fontWeight: 800, 
                      color: s.rating === 'A' ? 'var(--emerald-400)' : s.rating === 'B' ? 'var(--cobalt-400)' : 'var(--amber-400)' 
                    }}>{s.rating || '—'}</span>
                    <span className={`status-badge ${s.status === 'active' ? 'status-active' : 'status-neutral'}`}>
                      {s.status === 'active' ? '合作中' : '未啟用'}
                    </span>
                  </div>
                </td>
                <td>
                  {portalUsers[s.id]?.length > 0 ? (
                    <span style={{ 
                      display: 'inline-flex', 
                      alignItems: 'center', 
                      gap: 4, 
                      fontSize: 11, 
                      color: 'var(--emerald-400)', 
                      background: 'rgba(16, 185, 129, 0.1)', 
                      padding: '2px 8px', 
                      borderRadius: 4,
                      cursor: 'pointer'
                    }} onClick={() => { setEditingSupplier(s); setShowEdit(true); }}>
                      <Key size={10} /> {portalUsers[s.id][0].username}
                    </span>
                  ) : (
                    <span style={{ fontSize: 11, color: 'var(--slate-600)' }}>未開通</span>
                  )}
                </td>
                <td>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <button className="icon-action-btn" title="送審批" onClick={() => { setApprovalSupplier(s); setApprovalAction('approve'); setShowApprovalModal(true); }}><CheckCircle size={14} /></button>
                    <button className="icon-action-btn" title="停用" onClick={() => { setApprovalSupplier(s); setApprovalAction('deactivate'); setShowApprovalModal(true); }}><Lock size={14} /></button>
                    <button className="icon-action-btn" title="評核紀錄" onClick={() => { setSelectedEval(s); setShowEval(true); }}><ClipboardCheck size={14} /></button>
                    <button className="icon-action-btn" title="編輯供應商" onClick={() => { setEditingSupplier(s); setShowEdit(true); }}><Edit2 size={14} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* 新增供應商 Modal */}
      <FormModal 
        open={showAdd}
        title="新增供應商建檔"
        width={700}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯絡與地址', '交易條款'],
          getStepFields: (idx) => [basicFields, contactFields, paymentFields][idx]
        }}
        onClose={() => { setShowAdd(false); setCreateError(null); }}
        onSubmit={async (data) => {
          setCreateError(null);
          try {
            await suppliersApi.create(data);
            fetchSuppliers();
            setShowAdd(false);
          } catch (err: any) {
            const msg = err?.response?.data?.message || err?.message || '新增失敗，請確認必填欄位已填寫';
            setCreateError(msg);
            console.error('建立供應商失敗:', err);
          }
        }}
      />

      {/* 編輯供應商 Modal */}
      <FormModal 
        open={showEdit}
        title={`編輯: ${editingSupplier?.supplierName}`}
        width={750}
        isWizard={true}
        wizardConfig={{
          stepTitles: ['基本資料', '聯絡地址', '交易條款', '入口帳號'],
          getStepFields: (idx) => idx === 3 ? [{ name: '入口帳號', type: 'custom' }] : [basicFields, contactFields, paymentFields][idx]
        }}
        initialValues={(editingSupplier || {}) as any}
        customRenderers={{
          入口帳號: (
            <div style={{ padding: '8px 4px' }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <span style={{ fontSize: 13, fontWeight: 600 }}>供應商入口網帳號 (Portal)</span>
                <button className="btn btn-outline btn-sm" onClick={() => setShowPortalUserModal(true)}>
                  <Plus size={12} /> 新增帳號
                </button>
              </div>
              <table className="data-table" style={{ background: 'rgba(255,255,255,0.02)', borderRadius: 8 }}>
                <thead>
                   <tr><th>帳號名稱</th><th>角色</th><th>狀態</th><th>操作</th></tr>
                </thead>
                <tbody>
                  {(portalUsers[editingSupplier?.id || ''] || []).map(u => (
                    <tr key={u.id}>
                      <td className="mono" style={{ color: 'var(--cobalt-300)' }}>{u.username}</td>
                      <td>{u.role === 'admin' ? '主管理員' : '一般檢視'}</td>
                      <td><span className={`status-badge ${u.isActive ? 'status-active' : 'status-neutral'}`}>{u.isActive ? '有效' : '停用'}</span></td>
                      <td>
                        <button className="icon-action-btn" onClick={() => { setSelectedPortalUser(u); setShowPortalUserModal(true); }}><Edit2 size={12} /></button>
                      </td>
                    </tr>
                  ))}
                  {portalUsers[editingSupplier?.id || '']?.length === 0 && (
                    <tr><td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--slate-600)', fontSize: 12 }}>尚未建立入口帳號</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          )
        }}
        onClose={() => setShowEdit(false)}
        onSubmit={async (data) => {
          await suppliersApi.update(editingSupplier!.id, data);
          fetchSuppliers();
          setShowEdit(false);
        }}
      />

      {/* 評核 Modal */}
      <FormModal 
        open={showEval}
        title={`供應商評核紀錄 - ${selectedEval?.supplierName}`}
        width={600}
        onClose={() => setShowEval(false)}
        onSubmit={async () => setShowEval(false)}
        fields={[]}
        customRenderers={{
          '': (
            <div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16, marginBottom: 24 }}>
                <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>品質績效</div>
                  <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--emerald-400)' }}>92/100</div>
                </div>
                <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>交期達成</div>
                  <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--cobalt-400)' }}>88/100</div>
                </div>
                <div className="glass-card" style={{ padding: 16, textAlign: 'center' }}>
                  <div style={{ fontSize: 11, color: 'var(--slate-500)', marginBottom: 8 }}>配合度</div>
                  <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--amber-400)' }}>95/100</div>
                </div>
              </div>
              <div style={{ fontSize: 14, fontWeight: 600, marginBottom: 12 }}>歷史評核履歷</div>
              <table className="data-table">
                <thead><tr><th>日期</th><th>評等</th><th>評核人</th><th>結案狀態</th></tr></thead>
                <tbody>
                  <tr><td>2026/03/15</td><td><span style={{ color: 'var(--emerald-400)', fontWeight: 700 }}>A</span></td><td>採購課-王大明</td><td>已核准</td></tr>
                  <tr><td>2025/12/20</td><td><span style={{ color: 'var(--cobalt-400)', fontWeight: 700 }}>B</span></td><td>採購課-李小華</td><td>已核准</td></tr>
                </tbody>
              </table>
            </div>
          )
        }}
      />

      {/* 入口帳號 Modal */}
      <FormModal
        open={showPortalUserModal}
        title={selectedPortalUser ? '編輯入口帳號' : '新增入口帳號'}
        width={400}
        onClose={() => setShowPortalUserModal(false)}
        fields={[
          { name: 'username', label: '帳號名稱', type: 'text', required: true, placeholder: '供應商登入用' },
          { name: 'password', label: '登入密碼', type: 'text', required: !selectedPortalUser, placeholder: selectedPortalUser ? '留空則不修改' : '請設定密碼' },
          { name: 'role', label: '角色', type: 'select', options: [{ value: 'admin', label: '管理員' }, { value: 'viewer', label: '檢視者' }] },
          { name: 'isActive', label: '啟用帳號', type: 'checkbox' }
        ]}
        initialValues={selectedPortalUser || { isActive: true, role: 'viewer' }}
        onSubmit={async (values) => {
          if (selectedPortalUser) {
            await suppliersApi.updatePortalUser(selectedPortalUser.id, values);
          } else {
            await suppliersApi.createPortalUser(editingSupplier!.id, values);
          }
          fetchSuppliers();
          setShowPortalUserModal(false);
        }}
      />

      {/* 審批操作 Modal */}
      {showApprovalModal && approvalSupplier && (
        <div className="modal-overlay" onClick={() => setShowApprovalModal(false)}>
          <div className="modal-content" style={{ maxWidth: 400 }} onClick={e => e.stopPropagation()}>
            <h3 style={{ marginBottom: 20, display: 'flex', alignItems: 'center', gap: 8 }}>
              {approvalAction === 'approve' ? <CheckCircle size={20} color="var(--emerald-400)" /> : 
               approvalAction === 'reject' ? <XCircle size={20} color="var(--rose-400)" /> : 
               <Lock size={20} color="var(--amber-400)" />}
              {approvalAction === 'approve' ? '核准供應商' : approvalAction === 'reject' ? '駁回供應商' : '停用供應商'}
            </h3>
            <div style={{ marginBottom: 16 }}>
              <div style={{ fontSize: 13, color: 'var(--slate-400)' }}>供應商</div>
              <div style={{ fontSize: 15, fontWeight: 600, color: 'var(--slate-100)' }}>{approvalSupplier.supplierName}</div>
            </div>
            <div style={{ marginBottom: 16 }}>
              <label style={{ fontSize: 12, color: 'var(--slate-500)', display: 'block', marginBottom: 6 }}>備註 (選填)</label>
              <textarea
                className="form-input"
                rows={3}
                value={approvalComments}
                onChange={e => setApprovalComments(e.target.value)}
                placeholder="輸入審批意見..."
                style={{ width: '100%', resize: 'none' }}
              />
            </div>
            <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
              <button className="btn btn-ghost btn-sm" onClick={() => setShowApprovalModal(false)}>取消</button>
              <button 
                className={`btn btn-sm ${approvalAction === 'approve' ? 'btn-primary' : approvalAction === 'reject' ? 'btn-danger' : 'btn-warning'}`}
                onClick={async () => {
                  const employeeId = 'current-user-id'; // 應該從認證取得
                  try {
                    if (approvalAction === 'approve') {
                      await suppliersApi.approve(approvalSupplier.id, { employeeId, comments: approvalComments });
                    } else if (approvalAction === 'reject') {
                      await suppliersApi.reject(approvalSupplier.id, { employeeId, comments: approvalComments });
                    } else {
                      await suppliersApi.deactivate(approvalSupplier.id, { employeeId, comments: approvalComments });
                    }
                    fetchSuppliers();
                    setShowApprovalModal(false);
                    setApprovalComments('');
                  } catch (err) {
                    console.error('審批操作失敗', err);
                  }
                }}
              >
                {approvalAction === 'approve' ? '確認核准' : approvalAction === 'reject' ? '確認駁回' : '確認停用'}
              </button>
            </div>
          </div>
        </div>
      )}

      <style>{`
        .module-page { display: flex; flex-direction: column; gap: 20px; }
        .module-page-desc { font-family: var(--font-mono); font-size: 12px; color: var(--slate-500); margin-top: 4px; }
        .module-stat-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; }
        .module-stat-card { padding: 20px; }
        .module-stat-label { font-size: 11px; font-weight: 700; color: var(--slate-500); text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 8px; }
        .module-filter-bar { display: flex; gap: 20px; padding: 12px 20px; align-items: center; }
        .filter-search { display: flex; align-items: center; gap: 10px; }
        .search-input { background: transparent; border: none; outline: none; color: white; width: 100%; font-size: 14px; }
        .search-input::placeholder { color: var(--slate-600); }
        .icon-action-btn { background: transparent; border: none; color: var(--slate-500); cursor: pointer; padding: 6px; border-radius: 4px; transition: all 0.2s; }
        .icon-action-btn:hover { background: rgba(255,255,255,0.05); color: var(--cobalt-300); }
      `}</style>
    </div>
  );
};
