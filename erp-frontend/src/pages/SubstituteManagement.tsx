import React, { useState, useEffect } from 'react';
import { substituteApi, skusApi, SubstituteGroupDto, SubstituteItemDto, SkuDto } from '../api/api';
import { Plus, Edit2, Trash2, Search, Save, X } from 'lucide-react';

export const SubstituteManagement: React.FC = () => {
  const [groups, setGroups] = useState<SubstituteGroupDto[]>([]);
  const [skus, setSkus] = useState<SkuDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editingGroup, setEditingGroup] = useState<SubstituteGroupDto | null>(null);
  const [formData, setFormData] = useState({
    groupCode: '',
    groupName: '',
    description: '',
    substitutionType: 'full',
    allowAutoSubstitute: true,
    isBidirectional: true,
    items: [] as Array<{ skuId: string; priority: number; conversionRate: number; isPrimary: boolean }>
  });

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setLoading(true);
    try {
      const [groupsRes, skusRes] = await Promise.all([
        substituteApi.groups({ includeItems: true }),
        skusApi.all()
      ]);
      
      if (groupsRes.data?.success) setGroups(groupsRes.data.data || []);
      if (skusRes.data?.success) setSkus(skusRes.data.data || []);
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateGroup = () => {
    setEditingGroup(null);
    setFormData({
      groupCode: '',
      groupName: '',
      description: '',
      substitutionType: 'full',
      allowAutoSubstitute: true,
      isBidirectional: true,
      items: []
    });
    setShowForm(true);
  };

  const handleEditGroup = (group: SubstituteGroupDto) => {
    setEditingGroup(group);
    setFormData({
      groupCode: group.groupCode,
      groupName: group.groupName || '',
      description: group.description || '',
      substitutionType: group.substitutionType,
      allowAutoSubstitute: group.allowAutoSubstitute,
      isBidirectional: group.isBidirectional,
      items: group.items.map(item => ({
        skuId: item.skuId,
        priority: item.priority,
        conversionRate: item.conversionRate || 1,
        isPrimary: item.isPrimary
      }))
    });
    setShowForm(true);
  };

  const handleSaveGroup = async () => {
    try {
      if (editingGroup) {
        await substituteApi.updateGroup(editingGroup.id, formData);
      } else {
        await substituteApi.createGroup(formData);
      }
      setShowForm(false);
      loadData();
    } catch (error) {
      console.error('Failed to save group:', error);
      alert('儲存失敗');
    }
  };

  const handleDeleteGroup = async (id: string) => {
    if (!confirm('確定要刪除此替代料群組嗎？')) return;
    try {
      await substituteApi.deleteGroup(id);
      loadData();
    } catch (error) {
      console.error('Failed to delete group:', error);
      alert('刪除失敗');
    }
  };

  const addItem = () => {
    setFormData({
      ...formData,
      items: [...formData.items, { skuId: '', priority: 1, conversionRate: 1, isPrimary: false }]
    });
  };

  const removeItem = (index: number) => {
    setFormData({
      ...formData,
      items: formData.items.filter((_, i) => i !== index)
    });
  };

  const updateItem = (index: number, field: string, value: any) => {
    const newItems = [...formData.items];
    newItems[index] = { ...newItems[index], [field]: value };
    setFormData({ ...formData, items: newItems });
  };

  const filteredGroups = groups.filter(g =>
    g.groupCode.toLowerCase().includes(search.toLowerCase()) ||
    (g.groupName || '').toLowerCase().includes(search.toLowerCase())
  );

  if (loading) {
    return <div className="loading-container"><div className="spinner"></div></div>;
  }

  return (
    <div className="substitute-management">
      <div className="page-header">
        <h1>替代料管理</h1>
        <button className="btn btn-primary" onClick={handleCreateGroup}>
          <Plus size={18} /> 新增群組
        </button>
      </div>

      <div className="search-bar">
        <Search size={18} />
        <input
          type="text"
          placeholder="搜尋群組代碼或名稱..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <div className="groups-list">
        {filteredGroups.map(group => (
          <div key={group.id} className="group-card">
            <div className="group-header">
              <div className="group-info">
                <h3>{group.groupCode}</h3>
                <span className="group-name">{group.groupName}</span>
              </div>
              <div className="group-actions">
                <button className="btn-icon" onClick={() => handleEditGroup(group)}>
                  <Edit2 size={16} />
                </button>
                <button className="btn-icon danger" onClick={() => handleDeleteGroup(group.id)}>
                  <Trash2 size={16} />
                </button>
              </div>
            </div>
            <div className="group-details">
              <span className={`badge ${group.isActive ? 'active' : 'inactive'}`}>
                {group.isActive ? '啟用' : '停用'}
              </span>
              <span className="badge">{group.substitutionType === 'full' ? '完全替代' : '部分替代'}</span>
              <span className="item-count">{group.items.length} 個料件</span>
            </div>
            {group.items.length > 0 && (
              <div className="items-preview">
                {group.items.slice(0, 3).map(item => (
                  <span key={item.id} className="item-tag">
                    {item.skuNumber} (P{item.priority})
                  </span>
                ))}
                {group.items.length > 3 && (
                  <span className="more-items">+{group.items.length - 3} 更多</span>
                )}
              </div>
            )}
          </div>
        ))}
      </div>

      {showForm && (
        <div className="modal-overlay">
          <div className="modal">
            <div className="modal-header">
              <h2>{editingGroup ? '編輯替代料群組' : '新增替代料群組'}</h2>
              <button className="btn-icon" onClick={() => setShowForm(false)}>
                <X size={20} />
              </button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>群組代碼 *</label>
                <input
                  type="text"
                  value={formData.groupCode}
                  onChange={(e) => setFormData({ ...formData, groupCode: e.target.value })}
                  placeholder="例如：SUB-001"
                />
              </div>
              <div className="form-group">
                <label>群組名稱</label>
                <input
                  type="text"
                  value={formData.groupName}
                  onChange={(e) => setFormData({ ...formData, groupName: e.target.value })}
                  placeholder="例如：螺絲替代料"
                />
              </div>
              <div className="form-row">
                <div className="form-group">
                  <label>替代類型</label>
                  <select
                    value={formData.substitutionType}
                    onChange={(e) => setFormData({ ...formData, substitutionType: e.target.value })}
                  >
                    <option value="full">完全替代</option>
                    <option value="partial">部分替代</option>
                  </select>
                </div>
                <div className="form-group checkbox">
                  <label>
                    <input
                      type="checkbox"
                      checked={formData.allowAutoSubstitute}
                      onChange={(e) => setFormData({ ...formData, allowAutoSubstitute: e.target.checked })}
                    />
                    允許自動替代
                  </label>
                </div>
              </div>
              <div className="form-group">
                <label>說明</label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  placeholder="替代料群組說明..."
                />
              </div>

              <div className="items-section">
                <div className="section-header">
                  <h3>替代料明細</h3>
                  <button className="btn btn-sm" onClick={addItem}>
                    <Plus size={14} /> 新增料件
                  </button>
                </div>
                {formData.items.map((item, index) => (
                  <div key={index} className="item-row">
                    <select
                      value={item.skuId}
                      onChange={(e) => updateItem(index, 'skuId', e.target.value)}
                    >
                      <option value="">選擇料件...</option>
                      {skus.map(sku => (
                        <option key={sku.id} value={sku.id}>
                          {sku.skuNumber} - {sku.skuName}
                        </option>
                      ))}
                    </select>
                    <input
                      type="number"
                      placeholder="優先級"
                      value={item.priority}
                      onChange={(e) => updateItem(index, 'priority', parseInt(e.target.value))}
                      style={{ width: '80px' }}
                    />
                    <input
                      type="number"
                      step="0.1"
                      placeholder="轉換率"
                      value={item.conversionRate}
                      onChange={(e) => updateItem(index, 'conversionRate', parseFloat(e.target.value))}
                      style={{ width: '80px' }}
                    />
                    <label className="checkbox-inline">
                      <input
                        type="checkbox"
                        checked={item.isPrimary}
                        onChange={(e) => updateItem(index, 'isPrimary', e.target.checked)}
                      />
                      主料
                    </label>
                    <button className="btn-icon danger" onClick={() => removeItem(index)}>
                      <X size={16} />
                    </button>
                  </div>
                ))}
              </div>
            </div>
            <div className="modal-footer">
              <button className="btn btn-secondary" onClick={() => setShowForm(false)}>
                取消
              </button>
              <button className="btn btn-primary" onClick={handleSaveGroup}>
                <Save size={16} /> 儲存
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};