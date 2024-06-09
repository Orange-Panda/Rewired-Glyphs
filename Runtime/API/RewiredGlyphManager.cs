using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Responsible for initializing the <see cref="InputGlyphs"/> system and running <see cref="InputGlyphs"/> functionality through built in Unity functions.
	/// </summary>
	/// <remarks>
	/// You must add this component to your input manager for <see cref="InputGlyphs"/> system to fully function.
	/// </remarks>
	[AddComponentMenu("Rewired Glyphs/Rewired Glyph Manager")]
	public class RewiredGlyphManager : MonoBehaviour
	{
		[SerializeField, Tooltip("The default collection of glyphs to use in the Rewired Input Glyph system when initialized.")]
		private GlyphCollection glyphCollection;
		[SerializeField, Tooltip("Additional glyph collections to load on start which can be referenced via their collectionKey.")]
		private GlyphCollection[] additionalCollections = { };
		[SerializeField, Tooltip("The player ids to check input changes for rebuilding glyphs on hardware changes.")]
		private int[] playerIds = { 0 };
		[SerializeField, Tooltip("How frequently this manager will poll the device list all observed `playerIds` to check for hardware changes."), Range(0, 5)]
		private float hardwareChangePollingRate = 0.5f;

		private float timer;
		private readonly List<ObservedPlayer> observedPlayers = new List<ObservedPlayer>();

		private void Awake()
		{
			if (glyphCollection == null)
			{
				Debug.LogError($"No default GlyphCollection defined on \"{gameObject.name}\". Please add a Glyph Collection on the Rewired Glyph Manager.", gameObject);
			}
			else
			{
				InputGlyphs.LoadGlyphCollection(glyphCollection);
			}

			foreach (GlyphCollection collection in additionalCollections ?? Array.Empty<GlyphCollection>())
			{
				InputGlyphs.LoadGlyphCollection(collection, false);
			}
		}

		private void Start()
		{
			HashSet<int> playersAdded = new HashSet<int>();
			foreach (int playerId in playerIds)
			{
				if (!playersAdded.Add(playerId))
				{
					continue;
				}

				Player player = ReInput.players.GetPlayer(playerId);
				ObservedPlayer observedPlayer = new ObservedPlayer(player);
				observedPlayers.Add(observedPlayer);
			}
		}

		private void Update()
		{
			CheckForHardwareChanges();
			InputGlyphs.InvokeRebuild();
		}

		private void CheckForHardwareChanges()
		{
			if (timer > 0)
			{
				timer -= Time.deltaTime;
				return;
			}

			timer = hardwareChangePollingRate;
			foreach (ObservedPlayer observedPlayer in observedPlayers)
			{
				Controller lastActiveController = observedPlayer.player.controllers.GetMostRecentController();
				HardwareDefinition controllerType = InputGlyphs.GetHardwareDefinition(lastActiveController);
				bool isDifferentHardware = controllerType != observedPlayer.lastHardwareUsed;
				bool isHardwareSimilar = IsKeyboardMouse(observedPlayer.lastHardwareUsed) && IsKeyboardMouse(controllerType);
				if (isDifferentHardware && !isHardwareSimilar)
				{
					observedPlayer.lastHardwareUsed = controllerType;
					InputGlyphs.MarkGlyphsDirty();
				}
			}
		}

		private static bool IsKeyboardMouse(HardwareDefinition type)
		{
			return type is HardwareDefinition.Mouse or HardwareDefinition.Keyboard;
		}

		private class ObservedPlayer
		{
			public readonly Player player;
			public HardwareDefinition lastHardwareUsed;

			public ObservedPlayer(Player player)
			{
				this.player = player;
				lastHardwareUsed = HardwareDefinition.Unknown;
			}
		}
	}
}