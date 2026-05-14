#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Terrain
{
	public static class HighCliffTileIds
	{
		public const ushort RaiseSelectorTemplateId = 11900;
		public const ushort LowerSelectorTemplateId = 11910;
		public const ushort SwSelectorTemplateId = RaiseSelectorTemplateId;
		public const ushort SeSelectorTemplateId = LowerSelectorTemplateId;

		const ushort SwFaceTemplateBaseId = 12000;
		const ushort SeFaceTemplateBaseId = 12020;
		const ushort SOuterTemplateBaseId = 12040;
		const ushort SInnerTemplateBaseId = 12060;
		const ushort EOuterTemplateBaseId = 12080;
		const ushort WOuterTemplateBaseId = 12100;
		const ushort EInnerTemplateBaseId = 12120;
		const ushort WInnerTemplateBaseId = 12130;
		const ushort NeOuterTemplateBaseId = 12140;
		const ushort NwOuterTemplateBaseId = 12150;
		const ushort NOuterTemplateBaseId = 12160;

		const int FaceVariantCount = 9;
		const int SouthOuterVariantCount = 3;
		const int SouthInnerVariantCount = 3;
		const int EastWestOuterVariantCount = 9;
		const int InnerVariantCount = 3;
		const int NorthOuterVariantCount = 3;

		public static bool IsSelectorTemplate(ushort templateId)
		{
			return templateId == SwSelectorTemplateId || templateId == SeSelectorTemplateId;
		}

		public static bool IsTemplate(ushort templateId)
		{
			return IsSelectorTemplate(templateId) ||
				InRange(templateId, SwFaceTemplateBaseId, FaceVariantCount) ||
				InRange(templateId, SeFaceTemplateBaseId, FaceVariantCount) ||
				InRange(templateId, SOuterTemplateBaseId, SouthOuterVariantCount) ||
				InRange(templateId, SInnerTemplateBaseId, SouthInnerVariantCount) ||
				InRange(templateId, EOuterTemplateBaseId, EastWestOuterVariantCount) ||
				InRange(templateId, WOuterTemplateBaseId, EastWestOuterVariantCount) ||
				InRange(templateId, EInnerTemplateBaseId, InnerVariantCount) ||
				InRange(templateId, WInnerTemplateBaseId, InnerVariantCount) ||
				InRange(templateId, NeOuterTemplateBaseId, NorthOuterVariantCount) ||
				InRange(templateId, NwOuterTemplateBaseId, NorthOuterVariantCount) ||
				InRange(templateId, NOuterTemplateBaseId, NorthOuterVariantCount);
		}

		public static bool IsSouthWestFaceTemplate(ushort templateId) { return InRange(templateId, SwFaceTemplateBaseId, FaceVariantCount); }
		public static bool IsSouthEastFaceTemplate(ushort templateId) { return InRange(templateId, SeFaceTemplateBaseId, FaceVariantCount); }
		public static bool IsSouthOuterTemplate(ushort templateId) { return InRange(templateId, SOuterTemplateBaseId, SouthOuterVariantCount); }
		public static bool IsSouthInnerTemplate(ushort templateId) { return InRange(templateId, SInnerTemplateBaseId, SouthInnerVariantCount); }
		public static bool IsEastOuterTemplate(ushort templateId) { return InRange(templateId, EOuterTemplateBaseId, EastWestOuterVariantCount); }
		public static bool IsWestOuterTemplate(ushort templateId) { return InRange(templateId, WOuterTemplateBaseId, EastWestOuterVariantCount); }
		public static bool IsEastInnerTemplate(ushort templateId) { return InRange(templateId, EInnerTemplateBaseId, InnerVariantCount); }
		public static bool IsWestInnerTemplate(ushort templateId) { return InRange(templateId, WInnerTemplateBaseId, InnerVariantCount); }
		public static bool IsNorthEastOuterTemplate(ushort templateId) { return InRange(templateId, NeOuterTemplateBaseId, NorthOuterVariantCount); }
		public static bool IsNorthWestOuterTemplate(ushort templateId) { return InRange(templateId, NwOuterTemplateBaseId, NorthOuterVariantCount); }
		public static bool IsNorthOuterTemplate(ushort templateId) { return InRange(templateId, NOuterTemplateBaseId, NorthOuterVariantCount); }

		public static ushort SelectSouthWestFaceTemplateId(CPos cell) { return SelectVariantTemplateId(SwFaceTemplateBaseId, cell, 0, FaceVariantCount); }
		public static ushort SelectSouthEastFaceTemplateId(CPos cell) { return SelectVariantTemplateId(SeFaceTemplateBaseId, cell, 0, FaceVariantCount); }
		public static ushort SelectSouthOuterTemplateId(CPos cell) { return SelectVariantTemplateId(SOuterTemplateBaseId, cell, 1, SouthOuterVariantCount); }
		public static ushort SelectSouthInnerTemplateId(CPos cell) { return SelectVariantTemplateId(SInnerTemplateBaseId, cell, 2, SouthInnerVariantCount); }
		public static ushort SelectEastOuterTemplateId(CPos cell) { return SelectVariantTemplateId(EOuterTemplateBaseId, cell, 3, EastWestOuterVariantCount); }
		public static ushort SelectWestOuterTemplateId(CPos cell) { return SelectVariantTemplateId(WOuterTemplateBaseId, cell, 4, EastWestOuterVariantCount); }
		public static ushort SelectEastInnerTemplateId(CPos cell) { return SelectVariantTemplateId(EInnerTemplateBaseId, cell, 5, InnerVariantCount); }
		public static ushort SelectWestInnerTemplateId(CPos cell) { return SelectVariantTemplateId(WInnerTemplateBaseId, cell, 6, InnerVariantCount); }
		public static ushort SelectNorthEastOuterTemplateId(CPos cell) { return SelectVariantTemplateId(NeOuterTemplateBaseId, cell, 7, NorthOuterVariantCount); }
		public static ushort SelectNorthWestOuterTemplateId(CPos cell) { return SelectVariantTemplateId(NwOuterTemplateBaseId, cell, 8, NorthOuterVariantCount); }
		public static ushort SelectNorthOuterTemplateId(CPos cell) { return SelectVariantTemplateId(NOuterTemplateBaseId, cell, 9, NorthOuterVariantCount); }

		public static bool TryGetOuterCornerFaceTemplate(ushort templateId, out ushort faceTemplateId)
		{
			faceTemplateId = 0;
			return false;
		}

		static ushort SelectVariantTemplateId(ushort baseTemplateId, CPos cell, int salt, int count)
		{
			var selector = ((cell.X * 73856093) ^ (cell.Y * 19349663) ^ (salt * 83492791)) & int.MaxValue;
			return (ushort)(baseTemplateId + selector % count);
		}

		static bool InRange(ushort templateId, ushort baseTemplateId, int count)
		{
			return templateId >= baseTemplateId && templateId < baseTemplateId + count;
		}
	}
}
