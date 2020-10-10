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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition when this actor finishes a burst.")]
	public class GrantConditionOnBurstCompleteInfo : PausableConditionalTraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		[Desc("Name of the armaments that grant this condition.")]
		public readonly HashSet<string> ArmamentNames = new HashSet<string>() { "primary" };

		[FieldLoader.Require]
		public readonly int RevokeDelay;

		public override object Create(ActorInitializer init) { return new GrantConditionOnBurstComplete(this); }
	}

	public class GrantConditionOnBurstComplete : PausableConditionalTrait<GrantConditionOnBurstCompleteInfo>, ITick, INotifyCreated, INotifyBurstComplete
	{
		readonly GrantConditionOnBurstCompleteInfo info;

		int token = Actor.InvalidConditionToken;

		int ticks;

		public GrantConditionOnBurstComplete(GrantConditionOnBurstCompleteInfo info)
			: base(info)
		{
			this.info = info;
		}

		void INotifyBurstComplete.FiredBurst(Actor self, Target target, Armament a)
		{
			if (IsTraitDisabled || !Info.ArmamentNames.Contains(a.Info.Name))
				return;

			ticks = info.RevokeDelay;

			if (token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || IsTraitPaused || --ticks > 0)
				return;

			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}
	}
}
