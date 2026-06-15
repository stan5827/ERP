import React, { useState, useEffect, useRef } from 'react';
import { Search, Plus, Trash2, Edit2, Eye, ArrowLeftRight, ClipboardCheck, TrendingDown, RefreshCw, ChevronDown, Menu, X, Check, CheckSquare, Square, ChevronRight, Coins, Globe, History, TrendingUp } from 'lucide-react';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import { systemSettingsApi, currenciesApi, PermissionTreeDto, UserWithRolesDto, RoleWithPermissionsDto, ThemeDto, CurrencyDto, ExchangeRateDto } from '../api/api';
import { useTheme } from '../theme/ThemeProvider';
import { currencySchema, exchangeRateSchema } from '../utils/erpSchemas';
import { toast } from 'react-hot-toast';
import { FormModal } from '../components/FormModal';
import type { FormField } from '../components/FormModal';
import { themes, ThemeName, Theme } from '../theme/themes';

export const SystemSettings: React.FC = () => {
  const { themeName, setTheme, setCustomColors, customPrimaryColor, customAccentColor } = useTheme();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'permissions' | 'themes' | 'currencies'>('permissions');
  const [showAddRoleModal, setShowAddRoleModal] = useState(false);
  const [showAddPermissionModal, setShowAddPermissionModal] = useState(false);

  // Permissions tab state
  const { data: permTreeRes, isLoading: permissionsLoading } = useQuery({
    queryKey: ['permissions-tree'],
    queryFn: () => systemSettingsApi.getPermissionTree().then(res => res.data)
  });

  const { data: usersRes, isLoading: usersLoading } = useQuery({
    queryKey: ['users'],
    queryFn: () => systemSettingsApi.getUsers().then(res => res.data)
  });

  const { data: rolesRes, isLoading: rolesLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: () => systemSettingsApi.getRoles().then(res => res.data)
  });

  // Themes tab state
  const { data: themesRes, isLoading: themesLoading } = useQuery({
    queryKey: ['available-themes'],
    queryFn: () => systemSettingsApi.getThemes().then(res => res.data)
  });

  const permissionsTree = (permTreeRes as any)?.data || [];
  const users = (usersRes as any)?.data || [];
  const roles = (rolesRes as any)?.data || [];
  const availableThemes = (themesRes as any)?.data || [];

  return (
    <div className="page-container">
      <div className="module-header">
        <h1>系統設定</h1>
        <div className="tab-bar">
          <button 
            className={`tab-btn ${activeTab === 'permissions' ? 'active' : ''}`}
            onClick={() => setActiveTab('permissions')}
          >
            權限管理
          </button>
          <button 
            className={`tab-btn ${activeTab === 'themes' ? 'active' : ''}`}
            onClick={() => setActiveTab('themes')}
          >
            主題設定
          </button>
          <button 
            className={`tab-btn ${activeTab === 'currencies' ? 'active' : ''}`}
            onClick={() => setActiveTab('currencies')}
          >
            幣別與匯率
          </button>
        </div>
      </div>

      {activeTab === 'permissions' && (
        <PermissionsTab 
          permissionsTree={permissionsTree}
          users={users}
          roles={roles}
          isLoading={permissionsLoading || usersLoading || rolesLoading}
          onAddRole={() => setShowAddRoleModal(true)}
          onAddPermission={() => setShowAddPermissionModal(true)}
          queryClient={queryClient}
        />
      )}

      {activeTab === 'themes' && (
        <ThemesTab 
          themes={availableThemes}
          currentTheme={themeName}
          setTheme={setTheme}
          setCustomColors={setCustomColors}
          isLoading={themesLoading}
          queryClient={queryClient}
        />
      )}

      {activeTab === 'currencies' && (
        <CurrencyTab queryClient={queryClient} />
      )}

      {/* Add Role Modal */}
      <FormModal
        open={showAddRoleModal}
        title="新增角色"
        onClose={() => setShowAddRoleModal(false)}
        fields={[
          { name: 'roleName', label: '角色名稱 *', type: 'text', required: true, placeholder: '如: 倉管員' },
          { name: 'description', label: '角色描述', type: 'textarea', placeholder: '請輸入角色描述' },
        ]}
        onSubmit={async (data) => {
          try {
            await systemSettingsApi.createRole({ roleName: data.roleName as string, description: data.description as string });
            queryClient.invalidateQueries({ queryKey: ['roles'] });
            setShowAddRoleModal(false);
          } catch (err: any) {
            alert('新增失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
      />

      {/* Add Permission Modal */}
      <FormModal
        open={showAddPermissionModal}
        title="新增權限"
        onClose={() => setShowAddPermissionModal(false)}
        fields={[
          { name: 'permissionCode', label: '權限代碼 *', type: 'text', required: true, placeholder: '如: INV_TRANSFER_CREATE' },
          { name: 'moduleName', label: '模組名稱 *', type: 'text', required: true, placeholder: '如: 庫存管理' },
          { name: 'actionType', label: '動作類型 *', type: 'select', required: true, options: [
            { value: 'VIEW', label: '檢視' },
            { value: 'CREATE', label: '新增' },
            { value: 'EDIT', label: '編輯' },
            { value: 'DELETE', label: '刪除' },
            { value: 'EXPORT', label: '匯出' },
            { value: 'APPROVE', label: '審批' },
            { value: 'EXECUTE', label: '執行' },
          ]},
          { name: 'permissionType', label: '權限類型 *', type: 'select', required: true, options: [
            { value: 'menu', label: '選單' },
            { value: 'page', label: '頁面' },
            { value: 'action', label: '動作' },
          ]},
          { name: 'description', label: '權限描述', type: 'textarea', placeholder: '請輸入權限描述' },
          { name: 'sortOrder', label: '排序', type: 'number', placeholder: '0', min: 0 },
        ]}
        onSubmit={async (data) => {
          try {
            await systemSettingsApi.createPermission({
              permissionCode: data.permissionCode as string,
              moduleName: data.moduleName as string,
              actionType: data.actionType as string,
              permissionType: data.permissionType as string,
              description: data.description as string,
              sortOrder: parseInt(data.sortOrder as string) || 0
            });
            queryClient.invalidateQueries({ queryKey: ['permissions-tree'] });
            setShowAddPermissionModal(false);
          } catch (err: any) {
            alert('新增失敗: ' + (err.response?.data?.message || err.message));
          }
        }}
      />
    </div>
  );
};

// ========== Permissions Tab Component ==========
interface PermissionsTabProps {
  permissionsTree: PermissionTreeDto[];
  users: UserWithRolesDto[];
  roles: RoleWithPermissionsDto[];
  isLoading: boolean;
  onAddRole: () => void;
  onAddPermission: () => void;
  queryClient: any;
}

function PermissionsTab({ permissionsTree, users, roles, isLoading, onAddRole, onAddPermission, queryClient }: PermissionsTabProps) {
  const [activePermTab, setActivePermTab] = useState<'users' | 'roles' | 'tree'>('users');
  const [searchUser, setSearchUser] = useState('');
  const [editingUserRoles, setEditingUserRoles] = useState<string | null>(null);
  const [editingRolePerms, setEditingRolePerms] = useState<string | null>(null);
  const [selectedRolePerms, setSelectedRolePerms] = useState<string[]>([]);

  const updateUserRoles = useMutation({
    mutationFn: ({ userId, roleIds }: { userId: string; roleIds: string[] }) =>
      systemSettingsApi.updateUserRoles(userId, roleIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setEditingUserRoles(null);
      alert('使用者角色已更新');
    },
    onError: (err: any) => {
      alert('更新失敗: ' + (err.response?.data?.message || err.message));
    }
  });

  const updateRolePerms = useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: string; permissionIds: string[] }) =>
      systemSettingsApi.updateRolePermissions(roleId, permissionIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      setEditingRolePerms(null);
      alert('角色權限已更新');
    },
    onError: (err: any) => {
      alert('更新失敗: ' + (err.response?.data?.message || err.message));
    }
  });

  const deleteRole = useMutation({
    mutationFn: (roleId: string) => systemSettingsApi.deleteRole(roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      alert('角色已刪除');
    },
    onError: (err: any) => {
      alert('刪除失敗: ' + (err.response?.data?.message || err.message));
    }
  });

  const filteredUsers = users.filter(u => 
    u.username.toLowerCase().includes(searchUser.toLowerCase())
  );

  const handleEditUserRoles = (user: UserWithRolesDto) => {
    setEditingUserRoles(user.id);
  };

  const handleSaveUserRoles = (userId: string, roleIds: string[]) => {
    updateUserRoles.mutate({ userId, roleIds });
  };

  const handleEditRolePerms = (role: RoleWithPermissionsDto) => {
    setEditingRolePerms(role.id);
    setSelectedRolePerms(role.permissionIds);
  };

  const handleSaveRolePerms = (roleId: string) => {
    updateRolePerms.mutate({ roleId, permissionIds: selectedRolePerms });
  };

  const handleTogglePermission = (permId: string) => {
    setSelectedRolePerms(prev => 
      prev.includes(permId) 
        ? prev.filter(id => id !== permId)
        : [...prev, permId]
    );
  };

  const handleDeleteRole = (roleId: string, roleName: string) => {
    if (confirm(`確定要刪除角色「${roleName}」嗎？`)) {
      deleteRole.mutate(roleId);
    }
  };

  return (
    <div className="permissions-tab">
      <div className="tab-bar" style={{ marginBottom: 16 }}>
        <button className={`tab-btn ${activePermTab === 'users' ? 'active' : ''}`} onClick={() => setActivePermTab('users')}>
          使用者管理
        </button>
        <button className={`tab-btn ${activePermTab === 'roles' ? 'active' : ''}`} onClick={() => setActivePermTab('roles')}>
          角色管理
        </button>
        <button className={`tab-btn ${activePermTab === 'tree' ? 'active' : ''}`} onClick={() => setActivePermTab('tree')}>
          權限樹
        </button>
      </div>

      {activePermTab === 'users' && (
        <div className="users-section">
          <div className="filter-bar">
            <div className="filter-search">
              <Search size={14} />
              <input placeholder="搜尋使用者..." value={searchUser} onChange={e => setSearchUser(e.target.value)} />
            </div>
          </div>
          <div className="glass-card">
            {isLoading ? (
              <div className="loading">載入中...</div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>帳號</th>
                    <th>Email</th>
                    <th>角色</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUsers.map(user => (
                    <tr key={user.id}>
                      <td>{user.username}</td>
                      <td>{user.email || '-'}</td>
                      <td>
                        {editingUserRoles === user.id ? (
                          <RoleMultiSelect
                            roles={roles}
                            selectedRoleIds={user.roles.map(r => r.id)}
                            onChange={(roleIds) => handleSaveUserRoles(user.id, roleIds)}
                            onCancel={() => setEditingUserRoles(null)}
                          />
                        ) : (
                          <span className="role-tags">
                            {user.roles.length > 0 
                              ? user.roles.map(r => <span key={r.id} className="role-tag">{r.roleName}</span>)
                              : <span className="no-role">未設定</span>
                            }
                          </span>
                        )}
                      </td>
                      <td>
                        {editingUserRoles !== user.id && (
                          <button className="icon-action-btn" onClick={() => handleEditUserRoles(user)} title="編輯角色">
                            <Edit2 size={14} />
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {activePermTab === 'roles' && (
        <div className="roles-section">
          <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12, gap: 8 }}>
            <button className="btn btn-ghost btn-sm" onClick={onAddPermission}>新增權限</button>
            <button className="btn btn-primary btn-sm" onClick={onAddRole}>新增角色</button>
          </div>
          <div className="glass-card">
            {isLoading ? (
              <div className="loading">載入中...</div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>角色名稱</th>
                    <th>描述</th>
                    <th>權限數</th>
                    <th>系統角色</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {roles.map(role => (
                    <tr key={role.id}>
                      <td>{role.roleName}</td>
                      <td>{role.description || '-'}</td>
                      <td>{role.permissionIds.length}</td>
                      <td>{role.isSystem ? <span className="badge warning">系統</span> : '-'}</td>
                      <td>
                        <div style={{ display: 'flex', gap: 4 }}>
                          <button className="icon-action-btn" onClick={() => handleEditRolePerms(role)} title="編輯權限">
                            <Eye size={14} />
                          </button>
                          {!role.isSystem && (
                            <button className="icon-action-btn" onClick={() => handleDeleteRole(role.id, role.roleName)} title="刪除">
                              <Trash2 size={14} />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {editingRolePerms && (
            <RolePermissionEditor
              roles={roles}
              permissionsTree={permissionsTree}
              editingRolePerms={editingRolePerms}
              selectedRolePerms={selectedRolePerms}
              onTogglePermission={handleTogglePermission}
              onSave={() => handleSaveRolePerms(editingRolePerms)}
              onCancel={() => setEditingRolePerms(null)}
            />
          )}
        </div>
      )}

      {activePermTab === 'tree' && (
        <div className="tree-section">
          <div className="glass-card">
            {isLoading ? (
              <div className="loading">載入中...</div>
            ) : (
              <div className="permission-tree">
                {permissionsTree.length === 0 ? (
                  <div className="empty">尚無權限資料，請先新增權限</div>
                ) : (
                  permissionsTree.map(module => (
                    <PermissionTreeNode key={module.id} node={module} />
                  ))
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function CurrencyTab({ queryClient }: { queryClient: any }) {
  const [showAddCurrency, setShowAddCurrency] = useState(false);
  const [showAddRate, setShowAddRate] = useState(false);

  const { data: currenciesRes, isLoading: curLoading } = useQuery({
    queryKey: ['currencies'],
    queryFn: () => currenciesApi.listCurrencies().then(res => res.data)
  });

  const { data: ratesRes, isLoading: ratesLoading } = useQuery({
    queryKey: ['exchange-rates'],
    queryFn: () => currenciesApi.listRates().then(res => res.data)
  });

  const currencies = (currenciesRes as any)?.data || [];
  const rates = (ratesRes as any)?.data || [];

  return (
    <div className="currency-tab" style={{ display: 'flex', flexDirection: 'column', gap: 24 }}>
      <div className="section">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <h3 style={{ fontSize: 16, display: 'flex', alignItems: 'center', gap: 8 }}><Globe size={18} color="var(--lux-gold)" /> 幣別清單</h3>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAddCurrency(true)}><Plus size={14} /> 新增幣別</button>
        </div>
        <div className="glass-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>代碼</th>
                <th>名稱</th>
                <th>符號</th>
                <th>基準幣別</th>
                <th>狀態</th>
              </tr>
            </thead>
            <tbody>
              {currencies.map((c: any) => (
                <tr key={c.id}>
                  <td className="mono">{c.currencyCode}</td>
                  <td>{c.currencyName}</td>
                  <td className="mono">{c.symbol}</td>
                  <td>{c.isBaseCurrency ? <span className="badge success">基準</span> : '-'}</td>
                  <td>{c.isActive ? <span className="badge info">啟用</span> : <span className="badge secondary">停用</span>}</td>
                </tr>
              ))}
              {currencies.length === 0 && !curLoading && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 20, color: 'var(--slate-500)' }}>尚無幣別資料</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="section">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
          <h3 style={{ fontSize: 16, display: 'flex', alignItems: 'center', gap: 8 }}><TrendingUp size={18} color="var(--lux-gold)" /> 匯率維護</h3>
          <button className="btn btn-primary btn-sm" onClick={() => setShowAddRate(true)}><Plus size={14} /> 新增匯率</button>
        </div>
        <div className="glass-card">
          <table className="data-table">
            <thead>
              <tr>
                <th>來源</th>
                <th>目標</th>
                <th>匯率</th>
                <th>生效日期</th>
                <th>類型</th>
                <th>來源</th>
              </tr>
            </thead>
            <tbody>
              {rates.map((r: any) => (
                <tr key={r.id}>
                  <td className="mono">{r.fromCurrency}</td>
                  <td className="mono">{r.toCurrency}</td>
                  <td className="mono" style={{ color: 'var(--lux-gold)', fontWeight: 700 }}>{r.rate}</td>
                  <td>{r.effectiveDate?.split('T')[0]}</td>
                  <td>{r.rateType === 'Standard' ? '標準' : r.rateType}</td>
                  <td>{r.source === 'Manual' ? '手動' : r.source}</td>
                </tr>
              ))}
              {rates.length === 0 && !ratesLoading && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20, color: 'var(--slate-500)' }}>尚無匯率資料</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <FormModal
        open={showAddCurrency}
        title="新增幣別"
        onClose={() => setShowAddCurrency(false)}
        fields={[
          { name: 'currencyCode', label: '幣別代碼 *', type: 'text', required: true, placeholder: 'USD' },
          { name: 'currencyName', label: '幣別名稱 *', type: 'text', required: true, placeholder: '美元' },
          { name: 'symbol', label: '符號', type: 'text', placeholder: '$' },
          { name: 'isBaseCurrency', label: '基準幣別', type: 'checkbox' },
        ]}
        onSubmit={async (data) => {
          const result = currencySchema.safeParse(data);
          if (!result.success) {
            toast.error(result.error.issues[0].message);
            return;
          }
          await currenciesApi.createCurrency(result.data as any);
          queryClient.invalidateQueries({ queryKey: ['currencies'] });
          setShowAddCurrency(false);
          toast.success('幣別已新增');
        }}
      />

      <FormModal
        open={showAddRate}
        title="新增匯率"
        onClose={() => setShowAddRate(false)}
        fields={[
          { name: 'fromCurrency', label: '來源幣別 *', type: 'select', required: true, options: currencies.map((c: any) => ({ value: c.currencyCode, label: c.currencyCode })) },
          { name: 'toCurrency', label: '目標幣別 *', type: 'select', required: true, options: currencies.map((c: any) => ({ value: c.currencyCode, label: c.currencyCode })) },
          { name: 'rate', label: '匯率 *', type: 'number', required: true, step: 0.0001 },
          { name: 'effectiveDate', label: '生效日期 *', type: 'date', required: true, defaultValue: new Date().toISOString().split('T')[0] },
          { name: 'rateType', label: '匯率類型 *', type: 'select', required: true, options: [{ value: 'Standard', label: '標準' }, { value: 'Custom', label: '自訂' }] },
          { name: 'source', label: '數據來源 *', type: 'select', required: true, options: [{ value: 'Manual', label: '手動維護' }] },
        ]}
        onSubmit={async (data) => {
          const result = exchangeRateSchema.safeParse({ ...data, rate: Number(data.rate) });
          if (!result.success) {
            toast.error(result.error.issues[0].message);
            return;
          }
          await currenciesApi.createRate(result.data as any);
          queryClient.invalidateQueries({ queryKey: ['exchange-rates'] });
          setShowAddRate(false);
          toast.success('匯率已更新');
        }}
      />
    </div>
  );
}

// ========== Theme Tab Component ==========
interface ThemesTabProps {
  themes: ThemeDto[];
  currentTheme: string;
  setTheme: (name: ThemeName) => void;
  setCustomColors: (primary?: string, accent?: string) => void;
  isLoading: boolean;
  queryClient: any;
}

function ThemesTab({ themes, currentTheme, setTheme, setCustomColors, isLoading }: ThemesTabProps) {
  const [customPrimary, setCustomPrimary] = useState('#2d6ef5');
  const [customAccent, setCustomAccent] = useState('#2d6ef5');
  const [showSaveBtn, setShowSaveBtn] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const handleSelectTheme = (themeName: ThemeName) => {
    setTheme(themeName);
    setShowSaveBtn(false);
  };

  const handleColorChange = (primary?: string, accent?: string) => {
    setCustomPrimary(primary || '#2d6ef5');
    setCustomAccent(accent || '#2d6ef5');
    setShowSaveBtn(true);
    
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setCustomColors(primary, accent);
      setShowSaveBtn(false);
    }, 500);
  };

  const handleSaveCustomColors = () => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    setCustomColors(customPrimary, customAccent);
    setShowSaveBtn(false);
  };

  return (
    <div className="themes-tab">
      {isLoading ? (
        <div className="loading">載入中...</div>
      ) : (
        <>
          <div className="theme-cards">
            {themes.map(theme => (
              <div 
                key={theme.name} 
                className={`theme-card ${currentTheme === theme.name ? 'active' : ''}`}
                onClick={() => handleSelectTheme(theme.name as ThemeName)}
              >
                <div className="theme-preview" style={{
                  background: theme.colors?.background || '#04080f',
                  border: `2px solid ${currentTheme === theme.name ? theme.colors?.primary : theme.colors?.border}`
                }}>
                  <div className="preview-header" style={{ background: theme.colors?.surface, borderBottom: `1px solid ${theme.colors?.border}` }}>
                    <div className="preview-dot" style={{ background: theme.colors?.primary }} />
                  </div>
                  <div className="preview-sidebar" style={{ background: theme.colors?.surface, borderRight: `1px solid ${theme.colors?.border}` }}>
                    <div className="preview-item" style={{ background: currentTheme === theme.name ? theme.colors?.primary : 'transparent' }} />
                    <div className="preview-item" />
                  </div>
                  <div className="preview-content">
                    <div className="preview-card" style={{ background: theme.colors?.surface, border: `1px solid ${theme.colors?.border}` }}>
                      <div style={{ background: theme.colors?.primary, width: '60%', height: 8, borderRadius: 4 }} />
                    </div>
                  </div>
                </div>
                <div className="theme-info">
                  <div className="theme-name">
                    {theme.displayName}
                    {currentTheme === theme.name && <Check size={16} style={{ color: 'var(--emerald-400)', marginLeft: 4 }} />}
                  </div>
                  <div className="theme-desc">{theme.description}</div>
                </div>
              </div>
            ))}
          </div>

          {currentTheme === 'custom' && (
            <div className="custom-colors">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
                <h3>自訂顏色</h3>
                {showSaveBtn && (
                  <button className="btn btn-primary btn-sm" onClick={handleSaveCustomColors}>
                    儲存變更
                  </button>
                )}
              </div>
              <div className="color-pickers">
                <div className="color-picker">
                  <label>主色</label>
                  <input 
                    type="color" 
                    value={customPrimary} 
                    onChange={e => handleColorChange(e.target.value, customAccent)}
                  />
                  <span>{customPrimary}</span>
                </div>
                <div className="color-picker">
                  <label>強調色</label>
                  <input 
                    type="color" 
                    value={customAccent} 
                    onChange={e => handleColorChange(customPrimary, e.target.value)}
                  />
                  <span>{customAccent}</span>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

// ========== Helper Components ==========

function RoleMultiSelect({ roles, selectedRoleIds, onChange, onCancel }: {
  roles: RoleWithPermissionsDto[];
  selectedRoleIds: string[];
  onChange: (roleIds: string[]) => void;
  onCancel: () => void;
}) {
  const [selected, setSelected] = useState<string[]>(selectedRoleIds);

  const handleToggle = (roleId: string) => {
    const newSelected = selected.includes(roleId)
      ? selected.filter(id => id !== roleId)
      : [...selected, roleId];
    setSelected(newSelected);
    onChange(newSelected);
  };

  return (
    <div className="role-multi-select">
      {roles.map(role => (
        <label key={role.id} className="role-checkbox">
          <input 
            type="checkbox" 
            checked={selected.includes(role.id)}
            onChange={() => handleToggle(role.id)}
          />
          <span>{role.roleName}</span>
        </label>
      ))}
      <button className="btn btn-ghost btn-sm" onClick={onCancel}>完成</button>
    </div>
  );
}

function RolePermissionEditor({ roles, permissionsTree, editingRolePerms, selectedRolePerms, onTogglePermission, onSave, onCancel }: {
  roles: RoleWithPermissionsDto[];
  permissionsTree: PermissionTreeDto[];
  editingRolePerms: string;
  selectedRolePerms: string[];
  onTogglePermission: (permId: string) => void;
  onSave: () => void;
  onCancel: () => void;
}) {
  const role = roles.find(r => r.id === editingRolePerms);

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-card">
        <div className="modal-header">
          <h3>編輯角色權限 - {role?.roleName}</h3>
          <button onClick={onCancel}><X size={18} /></button>
        </div>
        <div className="modal-body">
          <div className="permission-tree-editor">
            {permissionsTree.map(module => (
              <PermissionTreeNode 
                key={module.id} 
                node={module} 
                selectedPerms={selectedRolePerms}
                onToggle={onTogglePermission}
              />
            ))}
          </div>
        </div>
        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onCancel}>取消</button>
          <button className="btn btn-primary" onClick={onSave}>儲存</button>
        </div>
      </div>
    </div>
  );
}

function PermissionTreeNode({ node, selectedPerms, onToggle, level = 0 }: {
  node: PermissionTreeDto;
  selectedPerms?: string[];
  onToggle?: (permId: string) => void;
  level?: number;
}) {
  const [expanded, setExpanded] = useState(level < 2);
  const hasChildren = node.children && node.children.length > 0;
  const isSelected = selectedPerms?.includes(node.id);

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'menu': return 'var(--cobalt-400)';
      case 'page': return 'var(--amber-400)';
      case 'action': return 'var(--emerald-400)';
      default: return 'var(--slate-400)';
    }
  };

  return (
    <div className="tree-node" style={{ marginLeft: level * 20 }}>
      <div className="tree-node-content">
        {hasChildren && (
          <button className="tree-expand-btn" onClick={() => setExpanded(!expanded)}>
            {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          </button>
        )}
        {!hasChildren && <div style={{ width: 20 }} />}
        
        {selectedPerms !== undefined && onToggle ? (
          <button 
            className="tree-checkbox"
            onClick={() => onToggle(node.id)}
            style={{ color: isSelected ? 'var(--emerald-400)' : 'var(--slate-400)' }}
          >
            {isSelected ? <CheckSquare size={16} /> : <Square size={16} />}
          </button>
        ) : null}
        
        <span className="tree-code" style={{ color: getTypeColor(node.permissionType) }}>
          {node.permissionCode}
        </span>
        <span className="tree-name">{node.moduleName}</span>
        <span className="tree-type-badge">{node.permissionType}</span>
      </div>
      
      {hasChildren && expanded && (
        <div className="tree-children">
          {node.children.map(child => (
            <PermissionTreeNode 
              key={child.id} 
              node={child} 
              selectedPerms={selectedPerms}
              onToggle={onToggle}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  );
}