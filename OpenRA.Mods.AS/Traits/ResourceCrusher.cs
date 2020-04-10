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
using System.Linq;
using OpenRA;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Allows the actor to crush and refine resources.")]
	public class ResourceCrusherInfo : ConditionalTraitInfo
	{
		[Desc("The resource this actor can crush.")]
		public readonly string ResourceType;

		[Desc("Percentage of the resource value earned.")]
		public int ValueModifier = 100;

		public readonly bool ShowTicks = true;
		public readonly int TickLifetime = 30;

		public override object Create(ActorInitializer init) { return new ResourceCrusher(init.Self, this); }
	}

	public class ResourceCrusher : ConditionalTrait<ResourceCrusherInfo>, ICrushResource, INotifyOwnerChanged
	{
		readonly ResourceType resourceType;
		readonly ResourceLayer resLayer;

		PlayerResources playerResources;

		public ResourceCrusher(Actor self, ResourceCrusherInfo info)
			: base(info)
		{
			resourceType = self.World.WorldActor.TraitsImplementing<ResourceType>()
				.FirstOrDefault(t => t.Info.Type == info.ResourceType);

			if (resourceType == null)
				throw new InvalidOperationException("No such resource type `{0}`".F(info.ResourceType));

			resLayer = self.World.WorldActor.Trait<ResourceLayer>();
			playerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
		}

		void ICrushResource.CrushResource(Actor self, CPos cell)
		{
			if (resourceType == resLayer.GetResourceType(cell))
			{
				var resource = resLayer.CrushResource(cell);
				var value = Util.ApplyPercentageModifiers(resourceType.Info.ValuePerUnit * resource.Value, new int[] { Info.ValueModifier });

				playerResources.ChangeCash(value);
				if (Info.ShowTicks && self.Owner.IsAlliedWith(self.World.RenderPlayer))
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(value), Info.TickLifetime)));
			}
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerResources = newOwner.PlayerActor.Trait<PlayerResources>();
		}
	}
}
