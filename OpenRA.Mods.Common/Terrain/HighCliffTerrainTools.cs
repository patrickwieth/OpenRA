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

using System.Collections.Generic;

namespace OpenRA.Mods.Common.Terrain
{
	public static class HighCliffTerrainTools
	{
		static readonly CVec[] FillNeighbors =
		{
			new(1, 0),
			new(-1, 0),
			new(0, 1),
			new(0, -1),
		};

		static readonly CVec[] RetileNeighbors =
		{
			new(1, 0),
			new(-1, 0),
			new(0, 1),
			new(0, -1),
			new(1, 1),
			new(-1, -1),
			new(1, -1),
			new(-1, 1),
		};

		static readonly CVec ScreenNorth = new(-1, -1);
		static readonly CVec ScreenSouth = new(1, 1);
		static readonly CVec ScreenEast = new(1, -1);
		static readonly CVec ScreenWest = new(-1, 1);

		public static bool IsHeightToolTemplate(ushort templateId)
		{
			return templateId == HighCliffTileIds.RaiseSelectorTemplateId || templateId == HighCliffTileIds.LowerSelectorTemplateId;
		}

		public static HashSet<CPos> CollectConnectedHeightRegion(Map map, CPos seed)
		{
			var region = new HashSet<CPos>();
			if (!map.Height.Contains(seed))
				return region;

			var targetRaised = IsRaised(map.TerrainHeightStep(seed));
			var queue = new Queue<CPos>();
			queue.Enqueue(seed);
			region.Add(seed);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				foreach (var delta in FillNeighbors)
				{
					var neighbor = current + delta;
					if (!map.Height.Contains(neighbor) || region.Contains(neighbor))
						continue;

					if (IsRaised(map.TerrainHeightStep(neighbor)) != targetRaised)
						continue;

					region.Add(neighbor);
					queue.Enqueue(neighbor);
				}
			}

			return region;
		}

		public static HashSet<CPos> CollectMinimumPlateauRegion(Map map, CPos seed, int radius = 1)
		{
			var region = new HashSet<CPos>();
			for (var dy = -radius; dy <= radius; dy++)
				for (var dx = -radius; dx <= radius; dx++)
				{
					var cell = seed + new CVec(dx, dy);
					if (map.Height.Contains(cell))
						region.Add(cell);
				}

			return region;
		}

		public static HashSet<CPos> ExpandRetileNeighborhood(Map map, IEnumerable<CPos> cells)
		{
			var expanded = new HashSet<CPos>();
			foreach (var cell in cells)
			{
				if (map.Height.Contains(cell))
					expanded.Add(cell);

				foreach (var delta in RetileNeighbors)
				{
					var neighbor = cell + delta;
					if (map.Height.Contains(neighbor))
						expanded.Add(neighbor);
				}
			}

			return expanded;
		}

		public static HashSet<CPos> GetCellsWithTargetHeight(Map map, IEnumerable<CPos> cells, byte targetHeight)
		{
			var changed = new HashSet<CPos>();
			foreach (var cell in cells)
			{
				if (!map.Height.Contains(cell))
					continue;

				if (map.Height[cell] != targetHeight)
					changed.Add(cell);
			}

			return changed;
		}

		public static void RetileHeightRegion(Map map, IEnumerable<CPos> candidateCells, ushort clearTemplateId)
		{
			foreach (var cell in candidateCells)
			{
				if (!map.Tiles.Contains(cell))
					continue;

				var templateId = SelectTemplateId(map, cell);
				if (templateId.HasValue)
				{
					map.Tiles[cell] = new TerrainTile(templateId.Value, 0);
					continue;
				}

				if (HighCliffTileIds.IsTemplate(map.Tiles[cell].Type))
					map.Tiles[cell] = new TerrainTile(clearTemplateId, 0);
			}
		}

		static ushort? SelectTemplateId(Map map, CPos cell)
		{
			if (!map.Height.Contains(cell))
				return null;

			if (!IsRaised(map.TerrainHeightStep(cell)))
				return null;

			var northRaised = IsNeighborRaised(map, cell, ScreenNorth);
			var southRaised = IsNeighborRaised(map, cell, ScreenSouth);
			var eastRaised = IsNeighborRaised(map, cell, ScreenEast);
			var westRaised = IsNeighborRaised(map, cell, ScreenWest);

			ushort? result;

			if (!northRaised && !southRaised && !eastRaised && !westRaised)
				result = HighCliffTileIds.SelectSouthOuterTemplateId(cell);
			else if (!northRaised)
			{
				if (!westRaised && eastRaised)
					result = HighCliffTileIds.SelectNorthWestOuterTemplateId(cell);
				else if (!eastRaised && westRaised)
					result = HighCliffTileIds.SelectNorthEastOuterTemplateId(cell);
				else
					result = HighCliffTileIds.SelectNorthOuterTemplateId(cell);
			}
			else if (!southRaised)
			{
				var eastSouthRaised = IsNeighborRaised(map, cell + ScreenEast, ScreenSouth);
				var westSouthRaised = IsNeighborRaised(map, cell + ScreenWest, ScreenSouth);
				var eastNorthRaised = IsNeighborRaised(map, cell + ScreenEast, ScreenNorth);
				var westNorthRaised = IsNeighborRaised(map, cell + ScreenWest, ScreenNorth);
				var eastCell = cell + ScreenEast;
				var westCell = cell + ScreenWest;
				var eastLooksLikeBayMiddle = !IsNeighborRaised(map, eastCell, ScreenSouth)
					&& IsNeighborRaised(map, eastCell, ScreenEast)
					&& IsNeighborRaised(map, eastCell, ScreenWest)
					&& IsNeighborRaised(map, eastCell + ScreenEast, ScreenSouth)
					&& IsNeighborRaised(map, eastCell + ScreenWest, ScreenSouth)
					&& !IsNeighborRaised(map, eastCell + ScreenEast, ScreenNorth)
					&& !IsNeighborRaised(map, eastCell + ScreenWest, ScreenNorth);
				var westLooksLikeBayMiddle = !IsNeighborRaised(map, westCell, ScreenSouth)
					&& IsNeighborRaised(map, westCell, ScreenEast)
					&& IsNeighborRaised(map, westCell, ScreenWest)
					&& IsNeighborRaised(map, westCell + ScreenEast, ScreenSouth)
					&& IsNeighborRaised(map, westCell + ScreenWest, ScreenSouth)
					&& !IsNeighborRaised(map, westCell + ScreenEast, ScreenNorth)
					&& !IsNeighborRaised(map, westCell + ScreenWest, ScreenNorth);

				if (eastRaised && westRaised && eastSouthRaised && westSouthRaised && !eastNorthRaised && !westNorthRaised)
				{
					if (!eastLooksLikeBayMiddle && !westLooksLikeBayMiddle)
						result = HighCliffTileIds.SelectSouthInnerTemplateId(cell);
					else if (westLooksLikeBayMiddle && !eastLooksLikeBayMiddle)
						result = HighCliffTileIds.SelectSouthWestFaceTemplateId(cell);
					else if (eastLooksLikeBayMiddle && !westLooksLikeBayMiddle)
						result = HighCliffTileIds.SelectSouthEastFaceTemplateId(cell);
					else
						result = HighCliffTileIds.SelectSouthOuterTemplateId(cell);
				}
				else if (!westRaised && eastRaised)
					result = HighCliffTileIds.SelectSouthWestFaceTemplateId(cell);
				else if (!eastRaised && westRaised)
					result = HighCliffTileIds.SelectSouthEastFaceTemplateId(cell);
				else
					result = HighCliffTileIds.SelectSouthOuterTemplateId(cell);
			}
			else if (!westRaised && eastRaised)
				result = HighCliffTileIds.SelectSouthWestFaceTemplateId(cell);
			else if (!eastRaised && westRaised)
				result = HighCliffTileIds.SelectSouthEastFaceTemplateId(cell);
			else
				result = null;

			if (result != null)
				Log.Write("debug", $"highcliff cell={cell} n={northRaised} s={southRaised} e={eastRaised} w={westRaised} tile={result}");

			return result;
		}

		static bool IsNeighborRaised(Map map, CPos cell, CVec offset)
		{
			var neighbor = cell + offset;
			return map.Height.Contains(neighbor) && IsRaised(map.TerrainHeightStep(neighbor));
		}


		static bool IsRaised(byte terrainHeightStep)
		{
			return terrainHeightStep > 0;
		}
	}
}
