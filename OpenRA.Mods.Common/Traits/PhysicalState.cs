#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[RequireExplicitImplementation]
	public interface INotifyPhysicalStateChanged
	{
		void PhysicalStateChanged(Actor self, PhysicalState physicalState, int oldValue, int newValue);
	}

	[Desc("Tracks a named physical state value with configurable relaxation behavior.")]
	public class PhysicalStateInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Name of this physical state. Used to reference it from other traits.")]
		public readonly string Name = null;

		[Desc("Minimum value for this physical state.")]
		public readonly int MinValue = 0;

		[Desc("Maximum value for this physical state.")]
		public readonly int MaxValue = 100;

		[Desc("Whether incoming value changes should be scaled relative to unit health (divided by HP/10000).")]
		public readonly bool RelativeToHealth = false;

		[Desc("Linear relaxation amount per tick towards RelaxedValue.")]
		public readonly int RelaxationLinear = 0;

		[Desc("Exponential relaxation rate towards RelaxedValue (0.0 = no exponential relaxation, 1.0 = instant).")]
		public readonly float RelaxationExp = 0f;

		[Desc("Delay in ticks before relaxation starts after external value change.")]
		public readonly int RelaxationDelay = 0;

		[Desc("Interval in ticks between relaxation applications.")]
		public readonly int RelaxationInterval = 1;

		[Desc("Target value for relaxation.")]
		public readonly int RelaxedValue = 0;

		[Desc("Initial value when the trait is created.")]
		public readonly int InitialValue = 0;

		public override object Create(ActorInitializer init) { return new PhysicalState(init.Self, this); }
	}

	public class PhysicalState : ConditionalTrait<PhysicalStateInfo>, ITick, ISync
	{
		readonly Actor self;
		readonly Health health;
		INotifyPhysicalStateChanged[] notifyPhysicalStateChanged = Array.Empty<INotifyPhysicalStateChanged>();

		int relaxationDelayTicks;
		int relaxationIntervalTicks;

		[Sync]
		int currentValue;

		public string Name => Info.Name;

		public int Value
		{
			get => currentValue;
			set => SetValue(value, true);
		}

		public int MinValue => Info.MinValue;
		public int MaxValue => Info.MaxValue;

		public PhysicalState(Actor self, PhysicalStateInfo info)
			: base(info)
		{
			this.self = self;
			health = self.TraitOrDefault<Health>();
			currentValue = Math.Clamp(info.InitialValue, info.MinValue, info.MaxValue);
		}

		protected override void Created(Actor self)
		{
			notifyPhysicalStateChanged = self.TraitsImplementing<INotifyPhysicalStateChanged>().ToArray();
			base.Created(self);
		}

		void SetValue(int newValue, bool isExternalChange)
		{
			var clampedValue = Math.Clamp(newValue, Info.MinValue, Info.MaxValue);
			
			if (clampedValue == currentValue)
				return;

			var oldValue = currentValue;
			currentValue = clampedValue;

			if (isExternalChange && Info.RelaxationDelay > 0)
				relaxationDelayTicks = Info.RelaxationDelay;

			foreach (var notify in notifyPhysicalStateChanged)
				notify.PhysicalStateChanged(self, this, oldValue, currentValue);
		}

		public void ApplyChange(int amount)
		{
			var scaledAmount = amount;
			
			if (Info.RelativeToHealth && health != null && health.MaxHP > 0)
				scaledAmount = (int)(amount * health.HP / 10000.0f);

			SetValue(currentValue + scaledAmount, true);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (relaxationDelayTicks > 0)
			{
				relaxationDelayTicks--;
				return;
			}

			if (++relaxationIntervalTicks >= Info.RelaxationInterval)
			{
				relaxationIntervalTicks = 0;
				ApplyRelaxation();
			}
		}

		void ApplyRelaxation()
		{
			if (currentValue == Info.RelaxedValue)
				return;

			var difference = Info.RelaxedValue - currentValue;

			if (Info.RelaxationLinear > 0)
			{
				var linearChange = Math.Sign(difference) * Math.Min(Info.RelaxationLinear, Math.Abs(difference));
				SetValue(currentValue + linearChange, false);
			}

			if (Info.RelaxationExp > 0f && currentValue != Info.RelaxedValue)
			{
				var expChange = (int)(difference * Info.RelaxationExp);
				if (expChange != 0)
					SetValue(currentValue + expChange, false);
			}
		}
	}
}
