import os
import re

def final_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()

    # Define known blocks and their clean versions
    # We use regex to find the blocks and replace them entirely.
    
    replacements = {
        r'const basicInfoFields: FormField\[\] = \[.*?\];': """const basicInfoFields: FormField[] = [
  { name: 'skuNumber', label: '品號 *', type: 'text', required: true, placeholder: '例如: M01-STL-001', description: '商品的唯一識別碼，建議使用分類縮寫+流水號方式命名。' },
  { name: 'skuName', label: '品名 *', type: 'text', required: true, placeholder: '輸入產品名稱', description: '產品的正式名稱。' },
  { name: 'specification', label: '規格規格', type: 'text', placeholder: '輸入規格', description: '尺寸、材質、顏色等詳細規格描述。' },
  { name: 'category', label: '產品類別', type: 'text', placeholder: '例如: M01, S01', description: '用於報表分析與自動化流程。' },
];""",
        r'const sourceTypeFields: FormField\[\] = \[.*?\];': """const sourceTypeFields: FormField[] = [
  { name: 'sourceType', label: '來源類型 *', type: 'select', required: true, options: [
    { value: 'make', label: '自製' },
    { value: 'buy', label: '採購' },
    { value: 'make_or_buy', label: '自製+採購' },
    { value: 'outsource', label: '委外加工' },
    { value: 'make_or_outsource', label: '自製+委外' },
  ], defaultValue: 'make', description: '定義商品的生產來源。' },
  { name: 'itemType', label: '物料類型', type: 'select', options: [
    { value: 'semi_finished', label: '半成品' },
    { value: 'finished', label: '成品' },
    { value: 'material', label: '原料' },
    { value: 'virtual', label: '虛擬件' },
  ], description: '物料在系統中的基本分類。' },
  { name: 'barcode', label: '條碼', type: 'text', placeholder: '輸入條碼' },
  { name: 'model', label: '型號', type: 'text', placeholder: '輸入型號' },
  { name: 'brand', label: '品牌', type: 'text', placeholder: '輸入品牌' },
  { name: 'color', label: '顏色', type: 'text', placeholder: '輸入顏色' },
  { name: 'drawingNo', label: '圖號', type: 'text', placeholder: '輸入圖號' },
  { name: 'drawingRevision', label: '圖紙版本', type: 'text', placeholder: '輸入版本' },
  { name: 'effectiveFrom', label: '生效日期', type: 'date' },
  { name: 'effectiveTo', label: '失效日期', type: 'date' },
  { name: 'shelfLifeDays', label: '保存期限(天)', type: 'number', placeholder: '0', min: 0, defaultValue: 0 },
];""",
        r'const inventoryFields =.*?\];': """const inventoryFields = (warehouseOptions: { value: string; label: string }[], binOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'isStocked', label: '是否管理庫存', type: 'checkbox', placeholder: '啟用庫存管理', defaultValue: true, description: '勾選後系統將追蹤其庫存異動與現有量。' },
  { name: 'abcClassification', label: 'ABC分類', type: 'select', options: [
    { value: 'A', label: 'A - 高價值' },
    { value: 'B', label: 'B - 中價值' },
    { value: 'C', label: 'C - 低價值' },
  ] },
  { name: 'safetyStock', label: '安全庫存量', type: 'number', placeholder: '0', min: 0 },
  { name: 'reorderPoint', label: '再訂購點 (ROP)', type: 'number', placeholder: '0', min: 0 },
  { name: 'maxStock', label: '最高庫存量', type: 'number', placeholder: '0', min: 0 },
  { name: 'avgDailyConsumption', label: '日均耗用量', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'purchasePriceCeiling', label: '進價漲幅最高(%)', type: 'number', placeholder: '0', min: 0, max: 100, step: 0.1 },
  { name: 'lotControlled', label: '批號管理', type: 'checkbox', placeholder: '啟用' },
  { name: 'serialControlled', label: '序號管理', type: 'checkbox', placeholder: '啟用' },
  { name: 'iqcRequired', label: '進料檢驗(IQC)', type: 'checkbox', placeholder: '需檢驗', defaultValue: true },
  { name: 'oqcRequired', label: '出貨檢驗(OQC)', type: 'checkbox', placeholder: '需檢驗' },
  { name: 'defaultWarehouseId', label: '預設倉庫', type: 'select', options: warehouseOptions },
  { name: 'defaultBinLocationId', label: '預設儲位', type: 'select', options: binOptions },
];""",
        r'const salesFields: FormField\[\] = \[.*?\];': """const salesFields: FormField[] = [
  { name: 'suggestedPrice', label: '建議售價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'basePrice', label: '標準售價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceA', label: '售價A (最高)', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceB', label: '售價B', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceC', label: '售價C', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceD', label: '售價D', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'priceE', label: '售價E (最低)', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'floorPrice', label: '最低底價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'discountRate', label: '預設折扣(%)', type: 'number', placeholder: '0', min: 0, max: 100, step: 0.1, defaultValue: 0 },
  { name: 'commissionAccount', label: '佣金科目', type: 'text', placeholder: '輸入科目碼' },
  { name: 'currency', label: '幣別', type: 'select', options: [
    { value: 'TWD', label: 'TWD - 新台幣' },
    { value: 'USD', label: 'USD - 美金' },
    { value: 'CNY', label: 'CNY - 人民幣' },
    { value: 'JPY', label: 'JPY - 日圓' },
  ], defaultValue: 'TWD' },
  { name: 'taxCode', label: '稅務代碼', type: 'text', placeholder: '例如: TX1', defaultValue: 'TX1' },
  { name: 'taxRate', label: '稅率', type: 'text', placeholder: '例如: 5%', defaultValue: '5%' },
];""",
        r'const costFields =.*?\];': """const costFields = (uomOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'purchasePrice', label: '採購單價', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'standardCost', label: '標準成本', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'latestPurchaseCost', label: '最近入庫成本', type: 'number', placeholder: '0', min: 0, step: 0.01 },
  { name: 'costMethod', label: '成本計價方式', type: 'select', options: [
    { value: 'standard', label: '標準成本法' },
    { value: 'average', label: '月加權平均' },
    { value: 'moving', label: '移動平均' },
  ] },
  { name: 'costUomId', label: '成本計算單位', type: 'select', options: uomOptions },
];""",
        r'const productionFields: FormField\[\] = \[.*?\];': """const productionFields: FormField[] = [
  { name: 'isManufacturable', label: '可製造項目', type: 'checkbox', defaultValue: true },
  { name: 'bomType', label: 'BOM類型', type: 'select', options: [
    { value: 'manufacture', label: '生產 BOM' },
    { value: 'kit', label: '組合 BOM' },
    { value: 'outsource', label: '委外 BOM' },
  ] },
  { name: 'isBomTop', label: 'BOM頂層', type: 'checkbox' },
  { name: 'isBomComponent', label: 'BOM子件', type: 'checkbox' },
  { name: 'isPhantom', label: '虛擬件', type: 'checkbox' },
  { name: 'isMto', label: '接單生產(MTO)', type: 'checkbox' },
  { name: 'isMts', label: '計畫生產(MTS)', type: 'checkbox', defaultValue: true },
  { name: 'isCustom', label: '客製品項', type: 'checkbox' },
  { name: 'canSubstitute', label: '可替代性', type: 'checkbox' },
  { name: 'isAlternative', label: '是替代件', type: 'checkbox' },
  { name: 'substituteSkuId', label: '替代品品號', type: 'text' },
  { name: 'routingId', label: '製程路線代號', type: 'text', placeholder: '例如: RTG-001' },
];""",
        r'const purchFields =.*?\];': """const purchFields = (supplierOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'isPurchasable', label: '可採購且在採購單顯示', type: 'checkbox', defaultValue: true },
  { name: 'isSellable', label: '可銷售且在銷售單顯示', type: 'checkbox', defaultValue: true },
  { name: 'defaultSupplierId', label: '預設供應商', type: 'select', options: supplierOptions },
  { name: 'supplierItemCode', label: '供應商端品號', type: 'text' },
  { name: 'supplierLeadTime', label: '供應商前置期(天)', type: 'number', defaultValue: 7 },
  { name: 'moq', label: '最小採購量(MOQ)', type: 'number', defaultValue: 1 },
  { name: 'leadTimeDays', label: '採購前置天數', type: 'number', defaultValue: 7 },
  { name: 'packingMethod', label: '包裝規範', type: 'text' },
];""",
        r'const uomFields =.*?\];': """const uomFields = (uomOptions: { value: string; label: string }[]): FormField[] => [
  { name: 'baseUomId', label: '庫存單位 *', type: 'select', required: true, options: uomOptions },
  { name: 'purchaseUomId', label: '採購單位', type: 'select', options: uomOptions },
  { name: 'purchaseToBaseFactor', label: '採購換算率(1:X)', type: 'number', defaultValue: 1 },
  { name: 'salesUomId', label: '銷售單位', type: 'select', options: uomOptions },
  { name: 'salesToBaseFactor', label: '銷售換算率(1:X)', type: 'number', defaultValue: 1 },
  { name: 'productionUomId', label: '生產單位', type: 'select', options: uomOptions },
  { name: 'productionToBaseFactor', label: '生產換算率(1:X)', type: 'number', defaultValue: 1 },
];"""
    }

    # Replace blocks
    for pattern, repl in replacements.items():
        content = re.sub(pattern, repl, content, flags=re.DOTALL)

    # Use a generic approach for any remaining non-ASCII strings within quotes
    # (Simplified: if a string literal contains non-ASCII and '?', replace it)
    # This is risky, so we won't do it blindly.
    
    # But we will replace common button/header text that we know are broken.
    content = content.replace('?啣??株?', '單據號碼')
    content = content.replace('?啣??交?', '建立時間')
    content = content.replace('?啣??交?', '建立日期') # Duplicate just in case
    content = content.replace('鈭箏', '操作人員')
    content = content.replace('?啣?憿?', '異動類型')
    content = content.replace('??', '品號')
    content = content.replace('??', '品名')
    content = content.replace('?賊?', '數量異動')
    content = content.replace('?澈', '倉庫')
    content = content.replace('摨思?', '儲位')
    content = content.replace('頛銝?..', '載入中...')
    content = content.replace('撠摨怠??啣?閮?', '無庫存異動紀錄')

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
        
    print("Final fix completed.")

if __name__ == "__main__":
    final_fix()
