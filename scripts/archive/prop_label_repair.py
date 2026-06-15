
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix unclosed braces in JSX attributes or expressions
# Pattern: something={ ... and no closing } before next something={ or next <tag
# Actually difficult with regex.

# Let's fix the most likely cultprit: unclosed quotes in labels or titles again
# Pattern: label: 'text' where text has messy chars but no closing quote
def repair_prop(match):
    prop = match.group(1)
    text = match.group(2)
    clean_text = re.sub(r"[';]+$", "", text).strip()
    return f"{prop}: '{clean_text}'"

content = re.sub(r"([a-zA-Z]+):\s*'([^'\n]+?)(?=\s*[},]|\s*\n|$)", repair_prop, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Prop label repair complete.")
