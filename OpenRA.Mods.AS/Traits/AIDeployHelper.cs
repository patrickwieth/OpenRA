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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Flags]
	public enum DeployTriggers
	{
		None = 0,
		Attack = 1,
		Damage = 2,
		Heal = 4,
		Periodically = 8
	}

	[Desc("If this unit is owned by an AI, issue a deploy order automatically.")]
	public class AIDeployHelperInfo : ConditionalTraitInfo
	{
		[Desc("Events leading to the actor getting uncloaked. Possible values are: None, Attack, Damage, Heal, Periodically.")]
		public readonly DeployTriggers DeployTrigger = DeployTriggers.Attack | DeployTriggers.Damage;

		[Desc("Chance of deploying when the trigger activates.")]
		public readonly int DeployChance = 50;

		[Desc("Delay between two successful deploy orders.")]
		public readonly int DeployTicks = 2500;

		[Desc("Delay to wait for the actor to undeploy (if capable to) after a successful deploy.")]
		public readonly int UndeployTicks = 450;

		public override object Create(ActorInitializer init) { return new AIDeployHelper(this); }
	}

	// TO-DO: Pester OpenRA to allow INotifyDeployTrigger to be used for other traits besides WithMakeAnimation. Like this one.
	public class AIDeployHelper : ConditionalTrait<AIDeployHelperInfo>, INotifyAttack, ITick, INotifyDamage, INotifyCreated, ISync
	{
		const string PrimaryBuildingOrderID = "PrimaryProducer";

		[Sync]
		int undeployTicks = -1, deployTicks;

		bool undeployable, deployed, primaryBuilding;
		IIssueDeployOrder[] deployTraits;

		public AIDeployHelper(AIDeployHelperInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			undeployable = self.Info.HasTraitInfo<GrantConditionOnDeployInfo>();
			deployTraits = self.TraitsImplementing<IIssueDeployOrder>().ToArray();
			primaryBuilding = self.Info.HasTraitInfo<PrimaryBuildingInfo>();
		}

		void TryDeploy(Actor self)
		{
			if (deployTicks > 0)
				return;

			if (self.World.SharedRandom.Next(100) > Info.DeployChance)
				return;

			var orders = deployTraits.Where(d => d.CanIssueDeployOrder(self)).Select(d => d.IssueDeployOrder(self, false));

			foreach (var o in orders)
				self.World.IssueOrder(o);

			if (primaryBuilding)
				self.World.IssueOrder(new Order(PrimaryBuildingOrderID, self, false));

			if (undeployable)
			{
				undeployTicks = Info.UndeployTicks;
				deployed = true;
			}

			deployTicks = Info.DeployTicks;
		}

		void Undeploy(Actor self)
		{
			self.World.IssueOrder(new Order("GrantConditionOnDeploy", self, false));
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || !self.Owner.IsBot)
				return;

			if (Info.DeployTrigger.HasFlag(DeployTriggers.Attack))
				TryDeploy(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !self.Owner.IsBot)
				return;

			if (deployed)
			{
				if (--undeployTicks < 0)
				{
					Undeploy(self);
					deployed = false;
				}

				return;
			}

			if (--deployTicks < 0 && Info.DeployTrigger.HasFlag(DeployTriggers.Periodically))
				TryDeploy(self);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || !self.Owner.IsBot)
				return;

			if (e.Damage.Value > 0 && Info.DeployTrigger.HasFlag(DeployTriggers.Damage))
				TryDeploy(self);

			if (e.Damage.Value < 0 && Info.DeployTrigger.HasFlag(DeployTriggers.Heal))
				TryDeploy(self);
		}
	}
}
