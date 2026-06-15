import os

def fix_type_options():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8') as f:
        content = f.read()

    repls = {
        "?券憿?": "全部類型",
        "?∟頃?脰疏": "採購入庫",
        "摰極?亙澈": "完工入庫",
        "?瑕?箏澈": "銷售出庫",
        "???": "領料出庫",
        "隤踵": "調撥",
        "隤踵": "調整"
    }

    for k, v in repls.items():
        content = content.replace(k, v)

    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Type options labels fixed.")

fix_type_options()
