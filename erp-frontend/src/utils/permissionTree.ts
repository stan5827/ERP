export interface PermissionTreeNode {
  id: string;
  permissionType: string;
  children: PermissionTreeNode[];
}

const permissionTypeLabels: Record<string, string> = {
  module: '模組',
  page: '頁面',
  action: '動作',
  menu: '選單'
};

const permissionNameLabels: Record<string, string> = {
  'Agent Flow': '智慧代理流程'
};

export function getPermissionTypeLabel(permissionType: string): string {
  return permissionTypeLabels[permissionType.toLowerCase()] ?? '其他';
}

export function getPermissionDisplayName(name: string): string {
  return permissionNameLabels[name] ?? name;
}

export function collectPermissionIds(node: PermissionTreeNode): string[] {
  return [node.id, ...node.children.flatMap(collectPermissionIds)];
}

export function collectActionPermissionIds(node: PermissionTreeNode): string[] {
  if (node.permissionType.toLowerCase() === 'action')
    return [node.id];
  const actionIds = node.children.flatMap(collectActionPermissionIds);
  return actionIds.length > 0 ? actionIds : [node.id];
}

export function getPermissionSelectionState(node: PermissionTreeNode, selectedIds: string[]) {
  const actionIds = collectActionPermissionIds(node);
  const selectedCount = actionIds.filter(id => selectedIds.includes(id)).length;
  const checked = selectedCount === actionIds.length;
  return {
    checked,
    indeterminate: selectedCount > 0 && !checked
  };
}

export function togglePermissionSelection(node: PermissionTreeNode, selectedIds: string[]): string[] {
  const allIds = collectPermissionIds(node);
  const { checked } = getPermissionSelectionState(node, selectedIds);
  return checked
    ? selectedIds.filter(id => !allIds.includes(id))
    : [...new Set([...selectedIds, ...allIds])];
}
