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
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public sealed class ActorExtsHeightTest
	{
		const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		sealed class TestOccupiesSpace : IOccupySpace
		{
			public TestOccupiesSpace(CPos topLeft, WPos centerPosition)
			{
				TopLeft = topLeft;
				CenterPosition = centerPosition;
			}

			public WPos CenterPosition { get; }
			public CPos TopLeft { get; }

			public (CPos Cell, SubCell SubCell)[] OccupiedCells()
			{
				return new[] { (TopLeft, SubCell.FullCell) };
			}
		}

		[TestCase(TestName = "TryGetGroundTerrainHeightStep ignores custom movement layer semantics")]
		public void TryGetGroundTerrainHeightStepReturnsGroundCellHeight()
		{
			var map = CreateIsometricMap();
			var groundCell = new CPos(4, 1);
			map.Height[groundCell] = 5;

			var actor = CreateActor(map, new CPos(4, 1, 1), map.CenterOfCell(groundCell) + new WVec(0, 0, 724));

			Assert.That(actor.TryGetGroundTerrainHeightStep(out var terrainHeightStep), Is.True);
			Assert.That(terrainHeightStep, Is.EqualTo((byte)5));
		}

		[TestCase(TestName = "TryGetTerrainSurfaceHeight returns ramp surface below actor")]
		public void TryGetTerrainSurfaceHeightReturnsRampSurface()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(5, 1);
			map.Height[cell] = 4;
			map.Ramp[cell] = 1;

			var offset = new WVec(256, 0, 0);
			var ramp = map.Grid.Ramps[map.Ramp[cell]];
			var surfaceZ = map.CellHeightStep.Length * map.Height[cell] + ramp.HeightOffset(offset.X, offset.Y);
			var center = map.CenterOfCell(cell);
			var actor = CreateActor(map, cell, new WPos(center.X + offset.X, center.Y + offset.Y, surfaceZ + 300));

			Assert.That(actor.TryGetTerrainSurfaceHeight(out var terrainSurfaceHeight), Is.True);
			Assert.That(terrainSurfaceHeight, Is.EqualTo(new WDist(surfaceZ)));
		}

		[TestCase(TestName = "TryGetAltitudeAboveTerrain returns actor altitude over terrain")]
		public void TryGetAltitudeAboveTerrainReturnsAltitudeOverTerrain()
		{
			var map = CreateIsometricMap();
			var cell = new CPos(5, 1);
			map.Height[cell] = 4;

			var center = map.CenterOfCell(cell);
			var actor = CreateActor(map, cell, center + new WVec(0, 0, 300));

			Assert.That(actor.TryGetAltitudeAboveTerrain(out var altitudeAboveTerrain), Is.True);
			Assert.That(altitudeAboveTerrain, Is.EqualTo(new WDist(300)));
		}

		[TestCase(TestName = "TryGetAltitudeAboveTerrain rejects actors outside the world")]
		public void TryGetAltitudeAboveTerrainRejectsActorsOutsideWorld()
		{
			var map = CreateIsometricMap();
			var actor = CreateActor(map, new CPos(4, 1), new WPos(0, 0, 0), false);

			Assert.That(actor.TryGetAltitudeAboveTerrain(out var altitudeAboveTerrain), Is.False);
			Assert.That(altitudeAboveTerrain, Is.EqualTo(WDist.Zero));
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

			var customTerrain = new CellLayer<byte>(grid.Type, new Size(width, height));
			customTerrain.Clear(byte.MaxValue);

			SetField(map, nameof(Map.Grid), grid);
			SetField(map, nameof(Map.Bounds), new Rectangle(0, 0, width, height));
			SetField(map, "projectionSafeBounds", new Rectangle(0, 0, width, height));
			SetAutoProperty(map, nameof(Map.Height), terrainHeight);
			SetAutoProperty(map, nameof(Map.Ramp), ramps);
			SetAutoProperty(map, nameof(Map.CustomTerrain), customTerrain);
			return map;
		}

		static Actor CreateActor(Map map, CPos topLeft, WPos centerPosition, bool isInWorld = true)
		{
			var world = (World)FormatterServices.GetUninitializedObject(typeof(World));
			SetField(world, nameof(World.Map), map);

			var actor = (Actor)FormatterServices.GetUninitializedObject(typeof(Actor));
			SetField(actor, nameof(Actor.World), world);
			SetAutoProperty(actor, nameof(Actor.OccupiesSpace), new TestOccupiesSpace(topLeft, centerPosition));
			SetAutoProperty(actor, nameof(Actor.IsInWorld), isInWorld);
			return actor;
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
