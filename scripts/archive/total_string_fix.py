
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    # Use regex to find lines like alert('...); or confirm('...); missing the closing '
    # We look for alert(' or confirm(' or console.log('
    # and if the line ends with ); but no closing '
    
    match = re.search(r"(alert|confirm|console\.(log|error|warn))\('([^']+)$", line.rstrip())
    if match:
        func = match.group(1)
        content = match.group(3)
        # Clean up the content from any trailing characters that were likely meant as code
        content = re.sub(r"\);.*$", "", content)
        content = re.sub(r"\).*$", "", content)
        content = content.strip()
        
        # Reconstruct the line
        indent = line[:line.find(func)]
        new_line = f"{indent}{func}('{content}');\n"
        new_lines.append(new_line)
    else:
        new_lines.append(line)

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(new_lines)

print("Total fix of unclosed strings complete.")
