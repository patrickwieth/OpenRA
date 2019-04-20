#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class FallToEarthAS : Activity
	{
		readonly Aircraft aircraft;
		readonly FallsToEarthASInfo info;
		int acceleration = 0;
		int spin = 0;
		WDist velocity;

		public FallToEarthAS(Actor self, FallsToEarthASInfo info)
		{
			this.info = info;
			aircraft = self.Trait<Aircraft>();
			velocity = info.Velocity;
			if (info.Spins)
			{
				spin = info.SpinInitial;
				acceleration = info.SpinAcceleration;
			}
		}

		public override Activity Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0)
			{
				if (info.ExplosionWeapon != null)
				{
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					info.ExplosionWeapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}

				self.Dispose();
				return null;
			}

			if (info.Spins)
			{
				spin += acceleration;
				aircraft.Facing = (aircraft.Facing + spin) % 256;
			}

			var move = info.Moves ? aircraft.FlyStep(aircraft.Facing) : WVec.Zero;
			velocity += info.VelocityAcceleration;
			move -= new WVec(WDist.Zero, WDist.Zero, velocity);
			aircraft.SetPosition(self, aircraft.CenterPosition + move);

			return this;
		}
	}
}
