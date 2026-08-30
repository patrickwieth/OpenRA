#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class ListMapsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--list-maps";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 1;
		}

		[Desc("List system maps as tab-separated UID, player count, and title fields.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;
			utility.ModData.MapCache.LoadMaps();
			foreach (var map in utility.ModData.MapCache
				.Where(map => map.Class == MapClassification.System && map.PlayerCount >= 2)
				.OrderBy(map => map.Title, StringComparer.OrdinalIgnoreCase))
			{
				var title = map.Title.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
				Console.WriteLine($"{map.Uid}\t{map.PlayerCount}\t{title}");
			}
		}
	}
}
