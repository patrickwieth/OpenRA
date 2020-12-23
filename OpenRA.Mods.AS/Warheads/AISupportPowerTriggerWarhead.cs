#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("This warhead triggers activation of support powers owned by AI in the vicinity.")]
	public class AISupportPowerTriggerWarhead : WarheadAS
	{
		[FieldLoader.Require]
		[Desc("The support power's order name which the warhead should activate.")]
		public readonly string OrderName = "";

		[Desc("Chance of activation when the trigger activates.")]
		public readonly int ActivationChance = 100;

		[Desc("Range used to find actors with AI ownership.")]
		public readonly WDist Range = WDist.FromCells(3);

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			if (firedBy.World.SharedRandom.Next(100) > ActivationChance)
				return;

			if (!target.IsValidFor(firedBy))
				return;

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsOnCircle(target.CenterPosition, Range);

			foreach (var actor in availableActors)
			{
				if (!actor.Owner.IsBot)
					continue;

				var supportPowerManager = actor.Owner.PlayerActor.Trait<SupportPowerManager>();
				var playerResource = actor.Owner.PlayerActor.Trait<PlayerResources>();

				foreach (var power in supportPowerManager.Powers.Values)
				{
					if (power.Ready && OrderName.StartsWith(power.Info.OrderName))
					{
						if (power.Info.Cost != 0 && playerResource.Cash + playerResource.Resources < power.Info.Cost)
							continue;

						actor.World.IssueOrder(new Order(power.Key, supportPowerManager.Self,
							Target.FromCell(actor.World, actor.World.Map.CellContaining(target.CenterPosition)), false)
						{ SuppressVisualFeedback = true });

						// Stop at the first successful support power activation.
						return;
					}
				}
			}
		}
	}
}
