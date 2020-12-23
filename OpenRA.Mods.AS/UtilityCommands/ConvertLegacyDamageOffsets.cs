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
	class ConvertLegacyDamageOffsets : IUtilityCommand
	{
		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		string IUtilityCommand.Name { get { return "--convert-damage-offsets"; } }

		IniFile rulesIni;
		IniFile artIni;
		MapGrid grid;

		[Desc("RULES.INI", "ART.INI", "Extract and converts legacy damage fire and smoke offsets from a TS/RA2 INI.")]
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

			technoTypes = rulesIni.GetSection("InfantryTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Infantry");
			Console.WriteLine();
			ImportValues(technoTypes);

			technoTypes = rulesIni.GetSection("VehicleTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Vehicles");
			Console.WriteLine();
			ImportValues(technoTypes);

			technoTypes = rulesIni.GetSection("AircraftTypes").Select(b => b.Value).Distinct();
			Console.WriteLine("# Aircraft");
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

				var results = rulesSection.Where(x => x.Key.StartsWith("DamageSmokeOffset"));
				foreach (var result in results)
				{
					if (!string.IsNullOrEmpty(result.Key))
					{
						var offsets = result.Value.Split(',');
						var x = int.Parse(offsets[0]);
						var y = int.Parse(offsets[1]);
						var z = int.Parse(offsets[2]);

						Console.WriteLine("\t" + result.Key + ": " + 4 * x + "," + 4 * y + "," + 4 * z);
					}
				}

				var artName = technoType.ToLowerInvariant();

				var image = rulesSection.GetValue("Image", string.Empty);
				if (!string.IsNullOrEmpty(image))
				{
					artName = image.ToLowerInvariant();
				}

				if (artIni.Sections.Any(s => s.Name == artName))
				{
					var artSection = artIni.GetSection(artName);

					int xOffset = 0, yOffset = 0;

					var foundation = artSection.GetValue("Foundation", string.Empty);
					if (!string.IsNullOrEmpty(foundation))
					{
						var size = foundation.Split('x');
						if (size.Length == 2)
						{
							var x = int.Parse(size[0]);
							var y = int.Parse(size[1]);

							xOffset = (x - y) * grid.TileSize.Width / 4;
							yOffset = (x + y) * grid.TileSize.Height / 4;
						}
					}

					var fireOffsets = artSection.Where(x => x.Key.StartsWith("DamageFireOffset"));

					foreach (var fireOffset in fireOffsets)
					{
						if (!string.IsNullOrEmpty(fireOffset.Key))
						{
							var offsets = fireOffset.Value.Split(',');
							var x = int.Parse(offsets[0]) - xOffset;
							var y = int.Parse(offsets[1]) - yOffset;

							Console.WriteLine("\t" + "DamageFireOffset: " + x + "," + y);
						}
					}
				}

				Console.WriteLine();
			}
		}
	}
}
