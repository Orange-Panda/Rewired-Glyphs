using JetBrains.Annotations;
using Rewired;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Responsible for initializing the <see cref="InputGlyphs"/> system and running <see cref="InputGlyphs"/> functionality through built-in Unity functions.
	/// </summary>
	/// <remarks>
	/// You must add this component to your input manager for <see cref="InputGlyphs"/> system to fully function.
	/// </remarks>
	[AddComponentMenu("Rewired Glyphs/Rewired Glyph Manager")]
	public class RewiredGlyphManager : MonoBehaviour
	{
		[Tooltip("The default collection of glyphs to use in the Rewired Input Glyph system when initialized.")]
		[SerializeField]
		private GlyphCollection glyphCollection;
		[Tooltip("Additional glyph collections to load on start which can be referenced via their collectionKey.")]
		[SerializeField]
		private GlyphCollection[] additionalCollections = { };
		[Tooltip("The player ids to check input changes for rebuilding glyphs on hardware changes.")]
		[SerializeField]
		private int[] playerIds = { 0 };
		[Tooltip("How frequently this manager will poll the device list all observed `playerIds` to check for hardware changes.")]
		[Range(0, 5)]
		[SerializeField]
		private float hardwareChangePollingRate = 0.5f;

		private float lastEvaluateTime;
		private readonly List<ObservedPlayer> observedPlayers = new List<ObservedPlayer>();

		private void Awake()
		{
			lastEvaluateTime = -hardwareChangePollingRate;
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
			float evaluationTimer = Mathf.Abs(Time.unscaledTime - lastEvaluateTime);
			bool hasRecentlyEvaluated = evaluationTimer < hardwareChangePollingRate;
			if (hasRecentlyEvaluated)
			{
				return;
			}

			lastEvaluateTime = Time.unscaledTime;
			foreach (ObservedPlayer observedPlayer in observedPlayers)
			{
				Controller recentController = observedPlayer.player.controllers.GetMostRecentController();
				bool isDifferentHardware = recentController != observedPlayer.lastController;
				bool isHardwareSimilar = IsKeyboardMouse(observedPlayer.lastController) && IsKeyboardMouse(recentController);
				if (isDifferentHardware && !isHardwareSimilar)
				{
					observedPlayer.lastController = recentController;
					InputGlyphs.MarkGlyphsDirty();
				}
			}
		}

		private static bool IsKeyboardMouse([CanBeNull] Controller controller)
		{
			return controller != null && controller.type.IsKeyboardOrMouse();
		}

		private class ObservedPlayer
		{
			public readonly Player player;
			public Controller lastController;

			public ObservedPlayer(Player player)
			{
				this.player = player;
				lastController = null;
			}
		}
	}
}