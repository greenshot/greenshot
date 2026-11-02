# Image formats in Greenshot 1.4

This describes Greenshot 1.4, which is still in development.

Greenshot opens images (with *Open image from file*, by drag & drop, from the command line, from the clipboard and in recipes) and
saves them (files, uploads, attachments, the clipboard). This page explains which formats work and how you can add more.

- [Opening images](#opening-images)
- [More formats with Windows codecs](#more-formats-with-windows-codecs)
- [Saving images](#saving-images)
- [Photos are turned upright](#photos-are-turned-upright)
- [Expert settings](#expert-settings)

## Opening images

On Windows, Greenshot opens images with the image codecs of Windows (the Windows Imaging Component, WIC). Every format Windows has
a codec for, Greenshot can open too:

- Always available through Windows: PNG, JPEG, BMP, GIF, TIFF, JPEG XR (.jxr, .wdp, .hdp, .wmp) and DDS.
- Always available with Greenshot's own readers: ICO, SVG, EMF, WMF, TGA, PBM and .greenshot files.
- With extra codecs from the Microsoft Store: see the next section.

If Windows can't read a PNG, JPEG, BMP, GIF or WebP file, Greenshot tries its own reader (ImageSharp) next. So WebP, for example,
opens even without the WebP codec from the Store.

*Open image from file* lists every format Greenshot can open on your PC. *All files* shows the rest, too.

## More formats with Windows codecs

Greenshot asks Windows at startup which image codecs are installed, so a **codec you install in Windows works in Greenshot
automatically**, after a restart of Greenshot. Nothing has to be set up in Greenshot.

Codecs Microsoft offers in the Microsoft Store, for example (most are free, some, like the HEVC Video Extensions, can cost money):

| Store extension | Adds |
|---|---|
| HEIF Image Extensions, with the HEVC Video Extensions | .heic and .heif, photos from iPhones and many Android phones |
| HEIF Image Extensions, with the AV1 Video Extension | .avif |
| Webp Image Extensions | .webp through Windows |
| Raw Image Extension | camera RAW files (.cr2, .cr3, .nef, .arw, .dng, .orf, .rw2 and more) |
| JPEG XL Image Extension | .jxl |

Codecs from other vendors work the same way when they install themselves as a Windows Imaging Component (WIC) codec.

Some extensions come with Windows already, or are installed by Windows updates. Which formats are available therefore differs from
PC to PC. A recipe or a colleague's file can need a codec your PC doesn't have; then Greenshot can't open that file until the codec
is installed.

## Saving images

Greenshot saves PNG, JPEG, BMP, GIF, TIFF, WebP, TGA, PBM, JPEG XR, ICO and .greenshot files on every PC, and PDF with the PDF
plugin.

- PNG files are written as small as possible without losing anything.
- Codecs installed in Windows are used for opening images only; saving doesn't depend on them.

## Photos are turned upright

Phones and cameras often store a photo sideways and add a note (EXIF orientation) how to turn it. When Windows opens such a photo
for Greenshot, Greenshot turns it upright. To switch this off, set `ProcessEXIFOrientation=False` in greenshot.ini.

## Expert settings

These are in greenshot.ini only:

- `OutputFilePngCompressionLevel` (1–9, default 9): how hard PNG files are compressed. Lower is faster and a little bigger.
- `ClipboardPngCompressionLevel` (1–9, default 8): the same for the PNG on the clipboard.
- `OutputFilePngPalette` (default True): write PNG files with few colors with a palette. Set it to False for programs which can't
  read such PNG files.
- `ProcessEXIFOrientation` (default True): turn photos upright when opening them.
