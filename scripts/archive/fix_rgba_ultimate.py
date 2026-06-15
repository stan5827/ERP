
import re

file_path = r'D:/ERP/erp-frontend/src/pages/Inventory.tsx'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Fix mangled rgba strings like rgba(45',110,245,0.15) -> rgba(45,110,245,0.15)
# This time we look for rgba( followed by [any chars] then ', then [any chars] then )
# AND we handle the single quote that might be around the whole thing or part of it.

# Pattern: rgba( digits ' , and more
# We want to remove any single quote that is immediately preceded or followed by digits OR commas inside the rgba parenthesis.

def rgba_repair(match):
    full_str = match.group(0)
    # Remove single quotes that are INSIDE the rgba() parentheses
    # Find the content inside rgba(...)
    rgba_match = re.search(r"rgba\((.*?)\)", full_str)
    if rgba_match:
        inner = rgba_match.group(1)
        # Remove any single quotes from the inner part
        new_inner = inner.replace("'", "")
        return full_str.replace(inner, new_inner)
    return full_str

# Global replacement for any rgba(...) that contains at least one single quote inside
new_content = re.sub(r"rgba\([^)]*?'[^)]*?\)", rgba_repair, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print("Ultimate RGBA string repair complete.")
