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
using OpenRA.Mods.Common.Graphics;
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
		const ushort RockCliffBrushTemplateId = 3990;
		const ushort WallCliffBrushTemplateId = 3991;
		const ushort HighCliffSwSelectorTemplateId = 11900;
		const ushort HighCliffSeSelectorTemplateId = 11910;
		const ushort HighCliffSwTemplateBaseId = 12000;
		const ushort HighCliffSeTemplateBaseId = 12020;
		const ushort HighCliffSOuterTemplateBaseId = 12040;
		const ushort HighCliffEOuterTemplateBaseId = 12080;
		const ushort HighCliffWOuterTemplateBaseId = 12100;
		const int HighCliffVariantCount = 9;

		static readonly CVec[] RockCliffBrushStampOffsets =
		{
			new(0, 0),
			new(1, 0),
			new(2, 0),
		};

		static readonly CVec[] WallCliffBrushStampOffsets =
		{
			new(-1, -1),
			new(0, 0),
			new(1, 1),
		};

		internal static readonly ushort[][] RockCliffBrushRoleTemplateIds =
		{
			new ushort[] { 4046, 4047, 4049, 4053, 4012, 4013, 4015, 4017, 4034, 4035, 4037, 4020, 4021, 4038, 4042, 4044 },
			new ushort[] { 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019, 4034, 4035, 4036, 4037, 4038, 4039, 4040, 4041, 4042, 4043, 4044, 4045 },
			new ushort[] { 4046, 4047, 4048, 4052, 4014, 4016, 4018, 4019, 4036, 4037, 4022, 4023, 4039, 4043, 4045 },
		};

		internal static readonly ushort[][] WallCliffBrushRightColumnTemplateIds =
		{
			new ushort[] { 6400, 6401, 6402 },
			new ushort[] { 6403, 6404, 6405 },
			new ushort[] { 6406, 6407, 6408 },
			new ushort[] { 6409, 6410, 6411 },
			new ushort[] { 6412, 6413, 6414 },
			new ushort[] { 6415, 6416, 6417 },
			new ushort[] { 6418, 6419, 6420 },
			new ushort[] { 6421, 6422, 6423 },
		};

		internal static readonly ushort[] WallCliffBrushRightEndColumnTemplateIds = { 6409, 6410, 6411 };

		internal static readonly ushort[][] WallCliffBrushLeftColumnTemplateIds =
		{
			new ushort[] { 6448, 6425, 6450 },
			new ushort[] { 6448, 6428, 6450 },
			new ushort[] { 6448, 6431, 6450 },
			new ushort[] { 6448, 6434, 6450 },
			new ushort[] { 6448, 6437, 6450 },
			new ushort[] { 6448, 6440, 6450 },
			new ushort[] { 6448, 6443, 6450 },
			new ushort[] { 6448, 6446, 6450 },
		};

		internal static readonly HashSet<ushort> RockCliffBrushAutotileTemplateIds = new(RockCliffBrushRoleTemplateIds.SelectMany(ids => ids));
		internal static readonly HashSet<ushort> WallCliffBrushAutotileTemplateIds =
			new(WallCliffBrushRightColumnTemplateIds.SelectMany(ids => ids).Concat(WallCliffBrushLeftColumnTemplateIds.SelectMany(ids => ids)));

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
		CPos? lastHeightToolCell;

		readonly ITiledTerrainRenderer terrainRenderer;
		readonly SpriteFont heightIndicatorFont;

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
<<<<<<< Updated upstream
			autoTile = world.WorldActor.TraitOrDefault<IAutoTile>();
=======
			heightIndicatorFont = Game.Renderer.Fonts["TinyBold"];
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
					strokeAutoTileSeeds.Clear();
=======
					lastHeightToolCell = null;
>>>>>>> Stashed changes
				}
				else if (mi.Event == MouseInputEvent.Up)
				{
					painting = false;
<<<<<<< Updated upstream
					FinalizeStrokeAutoTile();
=======
					lastHeightToolCell = null;
>>>>>>> Stashed changes
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
				if (HighCliffTerrainTools.IsHeightToolTemplate(Template))
					FloodFillHeightWithBrush(cell);
				else
					FloodFillWithBrush(cell);

				painting = false;
<<<<<<< Updated upstream
				strokeAutoTileSeeds.Clear();
=======
				lastHeightToolCell = null;
>>>>>>> Stashed changes
			}
			else
				PaintCell(cell, isMoving);

			return true;
		}

		void PaintCell(CPos cell, bool isMoving)
		{
			if (HighCliffTerrainTools.IsHeightToolTemplate(Template))
			{
				if (isMoving && lastHeightToolCell == cell)
					return;

				editorActionManager.Add(new AdjustTerrainHeightEditorAction(Template, world.Map, cell, false));
				lastHeightToolCell = cell;
				return;
			}

			var template = terrainInfo.Templates[Template];
			if (isMoving && PlacementOverlapsSameTemplate(template, cell))
				return;

<<<<<<< Updated upstream
			// Only base templates should trigger autotile resolution.
			// Non-base transition templates must be paintable directly for reference/test layouts.
			if (autoTile != null && autoTile.IsAutoTileTemplate(Template) && autoTile.IsAutoTileBaseTemplate(Template))
			{
				editorActionManager.Add(new AutoTileEditorAction(Template, world.Map, cell, autoTile));
				strokeAutoTileSeeds.Add(cell);
			}
=======
			if (Template == RockCliffBrushTemplateId)
				editorActionManager.Add(new PaintTileStampEditorAction(Template, world.Map, cell, RockCliffBrushStampOffsets, RockCliffBrushRoleTemplateIds, RockCliffBrushAutotileTemplateIds, CliffStampStyle.Rock));
			else if (Template == WallCliffBrushTemplateId)
				editorActionManager.Add(new PaintTileStampEditorAction(Template, world.Map, cell, WallCliffBrushStampOffsets, WallCliffBrushRightColumnTemplateIds, WallCliffBrushAutotileTemplateIds, CliffStampStyle.Wall));
>>>>>>> Stashed changes
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

		void FloodFillHeightWithBrush(CPos cell)
		{
			if (!world.Map.Contains(cell))
				return;

			editorActionManager.Add(new AdjustTerrainHeightEditorAction(Template, world.Map, cell, true));
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
			preview.Clear();
			if (Template == RockCliffBrushTemplateId)
			{
				for (var i = 0; i < RockCliffBrushStampOffsets.Length; i++)
				{
					var targetCell = cell + RockCliffBrushStampOffsets[i];
					if (!world.Map.Contains(targetCell))
						continue;

					var templateId = SelectBrushTemplate(targetCell, i, RockCliffBrushRoleTemplateIds);
					var template = terrainInfo.Templates[templateId];
					var targetPos = world.Map.CenterOfCell(targetCell);
					preview.AddRange(terrainRenderer.RenderPreview(worldRenderer, template, targetPos));
				}

				return;
			}

			if (Template == WallCliffBrushTemplateId)
			{
				var column = SelectWallCliffColumn(cell, 1);
				for (var i = 0; i < WallCliffBrushStampOffsets.Length; i++)
				{
					var targetCell = cell + WallCliffBrushStampOffsets[i];
					if (!world.Map.Contains(targetCell))
						continue;

					var templateId = column[i % column.Length];
					var template = terrainInfo.Templates[templateId];
					var targetPos = world.Map.CenterOfCell(targetCell);
					preview.AddRange(terrainRenderer.RenderPreview(worldRenderer, template, targetPos));
				}

				return;
			}

			var pos = world.Map.CenterOfCell(cell);
			preview.AddRange(terrainRenderer.RenderPreview(worldRenderer, TerrainTemplate, pos));
		}

		static ushort SelectBrushTemplate(CPos origin, int offsetIndex, IReadOnlyList<IReadOnlyList<ushort>> roleTemplateIds)
		{
			var selector = ((origin.X * 73856093) ^ (origin.Y * 19349663) ^ (offsetIndex * 83492791)) & int.MaxValue;
			var roleTemplates = roleTemplateIds[offsetIndex % roleTemplateIds.Count];
			return roleTemplates[selector % roleTemplates.Count];
		}

		static ushort[] SelectWallCliffColumn(CPos origin, int role)
		{
			if (role == 2)
				return WallCliffBrushRightEndColumnTemplateIds;

			var pools = role switch
			{
				0 => WallCliffBrushLeftColumnTemplateIds,
				_ => WallCliffBrushRightColumnTemplateIds,
			};
			var selector = ((origin.X * 73856093) ^ (origin.Y * 19349663) ^ (role * 83492791)) & int.MaxValue;
			return pools[selector % pools.Length];
		}

		internal static bool IsHighCliffBrushTemplate(ushort templateId)
		{
			return templateId == HighCliffSwSelectorTemplateId || templateId == HighCliffSeSelectorTemplateId;
		}

		internal static bool IsHighCliffTemplate(ushort templateId)
		{
			return IsHighCliffSwFaceTemplate(templateId) || IsHighCliffSeFaceTemplate(templateId) ||
				IsHighCliffSOuterTemplate(templateId) || IsHighCliffEOuterTemplate(templateId) || IsHighCliffWOuterTemplate(templateId);
		}

		internal static bool IsHighCliffSwFaceTemplate(ushort templateId)
		{
			return templateId >= HighCliffSwTemplateBaseId && templateId < HighCliffSwTemplateBaseId + HighCliffVariantCount;
		}

		internal static bool IsHighCliffSeFaceTemplate(ushort templateId)
		{
			return templateId >= HighCliffSeTemplateBaseId && templateId < HighCliffSeTemplateBaseId + HighCliffVariantCount;
		}

		internal static bool IsHighCliffSOuterTemplate(ushort templateId)
		{
			return templateId >= HighCliffSOuterTemplateBaseId && templateId < HighCliffSOuterTemplateBaseId + HighCliffVariantCount;
		}

		internal static bool IsHighCliffEOuterTemplate(ushort templateId)
		{
			return templateId >= HighCliffEOuterTemplateBaseId && templateId < HighCliffEOuterTemplateBaseId + HighCliffVariantCount;
		}

		internal static bool IsHighCliffWOuterTemplate(ushort templateId)
		{
			return templateId >= HighCliffWOuterTemplateBaseId && templateId < HighCliffWOuterTemplateBaseId + HighCliffVariantCount;
		}

		internal static ushort SelectHighCliffFaceTemplateId(CPos cell, HighCliffFacing facing)
		{
			return SelectHighCliffVariantTemplateId(facing == HighCliffFacing.SW ? HighCliffSwTemplateBaseId : HighCliffSeTemplateBaseId, cell, 0);
		}

		internal static ushort SelectHighCliffStartTemplateId(CPos cell, HighCliffFacing facing)
		{
			return SelectHighCliffVariantTemplateId(facing == HighCliffFacing.SW ? HighCliffWOuterTemplateBaseId : HighCliffEOuterTemplateBaseId, cell, 1);
		}

		internal static ushort SelectHighCliffEndTemplateId(CPos cell)
		{
			return SelectHighCliffVariantTemplateId(HighCliffSOuterTemplateBaseId, cell, 2);
		}

		static ushort SelectHighCliffVariantTemplateId(ushort baseTemplateId, CPos cell, int salt)
		{
			var selector = ((cell.X * 73856093) ^ (cell.Y * 19349663) ^ (salt * 83492791)) & int.MaxValue;
			return (ushort)(baseTemplateId + selector % HighCliffVariantCount);
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
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!HighCliffTerrainTools.IsHeightToolTemplate(Template))
				yield break;

			var map = wr.World.Map;
			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				if (!map.Height.Contains(uv))
					continue;

				var height = map.Height[uv];
				if (height == 0)
					continue;

				var center = map.CenterOfCell(uv.ToCPos(map));
				yield return new TextAnnotationRenderable(heightIndicatorFont, center + new WVec(0, 0, 1024), 0, Primitives.Color.White, height.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}
		}

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

<<<<<<< Updated upstream
	sealed class AutoTileEditorAction : IEditorAction
=======
	sealed class AdjustTerrainHeightEditorAction : IEditorAction
	{
		public string Text { get; }

		readonly Map map;
		readonly CPos cell;
		readonly byte targetHeight;
		readonly bool floodFill;
		readonly ushort clearTemplateId;
		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();

		public AdjustTerrainHeightEditorAction(ushort template, Map map, CPos cell, bool floodFill)
		{
			this.map = map;
			this.cell = cell;
			this.floodFill = floodFill;
			targetHeight = template == HighCliffTileIds.RaiseSelectorTemplateId ? (byte)1 : (byte)0;
			clearTemplateId = FindClearTemplateId((ITemplatedTerrainInfo)map.Rules.TerrainInfo);
			Text = targetHeight > 0 ? "Raise terrain" : "Lower terrain";
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var targetCells = floodFill ? HighCliffTerrainTools.CollectConnectedHeightRegion(map, cell) : HighCliffTerrainTools.CollectMinimumPlateauRegion(map, cell);
			var changedCells = HighCliffTerrainTools.GetCellsWithTargetHeight(map, targetCells, targetHeight);
			if (changedCells.Count == 0)
				return;

			var affectedCells = HighCliffTerrainTools.ExpandRetileNeighborhood(map, changedCells);
			foreach (var affectedCell in affectedCells)
			{
				if (!map.Tiles.Contains(affectedCell) || !undoCells.Add(affectedCell))
					continue;

				undoTiles.Enqueue(new UndoTile(affectedCell, map.Tiles[affectedCell], map.Height[affectedCell]));
			}

			foreach (var changedCell in changedCells)
				map.Height[changedCell] = targetHeight;

			HighCliffTerrainTools.RetileHeightRegion(map, affectedCells, clearTemplateId);
		}

		public void Undo()
		{
			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();
				map.Tiles[undoTile.Cell] = undoTile.MapTile;
				map.Height[undoTile.Cell] = undoTile.Height;
			}
		}

		static ushort FindClearTemplateId(ITemplatedTerrainInfo terrainInfo)
		{
			var clearTerrainIndex = terrainInfo.GetTerrainIndex("Clear");
			var clearTemplate = terrainInfo.Templates.Values
				.Where(t => t.Size.X == 1 && t.Size.Y == 1 && !HighCliffTileIds.IsTemplate(t.Id) && t.Contains(0) && t[0] != null && t[0].TerrainType == clearTerrainIndex)
				.OrderBy(t => t.Id)
				.FirstOrDefault();

			if (clearTemplate == null)
				throw new InvalidDataException("AdjustTerrainHeightEditorAction requires a 1x1 clear terrain template.");

			return clearTemplate.Id;
		}
	}

	enum CliffStampStyle
	{
		Rock,
		Wall,
	}

	enum HighCliffFacing
	{
		SW,
		SE,
	}

	sealed class PaintHighCliffEditorAction : IEditorAction
>>>>>>> Stashed changes
	{
		[FluentReference("id")]
		const string AddedTile = "notification-added-tile";

		public string Text { get; }

		readonly Map map;
		readonly CPos cell;
<<<<<<< Updated upstream
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
=======
		readonly HighCliffFacing facing;

		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();
		readonly TerrainTemplateInfo terrainTemplate;
		readonly ITemplatedTerrainInfo terrainInfo;

		public PaintHighCliffEditorAction(ushort template, Map map, CPos cell, HighCliffFacing facing)
		{
			this.map = map;
			this.cell = cell;
			this.facing = facing;
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
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
=======
			var mapTiles = map.Tiles;
			if (!mapTiles.Contains(cell))
				return;

			PaintCell(cell, EditorTileBrush.SelectHighCliffFaceTemplateId(cell, facing));
			RetileComponent(CollectComponent(cell));
		}

		void RetileComponent(HashSet<CPos> component)
		{
			if (component.Count == 0)
				return;

			var ordered = component
				.OrderBy(c => c.X)
				.ThenBy(c => c.Y)
				.ToList();

			for (var i = 0; i < ordered.Count; i++)
			{
				var target = ordered[i];
				var templateId = SelectTemplateForRole(target, i, ordered.Count);
				PaintCell(target, templateId);
			}
		}

		ushort SelectTemplateForRole(CPos target, int index, int count)
		{
			if (count == 1)
				return EditorTileBrush.SelectHighCliffFaceTemplateId(target, facing);

			if (index == 0)
				return EditorTileBrush.SelectHighCliffStartTemplateId(target, facing);

			if (index == count - 1)
				return EditorTileBrush.SelectHighCliffEndTemplateId(target);

			return EditorTileBrush.SelectHighCliffFaceTemplateId(target, facing);
		}

		HashSet<CPos> CollectComponent(CPos seed)
		{
			var mapTiles = map.Tiles;
			var component = new HashSet<CPos>();
			var queue = new Queue<CPos>();
			queue.Enqueue(seed);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (!mapTiles.Contains(current) || !component.Add(current))
					continue;

				foreach (var neighbor in EnumerateLineNeighbors(current))
				{
					if (!mapTiles.Contains(neighbor) || component.Contains(neighbor))
						continue;

					if (IsOwnedTemplate(mapTiles[neighbor].Type))
						queue.Enqueue(neighbor);
				}
			}

			return component;
		}

		bool IsOwnedTemplate(ushort templateId)
		{
			if (facing == HighCliffFacing.SW)
				return EditorTileBrush.IsHighCliffSwFaceTemplate(templateId) ||
					EditorTileBrush.IsHighCliffWOuterTemplate(templateId) ||
					EditorTileBrush.IsHighCliffSOuterTemplate(templateId);

			return EditorTileBrush.IsHighCliffSeFaceTemplate(templateId) ||
				EditorTileBrush.IsHighCliffEOuterTemplate(templateId) ||
				EditorTileBrush.IsHighCliffSOuterTemplate(templateId);
		}

		void PaintCell(CPos target, ushort templateId)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			if (!mapTiles.Contains(target))
				return;

			if (undoCells.Add(target))
				undoTiles.Enqueue(new UndoTile(target, mapTiles[target], mapHeight[target]));

			var templateInfo = terrainInfo.Templates[templateId];
			var baseHeight = mapHeight[target];
			mapTiles[target] = new TerrainTile(templateId, 0);
			mapHeight[target] = (byte)(baseHeight + templateInfo[0].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
		}

		static IEnumerable<CPos> EnumerateLineNeighbors(CPos cell)
		{
			yield return cell + new CVec(1, 0);
			yield return cell + new CVec(-1, 0);
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream

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
=======
	}

	sealed class PaintTileStampEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string AddedTile = "notification-added-tile";
>>>>>>> Stashed changes

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;
<<<<<<< Updated upstream
		readonly IAutoTile autoTile;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly TerrainTemplateInfo terrainTemplate;

		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();
		readonly HashSet<CPos> autoTileCells = new();
		const int AutoTileUpdateRadius = 3;
		const int AutoTileMaxPasses = 8;

		public AutoTileFloodFillEditorAction(ushort template, Map map, CPos cell, IAutoTile autoTile)
=======
		readonly IReadOnlyList<CVec> offsets;
		readonly IReadOnlyList<IReadOnlyList<ushort>> stampedTemplateIds;
		readonly HashSet<ushort> autotileTemplateIds;
		readonly CliffStampStyle cliffStampStyle;

		readonly Queue<UndoTile> undoTiles = new();
		readonly HashSet<CPos> undoCells = new();
		readonly TerrainTemplateInfo terrainTemplate;
		readonly ITemplatedTerrainInfo terrainInfo;

		public PaintTileStampEditorAction(ushort template, Map map, CPos cell, IReadOnlyList<CVec> offsets, IReadOnlyList<IReadOnlyList<ushort>> stampedTemplateIds, HashSet<ushort> autotileTemplateIds, CliffStampStyle cliffStampStyle)
>>>>>>> Stashed changes
		{
			this.template = template;
			this.map = map;
			this.cell = cell;
<<<<<<< Updated upstream
			this.autoTile = autoTile;

			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(FilledTile, "id", terrainTemplate.Id);
=======
			this.offsets = offsets;
			this.stampedTemplateIds = stampedTemplateIds;
			this.autotileTemplateIds = autotileTemplateIds;
			this.cliffStampStyle = cliffStampStyle;

			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(AddedTile, "id", terrainTemplate.Id);
>>>>>>> Stashed changes
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
<<<<<<< Updated upstream
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
=======
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var stampedCells = new List<CPos>();
			var wallColumn = cliffStampStyle == CliffStampStyle.Wall ? SelectWallCliffColumn(cell, 1) : null;

			for (var i = 0; i < offsets.Count; i++)
			{
				var offset = offsets[i];
				var target = cell + offset;
				if (!mapTiles.Contains(target))
					continue;

				if (undoCells.Add(target))
					undoTiles.Enqueue(new UndoTile(target, mapTiles[target], mapHeight[target]));

				var stampedTemplateId = cliffStampStyle == CliffStampStyle.Wall
					? wallColumn![i % wallColumn.Length]
					: SelectTemplateId(target, i);
				var stampedTemplate = terrainInfo.Templates[stampedTemplateId];
				var baseHeight = mapHeight[target];
				mapTiles[target] = new TerrainTile(stampedTemplateId, 0);
				mapHeight[target] = (byte)(baseHeight + stampedTemplate[0].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
				stampedCells.Add(target);
			}

			switch (cliffStampStyle)
			{
				case CliffStampStyle.Rock:
					RetileRockCliffComponent(stampedCells, mapTiles);
					break;
				case CliffStampStyle.Wall:
					RetileWallCliffComponent(stampedCells, mapTiles);
					break;
			}
		}

		HashSet<CPos> CollectCliffComponent(IReadOnlyList<CPos> stampedCells, CellLayer<TerrainTile> mapTiles)
		{
			if (stampedCells == null || stampedCells.Count == 0)
				return new HashSet<CPos>();

			var seeds = stampedCells.Where(mapTiles.Contains)
				.Where(c => autotileTemplateIds.Contains(mapTiles[c].Type))
				.ToList();
			if (seeds.Count == 0)
				return new HashSet<CPos>();

			var component = new HashSet<CPos>();
			var queue = new Queue<CPos>(seeds);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (!component.Add(current))
					continue;

				var neighbors = cliffStampStyle == CliffStampStyle.Wall ? EnumerateWallNeighbors(current) : EnumerateOrthogonalNeighbors(current);
				foreach (var neighbor in neighbors)
				{
					if (!mapTiles.Contains(neighbor))
						continue;

					if (!autotileTemplateIds.Contains(mapTiles[neighbor].Type))
						continue;

					if (!component.Contains(neighbor))
						queue.Enqueue(neighbor);
				}
			}

			return component;
		}

		void RetileRockCliffComponent(IReadOnlyList<CPos> stampedCells, CellLayer<TerrainTile> mapTiles)
		{
			var component = CollectCliffComponent(stampedCells, mapTiles);
			if (component.Count == 0)
				return;

			var rowExtents = component
				.GroupBy(c => c.Y)
				.ToDictionary(g => g.Key, g => (MinX: g.Min(c => c.X), MaxX: g.Max(c => c.X)));

			foreach (var target in component)
			{
				var (minX, maxX) = rowExtents[target.Y];
				var role = SelectLinearRole(target.X, minX, maxX);
				var roleTemplates = EditorTileBrush.RockCliffBrushRoleTemplateIds[role];
				var selector = ((target.X * 73856093) ^ (target.Y * 19349663) ^ (role * 83492791)) & int.MaxValue;
				var templateId = roleTemplates[selector % roleTemplates.Length];
				mapTiles[target] = new TerrainTile(templateId, 0);
			}
		}

		void RetileWallCliffComponent(IReadOnlyList<CPos> stampedCells, CellLayer<TerrainTile> mapTiles)
		{
			var component = CollectCliffComponent(stampedCells, mapTiles);
			if (component.Count == 0)
				return;

			var diagonalGroups = component
				.GroupBy(c => c.X - c.Y)
				.OrderBy(g => g.Key)
				.ToList();

			for (var columnIndex = 0; columnIndex < diagonalGroups.Count; columnIndex++)
			{
				var group = diagonalGroups[columnIndex]
					.OrderBy(c => c.X)
					.ToList();
				if (group.Count == 0)
					continue;

				var role = SelectLinearRole(columnIndex, 0, diagonalGroups.Count - 1);
				var columnTemplates = SelectWallCliffColumn(group[0], role);
				for (var i = 0; i < group.Count; i++)
				{
					var cellRole = SelectLinearRole(i, 0, group.Count - 1);
					mapTiles[group[i]] = new TerrainTile(columnTemplates[cellRole], 0);
				}
			}
		}

		static int SelectLinearRole(int coordinate, int minCoordinate, int maxCoordinate)
		{
			if (minCoordinate == maxCoordinate)
				return 1;

			if (coordinate == minCoordinate)
				return 0;

			if (coordinate == maxCoordinate)
				return 2;

			return 1;
		}

		static IEnumerable<CPos> EnumerateOrthogonalNeighbors(CPos cell)
		{
			yield return cell + new CVec(1, 0);
			yield return cell + new CVec(-1, 0);
			yield return cell + new CVec(0, 1);
			yield return cell + new CVec(0, -1);
		}

		static IEnumerable<CPos> EnumerateWallNeighbors(CPos cell)
		{
			yield return cell + new CVec(1, 1);
			yield return cell + new CVec(-1, -1);
			yield return cell + new CVec(1, -1);
			yield return cell + new CVec(-1, 1);
		}

		ushort SelectTemplateId(CPos origin, int offsetIndex)
		{
			if (stampedTemplateIds == null || stampedTemplateIds.Count == 0)
				return template;

			var roleTemplates = stampedTemplateIds[offsetIndex % stampedTemplateIds.Count];
			if (roleTemplates == null || roleTemplates.Count == 0)
				return template;

			var selector = ((origin.X * 73856093) ^ (origin.Y * 19349663) ^ (offsetIndex * 83492791)) & int.MaxValue;
			return roleTemplates[selector % roleTemplates.Count];
		}

		static ushort[] SelectWallCliffColumn(CPos origin, int role)
		{
			if (role == 2)
				return EditorTileBrush.WallCliffBrushRightEndColumnTemplateIds;

			var pools = role switch
			{
				0 => EditorTileBrush.WallCliffBrushLeftColumnTemplateIds,
				_ => EditorTileBrush.WallCliffBrushRightColumnTemplateIds,
			};
			var selector = ((origin.X * 73856093) ^ (origin.Y * 19349663) ^ (role * 83492791)) & int.MaxValue;
			return pools[selector % pools.Length];
>>>>>>> Stashed changes
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();
<<<<<<< Updated upstream

=======
>>>>>>> Stashed changes
				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}
<<<<<<< Updated upstream

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
=======
>>>>>>> Stashed changes
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
