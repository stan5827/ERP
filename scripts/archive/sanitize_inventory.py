import sys

def sanitize():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    try:
        with open(filename, 'rb') as f:
            data = f.read()
        
        # Try to decode as UTF-8 but ignore errors
        text = data.decode('utf-8', errors='ignore')
        
        # Now, replace any character that might break a string
        # The main issue is likely characters that were mangled into quotes or control chars
        # But here we just want to get it to parse
        
        with open(filename + '.sanitized', 'w', encoding='utf-8') as f:
            f.write(text)
            
        print("Sanitized file created.")
    except Exception as e:
        print(f"Error: {e}")

sanitize()
