# Translation Validation Tools

This document describes tools and scripts for validating translation files in the Greenshot project.

## Manual Validation

### Using xmllint

The `xmllint` tool can validate XML syntax:

```bash
# Validate a single file
xmllint --noout src/Greenshot/Languages/language-en-US.xml

# Validate all language files
for file in src/Greenshot/Languages/language-*.xml; do
    echo "Validating $file..."
    xmllint --noout "$file" || echo "ERROR in $file"
done

# Validate all plugin language files
for file in src/*/Languages/language*.xml; do
    echo "Validating $file..."
    xmllint --noout "$file" || echo "ERROR in $file"
done
```

### Resource Counting

Check if all languages have similar resource counts:

```bash
# Count resources in all main app language files
echo "Main Application Resource Counts:"
for file in src/Greenshot/Languages/language-*.xml; do
    count=$(grep -c '<resource name=' "$file")
    basename=$(basename "$file")
    printf "%-30s %d\n" "$basename" "$count"
done | sort -k2 -n

# Compare to English reference
english_count=$(grep -c '<resource name=' src/Greenshot/Languages/language-en-US.xml)
echo "English (reference) has $english_count resources"
```

### Finding Missing Translations

Find resources with empty values:

```bash
# Find empty resources in a specific file
grep '<resource name="[^"]*"></resource>' src/Greenshot/Languages/language-de-DE.xml

# Check all language files
for file in src/Greenshot/Languages/language-*.xml; do
    empty_count=$(grep -c '<resource name="[^"]*"></resource>' "$file" || echo "0")
    if [ "$empty_count" -gt 0 ]; then
        echo "$file has $empty_count empty resources"
    fi
done
```

### Comparing Resource Keys

Check if a language has the same resources as English:

```bash
#!/bin/bash
# compare_keys.sh <language-code>
# Example: ./compare_keys.sh de-DE

LANG_CODE=$1
EN_FILE="src/Greenshot/Languages/language-en-US.xml"
TARGET_FILE="src/Greenshot/Languages/language-$LANG_CODE.xml"

echo "Comparing $TARGET_FILE to English reference..."

# Extract resource names
grep -o 'resource name="[^"]*"' "$EN_FILE" | sort > /tmp/en_keys.txt
grep -o 'resource name="[^"]*"' "$TARGET_FILE" | sort > /tmp/target_keys.txt

# Find missing resources (in English but not in target)
echo "=== Missing in $LANG_CODE ==="
comm -23 /tmp/en_keys.txt /tmp/target_keys.txt

# Find extra resources (in target but not in English)
echo "=== Extra in $LANG_CODE (possibly obsolete) ==="
comm -13 /tmp/en_keys.txt /tmp/target_keys.txt

# Cleanup
rm /tmp/en_keys.txt /tmp/target_keys.txt
```

### Checking File Encoding

Verify files are UTF-8 with BOM:

```bash
# Check encoding of a file
file -i src/Greenshot/Languages/language-en-US.xml

# Check for BOM (should show "ef bb bf" at start)
hexdump -C src/Greenshot/Languages/language-en-US.xml | head -1

# Check all files
for file in src/Greenshot/Languages/language-*.xml; do
    encoding=$(file -b --mime-encoding "$file")
    if [ "$encoding" != "utf-8" ]; then
        echo "WARNING: $file is $encoding, not utf-8"
    fi
done
```

## Automated Validation Script

### Python Validation Script

Save as `tools/validate_translations.py`:

```python
#!/usr/bin/env python3
"""
Greenshot Translation Validator

Validates translation files for:
- XML well-formedness
- UTF-8 encoding
- Missing/extra resources compared to English
- Empty resources
- Placeholder consistency
"""

import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from collections import defaultdict

def validate_xml(file_path):
    """Check if file is well-formed XML"""
    try:
        ET.parse(file_path)
        return True, None
    except ET.ParseError as e:
        return False, str(e)

def check_encoding(file_path):
    """Check if file is UTF-8 with BOM"""
    with open(file_path, 'rb') as f:
        start = f.read(3)
        has_bom = start == b'\xef\xbb\xbf'
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            f.read()
        return True, has_bom
    except UnicodeDecodeError:
        return False, False

def get_resources(file_path):
    """Extract all resources from a language file"""
    tree = ET.parse(file_path)
    root = tree.getroot()
    resources = {}
    
    for resource in root.findall('.//resource'):
        name = resource.get('name')
        value = resource.text or ''
        resources[name] = value
    
    return resources

def check_placeholders(text):
    """Extract placeholder patterns from text"""
    import re
    return set(re.findall(r'\{(\d+)\}', text))

def validate_language_file(file_path, reference_resources=None):
    """Validate a single language file"""
    results = {
        'file': file_path,
        'valid_xml': False,
        'valid_encoding': False,
        'has_bom': False,
        'issues': []
    }
    
    # Check XML
    is_valid, error = validate_xml(file_path)
    results['valid_xml'] = is_valid
    if not is_valid:
        results['issues'].append(f"XML Error: {error}")
        return results
    
    # Check encoding
    is_utf8, has_bom = check_encoding(file_path)
    results['valid_encoding'] = is_utf8
    results['has_bom'] = has_bom
    if not is_utf8:
        results['issues'].append("File is not UTF-8 encoded")
    if not has_bom:
        results['issues'].append("File missing UTF-8 BOM")
    
    # Get resources
    resources = get_resources(file_path)
    results['resource_count'] = len(resources)
    
    # Check for empty resources
    empty = [name for name, value in resources.items() if not value.strip()]
    if empty:
        results['issues'].append(f"{len(empty)} empty resources: {', '.join(empty[:5])}")
    
    # Compare with reference if provided
    if reference_resources:
        # Missing resources
        missing = set(reference_resources.keys()) - set(resources.keys())
        if missing:
            results['issues'].append(f"{len(missing)} missing resources")
            results['missing_resources'] = list(missing)
        
        # Extra resources
        extra = set(resources.keys()) - set(reference_resources.keys())
        if extra:
            results['issues'].append(f"{len(extra)} extra resources (possibly obsolete)")
            results['extra_resources'] = list(extra)
        
        # Check placeholder consistency
        placeholder_mismatches = []
        for name in set(resources.keys()) & set(reference_resources.keys()):
            ref_placeholders = check_placeholders(reference_resources[name])
            trans_placeholders = check_placeholders(resources[name])
            if ref_placeholders != trans_placeholders:
                placeholder_mismatches.append(name)
        
        if placeholder_mismatches:
            results['issues'].append(f"{len(placeholder_mismatches)} placeholder mismatches")
            results['placeholder_mismatches'] = placeholder_mismatches[:10]
    
    return results

def main():
    """Main validation function"""
    # Determine repository root
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent
    
    # Find English reference
    en_file = repo_root / 'src' / 'Greenshot' / 'Languages' / 'language-en-US.xml'
    if not en_file.exists():
        print(f"ERROR: Could not find English reference file: {en_file}")
        return 1
    
    print("Loading English reference...")
    reference_resources = get_resources(en_file)
    print(f"English has {len(reference_resources)} resources\n")
    
    # Find all language files
    languages_dir = repo_root / 'src' / 'Greenshot' / 'Languages'
    language_files = sorted(languages_dir.glob('language-*.xml'))
    
    print(f"Found {len(language_files)} language files\n")
    print("=" * 80)
    
    all_results = []
    error_count = 0
    
    for lang_file in language_files:
        if lang_file.name == 'language-en-US.xml':
            continue  # Skip reference
        
        results = validate_language_file(lang_file, reference_resources)
        all_results.append(results)
        
        print(f"\n{lang_file.name}")
        print("-" * 40)
        print(f"Valid XML: {'✓' if results['valid_xml'] else '✗'}")
        print(f"UTF-8: {'✓' if results['valid_encoding'] else '✗'}")
        print(f"BOM: {'✓' if results['has_bom'] else '✗'}")
        print(f"Resources: {results['resource_count']}")
        
        if results['issues']:
            error_count += len(results['issues'])
            print(f"\nIssues ({len(results['issues'])}):")
            for issue in results['issues']:
                print(f"  - {issue}")
    
    # Summary
    print("\n" + "=" * 80)
    print(f"\nSummary: {len(all_results)} files validated")
    print(f"Total issues: {error_count}")
    
    return 0 if error_count == 0 else 1

if __name__ == '__main__':
    sys.exit(main())
```

Make it executable:
```bash
chmod +x tools/validate_translations.py
```

Run it:
```bash
python3 tools/validate_translations.py
```

## PowerShell Validation Script

For Windows users, save as `tools/Validate-Translations.ps1`:

```powershell
# Greenshot Translation Validator (PowerShell)

param(
    [string]$LanguageCode = $null,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"

# Find repository root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

# Paths
$EnglishFile = Join-Path $RepoRoot "src\Greenshot\Languages\language-en-US.xml"
$LanguagesDir = Join-Path $RepoRoot "src\Greenshot\Languages"

Write-Host "Greenshot Translation Validator" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Load English reference
if (-not (Test-Path $EnglishFile)) {
    Write-Host "ERROR: English reference not found: $EnglishFile" -ForegroundColor Red
    exit 1
}

Write-Host "Loading English reference..." -ForegroundColor Yellow
[xml]$EnglishXml = Get-Content $EnglishFile
$EnglishResources = $EnglishXml.language.resources.resource
$EnglishKeys = $EnglishResources | ForEach-Object { $_.name }
Write-Host "English has $($EnglishKeys.Count) resources`n" -ForegroundColor Green

# Get language files
$LanguageFiles = Get-ChildItem -Path $LanguagesDir -Filter "language-*.xml" | 
    Where-Object { $_.Name -ne "language-en-US.xml" }

if ($LanguageCode) {
    $LanguageFiles = $LanguageFiles | Where-Object { $_.Name -eq "language-$LanguageCode.xml" }
}

Write-Host "Validating $($LanguageFiles.Count) language file(s)...`n" -ForegroundColor Yellow
Write-Host ("=" * 80) -ForegroundColor Gray

$TotalIssues = 0

foreach ($File in $LanguageFiles) {
    Write-Host "`n$($File.Name)" -ForegroundColor Cyan
    Write-Host ("-" * 40) -ForegroundColor Gray
    
    $Issues = @()
    
    # Check XML
    try {
        [xml]$Xml = Get-Content $File.FullName
        Write-Host "Valid XML: ✓" -ForegroundColor Green
    }
    catch {
        Write-Host "Valid XML: ✗" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        $Issues += "XML parse error"
        continue
    }
    
    # Check encoding
    $Encoding = (Get-Content $File.FullName -Encoding Byte -TotalCount 3)
    $HasBOM = ($Encoding[0] -eq 0xEF -and $Encoding[1] -eq 0xBB -and $Encoding[2] -eq 0xBF)
    if ($HasBOM) {
        Write-Host "UTF-8 BOM: ✓" -ForegroundColor Green
    }
    else {
        Write-Host "UTF-8 BOM: ✗" -ForegroundColor Yellow
        $Issues += "Missing BOM"
    }
    
    # Get resources
    $Resources = $Xml.language.resources.resource
    $Keys = $Resources | ForEach-Object { $_.name }
    Write-Host "Resources: $($Keys.Count)"
    
    # Check for empty resources
    $Empty = $Resources | Where-Object { -not $_.InnerText.Trim() }
    if ($Empty.Count -gt 0) {
        Write-Host "  Empty resources: $($Empty.Count)" -ForegroundColor Yellow
        $Issues += "Empty resources"
    }
    
    # Compare with English
    $Missing = $EnglishKeys | Where-Object { $Keys -notcontains $_ }
    $Extra = $Keys | Where-Object { $EnglishKeys -notcontains $_ }
    
    if ($Missing.Count -gt 0) {
        Write-Host "  Missing resources: $($Missing.Count)" -ForegroundColor Red
        $Issues += "Missing resources"
        if ($Verbose) {
            Write-Host "    $($Missing -join ', ')" -ForegroundColor Gray
        }
    }
    
    if ($Extra.Count -gt 0) {
        Write-Host "  Extra resources: $($Extra.Count)" -ForegroundColor Yellow
        $Issues += "Extra resources"
        if ($Verbose) {
            Write-Host "    $($Extra -join ', ')" -ForegroundColor Gray
        }
    }
    
    $TotalIssues += $Issues.Count
}

# Summary
Write-Host "`n" -NoNewline
Write-Host ("=" * 80) -ForegroundColor Gray
Write-Host "`nValidation complete" -ForegroundColor Cyan
Write-Host "Total issues found: $TotalIssues" -ForegroundColor $(if ($TotalIssues -eq 0) { "Green" } else { "Yellow" })

exit $(if ($TotalIssues -eq 0) { 0 } else { 1 })
```

Run it:
```powershell
.\tools\Validate-Translations.ps1
.\tools\Validate-Translations.ps1 -LanguageCode de-DE
.\tools\Validate-Translations.ps1 -Verbose
```

## Integration with Build Process

### Pre-commit Hook

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash
# Validate translation files before commit

echo "Validating translation files..."

# Check if translation files are being committed
TRANSLATION_FILES=$(git diff --cached --name-only | grep 'Languages/language.*\.xml$')

if [ -n "$TRANSLATION_FILES" ]; then
    echo "Found translation file changes, validating..."
    
    for file in $TRANSLATION_FILES; do
        # Only validate if file still exists (not deleted)
        if [ -f "$file" ]; then
            echo "  Checking $file..."
            xmllint --noout "$file" 2>&1
            if [ $? -ne 0 ]; then
                echo "ERROR: XML validation failed for $file"
                exit 1
            fi
        fi
    done
    
    echo "All translation files validated successfully"
fi

exit 0
```

### GitHub Actions

Add to `.github/workflows/validate-translations.yml`:

```yaml
name: Validate Translations

on:
  pull_request:
    paths:
      - '**/Languages/**'
  push:
    branches:
      - main
    paths:
      - '**/Languages/**'

jobs:
  validate:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Install xmllint
      run: sudo apt-get install -y libxml2-utils
    
    - name: Validate XML files
      run: |
        find src -name 'language*.xml' -exec xmllint --noout {} \;
    
    - name: Check encoding
      run: |
        for file in src/*/Languages/language*.xml; do
          if ! file -i "$file" | grep -q 'charset=utf-8'; then
            echo "ERROR: $file is not UTF-8"
            exit 1
          fi
        done
```

## Future Enhancements

Potential validation improvements:

1. **Translation Memory**: Track common phrases and their approved translations
2. **Style Checker**: Verify formality level, capitalization conventions
3. **Length Checker**: Warn if translation is significantly longer than English
4. **Terminology Checker**: Verify glossary terms are used consistently
5. **Automated Sync**: Script to sync all languages when English changes
6. **Translation Dashboard**: Web interface showing completion status per language
7. **Spell Checker**: Integration with language-specific spell checkers
8. **Context Viewer**: Tool to show where each string appears in the UI

## Contributing

If you create additional validation tools:

1. Add them to the `tools/` directory
2. Document them in this file
3. Include usage examples
4. Consider making them cross-platform (Python preferred)
5. Add error handling and helpful error messages

---

**Version**: 1.0  
**Last Updated**: 2026-02-03
