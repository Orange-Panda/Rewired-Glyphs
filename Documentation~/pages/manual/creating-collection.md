# How to Create a Glyph Collection

This article will cover how to create a [GlyphCollection](xref:LMirman.RewiredGlyphs.GlyphCollection) from start to finish. By the end of this article you will have created glyphs to represent the most common device types: `Keyboard`, `Mouse`, `Xbox`, `Playstation`, and `Nintendo` controllers.

## Prerequisites

- Basic understanding of the Unity interface and concepts
- A unity project with Rewired and Rewired Glyph Manager prepared
- A collection of sprites to represent the controller actions.
	- If you don't have any there are many CC0/Public Domain offerings available such as [Xelu](https://thoseawesomeguys.com/prompts/), [Mr. Breakfast](https://mrbreakfastsdelight.itch.io/mr-breakfasts-free-prompts), and [Kenney](https://kenney.nl/assets/input-prompts)
	- If you utilize any of the assets above, while not required by their license, you are highly encouraged to donate to the creator of the asset for their time.
	- For this article we will be creating glyphs from the `Xelu` set
- Aseprite or an alternative program to create sprite sheets 

## Part 1 Assemble Sprites into a Sprite Sheet

If the sprites you acquired for your glyph are all separate (such that each icon is its own file) it is a good idea to assemble them into a sprite sheet. This reduces the number of files you have to track in your project, can improve texture optimization, and is generally a best practice for storing related sprites.

We will be using [Aseprite](https://www.aseprite.org/download) but there are likely other applications available that will offer similar function.

### 1.1 Import Sprites into Animation Track

Drag all of the sprites representing the controller type into a single Aseprite project animation track.

> <img src="../../attachments/manual/creating-collection/xelu-kb-aseprite-1.png" width="450">
> 
> Example of an Aseprite project with glyph sprites import

Sort the frames into an order that groups related symbols together.

> [!TIP]
>
> Aseprite can import all files at once into a single project if they are named in a numerical sequence.
>
> Below is a primitive Python script that renames all items in a specified directory in a numerical order.
> 
> <details>
> <summary>Basic Python Renaming Script</summary>
> 
> ```python
> import os
> 
> def rename_files_in_directory(directory):
> 	# Get a list of all files in the directory
> 	files = [f for f in os.listdir(directory) if os.path.isfile(os.path.join(directory, f))]
> 	files.sort()
> 
> 	# Rename each file
> 	for index, filename in enumerate(files):
> 		# Get the file extension
> 		file_extension = os.path.splitext(filename)[1]
> 		
> 		# Create the full path for the original and new file names
> 		new_name = f"{index + 1}{file_extension}"
> 		old_path = os.path.join(directory, filename)
> 		new_path = os.path.join(directory, new_name)
> 		
> 		# Rename the file
> 		os.rename(old_path, new_path)
> 			
> 		print(f"Renamed '{old_path}' to '{new_path}'")
> 
> # Usage
> directory_path = "B:\Downloads\Xelu\Keyboard & Mouse\Dark"
> rename_files_in_directory(directory_path)
> ```
> 
> </details>

### 1.2 Export Sprite Sheet

Once you have finished importing all sprites needed into the Aseprite project you can now export them all into a single sprite sheet.

Press `Ctrl+E` to bring up the export window or from `File -> Export -> Export Sprite Sheet`.

> <img src="../../attachments/manual/creating-collection/xelu-kb-aseprite-2.png" width="450">
>
> Example export settings window configuration

> <img src="../../attachments/manual/creating-collection/xelu-kb-aseprite-3.png" width="450">
>
> Example sprite sheet for Keyboard glyphs using a slightly modified Xelu set and the above export settings 

### 1.3 Repeat Process for All Controllers

Repeat step 1.1 and 1.2 for each device you have glyphs for.

> [!WARNING]
> 
> Before continuing to the next part you should have sprite assets ready for:
> 
> - Keyboard
> - Mouse
> - Xbox Gamepad
> - Playstation Gamepad
> - Switch Gamepad

## Part 2 Prepare Sprites in Unity

Now that we have our sprite sheets ready it is time to bring them into our Unity project.

> [!CAUTION]
>
> Ensure every Glyph sprite asset has a unique name, otherwise there will be a conflict when using them in TextMeshPro.

### 2.1 Import Sprite Assets to Project

Create a new folder in your Unity project for housing our sprite assets and our future Glyph Collection and Glyph Maps. Drag the sprite assets into your new created Unity project folder.

### 2.2 Configure Sprite Import Settings

Select all of your sprite assets and set their `Sprite Mode` to `Multiple` so we can divide it into many sprite from the sheet layout.

> [!TIP]
>
> If your sprites utilize pixel art consider changing `Filter Mode` to `Point (no filter)` and setting `Compression` to `None`.

### 2.3 Use Sprite Editor to Divide Sprite Sheet

For each sprite you imported use the [Sprite Editor](https://docs.unity3d.com/Manual/SpriteEditor.html) tool to slice the sprite sheet into its separate elements.

Refer to [the Official Unity Documentation](https://docs.unity3d.com/2021.3/Documentation/Manual/sprite-editor-use.html) for more information on using the Sprite Editor tool

> <img src="../../attachments/manual/creating-collection/sprite-editor-example.png" width="450">
>
> Example of sliced sprite sheet using Sprite Editor window

> [!WARNING]
>
> Before continuing to the next part you should now have:
> - All sprites used for glyphs are imported into the project
> - Sprite sheets have been sliced into its elements

## Part 3 Create and Assign Glyph Maps

Now that we have our sprites ready in Unity we can now create a [GlyphMap](xref:LMirman.RewiredGlyphs.GlyphMap) for each device so we can tell the program what sprite is associated with what input.

### 3.1 Create a Glyph Map

Let's begin by creating our first Glyph Map by right clicking in the Project window of Unity and navigating to `Create/Rewired Glyphs/Glyph Map`. 
This will create an empty Glyph Map at this location.

#### The Glyph Map Inspector

> <img src="../../attachments/manual/creating-collection/glyph-map-inspector.png" width="450">

With our glyph map selected lets take a look at the Inspector window.
Here we will find various fields such as:
- `Generate Keyboard`: If used while ReInput is ready, generates a list of *all* keyboard actions, including descriptions.
- `Generate Mouse`: If used while ReInput is ready, generates a list of *all* mouse actions, including descriptions.
- `Controller Data Files`: Used for generating and validating `Joystick` actions for a particular device.
- `Validate Map`: When true shows warnings and errors for this device if it is based on a `Joystick` device.
- `Based on`: Indicates what controller this map is intended to represent for validation and action generation purposes. Can only be a Joystick device and can **not** be a `Mouse` or `Keyboard`.
	- `Set by Hardware`: Validate this map based on a specific hardware device
	- `Set by Template`: Validate this map based on a controller template
	- `X` clear target if it isn't meant to represent a controller
- `Page`: Editor only shows 10 glyphs at a time, scroll to view more

**Glyph Editor**

- `Full Description`: The standard name of this input action such as `Left Stick Horizontal`, `A`, `Left Trigger`
- `Positive Description`: The name of the positive direction for Axis inputs such as `Left Stick Right`.
	- Leave empty if this is not an Axis input, will default to `Full Description`.
- `Negative Description`: The name of the negative direction for Axis inputs such as `Left Stick Left`.
	- Leave empty if this is not an Axis input, will default to `Full Description`.
- `Full Sprite`: The standard sprite for this input action.
- `Positive Sprite`: The sprite for positive direction of Axis inputs
	- Leave empty if this is not an Axis input.
- `Negative Sprite`: The sprite for the negative direction of Axis inputs
	- Leave empty if this is not an Axis input.

> <img src="../../attachments/manual/creating-collection/glyph-map-glyphs.png" width="450">

### 3.2 Generate Keyboard and Mouse Map

> [!NOTE]
>
> As of `v2.0.0`, due to technical limitations, the application will need to be running to generate `Mouse` and `Keyboard` glyph maps.

### 3.3 Generate Joystick Template Maps



### 3.4 Generate Joystick Device Maps



### 3.5 Populate Maps with Sprites



## Part 4 Assemble Glyph Collection

### 4.1 Create Glyph Collection Asset



### 4.2 Assign Hardware Maps

> [!NOTE]
>
> **What's the difference between a hardware map and a template map?**
>
> A map defined in a `Hardware` or `GUID` map represents a map whose elements are *directly* associated with the device. These maps are unique per device whereas a `Template` map represents a map whose elements are *translated* to a generic template that can be used by all devices.
>
> Hardware maps are great for showing sprites and descriptions that are special for that device while a generic map is great for compatibility across many devices.

> [!NOTE]
>
> Internally `Hardware Map` is a shortcut version of a `GUID Map`. Therefore if you want to represent a device that doesn't have a `Hardware Definition` you are not out luck since you could define it in a `GUID Map` instead. See the [Official Rewired Documentation](https://guavaman.com/projects/rewired/docs/HowTos.html#identifying-recognized-controllers) for a `.csv` file of all controllers and their GUID value.

### 4.3 Assign Template Maps



### 4.4 Assign Non-Input Glyph Values