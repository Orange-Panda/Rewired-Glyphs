# Quick Start

This article will guide you on how to get up and running with Rewired Glyphs as quickly as possible.

## Prerequisites

- Basic understanding of unity concepts
- A Unity project

## Procedure

1. Download and install the [Rewired](https://assetstore.unity.com/packages/tools/utilities/rewired-21676) asset into your Unity project.
	1. See [Rewired's Quick Start](https://guavaman.com/projects/rewired/docs/QuickStart.html) for more information on getting started with Rewired.
	2. Note: The Rewired asset will never be included in this package. Purchasing Rewired from the Unity Asset Store is required for this asset to function.
2. Install the package via Git in the Package Manager
	1. Ensure you have Git installed and your Unity Version supports Git package manager imports (2019+)
	2. In Unity go to `Window -> Package Manager`
	3. Press the + icon at the top left of the Package Manager window
	4. Choose "Add package from Git URL"
	5. Enter the following into the field and press enter: 
	   1. Tip: You can append a version to the end of the Git URL to lock it to a specific version such as `https://github.com/Orange-Panda/Rewired-Glyphs.git#v2.0.0`
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
7. You are ready to go! Check out the section below to start displaying the icons in your project!

## Further Reading

Now that the package has been setup it is time to show the glyphs to the user. There are many ways to display Glyphs in your project including:

#### Using Built-in Components

The simplest way to get started with showing glyphs in your game is to utilize one of the built-in components.

- [GlyphRichTextFormatter](xref:LMirman.RewiredGlyphs.Components.GlyphRichTextFormatter) - Attach this component to a Game Object with a `TMP_Text` and it will format text such as `<glyph "Jump">` into an inline sprite that represents that action.
	- `<glyph "Jump">` will show the jump input sprite for player at index 0
	- `<glyph "Move Horizontal" 1 Negative>` will show the move left input sprite for player at index 1
	- `<glyph 13 0>` will show the action #13 input sprite for player at index 0
    - `<glyph "Move Horizontal" Full>` will show the move left or right input sprite for player at index 0
- [GlyphImageOutput](xref:LMirman.RewiredGlyphs.Components.GlyphImageOutput) - Show an input icon for an action, even if there is no sprite.
- [GlyphTextOutput](xref:LMirman.RewiredGlyphs.Components.GlyphTextOutput) - Show an input description for an action.
- [GlyphHybridOutput](xref:LMirman.RewiredGlyphs.Components.GlyphHybridOutput) - Show an input icon for an action or fallback to text if there is no sprite available.
- [GlyphLabeledImageOutput](xref:LMirman.RewiredGlyphs.Components.GlyphLabeledImageOutput) - Show an input icon with an associated but independent `TMP_Text` and set their layout.

#### [Creating a Custom Component](xref:LMirman.RewiredGlyphs.Components.GlyphDisplay)

While the built-in components were written to make implementation as frictionless as possible they may not cover your use case or needs adequately. In such case you may need to create your own custom `GlyphDisplay` component. Inheriting from this class provides the `void SetGlyph(Glyph glyph, AxisRange axisRange)` which you can implement to show a desired glyph. It gets invoked whenever the InputGlyph system may display new outputs such as when the user changes device.

Most of the built-in components inherit from `GlyphDisplay` so consider referencing them for examples.

#### [Polling InputGlyphsAPI](xref:LMirman.RewiredGlyphs.InputGlyphs)

If you need even more flexibility than what `GlyphDisplay` provides you should consider taking advantage of the `InputGlyphs` API directly. This static class features various functions for getting Glyph information, configuring Glyph output, and switching Glyph collections.

The most important members to consider in your implementation are:
- `event Action RebuildGlyphs` - A static event which is invoked whenever the Glyph output of this class may have changed.
	- Make sure to unsubscribe from the event when your component is destroyed
- `Glyph GetCurrentGlyph(int actionID, Pole pole, out AxisRange axisRange, int playerIndex = 0)` - Get the `Glyph` for particular player's action.
- `void SetGlyphPreferences(HardwarePreference hardwarePreference, SymbolPreference symbolPreference)` - Set the preferred symbols you want to present to the user.