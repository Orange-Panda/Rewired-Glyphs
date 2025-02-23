using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Static class containing methods and properties to conveniently get <see cref="Sprite"/> and <see cref="string"/> information about Rewired input actions through <see cref="Glyph"/> queries.
	/// </summary>
	/// <remarks>
	/// Requires the <see cref="RewiredGlyphManager"/> component to be added to your Rewired `Input Manager` for the <see cref="InputGlyphs"/> system to function properly.
	/// <br/><br/>
	/// <b>Common Query Functions</b>
	/// <br/><br/>
	/// - <see cref="GetCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/>: Get glyph to represent an action for the player's most recently used device<br/>
	/// - <see cref="GetSpecificCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,LMirman.RewiredGlyphs.SymbolPreference,bool,string)"/>: Get glyph to represent an action for the player's most recently used device with hardware specific symbols<br/>
	/// - <see cref="GetGlyph(Rewired.Player,Rewired.ControllerType,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/>: Get glyph to represent an action for the player for a specific type of controller<br/>
	/// - <see cref="GetGlyphSet"/>: Outputs a list of <b>all</b> glyphs for a particular action for a player across all controller types<br/>
	/// <br/>
	/// <b>Other Useful Functionality</b>
	/// <br/><br/>
	/// - <see cref="RebuildGlyphs"/>: Event that is invoked when the output of Glyph queries may have changed (such as a user switching controller)<br/>
	/// - <see cref="LoadGlyphCollection"/>: Used for loading additional glyph collections and changing the default glyph collection<br/>
	/// - <see cref="SetGlyphPreferences"/>: Set the preferred symbols to show for Joystick controllers and preferred type of controller to show glyphs for<br/>
	/// - <see cref="MarkGlyphsDirty"/>: Inform InputGlyphs that it should dispatch the <see cref="RebuildGlyphs"/> event next <see cref="RewiredGlyphManager"/> update.
	/// Particularly important to use when Rewired data may have changed externally such as remapping controls<br/>
	/// </remarks>
	/// <seealso cref="GetCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/>
	/// <seealso cref="GetSpecificCurrentGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,LMirman.RewiredGlyphs.SymbolPreference,bool,string)"/>
	/// <seealso cref="GetGlyph(Rewired.Player,Rewired.ControllerType,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/>
	/// <seealso cref="GetGlyphSet"/>
	/// <seealso cref="RebuildGlyphs"/>
	/// <seealso cref="LoadGlyphCollection"/>
	/// <seealso cref="SetGlyphPreferences"/>
	/// <seealso cref="MarkGlyphsDirty"/>
	[PublicAPI]
	public static partial class InputGlyphs
	{
		internal static bool glyphsDirty;
		private static List<(ActionElementMap, AxisRange)> elementQueryOutput = new List<(ActionElementMap, AxisRange)>();

		/// <summary>
		/// The type Guid of the Controller Template.
		/// </summary>
		private static readonly Guid GamepadTemplateGuid = new Guid("83b427e4-086f-47f3-bb06-be266abd1ca5");
		private static readonly IList<ControllerTemplateElementTarget> TemplateTargets = new List<ControllerTemplateElementTarget>(2);
		/// <summary>
		/// Cache lookup for players
		/// </summary>
		private static readonly Dictionary<int, Player> Players = new Dictionary<int, Player>();
		/// <summary>
		/// Dictionary of <i><b>all</b></i> glyph collections mapped to their runtime counterpart.
		/// Includes glyph collections that may have conflicting keys.
		/// </summary>
		private static readonly Dictionary<GlyphCollection, RuntimeGlyphCollection> AllCollections = new Dictionary<GlyphCollection, RuntimeGlyphCollection>();
		/// <summary>
		/// Glyph collections that are able to be referenced by a string key.
		/// </summary>
		/// <remarks>
		/// Unlike <see cref="AllCollections"/> may only contain one collection per key at a time.
		/// </remarks>
		private static readonly Dictionary<string, RuntimeGlyphCollection> Collections = new Dictionary<string, RuntimeGlyphCollection>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// The glyph collection to used by default (due to invalid <see cref="Collections"/> lookup or no specifier provided).
		/// </summary>
		private static RuntimeGlyphCollection defaultCollection;

		/// <summary>
		/// Glyph representing an action that is invalid.
		/// Most commonly occurs due an invalid action name or action id.
		/// </summary>
		/// <remarks>
		/// Remedied by correcting the glyph display's target value
		/// </remarks>
		public static Glyph NullGlyph { get; private set; } = new Glyph("Null", controllerType: null, type: Glyph.Type.Null);

		/// <summary>
		/// Glyph representing an action that does not have an input assigned.
		/// </summary>
		/// <remarks>
		/// Remedied by assigning an input to the action either in the Rewired Input Manager at editor time or a control remapping tool at runtime.
		/// </remarks>
		public static Glyph UnboundGlyph { get; private set; } = new Glyph("Unbound", controllerType: null, type: Glyph.Type.Unbound);

		/// <summary>
		/// Glyph representing a query that occured before the InputGlyphs system was ready.
		/// </summary>
		/// <remarks>
		/// Remedied by ensuring the presence of a <see cref="RewiredGlyphManager"/> component on the Rewired Input Manager prefab.<br/><br/>
		/// The rewired glyph system is initialized in Awake.
		/// Depending on script execution order your display may query before the glyph system is ready.
		/// The safest approach is to query in OnEnable or Start.
		/// </remarks>
		public static Glyph UninitializedGlyph { get; private set; } = new Glyph("Uninitialized", controllerType: null, type: Glyph.Type.Uninitialized);

		/// <summary>
		/// The preferred hardware (Controller or Mouse/Keyboard) to display in <see cref="GetCurrentGlyph(int,Rewired.Pole,out Rewired.AxisRange,int,bool, string)"/>
		/// </summary>
		/// <seealso cref="SetGlyphPreferences"/>
		public static HardwarePreference PreferredHardware { get; private set; } = HardwarePreference.Auto;

		/// <summary>
		/// The preferred symbols to display for gamepad glyphs.
		/// </summary>
		/// <seealso cref="SetGlyphPreferences"/>
		public static SymbolPreference PreferredSymbols { get; private set; } = SymbolPreference.Auto;

		private static bool CanRetrieveGlyph => Application.isPlaying && ReInput.isReady;

		/// <summary>
		/// An event that is invoked when the output of the input glyph may have changed.
		/// This occurs usually when preferences are changed or the most recently used device by a player has changed.
		/// </summary>
		/// <remarks>
		/// The aforementioned preferences can mutate the output of glyph symbol queries.
		/// As such this event gives the opportunity for others to update the glyph output without having to query the InputGlyphs system every frame.
		/// </remarks>
		public static event Action RebuildGlyphs = delegate { };

		static InputGlyphs()
		{
			defaultCollection = RuntimeGlyphCollection.Default;
			ReInput.InitializedEvent += ReInputOnInitializedEvent;
			ReInput.ShutDownEvent += ReInputOnShutDownEvent;
		}

		/// <summary>
		/// Additively load a <see cref="GlyphCollection"/> into runtime memory on InputGlyphs, allowing it to be utilized by Glyph queries.
		/// <br/><br/>
		/// Provides the <see cref="setAsDefault"/> parameter to determine if this glyph should be used as the primary (default) glyph collection for queries.
		/// </summary>
		/// <example>
		/// You may consider creating and loading alternative glyph collections for light/dark themes, in-game teams, enviornments, or factions.
		/// </example>
		/// <remarks>
		/// <see cref="RewiredGlyphManager"/> invokes this method on Awake to set its <see cref="RewiredGlyphManager.glyphCollection"/> as the default when initializing.
		/// From that point onward you can use this method to add additional collections and change the default collection.
		/// <br/><br/>
		/// This method can safely be used even if a collection has already been loaded into memory, primarily for changing the default collection (with <see cref="setAsDefault"/> true).
		/// </remarks>
		/// <param name="collection">The collection to load into memory.</param>
		/// <param name="setAsDefault">When true set this collection as the default collection, using it for all queries that do not specify another collection</param>
		public static void LoadGlyphCollection(GlyphCollection collection, bool setAsDefault = true)
		{
			// Disregard preference if there is no default collection at all.
			bool didMutate = false;
			if (defaultCollection == null)
			{
				setAsDefault = true;
			}

			// ----- Find or create a RuntimeGlyphCollection -----
			bool hasRuntime = AllCollections.TryGetValue(collection, out RuntimeGlyphCollection runtimeCollection);
			if (!hasRuntime)
			{
				runtimeCollection = new RuntimeGlyphCollection(collection);
				AllCollections.Add(collection, runtimeCollection);
			}

			// ----- Determine if we are going to modify value, so we can dispatch update later. -----
			bool hasLookup = Collections.TryGetValue(collection.Key, out RuntimeGlyphCollection lookupCollection);
			if (hasLookup && lookupCollection != runtimeCollection)
			{
#if UNITY_EDITOR
				Debug.LogWarning($"Multiple glyph collections have an identical key: \"{runtimeCollection.collection.Key}\"!\n" +
				                 "Each collection's key should be a unique value otherwise they will conflict at runtime.");
#endif
				didMutate = true;
			}
			else if (!hasLookup)
			{
				didMutate = true;
			}

			// ----- Assign runtime collection to lookup table -----
			Collections[runtimeCollection.collection.Key] = runtimeCollection;

			// ----- Set this as default if parameter says so
			if (setAsDefault && runtimeCollection != defaultCollection)
			{
				didMutate = true;
				defaultCollection = runtimeCollection;
				UninitializedGlyph = runtimeCollection.uninitializedGlyph;
				UnboundGlyph = runtimeCollection.unboundGlyph;
				NullGlyph = runtimeCollection.nullGlyph;
			}

			// ----- Dispatch update next frame if the context changed -----
			if (didMutate)
			{
				MarkGlyphsDirty();
			}
		}

		/// <summary>
		/// Set the user preference for glyph display.
		/// </summary>
		/// <param name="hardwarePreference">The preferred hardware to show symbols for</param>
		/// <param name="symbolPreference">The preferred type of symbols to display</param>
		public static void SetGlyphPreferences(HardwarePreference hardwarePreference, SymbolPreference symbolPreference)
		{
			PreferredHardware = hardwarePreference;
			PreferredSymbols = symbolPreference;
			MarkGlyphsDirty();
		}

		#region Core
		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool, string)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the mouse or keyboard devices (in that order).
		/// </summary>
		/// <remarks>
		/// If an input is present on both the keyboard and mouse, precedence is given to the mouse.
		/// </remarks>
		public static Glyph GetKeyboardMouseGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false, [CanBeNull] string collectionKey = null)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap mouseMap = player.GetActionElementMap(ControllerType.Mouse, action.id, pole, forceAxis, out AxisRange expectedMouse);
			ActionElementMap keyboardMap = player.GetActionElementMap(ControllerType.Keyboard, action.id, pole, forceAxis, out AxisRange expectedKeyboard);
			if (mouseMap != null)
			{
				axisRange = expectedMouse;
				Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Mouse, mouseMap.elementIdentifierId, collectionKey);
				return glyph ?? GetGlyphCollection(collectionKey).GetFallbackGlyph(mouseMap.elementIdentifierName, ControllerType.Mouse);
			}

			if (keyboardMap != null)
			{
				axisRange = expectedKeyboard;
				Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Keyboard, keyboardMap.elementIdentifierId, collectionKey);
				return glyph ?? GetGlyphCollection(collectionKey).GetFallbackGlyph(keyboardMap.elementIdentifierName, ControllerType.Keyboard);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool, string)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the Keyboard device.
		/// </summary>
		/// <remarks>
		/// Does <b>not</b> show glyphs for the mouse.
		/// Use <see cref="GetKeyboardMouseGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/> if you want to show keyboard or mouse glyph.
		/// </remarks>
		public static Glyph GetKeyboardGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false, [CanBeNull] string collectionKey = null)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap keyboardMap = player.GetActionElementMap(ControllerType.Keyboard, action.id, pole, forceAxis, out AxisRange expectedAxis);
			if (keyboardMap == null)
			{
				return UnboundGlyph;
			}

			axisRange = expectedAxis;
			Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Keyboard, keyboardMap.elementIdentifierId, collectionKey);
			return glyph ?? GetGlyphCollection(collectionKey).GetFallbackGlyph(keyboardMap.elementIdentifierName, ControllerType.Keyboard);
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool, string)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the `Mouse` device.
		/// </summary>
		/// <remarks>
		/// Does <b>not</b> show glyphs for the keyboard.
		/// Use <see cref="GetKeyboardMouseGlyph(Rewired.Player,int,Rewired.Pole,out Rewired.AxisRange,bool,string)"/> if you want to show keyboard or mouse glyph.
		/// </remarks>
		public static Glyph GetMouseGlyph(this Player player, int actionID, Pole pole, out AxisRange axisRange, bool forceAxis = false, [CanBeNull] string collectionKey = null)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			InputAction action = ReInput.mapping.GetAction(actionID);
			if (action == null)
			{
				return NullGlyph;
			}

			ActionElementMap mouseMap = player.GetActionElementMap(ControllerType.Mouse, action.id, pole, forceAxis, out AxisRange expectedAxis);
			if (mouseMap != null)
			{
				axisRange = expectedAxis;
				Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Mouse, mouseMap.elementIdentifierId, collectionKey);
				return glyph ?? GetGlyphCollection(collectionKey).GetFallbackGlyph(mouseMap.elementIdentifierName, ControllerType.Mouse);
			}

			return UnboundGlyph;
		}

		/// <inheritdoc cref="InputGlyphs.GetCurrentGlyph(Player, int, Pole, out AxisRange, bool, string)"/>
		/// <summary>
		/// Get a <see cref="Glyph"/> to represent an action for the user for the `Joystick` device with a specific <see cref="SymbolPreference"/> .
		/// </summary>
		/// <remarks>
		/// This method should only be used if you want to explicitly determine the type of symbol (Xbox, Playstation, etc.) to show for the joystick glyph.<br/>
		/// Use <see cref="GetJoystickGlyph(Rewired.Player,int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,bool, string)"/> if you don't need to specify symbol preference.
		/// </remarks>
		/// <seealso cref="GetJoystickGlyph(Rewired.Player,int,Rewired.Controller,Rewired.Pole,out Rewired.AxisRange,bool, string)"/>
		[NotNull]
		public static Glyph GetSpecificJoystickGlyph(this Player player, int actionID, Controller controller, Pole pole, out AxisRange axisRange, SymbolPreference symbolPreference,
			bool forceAxis = false, [CanBeNull] string collectionKey = null)
		{
			axisRange = AxisRange.Full;
			if (!CanRetrieveGlyph)
			{
				return UninitializedGlyph;
			}

			// Initialize variables
			InputAction action = ReInput.mapping.GetAction(actionID);

			// Make sure the action expected is valid, escape with null glyph if invalid action given by developer.
			if (action == null)
			{
				return NullGlyph;
			}

			// -- Special case: Null controller provided, use default controller values.
			ActionElementMap map;
			AxisRange expectedAxis;
			if (controller == null)
			{
				map = player.GetActionElementMap(ControllerType.Joystick, action.id, pole, forceAxis, out expectedAxis);
				// Make sure the action is actually bound, if not escape with an unbound glyph.
				if (map == null)
				{
					return UnboundGlyph;
				}

				axisRange = expectedAxis;
				return GetJoystickGlyphFromElementMap(player, null, map, symbolPreference, collectionKey) ??
				       GetGlyphCollection(collectionKey).GetFallbackGlyph(map.elementIdentifierName, ControllerType.Joystick);
			}

			// Make sure the action is actually bound, if not escape with an unbound glyph.
			map = player.GetActionElementMap(controller, action.id, pole, forceAxis, out expectedAxis);
			if (map == null)
			{
				return UnboundGlyph;
			}

			axisRange = expectedAxis;
			return GetJoystickGlyphFromElementMap(player, controller, map, symbolPreference, collectionKey) ??
			       GetGlyphCollection(collectionKey).GetFallbackGlyph(map.elementIdentifierName, ControllerType.Joystick);
		}

		/// <summary>
		/// Get a list of <b>all</b> glyphs that could represent a particular action.
		/// </summary>
		/// <remarks>
		/// By design does not filter by controller type since you may decide to filter the <paramref name="output"/> list yourself using the glyph <see cref="Glyph.ControllerType"/>.
		/// <br/><br/>
		/// The results list will <b>not</b> include any <see cref="NullGlyph"/>, <see cref="UninitializedGlyph"/>, or <see cref="UnboundGlyph"/>.
		/// It is <i>your responsibility</i> to decide whether you'd like to show these glyphs depending on the returned <see cref="GlyphSetQueryResult"/> or output list values.
		/// </remarks>
		/// <example>
		/// Primitive example of how to call this method.
		/// <code>
		/// // Defining the list outside of the method is highly encouraged so we don't generate garbage memory every method call
		/// private List&lt;(Glyph, AxisRange)&gt; output = new List&lt;(Glyph, AxisRange)&gt;();
		///
		/// <br/>
		///
		/// private void SetGlyphs()
		/// {
		/// 		// !!! Specify `playerID`, `actionID`, and `pole` based on what you want to represent. !!!
		/// 		Player player = ReInput.players.GetPlayer(playerID);
		/// 		player.GetGlyphSet(actionID, pole, output);
		/// 		foreach ((Glyph, AxisRange) result in output)
		/// 		{
		/// 			// !!! Your functionality here. Use result.Item1 and glyph.Item2. !!!
		/// 		}
		/// }
		/// </code>
		/// </example>
		/// <param name="player">The player to check the input map of</param>
		/// <param name="actionID">The identifier of the action to represent</param>
		/// <param name="pole">
		/// The direction of the action to represent.
		/// <br/><br/>
		/// Usually positive unless it is an axis action such as Move Left in a Move Horizontal action.
		/// </param>
		/// <param name="output">
		/// A list storing the output results of this function that is defined by the calling type.
		/// The list is cleared when this method is called and will contain the results after the method executes.
		/// Must <b>not</b> be a null value.
		/// </param>
		/// <param name="forceAxis">
		/// Usually the Input Glyph system, when <paramref name="forceAxis"/> is false, only checks for single axis inputs such that it will only look for "Move Left" with <see cref="Pole.Negative"/> and "Move Right" with <see cref="Pole.Positive"/> and thus never checking "Move Horizontal" as an axis itself.
		/// <br/><br/>
		/// When true, explicitly request for the Input Glyph system to evaluate this glyph as the full axis ("Move Horizontal" in the example case) so we can represent it properly for axis inputs.
		/// <br/><br/>
		/// TL;DR: For axis actions: True represents the axis itself "Move Horizontal". False represents negative pole "Move Left" and positive pole "Move Right". In most cases should be false.
		/// </param>
		/// <param name="joystickSymbols">
		/// Determines the symbol type used to represent joystick glyphs.<br/><br/>
		/// When null (default): Use the <see cref="PreferredSymbols"/> for representing joystick glyphs.
		/// </param>
		/// <param name="collectionKey">Optionally used for getting glyphs from a non-default glyph collection. Should match the value of <see cref="GlyphCollection.Key"/></param>
		/// <returns>The resulting <see cref="GlyphSetQueryResult"/> informing the caller of if the query was successful or why it was unsuccessful if it wasn't</returns>
		public static GlyphSetQueryResult GetGlyphSet(this Player player, int actionID, Pole pole, [NotNull] List<(Glyph, AxisRange)> output, bool forceAxis = false,
			SymbolPreference? joystickSymbols = null, [CanBeNull] string collectionKey = null)
		{
			output.Clear();
			if (!CanRetrieveGlyph)
			{
				return GlyphSetQueryResult.ErrorUninitialized;
			}

			// --- Get all joystick glyphs ---
			if (player.GetAllActionElementMaps(ControllerType.Joystick, actionID, pole, forceAxis, elementQueryOutput) == GlyphSetQueryResult.ErrorUnknownAction)
			{
				// If the joystick outputs unknown action we can just bail now since we will get the same result from other controller types.
				return GlyphSetQueryResult.ErrorUnknownAction;
			}

			Controller last = player.controllers.GetMostRecentController(ControllerType.Joystick);
			SymbolPreference symbolPreference = joystickSymbols ?? PreferredSymbols;
			foreach ((ActionElementMap, AxisRange) element in elementQueryOutput)
			{
				Glyph glyph = GetJoystickGlyphFromElementMap(player, last, element.Item1, symbolPreference, collectionKey);
				if (glyph != null)
				{
					output.Add((glyph, element.Item2));
				}
			}

			// --- Get all keyboard glyphs ---
			player.GetAllActionElementMaps(ControllerType.Keyboard, actionID, pole, forceAxis, elementQueryOutput);
			foreach ((ActionElementMap, AxisRange) element in elementQueryOutput)
			{
				Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Keyboard, element.Item1.elementIdentifierId, collectionKey);
				if (glyph != null)
				{
					output.Add((glyph, element.Item2));
				}
			}

			// --- Get all mouse glyphs ---
			player.GetAllActionElementMaps(ControllerType.Mouse, actionID, pole, forceAxis, elementQueryOutput);
			foreach ((ActionElementMap, AxisRange) element in elementQueryOutput)
			{
				Glyph glyph = GetNativeGlyphFromGuidMap(ReInput.controllers.Mouse, element.Item1.elementIdentifierId, collectionKey);
				if (glyph != null)
				{
					output.Add((glyph, element.Item2));
				}
			}

			return GlyphSetQueryResult.Success;
		}
		#endregion

		/// <summary>
		/// Clear out the player cache of the input glyph system.
		/// </summary>
		/// <remarks>
		/// Invoking this method is rarely necessary but might be required in cases where the player list indices change.
		/// </remarks>
		public static void FlushPlayersCache()
		{
			Players.Clear();
		}

		/// <summary>
		/// Notify the glyph system that input state has changed and glyphs should be reevaluated.
		/// </summary>
		/// <remarks>
		/// For performance reasons the input glyph system only evaluates glyphs when it believes the output has changed such as a player using a different input device.
		/// By default, however, the InputGlyph system is unaware of changes such as control remapping and needs to be manually informed of such changes using this method.
		/// </remarks>
		public static void MarkGlyphsDirty()
		{
			glyphsDirty = true;
		}

		/// <summary>
		/// Will invoke <see cref="RebuildGlyphs"/> event when glyphs are dirty or <see cref="forceRebuild"/> parameter is true.
		/// </summary>
		/// <remarks>
		/// You usually shouldn't need to invoke this method.
		/// Consider using <see cref="MarkGlyphsDirty"/> instead if you want to inform the system the glyphs may have changed.
		/// </remarks>
		/// <param name="forceRebuild">When true will rebuild regardless of the value of <see cref="glyphsDirty"/>. When false will only rebuild if glyphs are dirty.</param>
		public static void InvokeRebuild(bool forceRebuild = false)
		{
			if (!glyphsDirty && !forceRebuild)
			{
				return;
			}

			foreach (Delegate evaluateDelegate in RebuildGlyphs.GetInvocationList())
			{
				try
				{
					evaluateDelegate.DynamicInvoke();
				}
				catch (Exception e)
				{
#if UNITY_EDITOR
					Debug.LogException(e);
#endif
				}
			}

			glyphsDirty = false;
		}

		#region Public Unsafe
		/// <inheritdoc cref="RuntimeGlyphCollection.GetNativeGlyphFromGuidMap(ControllerType, Guid, int)"/>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromGuidMap(ControllerType controllerType, Guid hardwareGuid, int elementID, [CanBeNull] string collectionKey = null)
		{
			return GetGlyphCollection(collectionKey).GetNativeGlyphFromGuidMap(controllerType, hardwareGuid, elementID);
		}

		/// <inheritdoc cref="RuntimeGlyphCollection.GetNativeGlyphFromGuidMap(ControllerType, Guid, int)"/>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromGuidMap([NotNull] Controller controller, int elementID, [CanBeNull] string collectionKey = null)
		{
			return GetGlyphCollection(collectionKey).GetNativeGlyphFromGuidMap(controller, elementID);
		}

		/// <inheritdoc cref="RuntimeGlyphCollection.GetNativeGlyphFromTemplateMap"/>
		[CanBeNull]
		public static Glyph GetNativeGlyphFromTemplateMap(SymbolPreference symbolPreference, int templateElementID, [CanBeNull] string collectionKey = null)
		{
			return GetGlyphCollection(collectionKey).GetNativeGlyphFromTemplateMap(symbolPreference, templateElementID);
		}
		#endregion

		#region Internal Use
		[NotNull]
		private static RuntimeGlyphCollection GetGlyphCollection([CanBeNull] string collectionKey = null)
		{
			return collectionKey != null && Collections.TryGetValue(collectionKey.ToCleansedCollectionKey(), out RuntimeGlyphCollection collection)
				? collection
				: defaultCollection ?? RuntimeGlyphCollection.Default;
		}

		private static void ReInputOnInitializedEvent()
		{
			FlushPlayersCache();
			glyphsDirty = true;
		}

		private static void ReInputOnShutDownEvent()
		{
			FlushPlayersCache();
		}

		internal static Controller GetMostRecentController(this Player.ControllerHelper controllers)
		{
			Controller last = controllers.GetLastActiveController();
			if (last != null)
			{
				return last;
			}

			ReInput.ControllerHelper allControllers = ReInput.controllers;
			last = allControllers.GetLastActiveController();
			if (last != null)
			{
				return last;
			}

			if (allControllers.joystickCount > 0)
			{
				return allControllers.Joysticks[0];
			}

			return allControllers.controllerCount > 0 ? allControllers.Controllers[0] : null;
		}

		internal static Controller GetMostRecentController(this Player.ControllerHelper controllers, ControllerType controllerType)
		{
			Controller last = controllers.GetLastActiveController(controllerType);
			if (last != null)
			{
				return last;
			}

			ReInput.ControllerHelper allControllers = ReInput.controllers;
			last = allControllers.GetLastActiveController(controllerType);
			if (last != null)
			{
				return last;
			}

			switch (controllerType)
			{
				case ControllerType.Keyboard:
					return allControllers.Keyboard;
				case ControllerType.Mouse:
					return allControllers.Mouse;
				case ControllerType.Joystick:
					return allControllers.joystickCount > 0 ? allControllers.Joysticks[0] : null;
				case ControllerType.Custom:
					return allControllers.customControllerCount > 0 ? allControllers.CustomControllers[0] : null;
				default:
					throw new ArgumentOutOfRangeException(nameof(controllerType), controllerType, null);
			}
		}

		[CanBeNull]
		private static Glyph GetJoystickGlyphFromElementMap(Player player, [CanBeNull] Controller controller, ActionElementMap map, SymbolPreference symbolPreference, [CanBeNull] string collectionKey = null)
		{
			// Try to retrieve a glyph that is specific to the user's controller hardware.
			if (controller != null && symbolPreference == SymbolPreference.Auto)
			{
				Glyph glyph = GetNativeGlyphFromGuidMap(controller, map.elementIdentifierId, collectionKey);
				if (glyph != null)
				{
					return glyph;
				}
			}

			// Try to retrieve a glyph that is mapped to the gamepad template (since at this point one was not found for the user's controller)
			// Determine the element expected on the template
			controller = player.controllers.GetFirstControllerWithTemplate(GamepadTemplateGuid);
			int templateElementId;
			if (controller != null)
			{
				IControllerTemplate template = controller.GetTemplate(GamepadTemplateGuid);
				int targets = template.GetElementTargets(map, TemplateTargets);
				templateElementId = targets > 0 ? TemplateTargets[0].element.id : -1;
			}
			else
			{
				templateElementId = -1;
			}

			// Use the template glyph if one exists.
			return GetNativeGlyphFromTemplateMap(symbolPreference, templateElementId, collectionKey);
		}

		/// <summary>
		/// Retrieve a cached player reference from <see cref="ReInput"/>.
		/// </summary>
		private static bool TryGetPlayer(int index, out Player player)
		{
			if (!CanRetrieveGlyph)
			{
				player = null;
				return false;
			}

			if (Players.TryGetValue(index, out player) && player != null)
			{
				return true;
			}

			player = ReInput.players.GetPlayer(index);
			if (player == null)
			{
				return false;
			}

			Players.Add(index, player);
			return true;
		}

		private static readonly List<ActionElementMap> MapLookupResults = new List<ActionElementMap>();

		/// <summary>
		/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
		/// </summary>
		private static ActionElementMap GetActionElementMap(this Player player, ControllerType controller, int actionID, Pole pole, bool getAsAxis, out AxisRange expectedAxis,
			int controllerId = 0)
		{
			InputAction inputAction = ReInput.mapping.GetAction(actionID);
			if (inputAction == null)
			{
				expectedAxis = AxisRange.Full;
				return null;
			}

			bool actionIsAxis = inputAction.type == InputActionType.Axis;
			int count = player.controllers.maps.GetElementMapsWithAction(controller, controllerId, actionID, false, MapLookupResults);
			for (int i = 0; i < count; i++)
			{
				ActionElementMap elementMap = MapLookupResults[i];
				switch (getAsAxis)
				{
					// Pick this when the element is an axis and we want to get an axis element.
					case true when actionIsAxis && elementMap.axisType == AxisType.Normal:
						expectedAxis = elementMap.axisRange;
						return elementMap;
					// Pick this when the element is an axis, but we want to get a specific axis range for it.
					case false when actionIsAxis && (elementMap.axisType != AxisType.None || elementMap.axisContribution == pole):
						expectedAxis = pole == Pole.Positive ? AxisRange.Positive : AxisRange.Negative;
						return elementMap;
					// Pick this when the action is a button and it has the expected axis contribution value
					case false when !actionIsAxis && elementMap.axisContribution == pole:
						expectedAxis = elementMap.axisRange;
						return elementMap;
				}
			}

			expectedAxis = AxisRange.Full;
			return null;
		}

		/// <summary>
		/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
		/// </summary>
		private static ActionElementMap GetActionElementMap(this Player player, [NotNull] Controller controller, int actionID, Pole pole, bool getAsAxis, out AxisRange expectedAxis)
		{
			return player.GetActionElementMap(controller.type, actionID, pole, getAsAxis, out expectedAxis, controller.id);
		}

		/// <summary>
		/// Get all mappings (if any) for this controller that has the correct polarity.
		/// </summary>
		/// <remarks>
		/// Stores its output in the <paramref name="output"/> list.
		/// This list must not be null and is to be created somewhere by the calling type.
		/// It will be cleared and populated with the output of this method, if any.
		/// <br/><br/>
		/// If the action is not valid or there are no mappings available for the action the method will still succeed.
		/// In such a case, the output list will be empty.
		/// </remarks>
		private static GlyphSetQueryResult GetAllActionElementMaps(this Player player, ControllerType controller, int actionID, Pole pole, bool getAsAxis,
			[NotNull] List<(ActionElementMap, AxisRange)> output)
		{
			output.Clear();
			InputAction inputAction = ReInput.mapping.GetAction(actionID);
			if (inputAction == null)
			{
				return GlyphSetQueryResult.ErrorUnknownAction;
			}

			bool actionIsAxis = inputAction.type == InputActionType.Axis;
			int count = player.controllers.maps.GetElementMapsWithAction(controller, actionID, false, MapLookupResults);
			for (int i = 0; i < count; i++)
			{
				ActionElementMap elementMap = MapLookupResults[i];
				switch (getAsAxis)
				{
					// Pick this when the element is an axis and we want to get an axis element.
					case true when actionIsAxis && elementMap.axisType == AxisType.Normal:
						output.Add((elementMap, elementMap.axisRange));
						break;
					// Pick this when the element is an axis, but we want to get a specific axis range for it.
					case false when actionIsAxis && (elementMap.axisType != AxisType.None || elementMap.axisContribution == pole):
						output.Add((elementMap, pole == Pole.Positive ? AxisRange.Positive : AxisRange.Negative));
						break;
					// Pick this when the action is a button and it has the expected axis contribution value
					case false when !actionIsAxis && elementMap.axisContribution == pole:
						output.Add((elementMap, elementMap.axisRange));
						break;
				}
			}

			return GlyphSetQueryResult.Success;
		}

		private static readonly Regex NonAlphaNumericRegex = new Regex("[^a-zA-Z0-9_-]");

		[Pure]
		internal static string ToCleansedCollectionKey(this string rawString)
		{
			return rawString == null ? string.Empty : NonAlphaNumericRegex.Replace(rawString.ToLowerInvariant(), string.Empty);
		}

		internal static bool IsKeyboardOrMouse(this ControllerType? controllerType)
		{
			return controllerType != null && (controllerType == ControllerType.Keyboard || controllerType == ControllerType.Mouse);
		}

		internal static bool IsKeyboardOrMouse(this ControllerType controllerType)
		{
			return controllerType == ControllerType.Keyboard || controllerType == ControllerType.Mouse;
		}
		#endregion
	}
}