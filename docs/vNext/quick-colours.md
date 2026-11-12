# Quick colours

Four visible swatches (black, red, yellow and blue) provide one-click colours. The Colours dropdown contains more fixed colours, a line/fill target selector, and six recent colours. The toolbar stays compact at narrow window widths.

Recent colours include picker choices and sampled foreground/background colours, are deduplicated, and use the existing RecentColors editor setting. Only targets supported by the current drawing tool or selection are enabled.

## Comparison with PR #338

[PR #338](https://github.com/greenshot/greenshot/pull/338), merged on 28 January 2022, introduced foreground/background colour presets and thickness/shadow keyboard shortcuts. This branch retains the current upstream shortcut handlers, including their later Ctrl modifier: Ctrl+number selects the fill preset, Ctrl+Shift+number selects the line preset, and Ctrl+Add/Subtract/Divide retains thickness/shadow behavior. The historical PR's bindings differ from current upstream; this branch does not restore the historical bindings.

The toolbar adds a mouse flow for choosing a fixed or recently used colour and explicitly selecting the line/fill target. It does not add a second keyboard preset system or remap keys. Shortcut colour events also feed the recent-colour list while an editor is bound. Selected-object colour changes use the existing field aggregator and undo path. Existing project colour serialization and the RecentColors configuration property are unchanged.