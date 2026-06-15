
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Aggressive replacement for broken alert/confirm/console patterns
# Fixes cases like:
# alert('text);'
# alert('text);
# alert('text)

def repair_func(match):
    func = match.group(1)
    text = match.group(2)
    # The text is the mess between (' and the next line or end of relevant part
    # We strip any ); or );' or ) or ; that might be at the end of the mess
    clean_text = re.sub(r"[\);']+$", "", text).strip()
    return f"{func}('{clean_text}')"

# Match func(' followed by anything that doesn't have a ' but HAS a ) or ; or end of line
content = re.sub(r"(alert|confirm|console\.(?:log|error|warn))\('([^'\n]*?)(?=\s*\);|\s*\n|$)", repair_func, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Aggressive string repair complete.")
