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
	[Desc("Used for silos defined on the player actor.")]
	class PlayerSiloInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly int Capacity = 0;

		public override object Create(ActorInitializer init) { return new PlayerSilo(init.Self, this); }
	}

	class PlayerSilo : IStoreResources, ISync
	{
		readonly PlayerSiloInfo info;

		[Sync] public int Stored { get { return player.ResourceCapacity == 0 ? 0 : (int)((long)info.Capacity * player.Resources / player.ResourceCapacity); } }

		PlayerResources player;
		public PlayerSilo(Actor self, PlayerSiloInfo info)
		{
			player = self.Trait<PlayerResources>();
			this.info = info;
		}

		int IStoreResources.Capacity
		{
			get { return info.Capacity; }
		}
	}
}
