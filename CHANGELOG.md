# Changelog
All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [UNRELEASED] - TBD

### Added
- Added `GlyphType`, `IsFallbackGlyph` and `IsInputGlyph` properties to `Glyph`
  - Allows for checking if a Glyph is intended to represent a `Null`, `Unbound`, or `Uninitialized` glyph through code.
- Added `forceAxis` parameter to `InputGlyphs` GetGlyph methods
  - When true, will get the glyph for the entire axis such as "Move Horizontal" instead of "Move Left" and "Move Right"
  - Defaults to false, providing a similar behavior to before
- Added improved documentation to `InputGlyphs` GetGlyph methods

### Changed
- Getting glyphs for button or split elements for an axis action is now significantly more reliable
  - Previously getting positive/negative actions such as "Move Horizontal" for joysticks would return the unbound glyph despite a full axis map to the entire horizontal joystick
  - Now InputGlyph will infer the Joystick Left and Joystick Right from the full axis map.

## [1.3.0] - 2023-10-31

### Added
- Added `useSpritesWhenAvailable` field to `GlyphRichTextFormatter`.
  - When `true` will replace glyph rich text with a TMP inline sprite if there is a Sprite available for the found glyph.
  - When `false` will always use the Glyph's description even if it has a sprite. 
  - Defaults to `true` which matches the behaviour of previous implementation
- Added `descriptionFormat` field to `GlyphRichTextFormatter` which controls the way descriptions are output.
  - Defaults to `[{0}]` which matches the behaviour of previous implementation
- Added `formatTextOnStart` field to `GlyphRichTextFormatter`
  - Defaults to `true` which matches the behaviour of previous implementation

### Fixed
- Fixed `GlyphRichTextFormatter` not having up to date glyphs if component was not enabled during glyph rebuild.

## [1.2.0] - 2023-10-22

### Added
- Added descriptions for `Positive` and `Negative` axis range on Glyph
  - If you have created custom components you are encouraged to use `GetDescription()` instead of the now obsolete ~~`Description`~~
- Added a brand new `Glyph Map` Editor
  - Quickly generate a glyph map based on your project's Rewired controller maps
  - Validate glyph values against the expected definitions from your project's `Controller Data Files` asset
- Added `GetKeyboardGlyph` and `GetMouseGlyph` functions to the `InputGlyphs` class
- Exposed previously private methods `GetNativeGlyphFromGuidMap`, `GetNativeGlyphFromHardwareMap`, and `GetNativeGlyphFromTemplateMap` in `InputGlyphs` class
  - These methods are not recommended in most cases, but may be useful if you need tight control over glyph output.

## [1.1.0] - 2023-09-11

### Added
- Added support for inserting new glyph definitions to glyph map in editor

### Fixed
- Fixed incorrect Dualsense and Dualshock 4 glyph map in package sample
  - Requires reimporting sample to fix
- Fixed some calls being made to Rewired when it is not active
- Fixed glyph system cache referencing null players when input manager is quit or restarted
- Fixed glyph display never initializing action id if Reinput system was not ready on Awake
- Fixed rebuilds not sometimes not occuring on later glyph displays if an exception occured in an earlier glyph display

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