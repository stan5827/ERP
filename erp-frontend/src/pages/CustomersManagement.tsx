import React, { useMemo, useState } from 'react';
import { Edit, Loader2, Plus, Search } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { customersApi, type CustomerDto } from '../api/sales';
import { FormModal, type FormTab } from '../components/FormModal';
import { useCustomers } from '../hooks/useApi';

const customerTabs: FormTab[] = [
  {
    label: '基本資料',
    fields: [
      { name: 'customerCode', label: '客戶代碼', type: 'text', required: true, placeholder: 'CUST-000' },
      { name: 'customerName', label: '客戶全銜', type: 'text', required: true, placeholder: '輸入企業正式名稱' },
      { name: 'taxId', label: '統一編號', type: 'text', placeholder: '8 位數字統編' },
      { name: 'contactPerson', label: '首要聯繫人', type: 'text', placeholder: '姓名' },
    ],
  },
  {
    label: '聯繫地址',
    fields: [
      { name: 'phone', label: '公司電話', type: 'text', placeholder: '02-xxxx-xxxx' },
      { name: 'email', label: '電子郵件', type: 'text', placeholder: 'corp@example.com' },
      { name: 'invoiceAddress', label: '發票掛帳地址', type: 'text', placeholder: '發票開立地址' },
      { name: 'deliveryAddress', label: '預設送貨地址', type: 'text', placeholder: '物流配送地址', sameAs: 'invoiceAddress' },
    ],
  },
  {
    label: '財務條件',
    fields: [
      {
        name: 'paymentTerms',
        label: '付款條件',
        type: 'select',
        options: [
          { value: '月結30天', label: '月結 30 天' },
          { value: '月結60天', label: '月結 60 天' },
          { value: '現結', label: '貨到現結' },
          { value: '預付', label: '訂金預付' },
        ],
      },
      {
        name: 'currency',
        label: '預設幣別',
        type: 'select',
        options: [
          { value: 'TWD', label: 'TWD - 新台幣' },
          { value: 'USD', label: 'USD - 美元' },
          { value: 'JPY', label: 'JPY - 日圓' },
        ],
      },
      { name: 'creditLimit', label: '信用額度控管', type: 'number', placeholder: '0 代表不限額' },
    ],
  },
];

export const CustomersManagement: React.FC = () => {
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<CustomerDto | null>(null);
  const { data, isLoading, isError, error } = useCustomers();
  const queryClient = useQueryClient();
  const filteredCustomers = useMemo(() => {
    const customers = data?.data ?? [];
    const normalizedSearch = search.trim().toLocaleLowerCase();
    if (!normalizedSearch) return customers;
    return customers.filter(customer =>
      customer.customerCode.toLocaleLowerCase().includes(normalizedSearch) ||
      customer.customerName.toLocaleLowerCase().includes(normalizedSearch),
    );
  }, [data?.data, search]);

  return (
    <div className="module-page page-enter">
      <div className="module-page-header">
        <div>
          <h2 className="module-page-title">客戶主檔管理</h2>
          <p className="module-page-desc">客戶資料、聯絡方式與交易條件維護</p>
        </div>
        <div className="module-page-actions">
          <button
            className="btn btn-primary btn-sm"
            onClick={() => {
              setSelectedCustomer(null);
              setShowForm(true);
            }}
          >
            <Plus size={14} />新增客戶
          </button>
        </div>
      </div>

      <div className="glass-card module-filter-bar">
        <div className="filter-search" style={{ flex: 1, maxWidth: 420 }}>
          <Search size={14} style={{ color: 'var(--slate-500)' }} />
          <input
            className="search-input"
            placeholder="搜尋客戶代碼或名稱..."
            value={search}
            onChange={event => setSearch(event.target.value)}
          />
        </div>
        <span style={{ color: 'var(--slate-500)', fontSize: 13 }}>
          共 {filteredCustomers.length} 筆客戶
        </span>
      </div>

      <div className="glass-card" style={{ overflow: 'hidden' }}>
        <table className="data-table">
          <thead>
            <tr>
              <th>客戶代碼</th>
              <th>企業全稱</th>
              <th>統一編號</th>
              <th>核心聯繫人</th>
              <th>電話</th>
              <th>電子郵件</th>
              <th>交易幣別</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={8} style={{ textAlign: 'center', padding: 40 }}>
                  <Loader2 className="animate-spin" size={22} style={{ color: 'var(--cobalt-400)' }} />
                  <div style={{ marginTop: 10, color: 'var(--slate-500)' }}>載入客戶資料中...</div>
                </td>
              </tr>
            ) : isError ? (
              <tr>
                <td colSpan={8} role="alert" style={{ textAlign: 'center', padding: 40, color: 'var(--crimson-400)' }}>
                  載入客戶資料失敗：{error instanceof Error ? error.message : '未知錯誤'}
                </td>
              </tr>
            ) : filteredCustomers.length === 0 ? (
              <tr>
                <td colSpan={8} style={{ textAlign: 'center', padding: 40, color: 'var(--slate-500)' }}>
                  {search ? '找不到符合條件的客戶' : '尚無客戶資料'}
                </td>
              </tr>
            ) : filteredCustomers.map(customer => (
              <tr key={customer.id}>
                <td className="mono" style={{ color: 'var(--theme-text)' }}>{customer.customerCode}</td>
                <td style={{ color: 'var(--slate-100)', fontWeight: 600 }}>{customer.customerName}</td>
                <td>{customer.taxId || '—'}</td>
                <td>{customer.contactPerson || '—'}</td>
                <td>{customer.phone || '—'}</td>
                <td>{customer.email || '—'}</td>
                <td>{customer.currency || 'TWD'}</td>
                <td>
                  <button
                    className="icon-action-btn"
                    title="編輯客戶"
                    onClick={() => {
                      setSelectedCustomer(customer);
                      setShowForm(true);
                    }}
                  >
                    <Edit size={14} />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <FormModal
        open={showForm}
        title={selectedCustomer ? '編輯客戶資料' : '新增客戶'}
        onClose={() => setShowForm(false)}
        initialValues={selectedCustomer ? { ...selectedCustomer } : undefined}
        width={700}
        isWizard
        wizardConfig={{
          stepTitles: ['基本資料', '聯繫資訊', '財務與信用'],
          getStepFields: index => customerTabs[index].fields,
        }}
        onSubmit={async values => {
          const payload = { ...values, creditLimit: Number(values.creditLimit) || 0 };
          if (selectedCustomer) {
            await customersApi.update(selectedCustomer.id, payload);
          } else {
            await customersApi.create(payload);
          }
          await queryClient.invalidateQueries({ queryKey: ['customers'] });
          setShowForm(false);
        }}
      />
    </div>
  );
};
