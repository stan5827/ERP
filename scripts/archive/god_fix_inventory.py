import os
import re

def fix_file():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    # Define a translation for common mojibake patterns
    translations = {
        '?啣??株?': '單據編號',
        '?啣?憿?': '異動類型',
        '??': '品號',
        '??': '品名',
        '?賊?': '異動量',
        '?澈': '倉庫',
        '摨思?': '儲位',
        '?啣??交?': '異動日期',
        '鈭箏': '執行人',
        '敺暺?': '待盤點',
        '?日?銝?': '盤點中',
        '撌脣???,: '已完成',
        '撌桃敺Ⅱ隤?': '差異待確認',
        '撱箇??日???': '新增盤點單',
        '頛銝?..': '載入中...',
        '撠摨怠??啣?閮?': '查無異動紀錄',
        '?券憿?': '全部類型',
        '?∟頃?脰疏': '採購收料',
        '摰極?亙澈': '完工入庫',
        '?瑕?箏澈': '銷售出庫',
        '???': '領料出庫',
        '隤踵': '庫存調撥',
        '隤踵': '庫存調整',
        '?€鞎?': '退貨',
        '?券?澈': '全部倉庫',
        '???啣??株?????..': '搜尋單號或品號...',
        '?湧??郎': '即將過期',
        '?血??郎': '效期警示',
        '銝€?祆釣??': '一般存貨',
        '摨怠?蝮賭摯??': '庫存總價值',
        '?趙??': '呆滯品項',
        '頞???': '超過保存期限',
        '?? ROP ?湔': '上次 ROP 更新',
        'ABC 摨怠???': 'ABC 庫存分析',
        '擃??': '高價值',
        '銝剖??': '中價值',
        '雿??': '低價值',
        '?趙??': '呆滯品分析',
        '隤踵?桃??選?靘?隞?禮2.9 & 禮4.5嚗??€': '調撥管理',
        '隤踵?株?': '調撥單號',
        '?€??': '狀態',
        '隤踹?澈': '調出倉庫',
        '隤踹?澈': '調入倉庫',
        '撱箇??交?': '建立日期',
        '??': '操作',
        '撱箇?隤踵??': '建立調撥單',
        '?阮': '草稿',
        '撌脣祟??': '已核准',
        '撌脣???,': '已完成',
        '撌脣?瘨?': '已取消',
        '撖拇': '核准',
        '??': '取消',
        '?瑁?隤踵': '執行調撥',
        '摰極?亙澈?嚗??辣 禮2.10 & 禮4.6嚗??€': '完工入庫管理',
        '?亙澈?株?': '入庫單號',
        '撌亙??': '工單號碼',
        '撱箇?摰極?亙澈??': '建立完工報工',
        '?瑁??亙澈': '執行入庫',
        '?箄疏?桃恣???€鞎其?璆剝???€?€': '出貨與物流追蹤',
        '敺???,': '待處理',
        '?€鞎其葉': '揀貨中',
        '??銝?': '包裝中',
        '撌脣': '已出貨',
    }

    # First, replace the structured blocks again to ensure accuracy
    # (Copied from previous god_fix_inventory.py)
    # ... I'll skip that and just do the general string replacement.

    for target, replacement in translations.items():
        content = content.replace(target, replacement)

    # Specific fix for the Parse Error if it persists
    # Often it's a broken string like `placeholder: '憒?M01-STL-001'`
    content = content.replace("憒?", "例如: ")

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Fix complete.")

fix_file()
