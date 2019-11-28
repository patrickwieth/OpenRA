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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Flags]
	public enum SupportPowerTriggers
	{
		None = 0,
		Attack = 1,
		Damage = 2,
		Heal = 4,
		Periodically = 8
	}

	[Desc("If this unit is owned by an AI, activate a support power.")]
	public class AISupportPowerTargeterHelperInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("The support power's order name which the trait should activate.")]
		public readonly string OrderName = "";

		[Desc("Events leading to the actor getting uncloaked. Possible values are: None, Attack, Damage, Heal and Periodically.")]
		public readonly SupportPowerTriggers Trigger = SupportPowerTriggers.Damage;

		[Desc("Chance of activation when the trigger activates.")]
		public readonly int ActivationChance = 100;

		[Desc("DamageType(s) that trigger activation when when `Trigger` is set to `Damage` or `Heal`. Leave empty to always trigger.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Delay between two activation tries when `Trigger` is set to `Periodically`.")]
		public readonly int Ticks = 1000;

		public override object Create(ActorInitializer init) { return new AISupportPowerTargeterHelper(this); }
	}

	public class AISupportPowerTargeterHelper : ConditionalTrait<AISupportPowerTargeterHelperInfo>, INotifyAttack, ITick, INotifyDamage, INotifyCreated, ISync, INotifyOwnerChanged
	{
		SupportPowerManager supportPowerManager;
		PlayerResources playerResource;
		int ticks;

		public AISupportPowerTargeterHelper(AISupportPowerTargeterHelperInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			supportPowerManager = self.Owner.PlayerActor.Trait<SupportPowerManager>();
			playerResource = self.Owner.PlayerActor.Trait<PlayerResources>();

			base.Created(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			supportPowerManager = newOwner.PlayerActor.Trait<SupportPowerManager>();
			playerResource = newOwner.PlayerActor.Trait<PlayerResources>();
		}

		void TryActivation(Actor self)
		{
			if (self.World.SharedRandom.Next(100) > Info.ActivationChance)
				return;

			foreach (var power in supportPowerManager.Powers.Values)
			{
				if (power.Ready && Info.OrderName.StartsWith(power.Info.OrderName))
				{
					if (power.Info.Cost != 0 && playerResource.Cash + playerResource.Resources < power.Info.Cost)
						continue;

					self.World.IssueOrder(new Order(power.Key, supportPowerManager.Self,
						Target.FromCell(self.World, self.World.Map.CellContaining(self.CenterPosition)), false)
							{ SuppressVisualFeedback = true });
				}
			}
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (!self.Owner.IsBot || IsTraitDisabled)
				return;

			if (Info.Trigger.HasFlag(SupportPowerTriggers.Attack))
				TryActivation(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (!self.Owner.IsBot || IsTraitDisabled)
				return;

			if (Info.Trigger.HasFlag(SupportPowerTriggers.Periodically) && --ticks < 0)
			{
				TryActivation(self);
				ticks = Info.Ticks;
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!self.Owner.IsBot || IsTraitDisabled)
				return;

			if (!Info.DamageTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DamageTypes))
				return;

			if (e.Damage.Value > 0 && Info.Trigger.HasFlag(SupportPowerTriggers.Damage))
				TryActivation(self);

			if (e.Damage.Value < 0 && Info.Trigger.HasFlag(SupportPowerTriggers.Heal))
				TryActivation(self);
		}
	}
}
