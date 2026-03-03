# Modern File Dialogs Come to Greenshot 1.4

**Date**: February 21, 2026  
**Version**: Greenshot 1.4.108  
**Status**: Development Build

We're excited to announce a significant user experience improvement in Greenshot 1.4: all file and folder dialogs have been modernized!

## What's Changed?

If you've been using Greenshot for a while, you may have noticed that our file save and folder selection dialogs looked a bit dated. That's because they were using the legacy Windows Forms dialogs that have been around since Windows XP.

With this update, **every file and folder dialog in Greenshot now uses the modern Windows Vista+ Common Item Dialog**. This brings a fresh, contemporary look and feel that matches the rest of your modern Windows experience.

## Key Benefits

### Support for Long File Paths

The most important improvement is **full support for long file paths** (over 260 characters). Previously, if you tried to save a screenshot to a deeply nested folder structure, you might have encountered errors or limitations. Those issues are now resolved.

### Modern User Experience

The new dialogs provide:
- A cleaner, more intuitive interface
- Better navigation with modern folder trees
- Quick access to common locations via the sidebar
- Improved keyboard shortcuts and accessibility

### Where You'll See the Changes

The modern dialogs appear throughout Greenshot:
- **Saving screenshots**: When you save an image from the capture or editor
- **Opening files**: When loading images in the file capture mode or editor
- **Folder selection**: When browsing for output directories in settings
- **Plugin configurations**: Including the ExternalCommand plugin settings

## Technical Details

For those interested in the technical side, we've replaced all legacy `SaveFileDialog`, `OpenFileDialog`, and `FolderBrowserDialog` components with modern builder patterns from the Dapplo.Windows.Dialogs package (v2.0.63). This package provides access to the Windows Common Item Dialog through COM/P-Invoke, giving us the best of both worlds: modern UI with native Windows integration.

## Try It Out

This feature is available now in Greenshot 1.4.108 continuous builds. Remember that 1.4 builds are unsigned development versions intended for testing. If you'd like to try them out:

1. Visit the [GitHub Releases page](https://github.com/greenshot/greenshot/releases)
2. Download the latest 1.4 continuous build marked as "UNSTABLE-UNSIGNED"
3. Test it in your workflow and [let us know](https://github.com/greenshot/greenshot/issues) what you think!

## Looking Forward

This is just one of many improvements coming in Greenshot 1.4. Stay tuned for more updates as we continue to modernize and enhance your screenshot experience.

For more details about all the changes in Greenshot 1.4, check out our [complete changelog](https://github.com/greenshot/greenshot/blob/main/docs/changelogs/CHANGELOG-1.4.md).

---

*Note: Greenshot 1.4 is currently in active development. For production use, we recommend the latest stable 1.3 release available at [getgreenshot.org](https://getgreenshot.org).*
