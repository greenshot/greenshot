# Text Obfuscation Feature

## Overview

This feature allows users to automatically obfuscate text in screenshots by searching for specific text patterns using OCR (Optical Character Recognition) and applying pixelation filters to matched text.

## How It Works

### Accessing the Feature

1. Capture or open a screenshot in the Greenshot Editor
2. Go to **Edit** → **Obfuscate Text...**
3. The Text Obfuscation dialog will open

### Dialog Components

The Text Obfuscation dialog includes:

- **Search for**: Text input field to enter the text or pattern to search for
- **Search button**: Manually trigger the search (also triggered automatically if Auto Search is checked)
- **Use Regular Expression**: Enable regex pattern matching
- **Case Sensitive**: Make the search case-sensitive
- **Auto Search**: Automatically search as you type
- **Search in**: Dropdown to select whether to search in Words or Lines
- **Matches found**: Display the number of matches found
- **Apply**: Apply pixelation to all matched text and close the dialog
- **Cancel**: Close the dialog without applying changes

### Workflow

1. **OCR Processing**: When the feature is first accessed, if OCR hasn't been performed yet:
   - The system automatically runs Windows 10 OCR on the screenshot
   - A wait cursor is displayed during OCR processing
   - If OCR fails or finds no text, an appropriate message is shown

2. **Search**: Enter text or a regex pattern in the search field:
   - Simple text search: Type any text (e.g., "password", "confidential")
   - Regex search: Enable "Use Regular Expression" and enter a pattern (e.g., `\d{3}-\d{2}-\d{4}` for SSN)
   - Choose whether to search in individual words or entire lines
   - Results are highlighted in real-time with yellow bounding boxes

3. **Preview**: Matched text is highlighted with:
   - Yellow border (3px thick)
   - Semi-transparent yellow fill (alpha = 50)
   - Preview updates automatically if Auto Search is enabled

4. **Apply**: Click Apply to:
   - Remove the yellow preview boxes
   - Create pixelation filters for each matched text area
   - Close the dialog

5. **Cancel**: Click Cancel to:
   - Remove all preview highlights
   - Close the dialog without making changes

## Implementation Details

### Files Added

- `src/Greenshot.Editor/Forms/TextObfuscationForm.cs` - Main form logic
- `src/Greenshot.Editor/Forms/TextObfuscationForm.Designer.cs` - Form designer code

### Files Modified

- `src/Greenshot.Editor/Forms/ImageEditorForm.cs` - Added menu item handler
- `src/Greenshot.Editor/Forms/ImageEditorForm.Designer.cs` - Added menu item definition
- `src/Greenshot/Languages/language-en-US.xml` - Added translation strings

### Translation Keys

The following language keys were added to `language-en-US.xml`:

- `editor_obfuscate_text` - Menu item text
- `editor_obfuscate_text_title` - Dialog title
- `editor_obfuscate_text_search` - Search label
- `editor_obfuscate_text_search_button` - Search button text
- `editor_obfuscate_text_regex` - Regex checkbox
- `editor_obfuscate_text_case_sensitive` - Case sensitive checkbox
- `editor_obfuscate_text_auto_search` - Auto search checkbox
- `editor_obfuscate_text_search_scope` - Search scope label
- `editor_obfuscate_text_scope_words` - Words option
- `editor_obfuscate_text_scope_lines` - Lines option
- `editor_obfuscate_text_matches` - Matches count (with {0} placeholder)
- `editor_obfuscate_text_apply` - Apply button
- `editor_obfuscate_text_error` - Error label
- `editor_obfuscate_text_no_capture` - No capture message
- `editor_obfuscate_text_no_ocr_provider` - No OCR provider message
- `editor_obfuscate_text_ocr_failed` - OCR failed message
- `editor_obfuscate_text_no_text` - No text found message

### Dependencies

- Requires Windows 10 OCR provider (Win10OcrProvider)
- Uses existing ObfuscateContainer and PixelizationFilter
- Uses existing Surface and DrawableContainer infrastructure

## Testing Instructions

### Prerequisites

- Windows 10 or later (for OCR support)
- Visual Studio 2019+ or MSBuild Tools for Windows
- .NET Framework 4.8.0

### Build Instructions

1. Clone the repository
2. Open `src/Greenshot.sln` in Visual Studio
3. Build the solution (Configuration: Debug or Release)

### Manual Testing Steps

1. **Basic Functionality**:
   - Launch Greenshot
   - Take a screenshot containing text
   - Open in editor
   - Go to Edit → Obfuscate Text
   - Verify dialog opens and OCR runs

2. **Simple Text Search**:
   - Enter a word that appears in the screenshot
   - Verify yellow boxes appear around matching text
   - Click Apply
   - Verify pixelation is applied to matched areas

3. **Regex Search**:
   - Enable "Use Regular Expression"
   - Enter a regex pattern (e.g., `\d+` for numbers)
   - Verify matches are highlighted
   - Test valid and invalid regex patterns

4. **Case Sensitivity**:
   - Search for text with different cases
   - Toggle "Case Sensitive" checkbox
   - Verify results change appropriately

5. **Search Scope**:
   - Search for text that appears in words vs. lines
   - Switch between "Words" and "Lines" in the dropdown
   - Verify different bounding boxes

6. **Auto Search**:
   - Enable Auto Search
   - Type in the search box
   - Verify preview updates in real-time

7. **Cancel Functionality**:
   - Perform a search
   - Click Cancel
   - Verify no changes are made to the image

8. **Error Handling**:
   - Test with an image with no text
   - Test with invalid regex patterns
   - Verify appropriate error messages

## Known Limitations

- Requires Windows 10 or later for OCR support
- OCR accuracy depends on Windows 10 OCR engine
- Preview rectangles are temporary and not editable
- Only supports pixelation filter (not blur or other filters)

## Future Enhancements

Potential improvements for future versions:

- Support for multiple obfuscation filter types (blur, solid color, etc.)
- Ability to manually adjust bounding boxes before applying
- Save/load search patterns
- Batch processing of multiple matches
- Support for other OCR providers
- Preview of pixelation effect before applying
