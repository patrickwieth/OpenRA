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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public static class ActorExts
	{
		public static bool IsAtGroundLevel(this Actor self)
		{
			if (self.OccupiesSpace == null)
				return false;

			if (!self.IsInWorld)
				return false;

			var map = self.World.Map;

			if (!map.Contains(self.Location))
				return false;

			return map.DistanceAboveTerrain(self.CenterPosition).Length == 0;
		}

		/// <summary>
		/// Tries to get the discrete ground terrain height step beneath the actor's current world position.
		/// This reflects the base terrain cell and intentionally ignores custom movement layers such as tunnels or bridges.
		/// </summary>
		public static bool TryGetGroundTerrainHeightStep(this Actor self, out byte terrainHeightStep)
		{
			terrainHeightStep = 0;

			if (!TryGetGroundMap(self, out var map))
				return false;

			var groundCell = map.CellContaining(self.CenterPosition);
			if (!map.Contains(groundCell))
				return false;

			terrainHeightStep = map.TerrainHeightStep(groundCell);
			return true;
		}

		/// <summary>
		/// Tries to get the exact ground terrain surface height beneath the actor's current world position.
		/// This reflects the base terrain surface and intentionally ignores custom movement layers such as tunnels or bridges.
		/// </summary>
		public static bool TryGetTerrainSurfaceHeight(this Actor self, out WDist terrainSurfaceHeight)
		{
			terrainSurfaceHeight = WDist.Zero;

			if (!TryGetGroundMap(self, out var map))
				return false;

			var groundCell = map.CellContaining(self.CenterPosition);
			if (!map.Contains(groundCell))
				return false;

			terrainSurfaceHeight = map.TerrainSurfaceHeight(self.CenterPosition);
			return true;
		}

		/// <summary>
		/// Tries to get the actor's altitude above the base terrain at its current world position.
		/// This is zero for actors resting on terrain and positive for actors above it.
		/// </summary>
		public static bool TryGetAltitudeAboveTerrain(this Actor self, out WDist altitudeAboveTerrain)
		{
			altitudeAboveTerrain = WDist.Zero;

			if (!TryGetGroundMap(self, out var map))
				return false;

			var groundCell = map.CellContaining(self.CenterPosition);
			if (!map.Contains(groundCell))
				return false;

			altitudeAboveTerrain = map.DistanceAboveTerrain(self.CenterPosition);
			return true;
		}

		static bool TryGetGroundMap(Actor self, out Map map)
		{
			map = null;

			if (self.OccupiesSpace == null || !self.IsInWorld)
				return false;

			map = self.World.Map;
			return true;
		}

		public static bool AppearsFriendlyTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.RelationshipWith(self.Owner);
			if (stance == PlayerRelationship.Ally)
				return true;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.RelationshipWith(self.EffectiveOwner.Owner) == PlayerRelationship.Ally;

			return false;
		}

		public static bool AppearsHostileTo(this Actor self, Actor toActor)
		{
			var stance = toActor.Owner.RelationshipWith(self.Owner);
			if (stance == PlayerRelationship.Ally)
				return false;

			if (self.EffectiveOwner != null && self.EffectiveOwner.Disguised && !toActor.Info.HasTraitInfo<IgnoresDisguiseInfo>())
				return toActor.Owner.RelationshipWith(self.EffectiveOwner.Owner) == PlayerRelationship.Enemy;

			return stance == PlayerRelationship.Enemy;
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<Actor> blockers)
		{
			foreach (var blocker in blockers)
				foreach (var moveBlocked in blocker.TraitsImplementing<INotifyBlockingMove>())
					moveBlocked.OnNotifyBlockingMove(blocker, self);
		}

		public static void NotifyBlocker(this Actor self, CPos position)
		{
			NotifyBlocker(self, self.World.ActorMap.GetActorsAt(position));
		}

		public static void NotifyBlocker(this Actor self, IEnumerable<CPos> positions)
		{
			NotifyBlocker(self, positions.SelectMany(p => self.World.ActorMap.GetActorsAt(p)));
		}

		public static CPos ClosestCell(this Actor self, IEnumerable<CPos> cells)
		{
			return cells.MinByOrDefault(c => (self.Location - c).LengthSquared);
		}
	}
}
