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
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	static class TerrainTemplateIndexHelper
	{
		public static bool TryGetIndex(TerrainTemplateInfo template, out byte index)
		{
			index = 0;
			if (template == null || template.TilesCount <= 0)
				return false;

			if (template.PickAny)
			{
				index = (byte)Game.CosmeticRandom.Next(0, template.TilesCount);
				return true;
			}

			for (var i = 0; i < template.TilesCount; i++)
			{
				if (template.Contains(i) && template[i] != null)
				{
					index = (byte)i;
					return true;
				}
			}

			return false;
		}
	}

	public sealed class EditorTileBrush : IEditorBrush
	{
		public readonly TerrainTemplateInfo TerrainTemplate;
		public readonly ushort Template;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActionManager editorActionManager;
		readonly IAutoTile autoTile;
		readonly HashSet<CPos> strokeAutoTileSeeds = new();

		bool painting;

		readonly ITiledTerrainRenderer terrainRenderer;

		CPos cell;
		readonly List<IRenderable> preview = new();

		public EditorTileBrush(EditorViewportControllerWidget editorWidget, ushort id, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException("EditorTileBrush can only be used with template-based tilesets");

			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			terrainRenderer = world.WorldActor.Trait<ITiledTerrainRenderer>();
			autoTile = world.WorldActor.TraitOrDefault<IAutoTile>();

			Template = id;
			TerrainTemplate = terrainInfo.Templates.First(t => t.Value.Id == id).Value;
			cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(Viewport.LastMousePos));
			UpdatePreview();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left)
			{
				if (mi.Event == MouseInputEvent.Down)
				{
					painting = true;
					strokeAutoTileSeeds.Clear();
				}
				else if (mi.Event == MouseInputEvent.Up)
				{
					painting = false;
					FinalizeStrokeAutoTile();
				}
			}

			if (!painting)
				return true;

			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Move)
				return true;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			var isMoving = mi.Event == MouseInputEvent.Move;

			if (mi.Modifiers.HasModifier(Modifiers.Shift))
			{
				FloodFillWithBrush(cell);
				painting = false;
				strokeAutoTileSeeds.Clear();
			}
			else
				PaintCell(cell, isMoving);

			return true;
		}

		void PaintCell(CPos cell, bool isMoving)
		{
			var template = terrainInfo.Templates[Template];
			if (isMoving && PlacementOverlapsSameTemplate(template, cell))
				return;

			// Only base templates should trigger autotile resolution.
			// Non-base transition templates must be paintable directly for reference/test layouts.
			if (autoTile != null && autoTile.IsAutoTileTemplate(Template) && autoTile.IsAutoTileBaseTemplate(Template))
			{
				editorActionManager.Add(new AutoTileEditorAction(Template, world.Map, cell, autoTile));
				strokeAutoTileSeeds.Add(cell);
			}
			else
				editorActionManager.Add(new PaintTileEditorAction(Template, world.Map, cell));
		}

		void FloodFillWithBrush(CPos cell)
		{
			var map = world.Map;
			if (!map.Contains(cell))
				return;

			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];

			if (replace.Type == Template)
				return;

			// Flood-fill should also only autotile when placing base templates.
			if (autoTile != null && autoTile.IsAutoTileTemplate(Template) && autoTile.IsAutoTileBaseTemplate(Template))
				editorActionManager.Add(new AutoTileFloodFillEditorAction(Template, map, cell, autoTile));
			else
				editorActionManager.Add(new FloodFillEditorAction(Template, map, cell));
		}

		void FinalizeStrokeAutoTile()
		{
			if (autoTile == null || strokeAutoTileSeeds.Count == 0)
				return;

			editorActionManager.Add(new AutoTileFinalizeEditorAction(world.Map, strokeAutoTileSeeds, autoTile));
			strokeAutoTileSeeds.Clear();
		}

		bool PlacementOverlapsSameTemplate(TerrainTemplateInfo template, CPos cell)
		{
			var map = world.Map;
			var mapTiles = map.Tiles;
			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++, i++)
				{
					if (template.Contains(i) && template[i] != null)
					{
						var c = cell + new CVec(x, y);
						if (mapTiles.Contains(c) && mapTiles[c].Type == template.Id)
							return true;
					}
				}
			}

			return false;
		}

		void UpdatePreview()
		{
			var pos = world.Map.CenterOfCell(cell);

			preview.Clear();
			preview.AddRange(terrainRenderer.RenderPreview(worldRenderer, TerrainTemplate, pos));
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self)
		{
			var currentCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (cell != currentCell)
			{
				cell = currentCell;
				UpdatePreview();
			}
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { return preview; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class PaintTileEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string AddedTile = "notification-added-tile";

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;

		readonly Queue<UndoTile> undoTiles = new();
		readonly TerrainTemplateInfo terrainTemplate;

		public PaintTileEditorAction(ushort template, Map map, CPos cell)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;

			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(AddedTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cell + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}
	}

	sealed class AutoTileEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string AddedTile = "notification-added-tile";

		public string Text { get; }

		readonly Map map;
		readonly CPos cell;
		readonly IAutoTile autoTile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly TerrainTemplateInfo terrainTemplate;

		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();
		readonly HashSet<CPos> autoTileCells = new();
		const int AutoTileUpdateRadius = 3;
		const int AutoTileMaxPasses = 8;

		public AutoTileEditorAction(ushort template, Map map, CPos cell, IAutoTile autoTile)
		{
			this.map = map;
			this.cell = cell;
			this.autoTile = autoTile;

			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(AddedTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			PaintTemplate(cell, terrainTemplate);
			ApplyAutoTile();
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void PaintTemplate(CPos cellToPaint, TerrainTemplateInfo templateInfo)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;

			var i = 0;
			for (var y = 0; y < templateInfo.Size.Y; y++)
			{
				for (var x = 0; x < templateInfo.Size.X; x++, i++)
				{
					if (templateInfo.Contains(i) && templateInfo[i] != null)
					{
						var index = templateInfo.PickAny ? (byte)Game.CosmeticRandom.Next(0, templateInfo.TilesCount) : (byte)i;
						var c = cellToPaint + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						RememberUndo(c, mapTiles[c], mapHeight[c]);

						mapTiles[c] = new TerrainTile(templateInfo.Id, index);
						mapHeight[c] = (byte)(baseHeight + templateInfo[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);

						AddAutoTileCells(c);
					}
				}
			}
		}

		void ApplyAutoTile()
		{
			if (autoTile == null || autoTileCells.Count == 0)
				return;

			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var totalUpdates = 0;
			for (var pass = 0; pass < AutoTileMaxPasses; pass++)
			{
				var normalizedTiles = NormalizeAutoTileCells(mapTiles);
				var updates = new List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)>();
				try
				{
					// Resolve only within the local neighborhood touched by this brush action.
					foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
					{
						var current = normalizedTiles.TryGetValue(c, out var originalTile) ? originalTile : mapTiles[c];
						if (!autoTile.IsAutoTileTemplate(current.Type))
							continue;

						var resolvedTemplateId = autoTile.ResolveTemplate(map, c);
						if (resolvedTemplateId == current.Type)
							continue;

						var resolvedTemplate = terrainInfo.Templates[resolvedTemplateId];
						if (resolvedTemplate.Size.X != 1 || resolvedTemplate.Size.Y != 1)
							continue;

						updates.Add((c, current, resolvedTemplateId));
					}
				}
				finally
				{
					RestoreNormalizedAutoTileCells(mapTiles, normalizedTiles);
				}

				if (updates.Count == 0)
					break;

				totalUpdates += updates.Count;
				LogAutoTilePass("AutoTileEditorAction", pass, updates);

				foreach (var update in updates)
				{
					var c = update.Cell;
					var current = update.Current;
					var resolvedTemplate = terrainInfo.Templates[update.ResolvedTemplateId];

					if (!TerrainTemplateIndexHelper.TryGetIndex(resolvedTemplate, out var index))
						continue;
					RememberUndo(c, current, mapHeight[c]);

					mapTiles[c] = new TerrainTile(update.ResolvedTemplateId, index);

					var baseHeight = mapHeight.Contains(c) ? mapHeight[c] : (byte)0;
					mapHeight[c] = (byte)(baseHeight + resolvedTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					AddAutoTileCells(c);
				}
			}

			Log.Write("debug", $"AutoTileEditorAction seed={cell} touched={autoTileCells.Count} updates={totalUpdates}");
		}

		static void LogAutoTilePass(string label, int pass, List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)> updates)
		{
			var sample = string.Join(", ", updates.Take(12).Select(u => $"({u.Cell.X},{u.Cell.Y}) {u.Current.Type}->{u.ResolvedTemplateId}"));
			Log.Write("debug", $"{label} pass={pass} updates={updates.Count} sample=[{sample}]");
		}

		Dictionary<CPos, TerrainTile> NormalizeAutoTileCells(CellLayer<TerrainTile> mapTiles)
		{
			var normalizedTiles = new Dictionary<CPos, TerrainTile>();
			foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
			{
				var current = mapTiles[c];
				if (!autoTile.IsAutoTileTemplate(current.Type) || autoTile.IsAutoTileBaseTemplate(current.Type))
					continue;

				var baseTemplateId = autoTile.GetAutoTileBaseTemplate(current.Type);
				if (baseTemplateId == current.Type)
					continue;

				var baseTemplate = terrainInfo.Templates[baseTemplateId];
				if (baseTemplate.Size.X != 1 || baseTemplate.Size.Y != 1)
					continue;

				normalizedTiles[c] = current;
				mapTiles[c] = new TerrainTile(baseTemplateId, current.Index);
			}

			return normalizedTiles;
		}

		static void RestoreNormalizedAutoTileCells(CellLayer<TerrainTile> mapTiles, Dictionary<CPos, TerrainTile> normalizedTiles)
		{
			foreach (var normalizedTile in normalizedTiles)
				mapTiles[normalizedTile.Key] = normalizedTile.Value;
		}

		void AddAutoTileCells(CPos cellToAdd)
		{
			foreach (var offset in GetAutoTileOffsets(map, cellToAdd))
				autoTileCells.Add(cellToAdd + offset);
		}

		static IEnumerable<CVec> GetAutoTileOffsets(Map map, CPos cell)
		{
			var offsets = new List<CVec>();
			if (map.Grid.Type == MapGridType.Rectangular || map.Grid.Type == MapGridType.RectangularIsometric)
			{
				for (var dy = -AutoTileUpdateRadius; dy <= AutoTileUpdateRadius; dy++)
					for (var dx = -AutoTileUpdateRadius; dx <= AutoTileUpdateRadius; dx++)
						offsets.Add(new CVec(dx, dy));
				return offsets;
			}

			var uv = cell.ToMPos(map.Grid.Type);
			for (var dv = -AutoTileUpdateRadius; dv <= AutoTileUpdateRadius; dv++)
			{
				for (var du = -AutoTileUpdateRadius; du <= AutoTileUpdateRadius; du++)
				{
					var n = new MPos(uv.U + du, uv.V + dv).ToCPos(map.Grid.Type);
					offsets.Add(n - cell);
				}
			}

			return offsets;
		}

		void RememberUndo(CPos cellToRemember, TerrainTile tile, byte height)
		{
			if (!undoCells.Add(cellToRemember))
				return;

			undoTiles.Enqueue(new UndoTile(cellToRemember, tile, height));
		}
	}

	sealed class AutoTileFinalizeEditorAction : IEditorAction
	{
		public string Text => "Retile";

		readonly Map map;
		readonly IAutoTile autoTile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly HashSet<CPos> autoTileCells = new();
		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();

		const int AutoTileUpdateRadius = 3;
		const int AutoTileMaxPasses = 8;
		const int AutoTileFinalizePadding = 8;

		public AutoTileFinalizeEditorAction(Map map, IEnumerable<CPos> seeds, IAutoTile autoTile)
		{
			this.map = map;
			this.autoTile = autoTile;
			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;

			var strokeSeeds = seeds.ToList();
			foreach (var seed in strokeSeeds)
				AddAutoTileCells(seed);

			AddAutoTileBoundingRegion(strokeSeeds);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			ApplyAutoTile();
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();
				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void ApplyAutoTile()
		{
			if (autoTile == null || autoTileCells.Count == 0)
				return;

			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var totalUpdates = 0;
			for (var pass = 0; pass < AutoTileMaxPasses; pass++)
			{
				var normalizedTiles = NormalizeAutoTileCells(mapTiles);
				var updates = new List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)>();
				try
				{
					foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
					{
						var current = normalizedTiles.TryGetValue(c, out var originalTile) ? originalTile : mapTiles[c];
						if (!autoTile.IsAutoTileTemplate(current.Type))
							continue;

						var resolvedTemplateId = autoTile.ResolveTemplate(map, c);
						if (resolvedTemplateId == current.Type)
							continue;

						var resolvedTemplate = terrainInfo.Templates[resolvedTemplateId];
						if (resolvedTemplate.Size.X != 1 || resolvedTemplate.Size.Y != 1)
							continue;

						updates.Add((c, current, resolvedTemplateId));
					}
				}
				finally
				{
					RestoreNormalizedAutoTileCells(mapTiles, normalizedTiles);
				}

				if (updates.Count == 0)
					break;

				totalUpdates += updates.Count;
				LogAutoTilePass("AutoTileFinalizeEditorAction", pass, updates);

				foreach (var update in updates)
				{
					var c = update.Cell;
					var current = update.Current;
					var resolvedTemplate = terrainInfo.Templates[update.ResolvedTemplateId];
					if (!TerrainTemplateIndexHelper.TryGetIndex(resolvedTemplate, out var index))
						continue;

					RememberUndo(c, current, mapHeight[c]);

					mapTiles[c] = new TerrainTile(update.ResolvedTemplateId, index);

					var baseHeight = mapHeight.Contains(c) ? mapHeight[c] : (byte)0;
					mapHeight[c] = (byte)(baseHeight + resolvedTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					AddAutoTileCells(c);
				}
			}

			Log.Write("debug", $"AutoTileFinalizeEditorAction seeds={autoTileCells.Count} updates={totalUpdates}");
		}

		static void LogAutoTilePass(string label, int pass, List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)> updates)
		{
			var sample = string.Join(", ", updates.Take(12).Select(u => $"({u.Cell.X},{u.Cell.Y}) {u.Current.Type}->{u.ResolvedTemplateId}"));
			Log.Write("debug", $"{label} pass={pass} updates={updates.Count} sample=[{sample}]");
		}

		Dictionary<CPos, TerrainTile> NormalizeAutoTileCells(CellLayer<TerrainTile> mapTiles)
		{
			var normalizedTiles = new Dictionary<CPos, TerrainTile>();
			foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
			{
				var current = mapTiles[c];
				if (!autoTile.IsAutoTileTemplate(current.Type) || autoTile.IsAutoTileBaseTemplate(current.Type))
					continue;

				var baseTemplateId = autoTile.GetAutoTileBaseTemplate(current.Type);
				if (baseTemplateId == current.Type)
					continue;

				var baseTemplate = terrainInfo.Templates[baseTemplateId];
				if (baseTemplate.Size.X != 1 || baseTemplate.Size.Y != 1)
					continue;

				normalizedTiles[c] = current;
				mapTiles[c] = new TerrainTile(baseTemplateId, current.Index);
			}

			return normalizedTiles;
		}

		static void RestoreNormalizedAutoTileCells(CellLayer<TerrainTile> mapTiles, Dictionary<CPos, TerrainTile> normalizedTiles)
		{
			foreach (var normalizedTile in normalizedTiles)
				mapTiles[normalizedTile.Key] = normalizedTile.Value;
		}

		void AddAutoTileCells(CPos cellToAdd)
		{
			foreach (var offset in GetAutoTileOffsets(map, cellToAdd))
				autoTileCells.Add(cellToAdd + offset);
		}

		void AddAutoTileBoundingRegion(List<CPos> seeds)
		{
			if (seeds.Count == 0)
				return;

			var minX = seeds.Min(c => c.X) - AutoTileFinalizePadding;
			var maxX = seeds.Max(c => c.X) + AutoTileFinalizePadding;
			var minY = seeds.Min(c => c.Y) - AutoTileFinalizePadding;
			var maxY = seeds.Max(c => c.Y) + AutoTileFinalizePadding;

			for (var y = minY; y <= maxY; y++)
			{
				for (var x = minX; x <= maxX; x++)
				{
					var c = new CPos(x, y);
					if (map.Tiles.Contains(c))
						autoTileCells.Add(c);
				}
			}
		}

		static IEnumerable<CVec> GetAutoTileOffsets(Map map, CPos cell)
		{
			var offsets = new List<CVec>();
			if (map.Grid.Type == MapGridType.Rectangular || map.Grid.Type == MapGridType.RectangularIsometric)
			{
				for (var dy = -AutoTileUpdateRadius; dy <= AutoTileUpdateRadius; dy++)
					for (var dx = -AutoTileUpdateRadius; dx <= AutoTileUpdateRadius; dx++)
						offsets.Add(new CVec(dx, dy));
				return offsets;
			}

			var uv = cell.ToMPos(map.Grid.Type);
			for (var dv = -AutoTileUpdateRadius; dv <= AutoTileUpdateRadius; dv++)
			{
				for (var du = -AutoTileUpdateRadius; du <= AutoTileUpdateRadius; du++)
				{
					var n = new MPos(uv.U + du, uv.V + dv).ToCPos(map.Grid.Type);
					offsets.Add(n - cell);
				}
			}

			return offsets;
		}

		void RememberUndo(CPos cellToRemember, TerrainTile tile, byte height)
		{
			if (!undoCells.Add(cellToRemember))
				return;

			undoTiles.Enqueue(new UndoTile(cellToRemember, tile, height));
		}
	}

	sealed class AutoTileFloodFillEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string FilledTile = "notification-filled-tile";

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;
		readonly IAutoTile autoTile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly TerrainTemplateInfo terrainTemplate;

		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();
		readonly HashSet<CPos> autoTileCells = new();
		const int AutoTileUpdateRadius = 3;
		const int AutoTileMaxPasses = 8;

		public AutoTileFloodFillEditorAction(ushort template, Map map, CPos cell, IAutoTile autoTile)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;
			this.autoTile = autoTile;

			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(FilledTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var queue = new Queue<CPos>();
			var touched = new CellLayer<bool>(map);
			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];

			void MaybeEnqueue(CPos newCell)
			{
				if (map.Contains(cell) && !touched[newCell])
				{
					queue.Enqueue(newCell);
					touched[newCell] = true;
				}
			}

			bool ShouldPaint(CPos cellToCheck)
			{
				for (var y = 0; y < terrainTemplate.Size.Y; y++)
				{
					for (var x = 0; x < terrainTemplate.Size.X; x++)
					{
						var c = cellToCheck + new CVec(x, y);
						if (!map.Contains(c) || mapTiles[c].Type != replace.Type)
							return false;
					}
				}

				return true;
			}

			CPos FindEdge(CPos refCell, CVec direction)
			{
				while (true)
				{
					var newCell = refCell + direction;
					if (!ShouldPaint(newCell))
						return refCell;
					refCell = newCell;
				}
			}

			queue.Enqueue(cell);
			while (queue.Count > 0)
			{
				var queuedCell = queue.Dequeue();
				if (!ShouldPaint(queuedCell))
					continue;

				var previousCell = FindEdge(queuedCell, new CVec(-1 * terrainTemplate.Size.X, 0));
				var nextCell = FindEdge(queuedCell, new CVec(1 * terrainTemplate.Size.X, 0));

				for (var x = previousCell.X; x <= nextCell.X; x += terrainTemplate.Size.X)
				{
					PaintSingleCell(new CPos(x, queuedCell.Y));
					var upperCell = new CPos(x, queuedCell.Y - 1 * terrainTemplate.Size.Y);
					var lowerCell = new CPos(x, queuedCell.Y + 1 * terrainTemplate.Size.Y);

					if (ShouldPaint(upperCell))
						MaybeEnqueue(upperCell);
					if (ShouldPaint(lowerCell))
						MaybeEnqueue(lowerCell);
				}
			}

			ApplyAutoTile();
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void PaintSingleCell(CPos cellToPaint)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cellToPaint + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						RememberUndo(c, mapTiles[c], mapHeight[c]);

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);

						AddAutoTileCells(c);
					}
				}
			}
		}

		void ApplyAutoTile()
		{
			if (autoTile == null || autoTileCells.Count == 0)
				return;

			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var totalUpdates = 0;
			for (var pass = 0; pass < AutoTileMaxPasses; pass++)
			{
				var normalizedTiles = NormalizeAutoTileCells(mapTiles);
				var updates = new List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)>();
				try
				{
					// Resolve only within the local neighborhood touched by this brush action.
					foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
					{
						var current = normalizedTiles.TryGetValue(c, out var originalTile) ? originalTile : mapTiles[c];
						if (!autoTile.IsAutoTileTemplate(current.Type))
							continue;

						var resolvedTemplateId = autoTile.ResolveTemplate(map, c);
						if (resolvedTemplateId == current.Type)
							continue;

						var resolvedTemplate = terrainInfo.Templates[resolvedTemplateId];
						if (resolvedTemplate.Size.X != 1 || resolvedTemplate.Size.Y != 1)
							continue;

						updates.Add((c, current, resolvedTemplateId));
					}
				}
				finally
				{
					RestoreNormalizedAutoTileCells(mapTiles, normalizedTiles);
				}

				if (updates.Count == 0)
					break;

				totalUpdates += updates.Count;
				LogAutoTilePass("AutoTileFloodFillEditorAction", pass, updates);

				foreach (var update in updates)
				{
					var c = update.Cell;
					var current = update.Current;
					var resolvedTemplate = terrainInfo.Templates[update.ResolvedTemplateId];

					if (!TerrainTemplateIndexHelper.TryGetIndex(resolvedTemplate, out var index))
						continue;
					RememberUndo(c, current, mapHeight[c]);

					mapTiles[c] = new TerrainTile(update.ResolvedTemplateId, index);

					var baseHeight = mapHeight.Contains(c) ? mapHeight[c] : (byte)0;
					mapHeight[c] = (byte)(baseHeight + resolvedTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					AddAutoTileCells(c);
				}
			}

			Log.Write("debug", $"AutoTileFloodFillEditorAction touched={autoTileCells.Count} updates={totalUpdates}");
		}

		static void LogAutoTilePass(string label, int pass, List<(CPos Cell, TerrainTile Current, ushort ResolvedTemplateId)> updates)
		{
			var sample = string.Join(", ", updates.Take(12).Select(u => $"({u.Cell.X},{u.Cell.Y}) {u.Current.Type}->{u.ResolvedTemplateId}"));
			Log.Write("debug", $"{label} pass={pass} updates={updates.Count} sample=[{sample}]");
		}

		Dictionary<CPos, TerrainTile> NormalizeAutoTileCells(CellLayer<TerrainTile> mapTiles)
		{
			var normalizedTiles = new Dictionary<CPos, TerrainTile>();
			foreach (var c in autoTileCells.OrderBy(c => c.Y).ThenBy(c => c.X))
			{
				var current = mapTiles[c];
				if (!autoTile.IsAutoTileTemplate(current.Type) || autoTile.IsAutoTileBaseTemplate(current.Type))
					continue;

				var baseTemplateId = autoTile.GetAutoTileBaseTemplate(current.Type);
				if (baseTemplateId == current.Type)
					continue;

				var baseTemplate = terrainInfo.Templates[baseTemplateId];
				if (baseTemplate.Size.X != 1 || baseTemplate.Size.Y != 1)
					continue;

				normalizedTiles[c] = current;
				mapTiles[c] = new TerrainTile(baseTemplateId, current.Index);
			}

			return normalizedTiles;
		}

		static void RestoreNormalizedAutoTileCells(CellLayer<TerrainTile> mapTiles, Dictionary<CPos, TerrainTile> normalizedTiles)
		{
			foreach (var normalizedTile in normalizedTiles)
				mapTiles[normalizedTile.Key] = normalizedTile.Value;
		}

		void AddAutoTileCells(CPos cellToAdd)
		{
			foreach (var offset in GetAutoTileOffsets(map, cellToAdd))
				autoTileCells.Add(cellToAdd + offset);
		}

		static IEnumerable<CVec> GetAutoTileOffsets(Map map, CPos cell)
		{
			var offsets = new List<CVec>();
			if (map.Grid.Type == MapGridType.Rectangular || map.Grid.Type == MapGridType.RectangularIsometric)
			{
				for (var dy = -AutoTileUpdateRadius; dy <= AutoTileUpdateRadius; dy++)
					for (var dx = -AutoTileUpdateRadius; dx <= AutoTileUpdateRadius; dx++)
						offsets.Add(new CVec(dx, dy));
				return offsets;
			}

			var uv = cell.ToMPos(map.Grid.Type);
			for (var dv = -AutoTileUpdateRadius; dv <= AutoTileUpdateRadius; dv++)
			{
				for (var du = -AutoTileUpdateRadius; du <= AutoTileUpdateRadius; du++)
				{
					var n = new MPos(uv.U + du, uv.V + dv).ToCPos(map.Grid.Type);
					offsets.Add(n - cell);
				}
			}

			return offsets;
		}

		void RememberUndo(CPos cellToRemember, TerrainTile tile, byte height)
		{
			if (!undoCells.Add(cellToRemember))
				return;

			undoTiles.Enqueue(new UndoTile(cellToRemember, tile, height));
		}
	}

	sealed class FloodFillEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string FilledTile = "notification-filled-tile";

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;

		readonly Queue<UndoTile> undoTiles = new();
		readonly TerrainTemplateInfo terrainTemplate;

		public FloodFillEditorAction(ushort template, Map map, CPos cell)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;

			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(FilledTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var queue = new Queue<CPos>();
			var touched = new CellLayer<bool>(map);
			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];

			void MaybeEnqueue(CPos newCell)
			{
				if (map.Contains(cell) && !touched[newCell])
				{
					queue.Enqueue(newCell);
					touched[newCell] = true;
				}
			}

			bool ShouldPaint(CPos cellToCheck)
			{
				for (var y = 0; y < terrainTemplate.Size.Y; y++)
				{
					for (var x = 0; x < terrainTemplate.Size.X; x++)
					{
						var c = cellToCheck + new CVec(x, y);
						if (!map.Contains(c) || mapTiles[c].Type != replace.Type)
							return false;
					}
				}

				return true;
			}

			CPos FindEdge(CPos refCell, CVec direction)
			{
				while (true)
				{
					var newCell = refCell + direction;
					if (!ShouldPaint(newCell))
						return refCell;
					refCell = newCell;
				}
			}

			queue.Enqueue(cell);
			while (queue.Count > 0)
			{
				var queuedCell = queue.Dequeue();
				if (!ShouldPaint(queuedCell))
					continue;

				var previousCell = FindEdge(queuedCell, new CVec(-1 * terrainTemplate.Size.X, 0));
				var nextCell = FindEdge(queuedCell, new CVec(1 * terrainTemplate.Size.X, 0));

				for (var x = previousCell.X; x <= nextCell.X; x += terrainTemplate.Size.X)
				{
					PaintSingleCell(new CPos(x, queuedCell.Y));
					var upperCell = new CPos(x, queuedCell.Y - 1 * terrainTemplate.Size.Y);
					var lowerCell = new CPos(x, queuedCell.Y + 1 * terrainTemplate.Size.Y);

					if (ShouldPaint(upperCell))
						MaybeEnqueue(upperCell);
					if (ShouldPaint(lowerCell))
						MaybeEnqueue(lowerCell);
				}
			}
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void PaintSingleCell(CPos cellToPaint)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cellToPaint + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}
	}

	sealed class UndoTile
	{
		public CPos Cell { get; }
		public TerrainTile MapTile { get; }
		public byte Height { get; }

		public UndoTile(CPos cell, TerrainTile mapTile, byte height)
		{
			Cell = cell;
			MapTile = mapTile;
			Height = height;
		}
	}
}
