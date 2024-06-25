# Rewired Input Glyphs

![Rewired Input Glyphs Logo](.github/images/Rewired-Glyphs%20Readme%20Logo.png)

An expansion for Guavaman's [Rewired Unity Asset](https://assetstore.unity.com/packages/tools/utilities/rewired-21676) that provides a simple to use API for showing input icons to the player at runtime based on their input method and bindings.

## Features

![Features](.github/images/Rewired-Glyphs%20Readme%20Features.png)

- Features numerous built in components to quickly show an icon for an action
- Automatically changes sprites to show the user's most recently used device
- Can show multiple sprites inline with text using TextMeshPro sprite sheets

## Requirements

- A Unity project of Unity 2020.3 or later
- Ownership of the [Rewired](https://assetstore.unity.com/packages/tools/utilities/rewired-21676) Unity asset

## Quick Start Guide

1. Download and install the [Rewired](https://assetstore.unity.com/packages/tools/utilities/rewired-21676) asset into your Unity project
	1. See [Rewired's Quick Start](https://guavaman.com/projects/rewired/docs/QuickStart.html) for more information on getting started with Rewired.
	2. Note: The Rewired asset will never be included in this package. Purchasing Rewired from the Unity Asset Store is required for this asset to function.
2. Install the package via Git in the Package Manager
	1. Ensure you have Git installed on your system
	2. In Unity go to `Window -> Package Manager`
	3. Press the + icon at the top left of the Package Manager window
	4. Choose "Add package from Git URL"
	5. Enter the following into the field and press enter: 
	   1. Tip: You can append a version to the end of the Git URL to lock it to a specific version such as `https://github.com/Orange-Panda/Rewired-Glyphs.git#2.0.0`
	```
	https://github.com/Orange-Panda/Rewired-Glyphs.git
	```
	6. Refer to [the official Unity documentation](https://docs.unity3d.com/Manual/upm-ui-giturl.html) for more info on installing packages through Git
3. Import the `Kenney Default Glyphs` (Pixel Art) or `Xelu Default Glyphs` sample from this package in the Package Manager.
	1. This will import CC0 licensed input sprites by [Kenney](https://kenney.nl/assets/input-prompts-pixel-16) or [Xelu](https://thoseawesomeguys.com/prompts/) alongside `GlyphMap` and `GlyphCollection` assets that associate inputs to glyphs.
	2. By default this should import to your project at `Assets/Samples/Rewired Input Glyphs/{version}/{sample name}`
	3. Feel free to move these files to any location you'd prefer
	4. If you use these glyphs in your application you are encouraged to support their creator when possible
4. Find the Rewired Input Manager prefab in your project and add the `Rewired Glyph Manager` component to it.
	1. This component and others in the package can be found in the Add Component menu under `Rewired Glyphs`
5. Add the `Glyph Collection` imported from step 3 to the `Rewired Glyph Manager`
6. Press the `Generate TMP Sprite Sheet` button on the `Rewired Glyph Manager`
	1. This generates TextMeshPro sprite sheets that allow for inline input glyph icons with your text.
7. You are ready to go! Check out the usage section below to start displaying the icons in your project!

## Usage

There are several ways to show input glyphs in your project such as:
- Built-in components
- Custom components
- Polling the `InputGlyphs` API directly

Let's cover some of these methods and how to use them in your project.

### Built-in Components

- `GlyphRichTextFormatter` - Attach this component to a Game Object with a `TMP_Text` and it will format text such as `<glyph "Jump">` into an inline sprite that represents that action.
	- `<glyph "Jump">` will show the jump input sprite for player at index 0
	- `<glyph "Move Horizontal" 1 Negative>` will show the move left input sprite for player at index 1
	- `<glyph 13 0>` will show the action #13 input sprite for player at index 0
    - `<glyph "Move Horizontal" Full>` will show the move left or right input sprite for player at index 0
- `GlyphImageOutput` - Show an input icon for an action, even if there is no sprite.
- `GlyphTextOutput` - Show an input description for an action.
- `GlyphHybridOutput` - Show an input icon for an action or fallback to text if there is no sprite available.
- `GlyphLabeledImageOutput` - Show an input icon with an associated but independent `TMP_Text` and set their layout.

### Custom Components

Custom components can be created by inheriting from `GlyphDisplay`.
This class provides the `void SetGlyph(Glyph glyph, AxisRange axisRange)` method which is automatically invoked when a glyph may have changed such as a input device changes or preferences modifications.

Most of the built-in components inherit from `GlyphDisplay` so consider referencing them for examples.

### Polling the InputGlyphs API

If you wanted tighter control over your implementation of presenting glyphs you can also use the `InputGlyphs` class to get glyph information yourself.
Some important members to consider are the following:

- `event Action RebuildGlyphs` - A static event which is invoked whenever the Glyph output of this class may have changed.
- `Glyph GetCurrentGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0)` - Get the `Glyph` for particular player's action.
- `void SetGlyphPreferences(HardwarePreference hardwarePreference, SymbolPreference symbolPreference)` - Set the preferred symbols you want to present to the user.

## Documentation

For more information check out [the official documentation](https://orange-panda.github.io/Rewired-Glyphs/) for articles and API reference.

## Getting Help

- Use the [Issues](https://github.com/Orange-Panda/Rewired-Glyphs/issues) or [Discussions](https://github.com/Orange-Panda/Rewired-Glyphs/discussions) of this GitHub repository for support.

## Credits

This package is developed by [Luke Mirman](https://lukemirman.com/) under the [MIT License](LICENSE.md).

- Joystick icon used in the logo and icons used in component icons are provided by Google Fonts under the Appache 2.0 license.
- [Lato font](https://fonts.google.com/specimen/Lato/about) in the logo is provided by Google Fonts under the Open Font License.
- Project sample glyph icons included in package were created by [Kenney](https://kenney.nl/assets/input-prompts-pixel-16) and [Xelu](https://thoseawesomeguys.com/prompts/) (respectively) and distributed under Creative Commons CC0 license.
    - Some icons were altered from their original state
- DocFX Documentation template provided by [Singulink](https://github.com/Singulink/SingulinkFX/) under MIT license
- Guavaman for developing the [Rewired](https://guavaman.com/projects/rewired/) asset this package supports

## Disclaimer

The development of this package is not associated with Rewired or Guavaman.
Do not contact Rewired support for any issues you encounter with this package.