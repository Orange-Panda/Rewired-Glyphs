using Rewired;
using System.Collections.Generic;
using UnityEngine;

namespace LMirman.RewiredGlyphs
{
	/// <summary>
	/// Responsible for initializing and running Input Glyph behaviors.
	/// </summary>
	/// <remarks>
	/// Add this component to your input manager for it to function.
	/// The Input Glyph system will not behave as expected without this present.
	/// </remarks>
	[AddComponentMenu("Rewired Glyphs/Rewired Glyph Manager")]
	public class RewiredGlyphManager : MonoBehaviour
	{
		[SerializeField, Tooltip("The collection of glyphs to use in the Rewired Input Glyph system.")]
		private GlyphCollection glyphCollection;
		[SerializeField, Tooltip("The player ids to check input changes for rebuilding glyphs on hardware changes.")]
		private int[] playerIds = { 0 };
		[SerializeField, Tooltip("How frequently the observer will poll players to check for hardware changes."), Range(0, 5)]
		private float hardwareChangePollingRate = 0.5f;

		private float timer;
		private readonly List<ObservedPlayer> observedPlayers = new List<ObservedPlayer>();

		private void Awake()
		{
			if (glyphCollection == null)
			{
				Debug.LogError("Please provide a Glyph Collection on the Input Glyph Observer.");
				return;
			}

			InputGlyphs.LoadGlyphCollection(glyphCollection);
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
			return type == HardwareDefinition.Mouse || type == HardwareDefinition.Keyboard;
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