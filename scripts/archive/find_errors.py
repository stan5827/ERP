def find_errors():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    
    for i, line in enumerate(lines):
        # Naive check for odd number of quotes
        single = line.count("'")
        double = line.count('"')
        if single % 2 != 0 or double % 2 != 0:
            print(f"Line {i+1}: {line.strip()}")

find_errors()
