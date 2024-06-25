# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.2.0] - UNRELEASED

### Changed

- Rewrote code that used C# 9.0 features, which was preventing package use in Unity 2020.3.
	- Unity 2020.3 is now declared the minimum required Unity version for this packaged. Earlier Unity releases are not supported.

## [2.1.0] - 2024-06-14

### Added

- Added new constructors to `Glyph` that utilize a `ControllerType` parameter
	- ⚠️ Marked the constructors that don't have this parameter as obsolete. They will not be removed until the next major release at earliest.

### Changed

- Fallback glyphs now have a `ControllerType` value for the device they were intended to represent
	- Previously was always `null`

### Fixed

- Fixed `hideKBM` functionality not behaving as expected for fallback glyphs

## [2.0.1] - 2024-06-11

### Fixed

- Fixed an issue where incorrect glyphs may have been shown if there were multiple controllers of different types plugged in
	- Example: If a first controller 'A' is an `Xbox One` controller and a controller 'B' is a `DualSense` controller, controller B would sometimes show incorrect glyphs.

## [2.0.0] - 2024-06-11

### ❇️ Upgrade Guide ❇️

This release includes ***major breaking*** changes which *will* require your attention in order to upgrade to this version from any `1.x` release

- `GlyphCollection` has been overhauled. For each `GlyphCollection` in your project you must reassign the hardware maps to the newly designated 'Controller Maps'
	- If you are using one of the `GlyphCollection` samples you are encouraged to reimport the updated `2.x` sample instead.
- If referenced `HardwareDefinition` in any of your scripts, they must be replaced/removed
	- Use `Controller` or `ControllerType, Guid` instead.
	- This affects `GetNativeGlyphFromGuidMap` method calls in particular

### ⚠️ Major Breaking Change - Hardware Definition Removed ⚠️

- `Hardware Definition` has been completely removed for more accurate glyph queries and an improved user experience when creating `GlyphCollection`
- Most methods that were directly associated with `HardwareDefinition` have been removed entirely.
- Methods that used `HardwareDefinition` to target a specific controller now use `Controller` or `ControllerType, Guid` instead.
	- The signature for some query methods have changed as a result such as `GetNativeGlyphFromGuidMap`
- ⚠️ You *will* have to update your `1.x` `GlyphCollection` when updating to v2.0.0 by reassigning your controller glyph maps to the collection!
- If you used any methods that used `HardwareDefinition` your code will *not* compile when upgrading to this version and will require changes.

### Added

- Added official `Documentation~` which can also be found online at https://orange-panda.github.io/Rewired-Glyphs/
	- This is an ongoing project, so you are encouraged to make article requests in the [Issues](https://github.com/Orange-Panda/Rewired-Glyphs/issues)
	  or [Discussions](https://github.com/Orange-Panda/Rewired-Glyphs/discussions) if you are unable to find documentation for your use case.
- Added an overhauled `GlyphCollection` editor user experience
- Added `ControllerType` property to `Glyph` to inform components about the device the glyph intends to represent.
	- All Template maps represent `Joystick` glyphs
	- Controller maps use the value defined in its entry on the `GlyphCollection`
- Added optional feature for hiding non-input glyphs (null, uninitialized, etc.) on built-in components (default does not hide)
	- Enable in `GlyphRichTextFormatter` using `hideInvalid` option in glyph tag (Example: `<glyph Jump hideInvalid>`)
- Added optional feature for hiding keyboard and mouse glyphs in built-in components (default does not hide)
	- Enable in `GlyphRichTextFormatter` using `hideKBM` option in glyph tag (Example: `<glyph Jump hideKBM>`)
- Added `ShouldHideGlyph(Glyph)` protected method to `GlyphDisplay` which can be used by inheritors to inform if they should hide the output glyph (due to the above rules)
	- If you don't implement this check in your `SetGlyph` component it will behave identically to before, but will not support these optional settings.
- Added `GetGlyphSet` method to `InputGlyphs` for getting *all* glyphs for an action across all controller types, including multiple bindings on a single controller.
- Added `collectionKey` string field to `GlyphCollection` for distinctly identifying and referencing collections at runtime
- ⚠️ **[Breaking]** - Added `collectionKey` optional parameter to all `InputGlyphs` methods for referencing secondary (non-default) collections
- Added `additionalCollections` field to `RewiredGlyphManager` for additively loading additional collection for reference by their `collectionKey`
	- This field also supports generating TMP sprite sheets (generates for default collection and collections included in additional collections)
	- Note: Make sure the names of the sprite sheets containing glyphs are unique since they are referenced by name in TextMeshPro. An error message has been added to notify about such collisions.
- Added optional specifier `set=collectionname` for `GlyphRichTextFormatter` to target secondary glyph collections
	- Example: `<glyph Jump set=dark>` where 'dark' is the `collectionKey` on some `GlyphCollection` that is loaded into InputGlyphs
- Added `Generate Keyboard` and `Generate Mouse` functionality to `Glyph Map` for generating default Glyph Map actions
	- Requires the application running due to technical limitations
- Added component icons to all major components and scriptable objects of the package
	- Icons sourced from [Google Icons](https://fonts.google.com/icons)
- Added confirmation dialogue before generating TMP sprite sheet on Rewired Glyph Manager
- Added custom property drawer for `Glyph`, improving the editor experience
- Added new glyphs sample to package: [Xelu Prompts](https://thoseawesomeguys.com/prompts/)

### Changed

- Changed order of sprites in Glyph editor (now ordered Full, Positive, Negative to match description order)
- Rewrote the way glyph collections are loaded into memory internally
	- Switching the active glyph collection is now much more performant
	- Loading a glyph collection now only dispatches a glyph update if it may have changed the output of glyph queries
- `GlyphCollection` now initializes non-input glyph values with default values when created.
- Remove set access to `TemplateEntry` and `GuidEntry`
- Updated `Kenney` sample glyphs to new `GlyphCollection` format

### Fixed

- ⚠️ **[Breaking]** - Fixed `GetSpecificCurrentGlyph` and `GetCurrentGlyph` not utilizing the value of its 'forceAxis' parameter.
	- If you were utilizing a value of `true` you may notice different output of this method.
	- This method was being used by `GlyphRichTextFormatter` therefore tags such as `<glyph "MoveH" pole=FullAxis>` will output differently.
- Fixed Glyph `Positive` and `Negative` description sometimes not returning the expected value
- Fixed description validation error in `GlyphMapEditor`
- Fixed `Kenney` glyph maps having some inaccurate/missing actions

## [1.5.0] - 2024-05-30

### Added

- Added `Specific` methods to `InputGlyphs` that enable getting joystick symbols of a specific type regardless of the value of `InputGlyphs.PreferredSymbols`
	- These methods are generally not recommended unless you need to explicitly show that symbol while `InputGlyphs.PreferredSymbols` has a differing value
- `GlyphRichTextFormatter` now supports specifier syntax for additional arguments such as `pole=Positive`, `player=2`, or `type=Joystick`
- `GlyphRichTextFormatter` now supports specifying the controller type for the glyph such as `type=Keyboard`, `type=Mouse`, or `type=Joystick`
	- Caution: Specifying controller type is more prone to showing `UNBOUND` glyph since it will not fall back to any other type if there is no glyph for that controller
- `GlyphRichTextFormatter` now supports specifying the symbol for the glyph such as  `symbol=Auto`, `symbol=Xbox`, `symbol=PS`, or `symbol=Switch`

### Changed

- ⚠️ **[Breaking]** - Editor scripts have been moved from the `LMirman.RewiredGlyphs` namespace to `LMirman.RewiredGlyphs.Editor` namespace.
	- Despite not being backward compatible, this change won't be incrementing the major version since the affected types don't have any practical public API functionality.

## [1.4.0] - 2024-02-11

### Added

- Added `GlyphType`, `IsFallbackGlyph` and `IsInputGlyph` properties to `Glyph`
	- Allows for checking if a Glyph is intended to represent a `Null`, `Unbound`, or `Uninitialized` glyph through code.
- Added `forceAxis` parameter to `InputGlyphs` GetGlyph methods
	- When true, will get the glyph for the entire axis such as "Move Horizontal" instead of "Move Left" and "Move Right"
	- Defaults to false, providing a similar behavior to before
	- `GlyphRichTextFormatter` can output the full axis with the "Full Axis" parameter. Example: `<glyph "Move Horizontal" Full>`
- Added improved documentation to `InputGlyphs` GetGlyph methods
- Added `GetGlyph` method to `InputGlyphs` which uses a ControllerType parameter to map to the other specific GetGlyph methods

### Changed

- Getting glyphs for button or split elements for an axis action is now significantly more reliable
	- Previously getting positive/negative actions such as "Move Horizontal" for joysticks would return the unbound glyph despite a full axis map to the entire horizontal joystick
	- Now InputGlyph will infer the Joystick Left and Joystick Right from the full axis map.

### Fixed

- Fixed `Glyph.GetDescription()` returning a null string in rare cases
- Fixed exception in `GlyphRichTextFormatter` when providing a null string to `SetFormattedText(string)` method
- Fixed no controller being recognized until it inputs at least once
	- Now defaults to the first joystick if the player has not yet input using a controller yet, ensuring there is *some* glyph shown initially

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