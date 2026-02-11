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

using System.Collections.Generic;
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public interface IAutoTile
	{
		bool IsAutoTileTemplate(ushort templateId);
		bool IsAutoTileBaseTemplate(ushort templateId);
		ushort ResolveTemplate(Map map, CPos cell);
	}

	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Defines auto-tiling rules for the map editor.")]
	public class AutoTileInfo : TraitInfo
	{
		public readonly bool Debug = false;
		public readonly bool Enabled = true;
		public readonly bool IgnoreTransitionNeighbors = false;
		public readonly bool UseHigherPriority = false;
		public readonly bool InvertMask = false;
		public readonly bool IncludeDiagonals = false;
		public readonly bool AllowEnclosedTransitions = false;
		public readonly bool IgnoreNonBaseTemplates = false;

		public class AutoTileGroupInfo
		{
			[FieldLoader.Require]
			public readonly int Priority = 0;

			public AutoTileGroupInfo(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}
		}

		public class AutoTileTransitionsInfo
		{
			[FieldLoader.Require]
			public readonly ushort N = 0;
			[FieldLoader.Require]
			public readonly ushort E = 0;
			[FieldLoader.Require]
			public readonly ushort S = 0;
			[FieldLoader.Require]
			public readonly ushort W = 0;
			[FieldLoader.Require]
			public readonly ushort NE = 0;
			[FieldLoader.Require]
			public readonly ushort NW = 0;
			[FieldLoader.Require]
			public readonly ushort SE = 0;
			[FieldLoader.Require]
			public readonly ushort SW = 0;
			[FieldLoader.Require]
			public readonly ushort WNE = 0;
			[FieldLoader.Require]
			public readonly ushort NES = 0;
			[FieldLoader.Require]
			public readonly ushort SWN = 0;
			[FieldLoader.Require]
			public readonly ushort ESW = 0;
			[FieldLoader.Require]
			public readonly ushort NESW = 0;
			public readonly ushort Hole = 0;
			public readonly ushort U_NW = 0;
			public readonly ushort U_NE = 0;
			public readonly ushort U_SE = 0;
			public readonly ushort U_SW = 0;
			public readonly ushort Parallel_NE_SW = 0;
			public readonly ushort Parallel_NW_SE = 0;
			public readonly ushort End_NW = 0;
			public readonly ushort End_NE = 0;
			public readonly ushort End_SE = 0;
			public readonly ushort End_SW = 0;

			public AutoTileTransitionsInfo(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
			}

			public bool TryGetTemplateForMask(byte mask, out ushort templateId)
			{
				switch (mask)
				{
					case AutoTileMasks.N: templateId = N; return true;
					case AutoTileMasks.E: templateId = E; return true;
					case AutoTileMasks.S: templateId = S; return true;
					case AutoTileMasks.W: templateId = W; return true;
					case AutoTileMasks.N | AutoTileMasks.E: templateId = NE; return true;
					case AutoTileMasks.N | AutoTileMasks.W: templateId = NW; return true;
					case AutoTileMasks.S | AutoTileMasks.E: templateId = SE; return true;
					case AutoTileMasks.S | AutoTileMasks.W: templateId = SW; return true;
					case AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.W: templateId = WNE; return true;
					case AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.S: templateId = NES; return true;
					case AutoTileMasks.N | AutoTileMasks.S | AutoTileMasks.W: templateId = SWN; return true;
					case AutoTileMasks.E | AutoTileMasks.S | AutoTileMasks.W: templateId = ESW; return true;
					case AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.S | AutoTileMasks.W: templateId = NESW; return true;
				}

				templateId = 0;
				return false;
			}
		}

		public class AutoTileStyleInfo
		{
			[FieldLoader.Require]
			public readonly string Group = null;
			[FieldLoader.Require]
			public readonly ushort BaseTemplate = 0;

			[FieldLoader.Ignore]
			public readonly Dictionary<string, AutoTileTransitionsInfo> Transitions = new();

			public AutoTileStyleInfo(MiniYaml yaml)
			{
				FieldLoader.Load(this, yaml);
				var transitions = yaml.NodeWithKeyOrDefault("Transitions");
				if (transitions != null)
				{
					foreach (var t in transitions.Value.Nodes)
						Transitions[t.Key] = new AutoTileTransitionsInfo(t.Value);
				}
			}
		}

		[FieldLoader.LoadUsing(nameof(LoadGroups))]
		public readonly Dictionary<string, AutoTileGroupInfo> Groups = null;

		[FieldLoader.LoadUsing(nameof(LoadStyles))]
		public readonly Dictionary<string, AutoTileStyleInfo> Styles = null;

		protected static object LoadGroups(MiniYaml yaml)
		{
			var ret = new Dictionary<string, AutoTileGroupInfo>();
			var groups = yaml.NodeWithKeyOrDefault("Groups");
			if (groups != null)
			{
				foreach (var g in groups.Value.Nodes)
					ret[g.Key] = new AutoTileGroupInfo(g.Value);
			}

			return ret;
		}

		protected static object LoadStyles(MiniYaml yaml)
		{
			var ret = new Dictionary<string, AutoTileStyleInfo>();
			var styles = yaml.NodeWithKeyOrDefault("Styles");
			if (styles != null)
			{
				foreach (var s in styles.Value.Nodes)
					ret[s.Key] = new AutoTileStyleInfo(s.Value);
			}

			return ret;
		}

		public override object Create(ActorInitializer init) { return new AutoTile(this); }
	}

	sealed class AutoTileStyle
	{
		public readonly string Id;
		public readonly string GroupId;
		public readonly ushort BaseTemplate;
		public readonly Dictionary<string, AutoTileInfo.AutoTileTransitionsInfo> Transitions;
		public readonly HashSet<ushort> TemplateIds;

		public AutoTileStyle(string id, AutoTileInfo.AutoTileStyleInfo info)
		{
			Id = id;
			GroupId = info.Group;
			BaseTemplate = info.BaseTemplate;
			Transitions = info.Transitions;
			TemplateIds = new HashSet<ushort> { BaseTemplate };

			foreach (var t in Transitions.Values)
			{
				void AddTemplateId(ushort templateId)
				{
					if (templateId != 0)
						TemplateIds.Add(templateId);
				}

				AddTemplateId(t.N);
				AddTemplateId(t.E);
				AddTemplateId(t.S);
				AddTemplateId(t.W);
				AddTemplateId(t.NE);
				AddTemplateId(t.NW);
				AddTemplateId(t.SE);
				AddTemplateId(t.SW);
				AddTemplateId(t.WNE);
				AddTemplateId(t.NES);
				AddTemplateId(t.SWN);
				AddTemplateId(t.ESW);
				AddTemplateId(t.NESW);
				AddTemplateId(t.Hole);
				AddTemplateId(t.U_NW);
				AddTemplateId(t.U_NE);
				AddTemplateId(t.U_SE);
				AddTemplateId(t.U_SW);
				AddTemplateId(t.Parallel_NE_SW);
				AddTemplateId(t.Parallel_NW_SE);
				AddTemplateId(t.End_NW);
				AddTemplateId(t.End_NE);
				AddTemplateId(t.End_SE);
				AddTemplateId(t.End_SW);
			}
		}
	}

	static class AutoTileMasks
	{
		public const byte N = 1;
		public const byte E = 2;
		public const byte S = 4;
		public const byte W = 8;
	}

	public class AutoTile : IAutoTile
	{
		readonly Dictionary<string, AutoTileInfo.AutoTileGroupInfo> groups;
		readonly Dictionary<ushort, AutoTileStyle> styleByTemplate = new();
		readonly Dictionary<ushort, HashSet<string>> transitionGroupsByTemplate = new();
		readonly Dictionary<ushort, Dictionary<string, byte>> edgeMasksByTemplate = new();
		readonly bool enabled;
		readonly bool ignoreTransitionNeighbors;
		readonly bool useHigherPriority;
		readonly bool invertMask;
		readonly bool includeDiagonals;
		readonly bool allowEnclosedTransitions;
		readonly bool ignoreNonBaseTemplates;
		readonly bool debug;

		static readonly (CVec offset, byte mask)[] RectNeighborDirs =
		{
			(new CVec(0, -1), AutoTileMasks.N),
			(new CVec(1, 0), AutoTileMasks.E),
			(new CVec(0, 1), AutoTileMasks.S),
			(new CVec(-1, 0), AutoTileMasks.W),
		};

		static readonly (CVec offset, byte mask)[] RectDiagonalDirs =
		{
			(new CVec(1, -1), AutoTileMasks.N | AutoTileMasks.E),
			(new CVec(1, 1), AutoTileMasks.S | AutoTileMasks.E),
			(new CVec(-1, 1), AutoTileMasks.S | AutoTileMasks.W),
			(new CVec(-1, -1), AutoTileMasks.N | AutoTileMasks.W),
		};

		const byte AllEdges = AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.S | AutoTileMasks.W;

		static (CVec offset, byte mask)[] GetNeighborDirs(Map map, CPos cell)
		{
			return RectNeighborDirs;
		}

		static (CVec offset, byte mask)[] GetDiagonalDirs(Map map, CPos cell)
		{
			return RectDiagonalDirs;
		}

		public AutoTile(AutoTileInfo info)
		{
			groups = info.Groups ?? new Dictionary<string, AutoTileInfo.AutoTileGroupInfo>();
			enabled = info.Enabled;
			ignoreTransitionNeighbors = info.IgnoreTransitionNeighbors;
			useHigherPriority = info.UseHigherPriority;
			invertMask = info.InvertMask;
			includeDiagonals = info.IncludeDiagonals;
			allowEnclosedTransitions = info.AllowEnclosedTransitions;
			ignoreNonBaseTemplates = info.IgnoreNonBaseTemplates;
			debug = info.Debug;
			if (!enabled)
				return;
			if (info.Styles == null)
				return;

			foreach (var style in info.Styles)
			{
				var built = new AutoTileStyle(style.Key, style.Value);
				foreach (var templateId in built.TemplateIds)
				{
					if (styleByTemplate.ContainsKey(templateId))
						throw new YamlException($"AutoTile template {templateId} is assigned to multiple styles.");

					styleByTemplate[templateId] = built;
				}

				AddEdgeMask(built.BaseTemplate, built.GroupId, AllEdges);

				foreach (var transition in style.Value.Transitions)
				{
					if (!groups.ContainsKey(transition.Key))
						continue;

					foreach (var templateId in GetTransitionTemplateIds(transition.Value))
						AddTransitionGroup(templateId, transition.Key);

					AddTransitionEdgeMasks(transition.Value, built.GroupId, transition.Key);
				}
			}
		}

		static IEnumerable<ushort> GetTransitionTemplateIds(AutoTileInfo.AutoTileTransitionsInfo transitions)
		{
			yield return transitions.N;
			yield return transitions.E;
			yield return transitions.S;
			yield return transitions.W;
			yield return transitions.NE;
			yield return transitions.NW;
			yield return transitions.SE;
			yield return transitions.SW;
			yield return transitions.WNE;
			yield return transitions.NES;
			yield return transitions.SWN;
			yield return transitions.ESW;
			yield return transitions.NESW;
			yield return transitions.Hole;
			yield return transitions.U_NW;
			yield return transitions.U_NE;
			yield return transitions.U_SE;
			yield return transitions.U_SW;
			yield return transitions.Parallel_NE_SW;
			yield return transitions.Parallel_NW_SE;
			yield return transitions.End_NW;
			yield return transitions.End_NE;
			yield return transitions.End_SE;
			yield return transitions.End_SW;
		}

		void AddTransitionGroup(ushort templateId, string groupId)
		{
			if (templateId == 0)
				return;

			if (!transitionGroupsByTemplate.TryGetValue(templateId, out var groupsForTemplate))
			{
				groupsForTemplate = new HashSet<string>();
				transitionGroupsByTemplate[templateId] = groupsForTemplate;
			}

			groupsForTemplate.Add(groupId);
		}

		void AddEdgeMask(ushort templateId, string groupId, byte mask, bool intersect = false)
		{
			if (templateId == 0)
				return;

			if (!edgeMasksByTemplate.TryGetValue(templateId, out var masksByGroup))
			{
				masksByGroup = new Dictionary<string, byte>();
				edgeMasksByTemplate[templateId] = masksByGroup;
			}

			if (!masksByGroup.TryGetValue(groupId, out var existing))
				masksByGroup[groupId] = mask;
			else
				masksByGroup[groupId] = intersect ? (byte)(existing & mask) : (byte)(existing | mask);
		}

		void AddTransitionEdgeMasks(AutoTileInfo.AutoTileTransitionsInfo transitions, string baseGroupId, string neighborGroupId)
		{
			void AddTransition(ushort templateId, byte mask)
			{
				if (templateId == 0)
					return;

				AddEdgeMask(templateId, neighborGroupId, mask);
				AddEdgeMask(templateId, baseGroupId, (byte)(AllEdges & ~mask), intersect: true);
			}

			AddTransition(transitions.N, AutoTileMasks.N);
			AddTransition(transitions.E, AutoTileMasks.E);
			AddTransition(transitions.S, AutoTileMasks.S);
			AddTransition(transitions.W, AutoTileMasks.W);
			AddTransition(transitions.NE, (byte)(AutoTileMasks.N | AutoTileMasks.E));
			AddTransition(transitions.NW, (byte)(AutoTileMasks.N | AutoTileMasks.W));
			AddTransition(transitions.SE, (byte)(AutoTileMasks.S | AutoTileMasks.E));
			AddTransition(transitions.SW, (byte)(AutoTileMasks.S | AutoTileMasks.W));
			AddTransition(transitions.WNE, (byte)(AutoTileMasks.W | AutoTileMasks.N | AutoTileMasks.E));
			AddTransition(transitions.NES, (byte)(AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.S));
			AddTransition(transitions.SWN, (byte)(AutoTileMasks.S | AutoTileMasks.W | AutoTileMasks.N));
			AddTransition(transitions.ESW, (byte)(AutoTileMasks.E | AutoTileMasks.S | AutoTileMasks.W));
			AddTransition(transitions.NESW, AllEdges);
			AddTransition(transitions.Hole, AllEdges);
			AddTransition(transitions.U_NW, AllEdges);
			AddTransition(transitions.U_NE, AllEdges);
			AddTransition(transitions.U_SE, AllEdges);
			AddTransition(transitions.U_SW, AllEdges);
			AddTransition(transitions.Parallel_NE_SW, AllEdges);
			AddTransition(transitions.Parallel_NW_SE, AllEdges);
			AddTransition(transitions.End_NW, (byte)(AutoTileMasks.N | AutoTileMasks.W));
			AddTransition(transitions.End_NE, (byte)(AutoTileMasks.N | AutoTileMasks.E));
			AddTransition(transitions.End_SE, (byte)(AutoTileMasks.S | AutoTileMasks.E));
			AddTransition(transitions.End_SW, (byte)(AutoTileMasks.S | AutoTileMasks.W));
		}

		bool HasNeighborGroupEdge(
			Map map,
			CPos n,
			AutoTileStyle style,
			AutoTileInfo.AutoTileGroupInfo neighborGroup,
			int groupPriority,
			string neighborGroupId,
			byte requiredMask,
			bool allowTransitions)
		{
			if (!map.Contains(n))
				return false;

			var templateId = map.Tiles[n].Type;
			if (!styleByTemplate.TryGetValue(templateId, out var neighborStyle))
				return false;

			if (!allowTransitions && ignoreTransitionNeighbors && templateId != neighborStyle.BaseTemplate)
				return false;

			byte mask = 0;
			var hasMask = edgeMasksByTemplate.TryGetValue(map.Tiles[n].Type, out var masksByGroup)
				&& masksByGroup.TryGetValue(neighborGroupId, out mask);
			if (hasMask)
			{
				if (requiredMask != 0 && (mask & requiredMask) != requiredMask)
					return false;
			}
			else
			{
				if (neighborStyle.GroupId != neighborGroupId)
					return false;
			}

			if (allowTransitions && hasMask && templateId != neighborStyle.BaseTemplate)
			{
				if (!HasTransitionAnchor(map, n, neighborGroupId, mask))
					return false;
			}

			if (IsNormalPriority(neighborGroup.Priority, groupPriority))
				return true;

			return allowEnclosedTransitions && IsOppositePriority(neighborGroup.Priority, groupPriority)
				&& IsEnclosedByGroup(map, n, style.GroupId);
		}

		bool HasTransitionAnchor(Map map, CPos cell, string groupId, byte mask)
		{
			if (mask == 0)
				return false;

			if ((mask & AutoTileMasks.N) != 0 && IsBaseTemplateInGroup(map, cell + new CVec(0, -1), groupId))
				return true;
			if ((mask & AutoTileMasks.E) != 0 && IsBaseTemplateInGroup(map, cell + new CVec(1, 0), groupId))
				return true;
			if ((mask & AutoTileMasks.S) != 0 && IsBaseTemplateInGroup(map, cell + new CVec(0, 1), groupId))
				return true;
			if ((mask & AutoTileMasks.W) != 0 && IsBaseTemplateInGroup(map, cell + new CVec(-1, 0), groupId))
				return true;

			return false;
		}

		bool IsBaseTemplateInGroup(Map map, CPos cell, string groupId)
		{
			if (!map.Contains(cell))
				return false;

			var templateId = map.Tiles[cell].Type;
			if (!styleByTemplate.TryGetValue(templateId, out var style))
				return false;

			return style.GroupId == groupId && templateId == style.BaseTemplate;
		}

		bool IsNeighborGroupTile(
			Map map,
			CPos n,
			AutoTileStyle style,
			AutoTileInfo.AutoTileGroupInfo neighborGroup,
			int groupPriority,
			string neighborGroupId,
			bool allowTransitions)
		{
			if (!map.Contains(n))
				return false;

			var templateId = map.Tiles[n].Type;
			if (!styleByTemplate.TryGetValue(templateId, out var neighborStyle))
				return false;

			if (!allowTransitions && ignoreTransitionNeighbors && templateId != neighborStyle.BaseTemplate)
				return false;

			var inGroup = allowTransitions
				? IsTemplateInGroupWithTransitions(templateId, neighborStyle, neighborGroupId)
				: IsTemplateInGroup(templateId, neighborStyle, neighborGroupId);

			if (!inGroup)
				return false;

			if (IsNormalPriority(neighborGroup.Priority, groupPriority))
				return true;

			return allowEnclosedTransitions && IsOppositePriority(neighborGroup.Priority, groupPriority)
				&& IsEnclosedByGroup(map, n, style.GroupId);
		}

		bool IsNeighborGroupTileAnchored(
			Map map,
			CPos n,
			AutoTileStyle style,
			AutoTileInfo.AutoTileGroupInfo neighborGroup,
			int groupPriority,
			string neighborGroupId,
			bool allowTransitions)
		{
			if (!IsNeighborGroupTile(map, n, style, neighborGroup, groupPriority, neighborGroupId, allowTransitions))
				return false;

			if (!allowTransitions)
				return true;

			var templateId = map.Tiles[n].Type;
			if (!styleByTemplate.TryGetValue(templateId, out var neighborStyle))
				return false;

			if (templateId == neighborStyle.BaseTemplate)
				return true;

			if (!edgeMasksByTemplate.TryGetValue(templateId, out var masksByGroup)
				|| !masksByGroup.TryGetValue(neighborGroupId, out var mask))
				return true;

			return HasTransitionAnchor(map, n, neighborGroupId, mask);
		}

		bool IsTemplateInGroup(ushort templateId, AutoTileStyle templateStyle, string groupId)
		{
			return templateStyle.GroupId == groupId;
		}

		bool IsTemplateInGroupWithTransitions(ushort templateId, AutoTileStyle templateStyle, string groupId)
		{
			if (templateStyle.GroupId == groupId)
				return true;

			return transitionGroupsByTemplate.TryGetValue(templateId, out var groups) && groups.Contains(groupId);
		}

		public bool IsAutoTileTemplate(ushort templateId) => styleByTemplate.ContainsKey(templateId);

		public bool IsAutoTileBaseTemplate(ushort templateId)
		{
			return styleByTemplate.TryGetValue(templateId, out var style)
				&& style.BaseTemplate == templateId;
		}

		public ushort ResolveTemplate(Map map, CPos cell)
		{
			if (!map.Contains(cell))
				return map.Tiles[cell].Type;

			if (!styleByTemplate.TryGetValue(map.Tiles[cell].Type, out var style))
				return map.Tiles[cell].Type;

			if (ignoreNonBaseTemplates && map.Tiles[cell].Type != style.BaseTemplate)
				return map.Tiles[cell].Type;

			if (!groups.TryGetValue(style.GroupId, out var group))
				return style.BaseTemplate;

			if (!TryFindNeighborGroup(map, cell, style, group.Priority, out var neighborGroupId, out var neighborStyleId))
			{
				if (debug)
					Log.Write("debug", $"AutoTile {cell}: style={style.Id} group={style.GroupId} no neighbor group");
				return style.BaseTemplate;
			}

			if (!TryResolveTransitions(style, neighborGroupId, neighborStyleId, out var transitions))
			{
				if (debug)
					Log.Write("debug", $"AutoTile {cell}: style={style.Id} neighborGroup={neighborGroupId} neighborStyle={neighborStyleId} no transitions");
				return style.BaseTemplate;
			}

			if (TryResolveSpecialTemplate(map, cell, style, group.Priority, neighborGroupId, transitions, out var specialTemplateId))
			{
				if (debug)
					Log.Write("debug", $"AutoTile {cell}: style={style.Id} neighborGroup={neighborGroupId} neighborStyle={neighborStyleId} special -> {specialTemplateId}");
				return specialTemplateId;
			}

			var mask = BuildMask(map, cell, style, group.Priority, neighborGroupId);
			if (mask == 0)
			{
				if (debug)
					Log.Write("debug", $"AutoTile {cell}: style={style.Id} neighborGroup={neighborGroupId} mask=0");
				return style.BaseTemplate;
			}

			if (allowEnclosedTransitions && mask == (AutoTileMasks.N | AutoTileMasks.E | AutoTileMasks.S | AutoTileMasks.W))
			{
				if (groups.TryGetValue(neighborGroupId, out var neighborGroup)
					&& IsNormalPriority(neighborGroup.Priority, group.Priority)
					&& IsEnclosedByGroup(map, cell, neighborGroupId))
					return style.BaseTemplate;
			}

			if (transitions.TryGetTemplateForMask(mask, out var templateId))
			{
				if (debug)
					Log.Write("debug", $"AutoTile {cell}: style={style.Id} neighborGroup={neighborGroupId} neighborStyle={neighborStyleId} mask={mask} -> {templateId}");
				return templateId;
			}

			if (debug)
				Log.Write("debug", $"AutoTile {cell}: style={style.Id} neighborGroup={neighborGroupId} neighborStyle={neighborStyleId} mask={mask} no template");
			return style.BaseTemplate;
		}

		bool TryResolveTransitions(AutoTileStyle style, string neighborGroupId, string neighborStyleId, out AutoTileInfo.AutoTileTransitionsInfo transitions)
		{
			if (neighborStyleId != null && style.Transitions.TryGetValue(neighborStyleId, out transitions))
				return true;

			return style.Transitions.TryGetValue(neighborGroupId, out transitions);
		}

		bool TryResolveSpecialTemplate(
			Map map,
			CPos cell,
			AutoTileStyle style,
			int groupPriority,
			string neighborGroupId,
			AutoTileInfo.AutoTileTransitionsInfo transitions,
			out ushort templateId)
		{
			templateId = 0;
			if (!groups.TryGetValue(neighborGroupId, out var neighborGroup))
				return false;

			const bool allowTransitions = true;
			bool Edge(CPos n, byte requiredMask)
			{
				return HasNeighborGroupEdge(map, n, style, neighborGroup, groupPriority, neighborGroupId, requiredMask, allowTransitions);
			}

			var nT = IsNeighborGroupTileAnchored(map, cell + new CVec(0, -1), style, neighborGroup, groupPriority, neighborGroupId, allowTransitions);
			var eT = IsNeighborGroupTileAnchored(map, cell + new CVec(1, 0), style, neighborGroup, groupPriority, neighborGroupId, allowTransitions);
			var sT = IsNeighborGroupTileAnchored(map, cell + new CVec(0, 1), style, neighborGroup, groupPriority, neighborGroupId, allowTransitions);
			var wT = IsNeighborGroupTileAnchored(map, cell + new CVec(-1, 0), style, neighborGroup, groupPriority, neighborGroupId, allowTransitions);
			var neT = Edge(cell + new CVec(1, -1), AutoTileMasks.N | AutoTileMasks.E);
			var nwT = Edge(cell + new CVec(-1, -1), AutoTileMasks.N | AutoTileMasks.W);
			var seT = Edge(cell + new CVec(1, 1), AutoTileMasks.S | AutoTileMasks.E);
			var swT = Edge(cell + new CVec(-1, 1), AutoTileMasks.S | AutoTileMasks.W);

			var allOrth = nT && eT && sT && wT;

			if (transitions.Hole != 0 && allOrth && neT && nwT && seT && swT)
			{
				templateId = transitions.Hole;
				return true;
			}

			if (transitions.WNE != 0 && nT && wT && eT && !sT && nwT && neT && swT && !seT)
			{
				templateId = transitions.WNE;
				return true;
			}

			if (transitions.NES != 0 && nT && eT && !wT && !sT && neT && nwT && seT && !swT)
			{
				templateId = transitions.NES;
				return true;
			}

			if (transitions.SWN != 0 && sT && wT && !nT && !eT && swT && nwT && seT && !neT)
			{
				templateId = transitions.SWN;
				return true;
			}

			if (transitions.ESW != 0 && sT && eT && !nT && !wT && seT && neT && swT && !nwT)
			{
				templateId = transitions.ESW;
				return true;
			}

			if (transitions.End_NW != 0 && nT && wT && !eT && !sT && !nwT)
			{
				templateId = transitions.End_NW;
				return true;
			}

			if (transitions.End_NE != 0 && nT && eT && !sT && !wT && !neT)
			{
				templateId = transitions.End_NE;
				return true;
			}

			if (transitions.End_SE != 0 && sT && eT && !nT && !wT && !seT)
			{
				templateId = transitions.End_SE;
				return true;
			}

			if (transitions.End_SW != 0 && sT && wT && !nT && !eT && !swT)
			{
				templateId = transitions.End_SW;
				return true;
			}

			if (allOrth)
			{
				if (transitions.U_NW != 0 && !nwT && neT && seT && swT)
				{
					templateId = transitions.U_NW;
					return true;
				}

				if (transitions.U_NE != 0 && !neT && nwT && seT && swT)
				{
					templateId = transitions.U_NE;
					return true;
				}

				if (transitions.U_SE != 0 && !seT && neT && nwT && swT)
				{
					templateId = transitions.U_SE;
					return true;
				}

				if (transitions.U_SW != 0 && !swT && neT && nwT && seT)
				{
					templateId = transitions.U_SW;
					return true;
				}

				if (transitions.Parallel_NE_SW != 0 && neT && swT && !nwT && !seT)
				{
					templateId = transitions.Parallel_NE_SW;
					return true;
				}

				if (transitions.Parallel_NW_SE != 0 && nwT && seT && !neT && !swT)
				{
					templateId = transitions.Parallel_NW_SE;
					return true;
				}
			}

			return false;
		}

		bool TryFindNeighborGroup(Map map, CPos cell, AutoTileStyle style, int groupPriority, out string neighborGroupId, out string neighborStyleId)
		{
			neighborGroupId = null;
			neighborStyleId = null;
			var selectedPriority = int.MinValue;
			string selectedGroupId = null;
			string selectedStyleId = null;

			void ConsiderNeighbor(CPos n, byte requiredMask)
			{
				if (!map.Contains(n))
					return;

				var templateId = map.Tiles[n].Type;
				if (!styleByTemplate.TryGetValue(templateId, out var neighborStyle))
					return;

				var candidateGroups = new List<string> { neighborStyle.GroupId };
				if (transitionGroupsByTemplate.TryGetValue(templateId, out var transitionGroups))
					candidateGroups.AddRange(transitionGroups);

				foreach (var candidateGroupId in candidateGroups)
				{
					if (!groups.TryGetValue(candidateGroupId, out var neighborGroup))
						continue;

					if (allowEnclosedTransitions && !HasTransitionsFor(style, candidateGroupId, neighborStyle.Id))
						continue;

					if (!HasNeighborGroupEdge(map, n, style, neighborGroup, groupPriority, candidateGroupId, requiredMask, allowTransitions: true))
						continue;

					if (neighborGroup.Priority <= selectedPriority)
						continue;

					selectedPriority = neighborGroup.Priority;
					selectedGroupId = candidateGroupId;
					selectedStyleId = neighborStyle.Id;
				}
			}

			foreach (var dir in GetNeighborDirs(map, cell))
			{
				byte requiredMask = dir.mask switch
				{
					AutoTileMasks.N => AutoTileMasks.S,
					AutoTileMasks.E => AutoTileMasks.W,
					AutoTileMasks.S => AutoTileMasks.N,
					AutoTileMasks.W => AutoTileMasks.E,
					_ => 0
				};

				ConsiderNeighbor(cell + dir.offset, requiredMask);
			}

			if (includeDiagonals)
			{
				foreach (var dir in GetDiagonalDirs(map, cell))
					ConsiderNeighbor(cell + dir.offset, dir.mask);
			}

			neighborGroupId = selectedGroupId;
			neighborStyleId = selectedStyleId;
			return neighborGroupId != null;
		}

		byte BuildMask(Map map, CPos cell, AutoTileStyle style, int groupPriority, string neighborGroupId)
		{
			byte mask = 0;

			if (!groups.TryGetValue(neighborGroupId, out var neighborGroup))
				return mask;

			const bool allowTransitions = true;
			foreach (var dir in GetNeighborDirs(map, cell))
			{
				var n = cell + dir.offset;
				byte requiredMask = dir.mask switch
				{
					AutoTileMasks.N => AutoTileMasks.S,
					AutoTileMasks.E => AutoTileMasks.W,
					AutoTileMasks.S => AutoTileMasks.N,
					AutoTileMasks.W => AutoTileMasks.E,
					_ => 0
				};

				if (HasNeighborGroupEdge(map, n, style, neighborGroup, groupPriority, neighborGroupId, requiredMask, allowTransitions)
					|| IsNeighborGroupTileAnchored(map, n, style, neighborGroup, groupPriority, neighborGroupId, allowTransitions))
					mask |= dir.mask;
			}

			if (includeDiagonals)
			{
				foreach (var dir in GetDiagonalDirs(map, cell))
				{
					if ((mask & dir.mask) != 0)
						continue;

					var n = cell + dir.offset;
					if (HasNeighborGroupEdge(map, n, style, neighborGroup, groupPriority, neighborGroupId, dir.mask, allowTransitions))
						mask |= dir.mask;
				}
			}

			return ApplyMaskDirectionOverrides(mask);
		}

		bool HasTransitionsFor(AutoTileStyle style, string neighborGroupId, string neighborStyleId)
		{
			if (neighborStyleId != null && style.Transitions.ContainsKey(neighborStyleId))
				return true;

			return style.Transitions.ContainsKey(neighborGroupId);
		}

		bool IsEnclosedByGroup(Map map, CPos cell, string enclosingGroupId)
		{
			foreach (var dir in GetNeighborDirs(map, cell))
			{
				var n = cell + dir.offset;
				if (!map.Contains(n))
					return false;

				if (!styleByTemplate.TryGetValue(map.Tiles[n].Type, out var neighborStyle))
					return false;

				if (neighborStyle.GroupId != enclosingGroupId)
					return false;
			}

			return true;
		}

		bool IsNormalPriority(int neighborPriority, int groupPriority)
		{
			return useHigherPriority ? neighborPriority > groupPriority : neighborPriority < groupPriority;
		}

		bool IsOppositePriority(int neighborPriority, int groupPriority)
		{
			return useHigherPriority ? neighborPriority < groupPriority : neighborPriority > groupPriority;
		}

		byte ApplyMaskDirectionOverrides(byte mask)
		{
			if (!invertMask)
				return mask;

			byte inverted = 0;
			if ((mask & AutoTileMasks.N) != 0)
				inverted |= AutoTileMasks.S;
			if ((mask & AutoTileMasks.S) != 0)
				inverted |= AutoTileMasks.N;
			if ((mask & AutoTileMasks.E) != 0)
				inverted |= AutoTileMasks.W;
			if ((mask & AutoTileMasks.W) != 0)
				inverted |= AutoTileMasks.E;

			return inverted;
		}
	}
}
