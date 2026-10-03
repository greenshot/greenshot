# Translation Validation Tools

This document describes tools and scripts for validating the language packs in the Greenshot project.

The language packs are INI files: `src/Greenshot/Languages/greenshot.{ietf}.ini` for the core and `src/Greenshot.Plugin.*/Languages/greenshot.{module}.{ietf}.ini` for the plugins. Every key is a `key=value` line inside a `[Section]`. Greenshot compares keys case-insensitive and ignores `_` and `-`. A key missing in a translation shows the English text, so missing keys are not errors, but they should be translated. See [TRANSLATION_GUIDE.md](TRANSLATION_GUIDE.md) for the format.

The commands below are bash and work in Git Bash on Windows. Run them from the repository root.

## Manual Validation

### Checking the Structure

Every line must be a `[Section]`, a `key=value` inside a section, a comment (starting with `;` or `#`) or empty:

```bash
# Check a single file
awk '{ sub(/\r$/, "") }
     FNR == 1 { insection = 0 }
     /^\[.+\]$/ { insection = 1; next }
     /^[[:space:]]*$/ || /^[;#]/ { next }
     !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' src/Greenshot/Languages/greenshot.en-US.ini

# Check all core and plugin language packs
awk '{ sub(/\r$/, "") }
     FNR == 1 { insection = 0 }
     /^\[.+\]$/ { insection = 1; next }
     /^[[:space:]]*$/ || /^[;#]/ { next }
     !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' \
    src/Greenshot/Languages/greenshot.*.ini src/Greenshot.Plugin.*/Languages/greenshot.*.ini
```

No output means no problems.

### Key Counting

Check how many keys each language has:

```bash
# Count keys in all main app language packs (without the [__language__] section)
echo "Main Application Key Counts:"
for file in src/Greenshot/Languages/greenshot.*.ini; do
    count=$(awk '/^\[/ { section = $0; next } /^[;#]/ || !/=/ || section == "[__language__]" { next } { n++ } END { print n + 0 }' "$file")
    printf "%-30s %d\n" "$(basename "$file")" "$count"
done | sort -k2 -n

# Count keys per section of one language pack
awk '/^\[/ { section = $0; next } /^[;#]/ || !/=/ { next } { count[section]++ }
     END { for (s in count) printf "%-16s %d\n", s, count[s] }' src/Greenshot/Languages/greenshot.de-DE.ini | sort
```

### Finding Empty Values

Find keys with an empty value (`about_translation=` is empty in en-US on purpose):

```bash
# Find empty values in a specific file
grep -n '^[^;#[][^=]*=[[:space:]]*$' src/Greenshot/Languages/greenshot.de-DE.ini

# Check all language packs
for file in src/Greenshot/Languages/greenshot.*.ini src/Greenshot.Plugin.*/Languages/greenshot.*.ini; do
    empty_count=$(grep -c '^[^;#[][^=]*=[[:space:]]*$' "$file")
    if [ "$empty_count" -gt 0 ]; then
        echo "$file has $empty_count empty values"
    fi
done
```

A translation should not contain empty values as placeholders: leave the line out, then English is shown.

### Comparing Keys

Check which keys of English are missing in a language, and which keys of the language are not in English. Save as `compare_keys.sh` and run it from the repository root:

```bash
#!/bin/bash
# compare_keys.sh <language-code> [module]
# Example: ./compare_keys.sh de-DE          (core language pack)
#          ./compare_keys.sh de-DE imgur    (Imgur plugin language pack)

LANG_CODE=$1
MODULE=$2

if [ -z "$MODULE" ]; then
    DIR="src/Greenshot/Languages"
    BASE="greenshot"
else
    DIR=$(ls -d src/Greenshot.Plugin.*/Languages | grep -i "Plugin\.$MODULE/")
    BASE="greenshot.$MODULE"
fi
EN_FILE="$DIR/$BASE.en-US.ini"
TARGET_FILE="$DIR/$BASE.$LANG_CODE.ini"

if [ -z "$LANG_CODE" ] || [ ! -f "$TARGET_FILE" ]; then
    echo "Usage: $0 <language-code> [module], language pack not found: $TARGET_FILE"
    exit 1
fi

# Prints "[section]normalizedkey<TAB>[Section] key" for every key; keys are compared
# like Greenshot does: case-insensitive, _ and - ignored
list_keys() {
    awk '{ sub(/\r$/, "") }
         /^\[/ { section = $0; next }
         /^[;#]/ || !/=/ || section == "[__language__]" { next }
         { key = substr($0, 1, index($0, "=") - 1)
           norm = tolower(section key); gsub(/[-_]/, "", norm)
           print norm "\t" section " " key }' "$1" | sort -t "$(printf '\t')" -k1,1 -u
}

echo "Comparing $TARGET_FILE to $EN_FILE..."
list_keys "$EN_FILE" > /tmp/en_keys.txt
list_keys "$TARGET_FILE" > /tmp/target_keys.txt

echo "=== Missing in $LANG_CODE (English is shown) ==="
join -t "$(printf '\t')" -v 1 /tmp/en_keys.txt /tmp/target_keys.txt | cut -f 2

echo "=== Extra in $LANG_CODE (not in English, never shown) ==="
join -t "$(printf '\t')" -v 2 /tmp/en_keys.txt /tmp/target_keys.txt | cut -f 2

rm /tmp/en_keys.txt /tmp/target_keys.txt
```

Example output:

```
$ ./compare_keys.sh fr-FR jira
Comparing src/Greenshot.Plugin.Jira/Languages/greenshot.jira.fr-FR.ini to src/Greenshot.Plugin.Jira/Languages/greenshot.jira.en-US.ini...
=== Missing in fr-FR (English is shown) ===
[Jira] column_issueType
[Jira] upload
=== Extra in fr-FR (not in English, never shown) ===
```

Extra keys are usually obsolete keys, typos, or keys in the wrong section.

### Checking File Encoding

Verify files are UTF-8 without BOM:

```bash
# Check encoding of a file (prints nothing when the file is valid UTF-8)
iconv -f UTF-8 -t UTF-8 src/Greenshot/Languages/greenshot.de-DE.ini > /dev/null

# Check for a BOM (a BOM shows as "ef bb bf"; the language packs start with "5b", the "[" of the first section)
head -c 3 src/Greenshot/Languages/greenshot.de-DE.ini | od -An -tx1

# Check all files
for file in src/Greenshot/Languages/greenshot.*.ini src/Greenshot.Plugin.*/Languages/greenshot.*.ini; do
    iconv -f UTF-8 -t UTF-8 "$file" > /dev/null 2>&1 || echo "WARNING: $file is not valid UTF-8"
    if [ "$(head -c 3 "$file" | od -An -tx1 | tr -d ' ')" = "efbbbf" ]; then
        echo "WARNING: $file starts with a BOM"
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

Validates the INI language packs (greenshot.{ietf}.ini and greenshot.{module}.{ietf}.ini) for:
- UTF-8 encoding
- Lines outside a [Section] or without key=value
- Duplicate keys
- Missing keys (English is shown for them) and extra keys compared to en-US
- Placeholder consistency ({0}, {1}, ...)
"""

import re
import sys
from pathlib import Path

PLACEHOLDER = re.compile(r'\{(\d+)[^}]*\}')


def normalize(key):
    """Keys are compared case-insensitive, _ and - are ignored"""
    return key.lower().replace('_', '').replace('-', '')


def parse_language_pack(file_path):
    """Returns ({(section, normalized key): (key, value)}, [problems])"""
    problems = []
    entries = {}
    raw = file_path.read_bytes()
    if raw.startswith(b'\xef\xbb\xbf'):
        problems.append("starts with a BOM, the language packs are UTF-8 without BOM")
        raw = raw[3:]
    try:
        text = raw.decode('utf-8')
    except UnicodeDecodeError as e:
        return entries, [f"not valid UTF-8: {e}"]

    section = None
    for number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped or stripped[0] in ';#':
            continue
        if stripped.startswith('[') and stripped.endswith(']'):
            section = stripped[1:-1].lower()
            continue
        if section is None:
            problems.append(f"line {number}: outside a [Section]: {stripped}")
            continue
        if '=' not in line:
            problems.append(f"line {number}: not key=value: {stripped}")
            continue
        key, value = line.split('=', 1)
        ident = (section, normalize(key.strip()))
        if ident in entries:
            problems.append(f"line {number}: duplicate key [{section}] {key.strip()}")
        entries[ident] = (key.strip(), value)
    return entries, problems


def placeholders(text):
    return set(PLACEHOLDER.findall(text))


def validate(file_path, reference):
    """Validates one language pack against the en-US pack, returns (problems, missing count)"""
    entries, problems = parse_language_pack(file_path)
    entries.pop(('__language__', 'description'), None)

    missing = [reference[k][0] for k in reference if k not in entries]
    for ident in entries:
        if ident not in reference:
            problems.append(f"extra key (not in en-US): [{ident[0]}] {entries[ident][0]}")
        elif placeholders(entries[ident][1]) != placeholders(reference[ident][1]):
            problems.append(f"placeholders differ from en-US: [{ident[0]}] {entries[ident][0]}")
    return problems, missing


def language_groups(repo_root):
    """Yields (en-US file, [translation files]) for the core and every plugin"""
    core = repo_root / 'src' / 'Greenshot' / 'Languages'
    yield core / 'greenshot.en-US.ini', sorted(core.glob('greenshot.*.ini'))
    for english in sorted(repo_root.glob('src/Greenshot.Plugin.*/Languages/greenshot.*.en-US.ini')):
        base = english.name[:-len('en-US.ini')]  # greenshot.{module}.
        yield english, sorted(english.parent.glob(base + '*.ini'))


def main():
    repo_root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent.parent
    error_count = 0
    file_count = 0

    for english, files in language_groups(repo_root):
        if not english.exists():
            print(f"ERROR: Could not find the English language pack: {english}")
            return 1
        reference, problems = parse_language_pack(english)
        reference.pop(('__language__', 'description'), None)
        print(f"\n{english.relative_to(repo_root)}: {len(reference)} keys")
        print("=" * 80)
        for problem in problems:
            print(f"  - {problem}")
        error_count += len(problems)

        for file_path in files:
            if file_path == english:
                continue
            file_count += 1
            problems, missing = validate(file_path, reference)
            print(f"{file_path.name:<45} {len(reference) - len(missing):>4}/{len(reference)} keys translated")
            for problem in problems:
                print(f"  - {problem}")
            error_count += len(problems)

    print(f"\nSummary: {file_count} files validated, {error_count} problems")
    return 0 if error_count == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
```

It checks the core language packs and the language packs of every plugin, each against its en-US pack, and prints how many keys of each file are translated. Missing keys are counted but not reported as problems; structure errors, duplicate keys, extra keys and placeholder differences are. The exit code is 1 when there are problems. An optional argument is the repository root (default: the parent of the script's folder).

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
# Checks the INI language packs greenshot.{ietf}.ini (core) and greenshot.{module}.{ietf}.ini (plugins)

param(
    [string]$LanguageCode = $null,
    [switch]$ShowMissing
)

$ErrorActionPreference = "Continue"

# Find repository root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

# Keys are compared case-insensitive, _ and - are ignored
function Get-NormalizedKey([string]$Key) {
    return $Key.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "")
}

# Reads a language pack: returns @{ Entries = @{ "section|normalizedkey" = @(key, value) }; Problems = @() }
function Read-LanguagePack([string]$Path) {
    $entries = @{}
    $problems = @()
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        $problems += "Starts with a BOM, the language packs are UTF-8 without BOM"
    }
    try {
        $lines = [System.IO.File]::ReadAllLines($Path, [System.Text.UTF8Encoding]::new($false, $true))
    }
    catch {
        return @{ Entries = $entries; Problems = @("Not valid UTF-8") }
    }
    $section = $null
    $number = 0
    foreach ($line in $lines) {
        $number++
        $trimmed = $line.Trim([char]0xFEFF, ' ', "`t")
        if ($trimmed -eq "" -or $trimmed.StartsWith(";") -or $trimmed.StartsWith("#")) { continue }
        if ($trimmed.StartsWith("[") -and $trimmed.EndsWith("]")) {
            $section = $trimmed.Substring(1, $trimmed.Length - 2).ToLowerInvariant()
            continue
        }
        if (-not $section) { $problems += "Line ${number}: outside a [Section]: $trimmed"; continue }
        $index = $line.IndexOf("=")
        if ($index -lt 0) { $problems += "Line ${number}: not key=value: $trimmed"; continue }
        $key = $line.Substring(0, $index).Trim()
        $id = "$section|$(Get-NormalizedKey $key)"
        if ($entries.ContainsKey($id)) { $problems += "Line ${number}: duplicate key [$section] $key" }
        $entries[$id] = @($key, $line.Substring($index + 1))
    }
    $entries.Remove("__language__|description")
    return @{ Entries = $entries; Problems = $problems }
}

function Get-Placeholders([string]$Text) {
    return (([regex]::Matches($Text, '\{(\d+)[^}]*\}') | ForEach-Object { $_.Groups[1].Value }) | Sort-Object -Unique) -join ","
}

# The core language packs and the language packs of every plugin, each with its en-US pack
$Groups = @()
$CoreDir = Join-Path $RepoRoot "src\Greenshot\Languages"
$Groups += @{ English = Join-Path $CoreDir "greenshot.en-US.ini"; Files = Get-ChildItem $CoreDir -Filter "greenshot.*.ini" }
foreach ($English in Get-ChildItem (Join-Path $RepoRoot "src\Greenshot.Plugin.*\Languages\greenshot.*.en-US.ini")) {
    $Base = $English.Name.Substring(0, $English.Name.Length - "en-US.ini".Length)
    $Groups += @{ English = $English.FullName; Files = Get-ChildItem $English.DirectoryName -Filter "$Base*.ini" }
}

Write-Host "Greenshot Translation Validator" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

$TotalIssues = 0
$FileCount = 0

foreach ($Group in $Groups) {
    if (-not (Test-Path $Group.English)) {
        Write-Host "ERROR: English language pack not found: $($Group.English)" -ForegroundColor Red
        exit 1
    }
    $Reference = Read-LanguagePack $Group.English
    $EnglishEntries = $Reference.Entries
    Write-Host "`n$(Split-Path -Leaf $Group.English): $($EnglishEntries.Count) keys" -ForegroundColor Cyan
    Write-Host ("-" * 80) -ForegroundColor Gray
    foreach ($Problem in $Reference.Problems) { Write-Host "  $Problem" -ForegroundColor Red }
    $TotalIssues += $Reference.Problems.Count

    $Files = $Group.Files | Where-Object { $_.FullName -ne (Resolve-Path $Group.English).Path }
    if ($LanguageCode) {
        $Files = $Files | Where-Object { $_.Name.EndsWith(".$LanguageCode.ini") }
    }

    foreach ($File in $Files) {
        $FileCount++
        $Pack = Read-LanguagePack $File.FullName
        $Problems = @($Pack.Problems)
        $Missing = @($EnglishEntries.Keys | Where-Object { -not $Pack.Entries.ContainsKey($_) } | ForEach-Object { $EnglishEntries[$_][0] })
        foreach ($Id in $Pack.Entries.Keys) {
            $Section = $Id.Split("|")[0]
            $Key = $Pack.Entries[$Id][0]
            if (-not $EnglishEntries.ContainsKey($Id)) {
                $Problems += "Extra key (not in en-US): [$Section] $Key"
            }
            elseif ((Get-Placeholders $Pack.Entries[$Id][1]) -ne (Get-Placeholders $EnglishEntries[$Id][1])) {
                $Problems += "Placeholders differ from en-US: [$Section] $Key"
            }
        }

        $Translated = $EnglishEntries.Count - $Missing.Count
        Write-Host ("{0,-45} {1,4}/{2} keys translated" -f $File.Name, $Translated, $EnglishEntries.Count)
        if ($ShowMissing -and $Missing.Count -gt 0) {
            Write-Host "  Missing: $($Missing -join ', ')" -ForegroundColor Gray
        }
        foreach ($Problem in $Problems) { Write-Host "  $Problem" -ForegroundColor Yellow }
        $TotalIssues += $Problems.Count
    }
}

# Summary
Write-Host ("`n" + "=" * 80) -ForegroundColor Gray
Write-Host "Validated $FileCount file(s), problems found: $TotalIssues" -ForegroundColor $(if ($TotalIssues -eq 0) { "Green" } else { "Yellow" })

exit $(if ($TotalIssues -eq 0) { 0 } else { 1 })
```

Run it:
```powershell
.\tools\Validate-Translations.ps1
.\tools\Validate-Translations.ps1 -LanguageCode de-DE
.\tools\Validate-Translations.ps1 -ShowMissing
```

`-ShowMissing` lists the keys of each file that are still English.

## Integration with Build Process

### Unit Tests

`src/Greenshot.Tests/Core/LanguagePackTests.cs` runs with the other tests and checks that:
- every property of the language interfaces has an English text
- every `{wpf:Text Section.Property}` in XAML refers to an existing property
- the language packs contain only known sections (spelled exactly as in en-US), and no line outside a section or without `key=value`
- runtime keys such as `WindowCaptureMode.Auto` and `Recipe.extension_border` are found

### Pre-commit Hook

Add to `.git/hooks/pre-commit`:

```bash
#!/bin/bash
# Check the structure of language packs before commit

echo "Validating translation files..."

# Check if language packs are being committed (added, copied or modified)
TRANSLATION_FILES=$(git diff --cached --name-only --diff-filter=ACM | grep 'Languages/greenshot[^/]*\.ini$')

if [ -n "$TRANSLATION_FILES" ]; then
    echo "Found translation file changes, validating..."

    PROBLEMS=$(awk '{ sub(/\r$/, "") }
         FNR == 1 { insection = 0 }
         /^\[.+\]$/ { insection = 1; next }
         /^[[:space:]]*$/ || /^[;#]/ { next }
         !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' $TRANSLATION_FILES)
    if [ -n "$PROBLEMS" ]; then
        echo "ERROR: lines outside a [Section] or without key=value:"
        echo "$PROBLEMS"
        exit 1
    fi

    for file in $TRANSLATION_FILES; do
        if ! iconv -f UTF-8 -t UTF-8 "$file" > /dev/null 2>&1; then
            echo "ERROR: $file is not valid UTF-8"
            exit 1
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
    - uses: actions/checkout@v4
    
    - name: Check structure of the language packs
      run: |
        problems=$(awk '{ sub(/\r$/, "") }
             FNR == 1 { insection = 0 }
             /^\[.+\]$/ { insection = 1; next }
             /^[[:space:]]*$/ || /^[;#]/ { next }
             !insection || !/=/ { print FILENAME ":" FNR ": " $0 }' \
            src/Greenshot/Languages/greenshot.*.ini src/Greenshot.Plugin.*/Languages/greenshot.*.ini)
        if [ -n "$problems" ]; then
          echo "$problems"
          exit 1
        fi
    
    - name: Check encoding
      run: |
        for file in src/Greenshot/Languages/greenshot.*.ini src/Greenshot.Plugin.*/Languages/greenshot.*.ini; do
          if ! iconv -f UTF-8 -t UTF-8 "$file" > /dev/null 2>&1; then
            echo "ERROR: $file is not UTF-8"
            exit 1
          fi
        done

    - name: Compare with en-US
      run: python3 tools/validate_translations.py
```

The last step needs `tools/validate_translations.py` from above in the repository. The unit tests in `src/Greenshot.Tests/Core/LanguagePackTests.cs` check the format placeholders as well.

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
9. **Interface Checker**: Verify every en-US key has a property on its language interface

## Contributing

If you create additional validation tools:

1. Add them to the `tools/` directory
2. Document them in this file
3. Include usage examples
4. Consider making them cross-platform (Python preferred)
5. Add error handling and helpful error messages

---

**Version**: 1.1  
**Last Updated**: 2026-10-03
