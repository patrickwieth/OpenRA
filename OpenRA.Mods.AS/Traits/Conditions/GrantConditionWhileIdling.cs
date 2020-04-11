#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Grants a condition while the actor is idling.")]
	public class GrantConditionWhileIdlingInfo : ITraitInfo
	{
		[GrantedConditionReference]
		[FieldLoader.Require]
		[Desc("The condition to grant.")]
		public readonly string Condition = null;

		public object Create(ActorInitializer init) { return new GrantConditionWhileIdling(this); }
	}

	public class GrantConditionWhileIdling : ITick, INotifyIdle, INotifyCreated
	{
		readonly GrantConditionWhileIdlingInfo info;

		ConditionManager manager;
		int token = ConditionManager.InvalidConditionToken;
		int delay;

		public GrantConditionWhileIdling(GrantConditionWhileIdlingInfo info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			manager = self.Trait<ConditionManager>();
		}

		void ITick.Tick(Actor self)
		{
			if (delay > 0 && token == ConditionManager.InvalidConditionToken)
				token = manager.GrantCondition(self, info.Condition);

			if (token != ConditionManager.InvalidConditionToken && --delay < 0)
				token = manager.RevokeCondition(self, token);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			delay = 2;
		}
	}
}
