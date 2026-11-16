#!/usr/bin/env python3
"""
SkuService 型別修正腳本
用途: 修正 SkuService.cs 中 decimal? → decimal 與 Guid? → string 的隱含轉換問題
用法: python tools/scripts/fix_sku.py
"""

import re
import sys
import os

# 設定路徑 - 相對於專案根目錄
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.normpath(os.path.join(SCRIPT_DIR, '..', '..'))
SKU_SERVICE_PATH = os.path.join(PROJECT_ROOT, 'src', 'ErpBackend', 'Services', 'SkuService.cs')

if not os.path.exists(SKU_SERVICE_PATH):
    print(f"❌ 找不到 SkuService.cs: {SKU_SERVICE_PATH}")
    sys.exit(1)

print(f"📄 讀取: {SKU_SERVICE_PATH}")

with open(SKU_SERVICE_PATH, 'r', encoding='utf-8') as f:
    c = f.read()

repls = [
    # Create method: (decimal)(dto.xxx ?? 0) → dto.xxx (SkuDto 已為非 null)
    ('Qty = (decimal)(dto.Qty ?? 0),', 'Qty = dto.Qty ?? 0,'),
    ('SafetyStock = (decimal)(dto.SafetyStock ?? 0),', 'SafetyStock = dto.SafetyStock ?? 0,'),
    ('ReorderPoint = (decimal)(dto.ReorderPoint ?? 0),', 'ReorderPoint = dto.ReorderPoint ?? 0,'),
    ('MaxStock = (decimal)(dto.MaxStock ?? 0),', 'MaxStock = dto.MaxStock ?? 0,'),
    ('PurchasePrice = (decimal)(dto.PurchasePrice ?? 0),', 'PurchasePrice = dto.PurchasePrice ?? 0,'),
    ('StandardCost = (decimal)(dto.StandardCost ?? 0),', 'StandardCost = dto.StandardCost ?? 0,'),
    ('SuggestedPrice = dto.SuggestedPrice,', 'SuggestedPrice = dto.SuggestedPrice ?? 0,'),
    ('BasePrice = dto.BasePrice,', 'BasePrice = dto.BasePrice ?? 0,'),
    ('AvgDailyConsumption = dto.AvgDailyConsumption,', 'AvgDailyConsumption = dto.AvgDailyConsumption ?? 0,'),
    # Update method: (decimal)(dto.xxx ?? 0) → dto.xxx
    ('entity.Qty = (decimal)(dto.Qty ?? 0);', 'entity.Qty = dto.Qty ?? 0;'),
    ('entity.SafetyStock = (decimal)(dto.SafetyStock ?? 0);', 'entity.SafetyStock = dto.SafetyStock ?? 0;'),
    ('entity.ReorderPoint = (decimal)(dto.ReorderPoint ?? 0);', 'entity.ReorderPoint = dto.ReorderPoint ?? 0;'),
    ('entity.MaxStock = (decimal)(dto.MaxStock ?? 0);', 'entity.MaxStock = dto.MaxStock ?? 0;'),
    ('entity.PurchasePrice = (decimal)(dto.PurchasePrice ?? 0);', 'entity.PurchasePrice = dto.PurchasePrice ?? 0;'),
    ('entity.StandardCost = (decimal)(dto.StandardCost ?? 0);', 'entity.StandardCost = dto.StandardCost ?? 0;'),
    ('entity.SuggestedPrice = dto.SuggestedPrice;', 'entity.SuggestedPrice = dto.SuggestedPrice ?? 0;'),
    ('entity.BasePrice = dto.BasePrice;', 'entity.BasePrice = dto.BasePrice ?? 0;'),
    ('entity.AvgDailyConsumption = dto.AvgDailyConsumption;', 'entity.AvgDailyConsumption = dto.AvgDailyConsumption ?? 0;'),
    # MapToDto: Guid.TryParse → 直接賦值 (model 已為 Guid?)
    ('RelatedSkuId = Guid.TryParse(s.RelatedSkuId, out var r) ? r : (Guid?)null,', 'RelatedSkuId = s.RelatedSkuId,'),
    ('RoutingId = Guid.TryParse(s.RoutingId, out var r) ? r : (Guid?)null,', 'RoutingId = s.RoutingId,'),
    ('SubstituteSkuId = Guid.TryParse(s.SubstituteSkuId, out var r) ? r : (Guid?)null,', 'SubstituteSkuId = s.SubstituteSkuId,'),
]

for old_str, new_str in repls:
    if old_str in c:
        c = c.replace(old_str, new_str)
        print(f"  ✅ 已取代: {old_str[:60]}...")
    else:
        print(f"  ⚠️  未找到: {old_str[:60]}...")

with open(SKU_SERVICE_PATH, 'w', encoding='utf-8') as f:
    f.write(c)

print(f"\n✅ 已儲存: {SKU_SERVICE_PATH}")
