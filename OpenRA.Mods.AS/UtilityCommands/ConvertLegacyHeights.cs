#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Mods.Common.FileFormats;

namespace OpenRA.Mods.AS.UtilityCommands
{
	class ConvertLegacyHeights : IUtilityCommand
	{
		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		string IUtilityCommand.Name { get { return "--convert-heights"; } }

		IniFile rulesIni;
		IniFile artIni;
		MapGrid grid;

		[Desc("RULES.INI", "ART.INI", "Extract and converts legacy height values from a TS/RA2 INI.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = utility.ModData;

			grid = Game.ModData.Manifest.Get<MapGrid>();

			rulesIni = new IniFile(File.Open(args[1], FileMode.Open));
			artIni = new IniFile(File.Open(args[2], FileMode.Open));

			var technoTypes = rulesIni.GetSection("BuildingTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Buildings");
			Console.WriteLine();
			ImportValues(technoTypes);
		}

		void ImportValues(IEnumerable<string> technoTypes)
		{
			foreach (var technoType in technoTypes)
			{
				var rulesSection = rulesIni.GetSection(technoType, allowFail: true);
				if (rulesSection == null)
					continue;

				Console.WriteLine(rulesSection.Name + ":");
				var artName = technoType.ToLowerInvariant();

				var image = rulesSection.GetValue("Image", string.Empty);
				if (!string.IsNullOrEmpty(image))
				{
					artName = image.ToLowerInvariant();
				}

				if (artIni.Sections.Any(s => s.Name == artName))
				{
					var artSection = artIni.GetSection(artName);

					var heightString = artSection.GetValue("Height", string.Empty);
					if (!string.IsNullOrEmpty(heightString))
					{
						var height = int.Parse(heightString);
						height *= grid.TileSize.Height / 2;

						Console.WriteLine("\t" + "Height: " + height);
					}
				}

				Console.WriteLine();
			}
		}
	}
}
