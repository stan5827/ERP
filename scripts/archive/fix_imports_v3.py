import os

def fix_imports_v3():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    with open(filename, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Add useEffect to imports
    content = content.replace("import React, { useState } from 'react';", "import React, { useState, useEffect } from 'react';")
    
    # Fix the missing React prefix for useEffect if any
    # (In my v3 I used React.useEffect, which is fine if React is imported)
    
    with open(filename, 'w', encoding='utf-8') as f:
        f.write(content)
    print("Imports fixed.")

fix_imports_v3()
