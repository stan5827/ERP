import sys

def try_big5():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    try:
        with open(filename, 'rb') as f:
            data = f.read()
        
        # Try BIG5
        try:
            text = data.decode('big5')
            print("Successfully decoded as BIG5!")
            with open(filename + '.big5', 'w', encoding='utf-8') as f:
                f.write(text)
        except Exception as e:
            print(f"Failed BIG5: {e}")
            
        # Try CP950 (Windows Traditional Chinese)
        try:
            text = data.decode('cp950')
            print("Successfully decoded as CP950!")
            with open(filename + '.cp950', 'w', encoding='utf-8') as f:
                f.write(text)
        except Exception as e:
            print(f"Failed CP950: {e}")

    except Exception as e:
        print(f"Outer Error: {e}")

try_big5()
