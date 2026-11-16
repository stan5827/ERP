import React, { useState, useEffect, useRef } from 'react';
import { Search, Plus, Trash2, Edit2, Eye, KeyRound, ChevronDown, X, Check, ChevronRight, Globe, TrendingUp, ShieldCheck, Wrench, Package, Factory, ClipboardCheck, ShoppingCart, Boxes, Wallet, Users, Bot, Leaf, UserRound } from 'lucide-react';
import { useQuery, useQueryClient, useMutation } from '@tanstack/react-query';
import type { QueryClient } from '@tanstack/react-query';
import { authApi, systemSettingsApi, currenciesApi, employeesApi, usersApi, PermissionTreeDto, UserWithRolesDto, RoleWithPermissionsDto, ThemeDto, CurrencyDto, ExchangeRateDto, EmployeeDto } from '../api/api';
import { useTheme } from '../theme/themeContext';
import { useRole } from '../contexts/useRole';
import { currencySchema, exchangeRateSchema } from '../utils/erpSchemas';
import { toast } from 'react-hot-toast';
import { FormModal } from '../components/FormModal';
import { ThemeName } from '../theme/themes';
import { getPermissionDisplayName, getPermissionSelectionState, getPermissionTypeLabel, togglePermissionSelection } from '../utils/permissionTree';
import { getRoleDisplayName as translateRoleDisplayName, matchesSystemUserSearch } from '../utils/systemSettingsDisplay';

const roleDisplayNames: Record<string, string> = {
  admin: '系統管理員',
  'ai analyst': 'AI 分析人員',
  'asset manager': '資產管理人員',
  employee: '一般員工',
  'engineering engineer': '工程人員',
  'engineering manager': '工程主管',
  'esg specialist': '永續管理人員',
  finance: '財務人員',
  'finance manager': '財務主管',
  'hr manager': '人資主管',
  'maintenance technician': '設備維護人員',
  operator: '現場作業員',
  'process engineer': '製程工程師',
  'product engineer': '產品工程師',
  'production planner': '生管與物料規劃人員',
  'production supervisor': '生產主管',
  'purchasing manager': '採購主管',
  'purchasing specialist': '採購專員',
  'quality inspector': '品檢人員',
  'quality engineer': '品質工程師',
  'quality manager': '品質主管',
  'r&d engineer': '研發人員',
  'sales manager': '業務主管',
  'sales representative': '業務人員',
  'warehouse manager': '倉儲主管',
  'warehouse operator': '倉管人員',
  'uat warehouse approver': 'UAT 倉儲覆核人員',
  'uat warehouse operator': 'UAT 倉儲作業人員',
};

const systemRoleDescriptions: Record<string, string> = {
  admin: '管理帳號、角色和全系統設定。只有負責系統維護的人員需要此角色。',
  'ai analyst': '查看 AI 分析、監控與排程建議，不負責維護其他部門資料。',
  'asset manager': '管理公司設備與資產資料，並處理折舊及資產異動。',
  employee: '一般員工使用的基本角色，可查看總覽與行事曆。',
  'engineering engineer': '適合負責工程資料的人員，可維護工藝、BOM 和工作中心。',
  'engineering manager': '適合工程主管，可維護工程主檔並審核工程相關事項。',
  'esg specialist': '維護碳排、能源與永續資料，整理 ESG 報告。',
  finance: '處理日常財務資料，並查看成本與預算資訊。',
  'finance manager': '適合財務主管，可管理預算、成本並審核財務作業。',
  'hr manager': '管理員工、人事、出勤、請假與薪資相關資料。',
  'maintenance technician': '適合設備維修人員，可登錄維修與保養紀錄。',
  operator: '適合產線作業人員，可查看工單並回報生產進度。',
  'process engineer': '適合規劃製造流程的人員，可維護工序、工藝路線與工作中心。',
  'product engineer': '適合負責產品結構的人員，可建立與維護產品 BOM。',
  'production planner': '規劃生產排程與物料需求，協助安排產能和工單。',
  'production supervisor': '帶領現場生產、追蹤工單進度並審核報工。',
  'purchasing manager': '管理採購與供應商，並審核採購作業。',
  'purchasing specialist': '處理日常請購、採購單與供應商資料。',
  'quality inspector': '執行進料、製程與成品檢驗，並登錄檢驗結果。',
  'quality engineer': '建立與維護檢驗規格，並查詢產品及製程資料。',
  'quality manager': '管理品質異常與檢驗結果，審核重要品質判定。',
  'r&d engineer': '管理研發專案、試作與研發相關紀錄。',
  'sales manager': '管理客戶、報價、訂單與出貨，並審核業務作業。',
  'sales representative': '維護客戶資料、報價與訂單，處理日常業務。',
  'warehouse manager': '管理庫存與倉儲作業，並覆核重要庫存流程。',
  'warehouse operator': '處理日常收貨、出貨、調撥與盤點。',
  'uat warehouse approver': '僅供測試環境使用，請勿指派給正式帳號。',
  'uat warehouse operator': '僅供測試環境使用，請勿指派給正式帳號。',
};

const getRoleDisplayName = (roleName: string) =>
  translateRoleDisplayName(roleName, roleDisplayNames);

const getRoleDisplayDescription = (roleName: string, description?: string) =>
  systemRoleDescriptions[roleName.trim().toLocaleLowerCase()]
    ?? (description ? `此角色的工作範圍由管理者設定：${description}` : '尚未填寫用途說明，可先查看此角色的權限再指派。');

function getRoleIcon(roleName: string) {
  const normalized = roleName.trim().toLocaleLowerCase();
  if (normalized === 'admin') return ShieldCheck;
  if (normalized.includes('quality')) return ClipboardCheck;
  if (normalized.includes('product')) return Package;
  if (normalized.includes('process') || normalized.includes('engineering')) return Wrench;
  if (normalized.includes('production') || normalized === 'operator') return Factory;
  if (normalized.includes('purchasing') || normalized.includes('sales')) return ShoppingCart;
  if (normalized.includes('warehouse')) return Boxes;
  if (normalized.includes('finance')) return Wallet;
  if (normalized.includes('hr') || normalized === 'employee') return Users;
  if (normalized.includes('ai')) return Bot;
  if (normalized.includes('esg')) return Leaf;
  if (normalized.includes('asset') || normalized.includes('maintenance')) return Wrench;
  return UserRound;
}

function getErrorMessage(error: unknown): string {
  if (typeof error !== 'object' || error === null)
    return error instanceof Error ? error.message : String(error);
  const response = 'response' in error ? error.response : undefined;
  const responseData = typeof response === 'object' && response !== null && 'data' in response
    ? response.data
    : undefined;
  const responseMessage = typeof responseData === 'object' && responseData !== null && 'message' in responseData
    ? responseData.message
    : undefined;
  return typeof responseMessage === 'string'
    ? responseMessage
    : error instanceof Error ? error.message : String(error);
}

export const SystemSettings: React.FC = () => {
  const { themeName, setTheme, setCustomColors } = useTheme();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'permissions' | 'themes' | 'currencies'>('permissions');
  const [showAddRoleModal, setShowAddRoleModal] = useState(false);
  const [showAddPermissionModal, setShowAddPermissionModal] = useState(false);

  const { currentRole, hasPermission } = useRole();
  const canManageAccess = currentRole === 'admin'
    || hasPermission('systemsettings:access', 'view');
  const canCreateAccess = currentRole === 'admin'
    || hasPermission('systemsettings:access', 'create');
  const canEditAccess = currentRole === 'admin'
    || hasPermission('systemsettings:access', 'edit');
  const canDeleteAccess = currentRole === 'admin'
    || hasPermission('systemsettings:access', 'delete');
  const canViewThemes = currentRole === 'admin'
    || hasPermission('systemsettings:themes', 'view');

  // Permissions tab state
  const { data: permTreeRes, isLoading: permissionsLoading } = useQuery({
    queryKey: ['permissions-tree'],
    queryFn: () => systemSettingsApi.getPermissionTree().then(res => res.data),
    enabled: canManageAccess && activeTab === 'permissions',
  });

  const { data: usersRes, isLoading: usersLoading } = useQuery({
    queryKey: ['users'],
    queryFn: () => systemSettingsApi.getUsers().then(res => res.data),
    enabled: canManageAccess && activeTab === 'permissions',
  });

  const { data: rolesRes, isLoading: rolesLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: () => systemSettingsApi.getRoles().then(res => res.data),
    enabled: canManageAccess && activeTab === 'permissions',
  });

  // Themes tab state
  const { data: themesRes, isLoading: themesLoading } = useQuery({
    queryKey: ['available-themes'],
    queryFn: () => systemSettingsApi.getThemes().then(res => res.data),
    enabled: canViewThemes && activeTab === 'themes',
  });

  const permissionsTree: PermissionTreeDto[] = permTreeRes?.data ?? [];
  const users: UserWithRolesDto[] = usersRes?.data ?? [];
  const roles: RoleWithPermissionsDto[] = rolesRes?.data ?? [];
  const availableThemes: ThemeDto[] = themesRes?.data ?? [];

  return (
    <div className="page-container">
      <div className="module-header">
        <h1>系統設定</h1>
        <div className="tab-bar">
          {canManageAccess && (
            <button
              className={`tab-btn ${activeTab === 'permissions' ? 'active' : ''}`}
              onClick={() => setActiveTab('permissions')}
            >
              權限管理
            </button>
          )}
          {canViewThemes && (
            <button
              className={`tab-btn ${activeTab === 'themes' ? 'active' : ''}`}
              onClick={() => setActiveTab('themes')}
            >
              主題設定
            </button>
          )}
          <button 
            className={`tab-btn ${activeTab === 'currencies' ? 'active' : ''}`}
            onClick={() => setActiveTab('currencies')}
          >
            幣別與匯率
          </button>
        </div>
      </div>

      {activeTab === 'permissions' && canManageAccess && (
        <PermissionsTab 
          permissionsTree={permissionsTree}
          users={users}
          roles={roles}
          isLoading={permissionsLoading || usersLoading || rolesLoading}
          onAddRole={() => setShowAddRoleModal(true)}
          onAddPermission={() => setShowAddPermissionModal(true)}
          canCreateAccess={canCreateAccess}
          canEditAccess={canEditAccess}
          canDeleteAccess={canDeleteAccess}
          queryClient={queryClient}
        />
      )}

      {activeTab === 'themes' && canViewThemes && (
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
          } catch (err: unknown) {
            alert('新增失敗: ' + getErrorMessage(err));
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
          } catch (err: unknown) {
            alert('新增失敗: ' + getErrorMessage(err));
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
  canCreateAccess: boolean;
  canEditAccess: boolean;
  canDeleteAccess: boolean;
  queryClient: QueryClient;
}

function PermissionsTab({
  permissionsTree,
  users,
  roles,
  isLoading,
  onAddRole,
  onAddPermission,
  canCreateAccess,
  canEditAccess,
  canDeleteAccess,
  queryClient,
}: PermissionsTabProps) {
  const [activePermTab, setActivePermTab] = useState<'users' | 'roles' | 'tree'>('users');
  const [searchUser, setSearchUser] = useState('');
  const [searchRole, setSearchRole] = useState('');
  const [showCreateUserModal, setShowCreateUserModal] = useState(false);
  const [resetPasswordUser, setResetPasswordUser] = useState<UserWithRolesDto | null>(null);
  const [editingUserRoles, setEditingUserRoles] = useState<string | null>(null);
  const [editingUserEmail, setEditingUserEmail] = useState('');
  const [editingRolePerms, setEditingRolePerms] = useState<string | null>(null);
  const [selectedRolePerms, setSelectedRolePerms] = useState<string[]>([]);

  const {
    data: employeesRes,
    isLoading: employeesLoading,
    error: employeesError,
    refetch: refetchEmployees
  } = useQuery({
    queryKey: ['employees', 'user-provisioning'],
    queryFn: async () => {
      const pageSize = 200;
      const firstPage = await employeesApi.list({ status: 'active', page: '1', pageSize: String(pageSize) });
      const firstPageData = firstPage.data.data ?? [];
      const activeEmployees = [...firstPageData];
      const totalCount = firstPage.data.totalCount ?? firstPageData.length;

      for (let page = 2; activeEmployees.length < totalCount; page += 1) {
        const nextPage = await employeesApi.list({ status: 'active', page: String(page), pageSize: String(pageSize) });
        const nextPageData = nextPage.data.data ?? [];
        if (nextPageData.length === 0) break;
        activeEmployees.push(...nextPageData);
      }

      return activeEmployees;
    },
    enabled: activePermTab === 'users'
  });
  const {
    data: linkedEmployeeIdsRes,
    isLoading: linkedEmployeesLoading,
    error: linkedEmployeesError,
    refetch: refetchLinkedEmployees
  } = useQuery({
    queryKey: ['users', 'linked-employee-ids'],
    queryFn: () => systemSettingsApi.getLinkedEmployeeIds().then(res => res.data),
    enabled: activePermTab === 'users'
  });
  const employees = employeesRes ?? [];
  const linkedEmployeeIds = new Set(linkedEmployeeIdsRes?.data ?? []);
  const availableEmployees = employees.filter(employee => !linkedEmployeeIds.has(employee.id));
  const activeRoles = roles.filter(role => role.isActive);
  const filteredRoles = roles.filter(role =>
    `${getRoleDisplayName(role.roleName)} ${getRoleDisplayDescription(role.roleName, role.description)}`
      .toLocaleLowerCase()
      .includes(searchRole.trim().toLocaleLowerCase())
  );

  const updateRolePerms = useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: string; permissionIds: string[] }) =>
      systemSettingsApi.updateRolePermissions(roleId, permissionIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      setEditingRolePerms(null);
      alert('角色權限已更新');
    },
    onError: (err: unknown) => {
      alert('更新失敗: ' + getErrorMessage(err));
    }
  });

  const deleteRole = useMutation({
    mutationFn: (roleId: string) => systemSettingsApi.deleteRole(roleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      alert('角色已刪除');
    },
    onError: (err: unknown) => {
      alert('刪除失敗: ' + getErrorMessage(err));
    }
  });

  const employeeLabels = new Map(employees.map(employee => [
    employee.id,
    `${employee.employeeNo} - ${employee.chineseName}`
  ]));
  const filteredUsers = users.filter(user =>
    matchesSystemUserSearch(
      user,
      user.employeeId ? employeeLabels.get(user.employeeId) : undefined,
      searchUser,
    )
  );

  const getCreateUserErrorMessage = (error: unknown): string => {
    const responseData = typeof error === 'object' && error !== null && 'response' in error
      ? (error as { response?: { data?: { message?: string } } }).response?.data
      : undefined;
    const serverMessage = responseData?.message
      ?? (error instanceof Error ? error.message : undefined);
    const translatedMessages: Record<string, string> = {
      'Username already exists': '此帳號已存在，請使用其他帳號。',
      'An employee must be linked to the account': '請選擇要綁定的員工。',
      'Employee does not exist': '找不到所選員工，請重新整理後再試。',
      'Only active employees can be linked to an account': '只能為在職員工建立帳號。',
      'Employee already has an account': '此員工已綁定其他使用者帳號。',
      'Employee must have an identity document, hire date, department, and job title before account provisioning': '員工需先補齊身分證件、到職日、部門與職稱資料。',
      'Employee must be assigned to an active department before account provisioning': '員工必須指派至啟用中的部門。',
      'Employee must be assigned to an active job title before account provisioning': '員工必須指派至啟用中的職稱。',
      'One or more roles do not exist': '所選角色不存在或已停用，請重新選擇。',
      'Username and password are invalid; password must be at least 8 characters and contain both letters and numbers': '帳號不可空白；密碼至少 8 個字元，並須同時包含英文字母與數字。'
    };

    return (serverMessage && translatedMessages[serverMessage])
      || serverMessage
      || '建立使用者失敗，請稍後再試。';
  };

  const handleCreateUser = async (data: Record<string, unknown>) => {
    const password = String(data.password ?? '');
    if (!/[A-Za-z]/.test(password) || !/[0-9]/.test(password)) {
      throw new Error('密碼至少 8 個字元，並須同時包含英文字母與數字。');
    }

    try {
      await usersApi.create({
        username: String(data.username ?? '').trim(),
        password,
        email: String(data.email ?? '').trim() || undefined,
        employeeId: String(data.employeeId ?? ''),
        roleIds: Array.isArray(data.roleIds) ? data.roleIds.map(String) : []
      });
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      setShowCreateUserModal(false);
      toast.success('使用者建立成功；首次登入時必須變更密碼。');
    } catch (error) {
      throw new Error(getCreateUserErrorMessage(error));
    }
  };

  const handleEditUserRoles = (user: UserWithRolesDto) => {
    setEditingUserRoles(user.id);
    setEditingUserEmail(user.email ?? '');
  };

  const handleSaveUser = async (user: UserWithRolesDto, roleIds: string[]) => {
    const selectedRoleIds = new Set(roleIds);
    const hasRoleChanges = user.roles.length !== selectedRoleIds.size
      || user.roles.some(role => !selectedRoleIds.has(role.id));
    const email = editingUserEmail.trim() || null;
    const emailChanged = (user.email ?? null) !== email;
    if (!hasRoleChanges && !emailChanged) {
      setEditingUserRoles(null);
      return;
    }

    let reason: string | null = null;
    if (hasRoleChanges) {
      const input = window.prompt('請輸入角色異動原因（將記錄於稽核日誌）');
      if (input === null) return;
      if (!input.trim()) {
        toast.error('請提供角色異動原因。');
        return;
      }
      reason = input.trim();
    }

    try {
      if (emailChanged)
        await usersApi.update(user.id, { email });
      if (hasRoleChanges)
        await usersApi.updateRoles(user.id, { roleIds, reason: reason! });
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      setEditingUserRoles(null);
      toast.success(hasRoleChanges ? '使用者資料與角色已更新' : '使用者 Email 已更新');
    } catch (error: unknown) {
      toast.error(getErrorMessage(error) || '使用者資料更新失敗');
    }
  };

  const handleEditRolePerms = (role: RoleWithPermissionsDto) => {
    setEditingRolePerms(role.id);
    setSelectedRolePerms(role.permissionIds);
  };

  const handleSaveRolePerms = (roleId: string) => {
    updateRolePerms.mutate({ roleId, permissionIds: selectedRolePerms });
  };

  const handleTogglePermission = (node: PermissionTreeDto) => {
    setSelectedRolePerms(prev => togglePermissionSelection(node, prev));
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
          <div className="filter-bar" style={{ justifyContent: 'space-between', gap: 12 }}>
            <div className="filter-search">
              <Search size={14} />
              <input placeholder="搜尋使用者..." value={searchUser} onChange={e => setSearchUser(e.target.value)} />
            </div>
            {employeesError || linkedEmployeesError ? (
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => {
                  void refetchEmployees();
                  void refetchLinkedEmployees();
                }}
              >
                重新載入員工
              </button>
            ) : (
              <button
                className="btn btn-primary btn-sm"
                onClick={() => setShowCreateUserModal(true)}
                disabled={employeesLoading || linkedEmployeesLoading || availableEmployees.length === 0 || activeRoles.length === 0}
                title={activeRoles.length === 0 ? '請先建立或啟用至少一個角色' : undefined}
              >
                <Plus size={14} /> 建立使用者
              </button>
            )}
          </div>
          {(employeesError || linkedEmployeesError) && (
            <div role="alert" style={{ color: 'var(--crimson-400)', marginBottom: 12 }}>
              {employeesError
                ? `員工資料載入失敗：${employeesError instanceof Error ? employeesError.message : '未知錯誤'}`
                : `員工帳號綁定資料載入失敗：${linkedEmployeesError instanceof Error ? linkedEmployeesError.message : '未知錯誤'}`}
            </div>
          )}
          {!employeesError && !linkedEmployeesError && !employeesLoading && !linkedEmployeesLoading && availableEmployees.length === 0 && (
            <div style={{ color: 'var(--slate-500)', marginBottom: 12, fontSize: 12 }}>
              {employees.length === 0 ? '目前沒有可建立帳號的在職員工。' : '所有在職員工都已綁定使用者帳號。'}
            </div>
          )}
          {activeRoles.length === 0 && (
            <div style={{ color: 'var(--slate-500)', marginBottom: 12, fontSize: 12 }}>
              尚無可指派的啟用角色，請先至「角色管理」建立或啟用角色。
            </div>
          )}
          <div className="glass-card">
            {isLoading ? (
              <div className="loading">載入中...</div>
            ) : (
              <table className="data-table">
                <thead>
                  <tr>
                    <th>帳號</th>
                    <th>Email</th>
                    <th>員工</th>
                    <th>角色</th>
                    <th>操作</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUsers.map(user => (
                    <tr key={user.id}>
                      <td>{user.username}</td>
                      <td>
                        {editingUserRoles === user.id ? (
                          <input
                            type="email"
                            value={editingUserEmail}
                            onChange={event => setEditingUserEmail(event.target.value)}
                            placeholder="name@example.com"
                            aria-label={`${user.username} Email`}
                            style={{ width: 'min(220px, 100%)' }}
                          />
                        ) : user.email || '-'}
                      </td>
                      <td>{user.employeeId ? employeeLabels.get(user.employeeId) ?? user.employeeId : '-'}</td>
                      <td>
                        {editingUserRoles === user.id ? (
                          <RoleMultiSelect
                            roles={roles.filter(role => role.isActive)}
                            selectedRoleIds={user.roles.map(r => r.id)}
                            onSave={(roleIds) => void handleSaveUser(user, roleIds)}
                            onCancel={() => setEditingUserRoles(null)}
                          />
                        ) : (
                          <span className="role-tags">
                            {user.roles.length > 0 
                                ? user.roles.map(r => <span key={r.id} className="role-tag">{getRoleDisplayName(r.roleName)}</span>)
                              : <span className="no-role">未設定</span>
                            }
                          </span>
                        )}
                      </td>
                      <td>
                        {editingUserRoles !== user.id && (
                          <>
                            <button className="icon-action-btn" onClick={() => setResetPasswordUser(user)} title="管理員重設密碼" aria-label={`重設 ${user.username} 的密碼`}>
                              <KeyRound size={14} />
                            </button>
                            {user.username.toLowerCase() !== 'admin' && (
                              <button className="icon-action-btn" onClick={() => handleEditUserRoles(user)} title="編輯使用者資料與角色">
                                <Edit2 size={14} />
                              </button>
                            )}
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
          <FormModal
            open={showCreateUserModal}
            title="建立使用者"
            onClose={() => setShowCreateUserModal(false)}
            fields={[
              { name: 'username', label: '帳號', type: 'text', required: true, placeholder: '輸入登入帳號' },
              {
                name: 'password',
                label: '初始密碼',
                type: 'password',
                required: true,
                min: 8,
                placeholder: '至少 8 個字元，含英文字母與數字',
                description: '首次登入後必須變更密碼。密碼至少 8 個字元，並須同時包含英文字母與數字。'
              },
              { name: 'email', label: 'Email（選填）', type: 'text', placeholder: 'name@example.com' },
              {
                name: 'employeeId',
                label: '綁定員工',
                type: 'select',
                required: true,
                searchable: true,
                placeholder: '搜尋員工編號或姓名',
                options: availableEmployees.map((employee: EmployeeDto) => ({
                  value: employee.id,
                  label: `${employee.employeeNo} - ${employee.chineseName}`
                })),
                description: employeesLoading
                  ? '正在載入可綁定員工...'
                  : employeesError
                    ? '無法載入員工清單，請重新整理後再試。'
                    : availableEmployees.length > 0
                      ? '僅列出尚未綁定帳號的在職員工；員工需先具備身分證件、到職日、部門及職稱。'
                      : '目前沒有可綁定的在職員工；請先建立符合條件的員工資料。'
              },
              {
                name: 'roleIds',
                label: '指派角色',
                type: 'multi-select',
                required: true,
                options: activeRoles.map(role => ({ value: role.id, label: getRoleDisplayName(role.roleName) }))
              }
            ]}
            onSubmit={handleCreateUser}
          />
          <FormModal
            open={resetPasswordUser !== null}
            title={`重設密碼 - ${resetPasswordUser?.username ?? ''}`}
            onClose={() => setResetPasswordUser(null)}
            fields={[
              { name: 'secret', label: '管理員重設密鑰', type: 'password', required: true, description: '需輸入部署環境設定的 ADMIN_RESET_SECRET。' },
              { name: 'newPassword', label: '新密碼', type: 'password', required: true, min: 8, placeholder: '至少 8 個字元，含英文字母與數字' },
              { name: 'confirmPassword', label: '確認新密碼', type: 'password', required: true, min: 8, placeholder: '再次輸入新密碼' }
            ]}
            onSubmit={async data => {
              const newPassword = String(data.newPassword ?? '');
              if (newPassword !== String(data.confirmPassword ?? ''))
                throw new Error('兩次輸入的密碼不一致。');
              if (newPassword.length < 8 || !/[A-Za-z]/.test(newPassword) || !/[0-9]/.test(newPassword))
                throw new Error('密碼至少 8 個字元，並須同時包含英文字母與數字。');
              if (!resetPasswordUser)
                throw new Error('找不到要重設密碼的使用者，請關閉視窗後重試。');

              await authApi.adminReset({
                secret: String(data.secret ?? ''),
                username: resetPasswordUser.username,
                newPassword
              });
              setResetPasswordUser(null);
              toast.success('密碼已重設；使用者下次登入時必須變更密碼。');
            }}
          />
        </div>
      )}

      {activePermTab === 'roles' && (
        <div className="roles-section">
          <div className="role-guide-banner">
            <div className="role-guide-icon"><Users size={24} /></div>
            <div>
              <h3>先選工作內容，再指派角色</h3>
              <p>角色決定同仁可以查看、建立、修改或審核哪些資料。請選最符合日常工作的角色；需要跨職務時可以指派多個角色。</p>
            </div>
          </div>
          <div className="role-toolbar">
            <label className="filter-search">
              <Search size={14} />
              <input
                placeholder="搜尋角色或工作內容..."
                value={searchRole}
                onChange={event => setSearchRole(event.target.value)}
              />
            </label>
            <div className="role-toolbar-actions">
              {canCreateAccess && <button className="btn btn-ghost btn-sm" onClick={onAddPermission}>新增權限</button>}
              {canCreateAccess && <button className="btn btn-primary btn-sm" onClick={onAddRole}><Plus size={14} /> 新增角色</button>}
            </div>
          </div>
          {isLoading ? (
            <div className="glass-card loading">載入中...</div>
          ) : filteredRoles.length === 0 ? (
            <div className="glass-card role-empty-state">找不到符合「{searchRole}」的角色。</div>
          ) : (
            <div className="role-card-grid">
              {filteredRoles.map(role => {
                const RoleIcon = getRoleIcon(role.roleName);
                const isAdmin = role.roleName.toLowerCase() === 'admin';
                return (
                  <article className={`role-card glass-card${role.isActive ? '' : ' inactive'}`} key={role.id}>
                    <div className="role-card-heading">
                      <div className="role-card-icon"><RoleIcon size={22} /></div>
                      <div className="role-card-title">
                        <div className="role-card-badges">
                          <span className={`role-kind-badge${role.isSystem ? ' system' : ''}`}>
                            {role.isSystem ? '系統預設' : '自訂角色'}
                          </span>
                          {!role.isActive && <span className="role-kind-badge inactive">已停用</span>}
                        </div>
                        <h3>{getRoleDisplayName(role.roleName)}</h3>
                      </div>
                    </div>
                    <p className="role-card-description">{getRoleDisplayDescription(role.roleName, role.description)}</p>
                    <div className="role-card-footer">
                      <span className="role-permission-count"><KeyRound size={14} /> {role.permissionIds.length} 項權限</span>
                      <div className="role-card-actions">
                        {isAdmin
                          ? <span className="role-admin-label"><ShieldCheck size={14} /> 全域管理</span>
                          : (
                            canEditAccess && (
                              <button className="btn btn-ghost btn-sm" onClick={() => handleEditRolePerms(role)}>
                                <Eye size={14} /> 查看／調整權限
                              </button>
                            )
                          )}
                        {!role.isSystem && canDeleteAccess && (
                          <button
                            className="icon-action-btn"
                            onClick={() => handleDeleteRole(role.id, getRoleDisplayName(role.roleName))}
                            title="刪除角色"
                            aria-label={`刪除角色 ${getRoleDisplayName(role.roleName)}`}
                          >
                            <Trash2 size={14} />
                          </button>
                        )}
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}

          {editingRolePerms && (
            <RolePermissionEditor
              roles={roles}
              permissionsTree={permissionsTree}
              editingRolePerms={editingRolePerms}
              selectedRolePerms={selectedRolePerms}
              onTogglePermission={handleTogglePermission}
              onSave={() => handleSaveRolePerms(editingRolePerms)}
              canEditAccess={canEditAccess}
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
              permissionsTree.length === 0
                ? <div className="empty">尚無權限資料，請先新增權限</div>
                : <PermissionTreeExplorer permissionsTree={permissionsTree} />
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function CurrencyTab({ queryClient }: { queryClient: QueryClient }) {
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

  const currencies: Array<CurrencyDto & { isActive?: boolean }> = currenciesRes?.data ?? [];
  const rates: Array<ExchangeRateDto & { rateType?: string; source?: string }> = ratesRes?.data ?? [];

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
              {currencies.map(c => (
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
              {rates.map(r => (
                <tr key={r.id}>
                  <td className="mono">{r.fromCurrency}</td>
                  <td className="mono">{r.toCurrency}</td>
                  <td className="mono" style={{ color: 'var(--lux-gold)', fontWeight: 700 }}>{r.rate}</td>
                  <td>{r.validFrom.split('T')[0]}</td>
                  <td>{r.rateType ? r.rateType === 'Standard' ? '標準' : r.rateType : '-'}</td>
                  <td>{r.source ? r.source === 'Manual' ? '手動' : r.source : '-'}</td>
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
          await currenciesApi.createCurrency(result.data);
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
          { name: 'fromCurrency', label: '來源幣別 *', type: 'select', required: true, options: currencies.map(c => ({ value: c.currencyCode, label: c.currencyCode })) },
          { name: 'toCurrency', label: '目標幣別 *', type: 'select', required: true, options: currencies.map(c => ({ value: c.currencyCode, label: c.currencyCode })) },
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
          await currenciesApi.createRate({ ...result.data, validFrom: result.data.effectiveDate });
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
  queryClient: QueryClient;
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

function RoleMultiSelect({ roles, selectedRoleIds, onSave, onCancel }: {
  roles: RoleWithPermissionsDto[];
  selectedRoleIds: string[];
  onSave: (roleIds: string[]) => void;
  onCancel: () => void;
}) {
  const [selected, setSelected] = useState<string[]>(selectedRoleIds);

  const handleToggle = (roleId: string) => {
    const newSelected = selected.includes(roleId)
      ? selected.filter(id => id !== roleId)
      : [...selected, roleId];
    setSelected(newSelected);
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
          <span>{getRoleDisplayName(role.roleName)}</span>
        </label>
      ))}
      <button className="btn btn-primary btn-sm" onClick={() => onSave(selected)}>儲存變更</button>
      <button className="btn btn-ghost btn-sm" onClick={onCancel}>取消</button>
    </div>
  );
}

function RolePermissionEditor({ roles, permissionsTree, editingRolePerms, selectedRolePerms, onTogglePermission, onSave, canEditAccess, onCancel }: {
  roles: RoleWithPermissionsDto[];
  permissionsTree: PermissionTreeDto[];
  editingRolePerms: string;
  selectedRolePerms: string[];
  onTogglePermission: (node: PermissionTreeDto) => void;
  onSave: () => void;
  canEditAccess: boolean;
  onCancel: () => void;
}) {
  const role = roles.find(r => r.id === editingRolePerms);

  return (
    <div className="modal-overlay">
      <div className="modal-content glass-card">
        <div className="modal-header">
          <h3>編輯角色權限 - {role ? getRoleDisplayName(role.roleName) : ''}</h3>
          <button onClick={onCancel}><X size={18} /></button>
        </div>
        <div className="modal-body">
          <PermissionTreeExplorer
            permissionsTree={permissionsTree}
            selectedPerms={selectedRolePerms}
            onToggle={onTogglePermission}
            compact
          />
        </div>
        <div className="modal-footer">
          <button className="btn btn-ghost" onClick={onCancel}>取消</button>
          {canEditAccess && <button className="btn btn-primary" onClick={onSave}>儲存</button>}
        </div>
      </div>
    </div>
  );
}

function filterPermissionNode(node: PermissionTreeDto, search: string): PermissionTreeDto | null {
  if (!search)
    return node;

  const children = node.children
    .map(child => filterPermissionNode(child, search))
    .filter((child): child is PermissionTreeDto => child !== null);
  const matches = `${node.moduleName} ${node.permissionCode} ${node.description ?? ''}`
    .toLocaleLowerCase()
    .includes(search);
  if (!matches && children.length === 0)
    return null;

  return { ...node, children: matches ? node.children : children };
}

function getPermissionCounts(node: PermissionTreeDto, selectedPerms?: string[]) {
  const isAction = node.permissionType.toLowerCase() === 'action';
  const children = node.children ?? [];
  return {
    actions: isAction ? 1 : children.reduce((total, child) => total + getPermissionCounts(child).actions, 0),
    selected: isAction && selectedPerms?.includes(node.id)
      ? 1
      : children.reduce((total, child) => total + getPermissionCounts(child, selectedPerms).selected, 0)
  };
}

function PermissionTreeExplorer({ permissionsTree, selectedPerms, onToggle, compact = false }: {
  permissionsTree: PermissionTreeDto[];
  selectedPerms?: string[];
  onToggle?: (node: PermissionTreeDto) => void;
  compact?: boolean;
}) {
  const [search, setSearch] = useState('');
  const [expandedModules, setExpandedModules] = useState<Set<string>>(() => new Set());
  const filteredTree = permissionsTree
    .map(node => filterPermissionNode(node, search.trim().toLocaleLowerCase()))
    .filter((node): node is PermissionTreeDto => node !== null);
  const allExpanded = filteredTree.length > 0 && filteredTree.every(node => expandedModules.has(node.id));

  const toggleAll = () => {
    setExpandedModules(allExpanded
      ? new Set()
      : new Set(filteredTree.map(node => node.id)));
  };

  return (
    <div className={`permission-tree-explorer${compact ? ' compact' : ''}`}>
      <div className="permission-tree-toolbar">
        <label className="permission-tree-search">
          <Search size={15} />
          <input
            aria-label="搜尋權限"
            placeholder="搜尋模組、頁面或動作..."
            value={search}
            onChange={event => setSearch(event.target.value)}
          />
          {search && (
            <button type="button" onClick={() => setSearch('')} aria-label="清除搜尋">
              <X size={14} />
            </button>
          )}
        </label>
        <span className="permission-tree-count">{filteredTree.length} 個權限模組</span>
        <button className="btn btn-ghost btn-sm" type="button" onClick={toggleAll}>
          {allExpanded ? '全部收合' : '全部展開'}
        </button>
      </div>
      {filteredTree.length === 0 ? (
        <div className="permission-tree-empty">沒有符合搜尋條件的權限。</div>
      ) : (
        <div className="permission-module-grid">
          {filteredTree.map(module => {
            const expanded = expandedModules.has(module.id);
            const counts = getPermissionCounts(module, selectedPerms);
            const title = module.children[0]?.moduleName ?? module.moduleName;
            return (
              <section className={`permission-module-card${expanded ? ' expanded' : ''}`} key={module.id}>
                <button
                  type="button"
                  className="permission-module-toggle"
                  aria-expanded={expanded}
                  aria-label={`${expanded ? '收合' : '展開'}${getPermissionDisplayName(title)}權限`}
                  onClick={() => setExpandedModules(previous => {
                    const next = new Set(previous);
                    if (next.has(module.id)) next.delete(module.id);
                    else next.add(module.id);
                    return next;
                  })}
                >
                  <span className="permission-module-icon"><KeyRound size={17} /></span>
                  <span className="permission-module-title">
                    <span className="permission-module-category">{getPermissionDisplayName(module.moduleName)}</span>
                    <strong>{getPermissionDisplayName(title)}</strong>
                    <span className="permission-module-summary">
                      {module.children.length} 個頁面・{counts.actions} 項權限
                      {selectedPerms && `・已選 ${counts.selected} 項`}
                    </span>
                  </span>
                  <ChevronDown className="permission-module-chevron" size={17} />
                </button>
                {expanded && (
                  <div className="permission-module-details">
                    {module.children.map(child => (
                      <PermissionTreeNode
                        key={child.id}
                        node={child}
                        selectedPerms={selectedPerms}
                        onToggle={onToggle}
                        level={1}
                        initiallyExpanded
                      />
                    ))}
                  </div>
                )}
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}

function PermissionTreeNode({ node, selectedPerms, onToggle, level = 0, initiallyExpanded }: {
  node: PermissionTreeDto;
  selectedPerms?: string[];
  onToggle?: (node: PermissionTreeDto) => void;
  level?: number;
  initiallyExpanded?: boolean;
}) {
  const [expanded, setExpanded] = useState(initiallyExpanded ?? level < 2);
  const checkboxRef = useRef<HTMLInputElement>(null);
  const hasChildren = node.children && node.children.length > 0;
  const { checked: isSelected, indeterminate: isPartiallySelected } = selectedPerms
    ? getPermissionSelectionState(node, selectedPerms)
    : { checked: false, indeterminate: false };

  useEffect(() => {
    if (checkboxRef.current)
      checkboxRef.current.indeterminate = isPartiallySelected;
  }, [isPartiallySelected]);

  return (
    <div className="tree-node" style={{ marginLeft: level * 20 }}>
      <div className="tree-node-content">
        {hasChildren && (
          <button
            className="tree-expand-btn"
            aria-label={`${expanded ? '收合' : '展開'}${getPermissionDisplayName(node.moduleName ?? '')}`}
            onClick={() => setExpanded(!expanded)}
          >
            {expanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
          </button>
        )}
        {!hasChildren && <div style={{ width: 20 }} />}
        
        {selectedPerms !== undefined && onToggle ? (
          <input
            ref={checkboxRef}
            className="tree-checkbox"
            type="checkbox"
            checked={isSelected}
            aria-label={`授權 ${getPermissionDisplayName(node.moduleName ?? '')}`}
            onChange={() => onToggle(node)}
          />
        ) : null}
        
        <span className="tree-name" title={node.description}>{getPermissionDisplayName(node.moduleName ?? '')}</span>
        <span className="tree-type-badge">{getPermissionTypeLabel(node.permissionType ?? '')}</span>
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
