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

using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	public sealed class HighCliffTerrainToolsTest
	{
		const ushort ClearTemplateId = 1;
		const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		[TestCase(TestName = "Connected height region treats all positive heights as the same plateau")]
		public void CollectConnectedHeightRegionTreatsPositiveHeightsAsOneLevel()
		{
			var map = CreateIsometricMap();
			map.Height[new CPos(10, 5)] = 2;
			map.Height[new CPos(11, 5)] = 1;
			map.Height[new CPos(10, 6)] = 3;
			map.Height[new CPos(12, 5)] = 0;

			var region = HighCliffTerrainTools.CollectConnectedHeightRegion(map, new CPos(10, 5));

			Assert.That(region.SetEquals(new[] { new CPos(10, 5), new CPos(11, 5), new CPos(10, 6) }), Is.True);
		}

		[TestCase(TestName = "Minimum plateau region expands a single click to 3x3")]
		public void CollectMinimumPlateauRegionExpandsSingleClickToThreeByThree()
		{
			var map = CreateIsometricMap();

			var region = HighCliffTerrainTools.CollectMinimumPlateauRegion(map, new CPos(10, 5));

			Assert.That(region.Count, Is.EqualTo(9));
			Assert.That(region.Contains(new CPos(9, 4)), Is.True);
			Assert.That(region.Contains(new CPos(10, 5)), Is.True);
			Assert.That(region.Contains(new CPos(11, 6)), Is.True);
		}


		[TestCase(TestName = "Retiler places north outer cliff on north edge")]
		public void RetilerPlacesNorthOuterCliff()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, 1)] = 1;
			map.Height[cell + new CVec(1, -1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsNorthOuterTemplate(map.Tiles[cell].Type), Is.True);
		}

		[TestCase(TestName = "Retiler places north west outer cliff on exposed north west corner")]
		public void RetilerPlacesNorthWestOuterCliff()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(1, -1)] = 1;
			map.Height[cell + new CVec(1, 1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsNorthWestOuterTemplate(map.Tiles[cell].Type), Is.True);
		}

		[TestCase(TestName = "Retiler places north east outer cliff on exposed north east corner")]
		public void RetilerPlacesNorthEastOuterCliff()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, 1)] = 1;
			map.Height[cell + new CVec(1, 1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsNorthEastOuterTemplate(map.Tiles[cell].Type), Is.True);
		}

		[TestCase(TestName = "Retiler places north outer cliff on exposed north edge between raised sides")]
		public void RetilerPlacesNorthOuterCliffBetweenRaisedSides()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, 1)] = 1;
			map.Height[cell + new CVec(1, -1)] = 1;
			map.Height[cell + new CVec(1, 1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsNorthOuterTemplate(map.Tiles[cell].Type), Is.True);
		}


		[TestCase(TestName = "Retiler places south inner cliff on straight south edge")]
		public void RetilerPlacesSouthInnerCliff()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, 1)] = 1;
			map.Height[cell + new CVec(1, -1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			map.Height[cell + new CVec(-1, -1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsSouthInnerTemplate(map.Tiles[cell].Type), Is.True);
		}

		[TestCase(TestName = "Retiler places south west face on south west edge")]
		public void RetilerPlacesSouthWestFace()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, -1)] = 1;
			map.Height[cell + new CVec(1, -1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(HighCliffTileIds.IsSouthWestFaceTemplate(map.Tiles[cell].Type), Is.True);
		}

		[TestCase(TestName = "Target height helper normalizes positive terrain to binary target")]
		public void GetCellsWithTargetHeightNormalizesToBinaryTarget()
		{
			var map = CreateIsometricMap();
			map.Height[new CPos(10, 5)] = 2;
			map.Height[new CPos(11, 5)] = 1;
			map.Height[new CPos(12, 5)] = 0;

			var changedCells = HighCliffTerrainTools.GetCellsWithTargetHeight(map, new[] { new CPos(10, 5), new CPos(11, 5), new CPos(12, 5) }, 1);

			Assert.That(changedCells.SetEquals(new[] { new CPos(10, 5), new CPos(12, 5) }), Is.True);
		}

		[TestCase(TestName = "Retiler restores clear tile when plateau is lowered to zero")]
		public void RetilerRestoresClearTileWhenHeightFlattens()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(10, 5);
			map.Height[cell] = 1;
			map.Height[cell + new CVec(-1, -1)] = 1;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);
			Assert.That(map.Tiles[cell].Type, Is.Not.EqualTo(ClearTemplateId));

			map.Height[cell] = 0;
			HighCliffTerrainTools.RetileHeightRegion(map, new[] { cell }, ClearTemplateId);

			Assert.That(map.Tiles[cell].Type, Is.EqualTo(ClearTemplateId));
		}

		static Map CreateIsometricMap(int width = 20, int height = 20)
		{
			var map = (Map)FormatterServices.GetUninitializedObject(typeof(Map));
			var grid = new MapGrid(new MiniYaml("", new[]
			{
				new MiniYamlNode("Type", nameof(MapGridType.RectangularIsometric)),
				new MiniYamlNode("MaximumTerrainHeight", "16")
			}));

			var terrainHeight = new CellLayer<byte>(grid.Type, new Size(width, height));
			terrainHeight.Clear(0);

			var ramps = new CellLayer<byte>(grid.Type, new Size(width, height));
			ramps.Clear(0);

			var tiles = new CellLayer<TerrainTile>(grid.Type, new Size(width, height));
			tiles.Clear(new TerrainTile(ClearTemplateId, 0));

			SetField(map, nameof(Map.Grid), grid);
			SetAutoProperty(map, nameof(Map.Height), terrainHeight);
			SetAutoProperty(map, nameof(Map.Ramp), ramps);
			SetAutoProperty(map, nameof(Map.Tiles), tiles);
			return map;
		}

		static void SetAutoProperty(object target, string propertyName, object value)
		{
			SetField(target, $"<{propertyName}>k__BackingField", value);
		}

		static void SetField(object target, string fieldName, object value)
		{
			var field = target.GetType().GetField(fieldName, InstanceFields);
			Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}.");
			field!.SetValue(target, value);
		}
	}
}
