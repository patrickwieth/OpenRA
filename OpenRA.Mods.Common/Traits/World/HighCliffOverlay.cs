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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	public sealed class HighCliffOverlayInfo : TraitInfo, Requires<ITiledTerrainRendererInfo>
	{
		[Desc("How far tile changes can affect high-cliff overlay redraws.")]
		public readonly int UpdateRadius = 1;

		[Desc("Font used for temporary high-cliff template debug labels.")]
		public readonly string DebugFont = "TinyBold";

		public override object Create(ActorInitializer init) { return new HighCliffOverlay(init.Self, this); }
	}

	public sealed class HighCliffOverlay : IRenderOverlay, IWorldLoaded, INotifyActorDisposing, IRenderAnnotations
	{
		readonly World world;
		readonly Map map;
		readonly HighCliffOverlayInfo info;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly DefaultTerrain terrainInfo;
		readonly SpriteFont debugFont;

		WorldRenderer worldRenderer;
		TerrainSpriteLayer[] renderLayers;
		bool disposed;

		public HighCliffOverlay(Actor self, HighCliffOverlayInfo info)
		{
			world = self.World;
			map = world.Map;
			this.info = info;
			terrainRenderer = self.Trait<ITiledTerrainRenderer>();
			terrainInfo = map.Rules.TerrainInfo as DefaultTerrain;
			debugFont = Game.Renderer.Fonts[info.DebugFont];
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer wr)
		{
			worldRenderer = wr;
			renderLayers = new[]
			{
				new TerrainSpriteLayer(w, wr, terrainRenderer.MissingTile, BlendMode.Alpha, w.Type != WorldType.Editor),
				new TerrainSpriteLayer(w, wr, terrainRenderer.MissingTile, BlendMode.Alpha, w.Type != WorldType.Editor),
			};

			foreach (var cell in map.AllCells)
				UpdateCell(cell);

			map.Tiles.CellEntryChanged += UpdateNeighborhood;
		}

		void UpdateNeighborhood(CPos cell)
		{
			for (var dy = -info.UpdateRadius; dy <= info.UpdateRadius; dy++)
			{
				for (var dx = -info.UpdateRadius; dx <= info.UpdateRadius; dx++)
				{
					var c = cell + new CVec(dx, dy);
					if (map.Contains(c))
						UpdateCell(c);
				}
			}
		}

		void UpdateCell(CPos cell)
		{
			if (renderLayers == null || terrainInfo == null || !map.Contains(cell))
				return;

			var tile = map.Tiles[cell];
			if (!HighCliffTileIds.IsTemplate(tile.Type))
			{
				foreach (var layer in renderLayers)
					layer.Clear(cell);

				return;
			}

			Log.Write("debug", $"render-highcliff cell={cell} tile={tile.Type} fam={ClassifyLabel(tile.Type)}");

			if (HighCliffTileIds.TryGetOuterCornerFaceTemplate(tile.Type, out var faceTemplateId))
				UpdateLayer(0, cell, new TerrainTile(faceTemplateId, 0));
			else
				renderLayers[0].Clear(cell);

			UpdateLayer(1, cell, tile);
		}

		void UpdateLayer(int layerIndex, CPos cell, TerrainTile tile)
		{
			var sprite = terrainRenderer.TileSprite(tile);
			var palette = worldRenderer.Palette(GetPalette(tile.Type));
			renderLayers[layerIndex].Update(cell, sprite, palette);
		}

		string GetPalette(ushort templateId)
		{
			var palette = terrainInfo.Palette;
			if (terrainInfo.Templates.TryGetValue(templateId, out var template))
				palette = ((DefaultTerrainTemplateInfo)template).Palette ?? palette;

			return palette;
		}

		void IRenderOverlay.Render(WorldRenderer wr)
		{
			if (renderLayers == null)
				return;

			foreach (var layer in renderLayers)
				layer.Draw(wr.Viewport);
		}


		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			foreach (var uv in wr.Viewport.VisibleCellsInsideBounds.CandidateMapCoords)
			{
				if (self.World.ShroudObscures(uv))
					continue;

				var cell = uv.ToCPos(wr.World.Map);
				if (!map.Contains(cell))
					continue;

				var tile = map.Tiles[cell];
				if (!HighCliffTileIds.IsTemplate(tile.Type))
					continue;

				var center = wr.World.Map.CenterOfCell(cell);
				var label = ClassifyLabel(tile.Type);
				yield return new TextAnnotationRenderable(debugFont, center, 0, OpenRA.Primitives.Color.White, label);
			}
		}

		static string ClassifyLabel(ushort templateId)
		{
			if (HighCliffTileIds.IsSouthWestFaceTemplate(templateId))
				return "SWF";

			if (HighCliffTileIds.IsSouthEastFaceTemplate(templateId))
				return "SEF";

			if (HighCliffTileIds.IsSouthOuterTemplate(templateId))
				return "SO";

			if (HighCliffTileIds.IsSouthInnerTemplate(templateId))
				return "SI";

			if (HighCliffTileIds.IsWestOuterTemplate(templateId))
				return "WO";

			if (HighCliffTileIds.IsEastOuterTemplate(templateId))
				return "EO";

			if (HighCliffTileIds.IsWestInnerTemplate(templateId))
				return "WI";

			if (HighCliffTileIds.IsEastInnerTemplate(templateId))
				return "EI";

			if (HighCliffTileIds.IsNorthWestOuterTemplate(templateId))
				return "NWO";

			if (HighCliffTileIds.IsNorthEastOuterTemplate(templateId))
				return "NEO";

			if (HighCliffTileIds.IsNorthOuterTemplate(templateId))
				return "NO";

			return templateId.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			map.Tiles.CellEntryChanged -= UpdateNeighborhood;
			if (renderLayers != null)
				foreach (var layer in renderLayers)
					layer.Dispose();

			disposed = true;
		}
	}
}
