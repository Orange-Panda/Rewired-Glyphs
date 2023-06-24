# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2023-06-23

### Fixed
- Fixed `Rewired Glyph Manager` not marking glyph maps and collection dirty when generating TextMeshPro sprite sheets 

## [1.0.0] - 2023-06-23

### Added
- Added support for inline input glyphs using `GlyphRichTextFormatter`
- Added support for entering a glyph map into a collection via hardware Guid
- Added sample glyphs and glyph mappings to improve onboarding experience
- Fallback glyphs now use the null glyph's sprite. If you do not wish to show this sprite evaluate the `Glyph.PreferDescription` value.

## [0.2.0] - 2023-06-21

### Added
- Input glyphs can now be retrieved on a per player basis.
- `GlyphDisplay` can now use action ids instead of names.

### Changes
- `GenericEntry` on `InputGlyphCollection` has been renamed to `TemplateEntry`
- `GenericMaps` on `InputGlyphCollection` has been renamed to `TemplateMaps`
- `Entry` on `InputGlyphCollection` has been renamed to `HardwareEntry`
- `Maps` on `InputGlyphCollection` has been renamed to `HardwareMaps`
- Enums that were previously nested within `InputGlyphs` have been moved to outer scope
- Everything in the package is now within the `LMirman.RewiredGlyphs` namespace
- InputGlyphs now uses an integer parameter for actions instead of string
- Most class names containing `InputGlyph` have been simplified to just `Glyph`
  - `InputGlyphs` will not be renamed.
- `InputGlyphObserver` is now `RewiredGlyphManager` and is required to be added to the Rewired `Input Manager` for Input Glyph system to function.

### Removed
- The glyph collection is no longer referenced through the `Resources` folder.

## [0.1.0] - 2023-06-20

### Added
- Package created.