
import sys

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Define the start and end of the block to replace
# We use unique markers around the two FormModals at 2429-2511
start_marker = "      <FormModal\n\n        open={showAddWh}"
end_marker = "      />\n\n    </div>"

# Actually, I'll just use a more reliable substring match
target_start = content.find(start_marker)
# Find the second FormModal close
first_close = content.find("      />", target_start)
second_modal_start = content.find("<FormModal", first_close)
target_end = content.find("      />", second_modal_start) + len("      />")

if target_start != -1 and target_end != -1:
    new_block = """      <FormModal
        open={showAddWh}
        title="新增倉庫"
        onClose={() => setShowAddWh(false)}
        fields={[
          { name: 'warehouseCode', label: '倉庫代碼 *', type: 'text', required: true, placeholder: '請輸入代碼，例如 WH-01' },
          { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true, placeholder: '例如：主成品倉' },
          { name: 'type', label: '類型', type: 'select', options: [
              { value: 'normal', label: '一般倉' },
              { value: 'wip', label: '在製品倉' },
              { value: 'scrap', label: '廢料倉' },
            ], defaultValue: 'normal' },
          { name: 'address', label: '地址/位置', type: 'text' },
          { name: 'isActive', label: '啟用此倉庫', type: 'checkbox', defaultValue: true }
        ]}
        onSubmit={async (data) => {
          try {
            await warehousesApi.create(data);
            queryClient.invalidateQueries({ queryKey: ['warehouses'] });
            setShowAddWh(false);
            alert('倉庫新增成功');
          } catch (err: any) {
            console.error('新增倉庫失敗:', err);
            alert('新增倉庫失敗：' + (err?.response?.data?.message || '未知錯誤'));
          }
        }}
      />

      <FormModal
        open={!!editingWarehouse}
        title="編輯倉庫"
        onClose={() => setEditingWarehouse(null)}
        fields={[
          { name: 'warehouseCode', label: '倉庫代碼 *', type: 'text', required: true, placeholder: '例如 WH-01', defaultValue: editingWarehouse?.warehouseCode || '' },
          { name: 'warehouseName', label: '倉庫名稱 *', type: 'text', required: true, placeholder: '例如：主成品倉', defaultValue: editingWarehouse?.warehouseName || '' },
          { name: 'type', label: '類型', type: 'select', options: [
              { value: 'normal', label: '一般倉' },
              { value: 'wip', label: '在製品倉' },
              { value: 'scrap', label: '廢料倉' },
            ], defaultValue: editingWarehouse?.type || 'normal' },
          { name: 'address', label: '地址/位置', type: 'text', defaultValue: editingWarehouse?.address || '' },
          { name: 'isActive', label: '啟用此倉庫', type: 'checkbox', defaultValue: editingWarehouse?.isActive ?? true }
        ]}
        onSubmit={async (data) => {
          if (!editingWarehouse) return;
          try {
            await updateWh.mutateAsync({ id: editingWarehouse.id, data });
          } catch (err) {
            // Error handled by mutation
          }
        }}
      />"""
    new_content = content[:target_start] + new_block + content[target_end:]
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(new_content)
    print("Warehouse FormModals restored successfully.")
else:
    print(f"Could not find the target range. Start: {target_start}, End: {target_end}")
