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
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using OpenRA.Primitives;

namespace OpenRA.Test
{
	[TestFixture]
	public sealed class MapHeightTest
	{
		const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		[TestCase(TestName = "TerrainHeightStep returns stored ground height")]
		public void TerrainHeightStepReturnsStoredCellHeight()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(4, 1);
			map.Height[cell] = 3;

			Assert.That(map.TerrainHeightStep(cell), Is.EqualTo((byte)3));
		}

		[TestCase(TestName = "TerrainHeightStep rejects custom movement layers")]
		public void TerrainHeightStepRejectsCustomMovementLayers()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(4, 1, 1);

			var ex = Assert.Throws<ArgumentOutOfRangeException>(() => map.TerrainHeightStep(cell));
			StringAssert.Contains("default ground layer", ex.Message);
		}

		[TestCase(TestName = "TerrainHeightStep from world position uses containing cell")]
		public void TerrainHeightStepFromWorldPositionUsesContainingCell()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(4, 1);
			map.Height[cell] = 5;
			map.Ramp[cell] = 1;

			var position = map.CenterOfCell(cell) + new WVec(128, 0, 200);
			Assert.That(map.TerrainHeightStep(position), Is.EqualTo((byte)5));
		}

		[TestCase(TestName = "TerrainSurfaceHeight returns flat cell surface")]
		public void TerrainSurfaceHeightReturnsFlatCellSurface()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(5, 1);
			map.Height[cell] = 4;

			var center = map.CenterOfCell(cell);
			var position = center + new WVec(0, 0, 300);

			Assert.That(map.TerrainSurfaceHeight(position), Is.EqualTo(new WDist(center.Z)));
		}

		[TestCase(TestName = "TerrainSurfaceHeight accounts for ramp interpolation")]
		public void TerrainSurfaceHeightAccountsForRampInterpolation()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(5, 1);
			map.Height[cell] = 4;
			map.Ramp[cell] = 1;

			var offset = new WVec(256, 0, 0);
			var ramp = map.Grid.Ramps[map.Ramp[cell]];
			var surfaceZ = map.CellHeightStep.Length * map.Height[cell] + ramp.HeightOffset(offset.X, offset.Y);
			var center = map.CenterOfCell(cell);
			var position = new WPos(center.X + offset.X, center.Y + offset.Y, surfaceZ + 300);

			Assert.That(map.TerrainSurfaceHeight(position), Is.EqualTo(new WDist(surfaceZ)));
		}

		static Map CreateIsometricMap(int width = 8, int height = 8)
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

			SetField(map, nameof(Map.Grid), grid);
			SetAutoProperty(map, nameof(Map.Height), terrainHeight);
			SetAutoProperty(map, nameof(Map.Ramp), ramps);
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
