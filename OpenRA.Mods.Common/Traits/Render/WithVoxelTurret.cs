#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithVoxelTurretInfo : ConditionalTraitInfo, IRenderActorPreviewVoxelsInfo, Requires<RenderVoxelsInfo>, Requires<TurretedInfo>, Requires<ArmamentInfo>
	{
		[Desc("Voxel sequence name to use")]
		public readonly string Sequence = "turret";

		[Desc("Turreted 'Turret' key to display")]
		public readonly string Turret = "primary";

		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		[Desc("Defines if the Voxel should have a shadow.")]
		public readonly bool ShowShadow = true;

		[Desc("Render recoil, should be activated when no VoxelBarrel is present (as this will also recoil)")]
		public readonly bool Recoils = false;

		public override object Create(ActorInitializer init) { return new WithVoxelTurret(init.Self, this); }

		public IEnumerable<ModelAnimation> RenderPreviewVoxels(
			ActorPreviewInitializer init, RenderVoxelsInfo rv, string image, Func<WRot> orientation, int facings, PaletteReference p)
		{
			if (!EnabledByDefault)
				yield break;

			var t = init.Actor.TraitInfos<TurretedInfo>()
				.First(tt => tt.Turret == Turret);

			var model = init.World.ModelCache.GetModelSequence(image, Sequence);
			var turretOffset = t.PreviewPosition(init, orientation);
			var turretOrientation = t.PreviewOrientation(init, orientation, facings);
			yield return new ModelAnimation(model, turretOffset, turretOrientation, () => false, () => 0, ShowShadow);
		}
	}

	public class WithVoxelTurret : ConditionalTrait<WithVoxelTurretInfo>
	{
		readonly Turreted turreted;
		readonly Armament[] arms;
		readonly BodyOrientation body;

		public WithVoxelTurret(Actor self, WithVoxelTurretInfo info)
			: base(info)
		{
			body = self.Trait<BodyOrientation>();
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == Info.Turret);
			arms = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == info.Turret).ToArray();

			var rv = self.Trait<RenderVoxels>();
			rv.Add(new ModelAnimation(self.World.ModelCache.GetModelSequence(rv.Image, Info.Sequence),
				() => TurretOffset(self), () => turreted.WorldOrientation,
				() => IsTraitDisabled, () => 0, info.ShowShadow));
		}

		protected virtual WVec TurretOffset(Actor self)
		{
			if (!Info.Recoils)
				return turreted.Position(self);
			else{
				// offset in turret coordinates
				var localOffset = Info.LocalOffset;

				foreach (var arm in arms) {
					localOffset += new WVec(-arm.Recoil, WDist.Zero, WDist.Zero);
				}

				// Turret coordinates to body coordinates
				var bodyOrientation = body.QuantizeOrientation(self.Orientation);
				localOffset = localOffset.Rotate(turreted.WorldOrientation) + turreted.Offset.Rotate(bodyOrientation);

				// Body coordinates to world coordinates
				return body.LocalToWorld(localOffset);
			}
		}
	}
}
