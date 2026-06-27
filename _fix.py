import sys

# Fix app.ts - add taxRate to overlay.update call
content = open("legacy/typescript-prototype/src/engine/app.ts","r",encoding="utf-8").read()

old = '    this.overlay.update({\n      metrics: this.city.metrics,\n      selectedTool: this.selectedTool,\n      overlayMode: this.overlayMode,'

new = '    this.overlay.update({\n      metrics: this.city.metrics,\n      taxRate: this.city.taxRate,\n      selectedTool: this.selectedTool,\n      overlayMode: this.overlayMode,'

if old in content:
    content = content.replace(old, new)
    open("legacy/typescript-prototype/src/engine/app.ts","w",encoding="utf-8").write(content)
    print("app.ts fixed")
else:
    print("Pattern not found in app.ts")
    # Show what's actually around line 76
    lines = content.split('\n')
    for i in range(74, min(84, len(lines))):
        print(f"  {i+1}: {lines[i]}")
