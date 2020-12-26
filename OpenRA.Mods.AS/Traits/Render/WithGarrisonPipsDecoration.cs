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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits.Render
{
	public class WithGarrisonPipsDecorationInfo : WithDecorationBaseInfo, Requires<GarrisonableInfo>
	{
		[Desc("Number of pips to display. Defaults to Cargo.MaxWeight.")]
		public readonly int PipCount = -1;

		[Desc("If non-zero, override the spacing between adjacent pips.")]
		public readonly int2 PipStride = int2.Zero;

		[Desc("Image that defines the pip sequences.")]
		public readonly string Image = "pips";

		[SequenceReference("Image")]
		[Desc("Sequence used for empty pips.")]
		public readonly string EmptySequence = "pip-empty";

		[SequenceReference("Image")]
		[Desc("Sequence used for full pips that aren't defined in CustomPipSequences.")]
		public readonly string FullSequence = "pip-green";

		// TODO: [SequenceReference] isn't smart enough to use Dictionaries.
		[Desc("Pip sequence to use for specific passenger actors.")]
		public readonly Dictionary<string, string> CustomPipSequences = new Dictionary<string, string>();

		[PaletteReference]
		public readonly string Palette = "chrome";

		public override object Create(ActorInitializer init) { return new WithGarrisonPipsDecoration(init.Self, this); }
	}

	public class WithGarrisonPipsDecoration : WithDecorationBase<WithGarrisonPipsDecorationInfo>
	{
		readonly Garrisonable garrisonable;
		readonly Animation pips;
		readonly int pipCount;

		public WithGarrisonPipsDecoration(Actor self, WithGarrisonPipsDecorationInfo info)
			: base(self, info)
		{
			garrisonable = self.Trait<Garrisonable>();
			pipCount = info.PipCount > 0 ? info.PipCount : garrisonable.Info.MaxWeight;
			pips = new Animation(self.World, info.Image);
		}

		string GetPipSequence(int i)
		{
			var n = i * garrisonable.Info.MaxWeight / pipCount;

			foreach (var g in garrisonable.Garrisoners)
			{
				var pi = g.Info.TraitInfo<GarrisonerInfo>();
				if (n < pi.Weight)
				{
					var sequence = Info.FullSequence;
					if (pi.CustomPipType != null && !Info.CustomPipSequences.TryGetValue(pi.CustomPipType, out sequence))
						Log.Write("debug", "Actor type {0} defines a custom pip type {1} that is not defined for actor type {2}".F(g.Info.Name, pi.CustomPipType, self.Info.Name));

					return sequence;
				}

				n -= pi.Weight;
			}

			return Info.EmptySequence;
		}

		protected override IEnumerable<IRenderable> RenderDecoration(Actor self, WorldRenderer wr, int2 screenPos)
		{
			pips.PlayRepeating(Info.EmptySequence);

			var palette = wr.Palette(Info.Palette);
			var pipSize = pips.Image.Size.XY.ToInt2();
			var pipStride = Info.PipStride != int2.Zero ? Info.PipStride : new int2(pipSize.X, 0);

			screenPos -= pipSize / 2;
			for (var i = 0; i < pipCount; i++)
			{
				pips.PlayRepeating(GetPipSequence(i));
				yield return new UISpriteRenderable(pips.Image, self.CenterPosition, screenPos, 0, palette, 1f);

				screenPos += pipStride;
			}
		}
	}
}
