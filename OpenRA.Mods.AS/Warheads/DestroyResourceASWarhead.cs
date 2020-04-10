#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class DestroyResourceASWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Size of the area. The resources are seeded within this area.", "Provide 2 values for a ring effect (outer/inner).")]
		public readonly int[] Size = { 0, 0 };

		[FieldLoader.Require]
		[Desc("Types of resource which should be destroyed.")]
		public readonly string[] ResourceTypes;

		[Desc("Amount of resources to be destroyed per cell. 0 means destroy all.")]
		public readonly int Density = 0;

		readonly HashSet<ResourceTypeInfo> resourceTypeInfos = new HashSet<ResourceTypeInfo>();

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			var definedResourceTypeInfos = rules.Actors["world"].TraitInfos<ResourceTypeInfo>();
			foreach (var resourceTypeInfo in ResourceTypes)
			{
				var resTypeInfo = definedResourceTypeInfos.FirstOrDefault(x => x.Type == resourceTypeInfo);
				if (resTypeInfo != null)
					resourceTypeInfos.Add(resTypeInfo);
			}
		}

		// TODO: Allow maximum resource removal to be defined in total.
		public override void DoImpact(Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(target.CenterPosition);
			var resLayer = world.WorldActor.Trait<ResourceLayer>();

			var minRange = (Size.Length > 1 && Size[1] > 0) ? Size[1] : 0;
			var allCells = world.Map.FindTilesInAnnulus(targetTile, minRange, Size[0]);

			// Destroy resources in the selected tiles
			foreach (var cell in allCells)
			{
				if (resourceTypeInfos.Contains(resLayer.GetResourceType(cell).Info))
					resLayer.DestroyDensity(cell, Density);
			}
		}
	}
}
