#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

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

	public class GrantConditionWhileIdling : ITick, INotifyIdle
	{
		readonly GrantConditionWhileIdlingInfo info;

		int token = Actor.InvalidConditionToken;
		int delay;

		public GrantConditionWhileIdling(GrantConditionWhileIdlingInfo info)
		{
			this.info = info;
		}

		void ITick.Tick(Actor self)
		{
			if (delay > 0 && token == Actor.InvalidConditionToken)
				token = self.GrantCondition(info.Condition);

			if (token != Actor.InvalidConditionToken && --delay < 0)
				token = self.RevokeCondition(token);
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			delay = 2;
		}
	}
}
