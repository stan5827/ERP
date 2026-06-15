import sys

def try_fix():
    filename = r'd:\ERP\erp-frontend\src\pages\Inventory.tsx'
    try:
        with open(filename, 'rb') as f:
            data = f.read()
        
        print(f"File size: {len(data)}")
        
        # Test if it's UTF-8
        try:
            text = data.decode('utf-8')
            print("Decoding as UTF-8 successful.")
            # Check if it contains the mojibake signature
            if '?' in text or '' in text:
                print("Detected mojibake.")
                # UTF-8 -> Latin1 -> UTF-8 fix
                try:
                    # Some PowerShell versions read UTF-8 as Latin1 if no BOM
                    # Then they write it back as UTF-8
                    recovered = text.encode('latin1').decode('utf-8')
                    print("Recovery success (Latin1)!")
                    with open(filename + '.fixed', 'w', encoding='utf-8') as f:
                        f.write(recovered)
                    return
                except:
                    pass
                
                try:
                    # Windows-1252 variation
                    recovered = text.encode('cp1252').decode('utf-8')
                    print("Recovery success (CP1252)!")
                    with open(filename + '.fixed', 'w', encoding='utf-8') as f:
                        f.write(recovered)
                    return
                except:
                    pass
        except:
            print("Not UTF-8.")

    except Exception as e:
        print(f"Error: {e}")

try_fix()
