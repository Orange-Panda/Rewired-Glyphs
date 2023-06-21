using JetBrains.Annotations;
using Rewired;
using System.Collections.Generic;

namespace LMirman.RewiredGlyphs
{
	[PublicAPI]
	public static class RewiredExtensions
	{
		private static readonly List<ActionElementMap> MapLookupResults = new List<ActionElementMap>();
		
		/// <summary>
		/// Find the first mapping that is for this controller and with the correct pole direction. Null if no such map exists.
		/// </summary>
		internal static ActionElementMap GetActionElementMap(this Player player, ControllerType controller, int actionID, Pole pole)
		{
			int count = player.controllers.maps.GetElementMapsWithAction(controller, actionID, false, MapLookupResults);
			for (int i = 0; i < count; i++)
			{
				if (MapLookupResults[i].axisContribution == pole)
				{
					return MapLookupResults[i];
				}
			}

			return null;
		}
	}
}