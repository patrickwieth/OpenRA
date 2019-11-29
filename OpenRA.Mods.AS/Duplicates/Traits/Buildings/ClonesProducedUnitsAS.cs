#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Creates a free duplicate of produced units.")]
	public class ClonesProducedUnitsASInfo : ConditionalTraitInfo, Requires<ProductionInfo>, Requires<ExitInfo>
	{
		[FieldLoader.Require]
		[Desc("Uses the \"Cloneable\" trait to determine whether or not we should clone a produced unit.")]
		public readonly BitSet<CloneableType> CloneableTypes = default(BitSet<CloneableType>);

		[FieldLoader.Require]
		[Desc("e.g. Infantry, Vehicles, Aircraft, Buildings")]
		public readonly string ProductionType = "";

		public override object Create(ActorInitializer init) { return new ClonesProducedUnitsAS(init, this); }
	}

	public class ClonesProducedUnitsAS : ConditionalTrait<ClonesProducedUnitsASInfo>, INotifyOtherProduction
	{
		readonly Production[] productionTraits;

		public ClonesProducedUnitsAS(ActorInitializer init, ClonesProducedUnitsASInfo info)
			: base(info)
		{
			productionTraits = init.Self.TraitsImplementing<Production>().ToArray();
		}

		public void UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			if (IsTraitDisabled)
				return;

			// No recursive cloning!
			if (producer.Owner != self.Owner || producer.Info.HasTraitInfo<ClonesProducedUnitsASInfo>())
				return;

			var ci = produced.Info.TraitInfoOrDefault<CloneableInfo>();
			if (ci == null || !Info.CloneableTypes.Overlaps(ci.Types))
				return;

			var factionInit = init.GetOrDefault<FactionInit>();

			// Stop as soon as one production trait successfully produced
			foreach (var p in productionTraits)
			{
				if (!string.IsNullOrEmpty(Info.ProductionType) && !p.Info.Produces.Contains(Info.ProductionType))
					continue;

				var inits = new TypeDictionary
				{
					new OwnerInit(self.Owner),
					factionInit ?? new FactionInit(BuildableInfo.GetInitialFaction(produced.Info, p.Faction))
				};

				if (p.Produce(self, produced.Info, Info.ProductionType, inits))
					return;
			}
		}
	}
}
