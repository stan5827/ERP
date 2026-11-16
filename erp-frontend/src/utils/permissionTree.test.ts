import { describe, expect, it } from 'vitest';
import {
  getPermissionDisplayName,
  getPermissionSelectionState,
  getPermissionTypeLabel,
  togglePermissionSelection,
  type PermissionTreeNode
} from './permissionTree';

const permissionTree: PermissionTreeNode = {
  id: 'sales',
  permissionType: 'module',
  children: [{
    id: 'sales:page',
    permissionType: 'page',
    children: [
      { id: 'sales:view', permissionType: 'action', children: [] },
      { id: 'sales:create', permissionType: 'action', children: [] },
      { id: 'sales:edit', permissionType: 'action', children: [] }
    ]
  }]
};

describe('permission tree display labels', () => {
  it.each([
    ['module', '模組'],
    ['page', '頁面'],
    ['action', '動作'],
    ['menu', '選單']
  ])('translates the %s node type to Traditional Chinese', (permissionType, label) => {
    expect(getPermissionTypeLabel(permissionType)).toBe(label);
  });

  it('uses Traditional Chinese for the Agent Flow page name', () => {
    expect(getPermissionDisplayName('Agent Flow')).toBe('智慧代理流程');
  });

  it('keeps existing Chinese permission names unchanged', () => {
    expect(getPermissionDisplayName('全廠戰情室')).toBe('全廠戰情室');
  });
});

describe('permission tree selection', () => {
  it('selects a parent and all descendants together', () => {
    expect(togglePermissionSelection(permissionTree, [])).toEqual([
      'sales',
      'sales:page',
      'sales:view',
      'sales:create',
      'sales:edit'
    ]);
  });

  it('reports a parent as indeterminate when only some actions are selected', () => {
    expect(getPermissionSelectionState(permissionTree, ['sales:view'])).toEqual({
      checked: false,
      indeterminate: true
    });
  });

  it('reports a parent as checked when all descendant actions are selected', () => {
    expect(getPermissionSelectionState(permissionTree, [
      'sales:view',
      'sales:create',
      'sales:edit'
    ])).toEqual({
      checked: true,
      indeterminate: false
    });
  });

  it('unchecks the full branch while preserving permissions outside it', () => {
    expect(togglePermissionSelection(permissionTree, [
      'sales',
      'sales:page',
      'sales:view',
      'sales:create',
      'sales:edit',
      'inventory:view'
    ])).toEqual(['inventory:view']);
  });
});
