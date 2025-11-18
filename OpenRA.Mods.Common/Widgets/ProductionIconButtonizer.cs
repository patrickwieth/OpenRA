using System;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public static class ProductionIconButtonizer
	{
		public const int BarHeight = 8;
		const int TextPadding = 2;

		static readonly Color DefaultBarTopColor = Color.FromArgb(150, 70, 70, 70);
		static readonly Color DefaultBarBottomColor = Color.FromArgb(180, 40, 40, 40);
		static readonly Color DefaultTextLight = Color.FromArgb(235, 235, 235);
		static readonly Color DefaultTextDark = Color.FromArgb(40, 40, 40);

		static readonly Dictionary<ProductionButtonStyle, ButtonVisualStyle> ButtonStyles = new()
		{
			{ ProductionButtonStyle.Ra2, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(200, 0, 0, 0),
				topEdge: Color.FromArgb(120, 245, 245, 245),
				rightEdge: Color.FromArgb(190, 220, 220, 220),
				bottomEdge: Color.FromArgb(160, 10, 10, 10),
				topHighlight: Color.FromArgb(70, 255, 255, 255),
				topHighlightHeight: 1,
				barTopColor: DefaultBarTopColor,
				barBottomColor: DefaultBarBottomColor,
				textLight: DefaultTextLight,
				textDark: DefaultTextDark) },
			{ ProductionButtonStyle.Ra1, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(255, 255, 255, 255),
				topEdge: Color.FromArgb(235, 235, 235, 235),
				rightEdge: Color.FromArgb(40, 40, 40, 40),
				bottomEdge: Color.FromArgb(20, 20, 20, 20),
				topHighlight: Color.FromArgb(60, 255, 255, 255),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(180, 70, 70, 70),
				barBottomColor: Color.FromArgb(200, 40, 40, 40),
				textLight: Color.FromArgb(240, 240, 240),
				textDark: Color.FromArgb(20, 20, 20)) },
			{ ProductionButtonStyle.Td, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(245, 250, 235, 200),
				topEdge: Color.FromArgb(235, 245, 225, 190),
				rightEdge: Color.FromArgb(80, 70, 50, 25),
				bottomEdge: Color.FromArgb(60, 55, 35, 15),
				topHighlight: Color.FromArgb(70, 255, 235, 180),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(170, 120, 95, 55),
				barBottomColor: Color.FromArgb(190, 85, 60, 30),
				textLight: Color.FromArgb(240, 235, 210),
				textDark: Color.FromArgb(35, 25, 10)) },
			{ ProductionButtonStyle.Ts, new ButtonVisualStyle(
				borderThickness: 1,
				leftEdge: Color.FromArgb(180, 120, 175, 190),
				topEdge: Color.FromArgb(180, 145, 205, 215),
				rightEdge: Color.FromArgb(170, 40, 80, 100),
				bottomEdge: Color.FromArgb(170, 30, 60, 80),
				topHighlight: Color.FromArgb(70, 180, 230, 255),
				topHighlightHeight: 1,
				barTopColor: Color.FromArgb(150, 50, 80, 95),
				barBottomColor: Color.FromArgb(190, 35, 55, 70),
				textLight: Color.FromArgb(220, 235, 240),
				textDark: Color.FromArgb(25, 35, 45)) },
			{ ProductionButtonStyle.None, new ButtonVisualStyle(
				borderThickness: 0,
				leftEdge: Color.Transparent,
				topEdge: Color.Transparent,
				rightEdge: Color.Transparent,
				bottomEdge: Color.Transparent,
				topHighlight: null,
				topHighlightHeight: 0,
				barTopColor: DefaultBarTopColor,
				barBottomColor: DefaultBarBottomColor,
				textLight: DefaultTextLight,
				textDark: DefaultTextDark) },
		};

		public static void Draw(ProductionIcon icon, Rectangle rect, string fallbackText, string fallbackFont)
		{
			if (icon == null || !icon.Buttonize)
				return;

			var style = ResolveStyle(icon.ButtonStyle);
			DrawFrame(rect, style);

			var label = icon.ButtonLabel ?? fallbackText;
			if (string.IsNullOrEmpty(label))
				return;

			var fontName = icon.ButtonLabelFont ?? fallbackFont;
			DrawLabel(rect, label, fontName, style);
		}

		static ButtonVisualStyle ResolveStyle(ProductionButtonStyle style)
		{
			if (!ButtonStyles.TryGetValue(style, out var resolved))
				resolved = ButtonStyles[ProductionButtonStyle.Ra2];

			return resolved;
		}

		static void DrawFrame(Rectangle rect, ButtonVisualStyle style)
		{
			var thickness = Math.Min(style.BorderThickness, Math.Min(rect.Width / 2, rect.Height / 2));
			if (thickness > 0)
			{
				WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Top, rect.Width, thickness), style.TopEdge);
				WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), style.BottomEdge);

				var sideHeight = Math.Max(0, rect.Height - 2 * thickness);
				if (sideHeight > 0)
				{
					WidgetUtils.FillRectWithColor(new Rectangle(rect.Left, rect.Top + thickness, thickness, sideHeight), style.LeftEdge);
					WidgetUtils.FillRectWithColor(new Rectangle(rect.Right - thickness, rect.Top + thickness, thickness, sideHeight), style.RightEdge);
				}
			}

			if (style.TopHighlight.HasValue)
			{
				var highlightHeight = Math.Min(style.TopHighlightHeight, Math.Max(0, thickness));
				if (highlightHeight > 0)
				{
					var highlightRect = new Rectangle(
						rect.Left,
						rect.Top,
						rect.Width,
						highlightHeight);
					WidgetUtils.FillRectWithColor(highlightRect, style.TopHighlight.Value);
				}
			}
		}

		static void DrawLabel(Rectangle rect, string label, string fontName, ButtonVisualStyle style)
		{
			var font = Game.Renderer.Fonts[fontName];
			var barHeight = Math.Max(4, Math.Min(BarHeight, rect.Height / 3));
			var bar = new Rectangle(rect.Left, rect.Bottom - barHeight, rect.Width, barHeight);
			WidgetUtils.FillRectWithColor(bar, style.BarTopColor, style.BarTopColor, style.BarBottomColor, style.BarBottomColor);

			var maxWidth = Math.Max(0, bar.Width - TextPadding * 2);
			var text = WidgetUtils.TruncateText(label, maxWidth, font);
			var textSize = font.Measure(text);
			var x = bar.Left + (bar.Width - textSize.X) / 2;
			var y = bar.Top + (bar.Height - textSize.Y) / 2;
			var pos = new int2(x, y);

			// Pixel-like text: draw a dark outline then a light fill
			font.DrawText(text, pos + new int2(-1, 0), style.TextDark);
			font.DrawText(text, pos + new int2(1, 0), style.TextDark);
			font.DrawText(text, pos + new int2(0, -1), style.TextDark);
			font.DrawText(text, pos + new int2(0, 1), style.TextDark);
			font.DrawText(text, pos, style.TextLight);
		}

		readonly struct ButtonVisualStyle
		{
			public readonly int BorderThickness;
			public readonly Color LeftEdge;
			public readonly Color TopEdge;
			public readonly Color RightEdge;
			public readonly Color BottomEdge;
			public readonly Color? TopHighlight;
			public readonly int TopHighlightHeight;
			public readonly Color BarTopColor;
			public readonly Color BarBottomColor;
			public readonly Color TextLight;
			public readonly Color TextDark;

			public ButtonVisualStyle(
				int borderThickness,
				Color leftEdge,
				Color topEdge,
				Color rightEdge,
				Color bottomEdge,
				Color? topHighlight,
				int topHighlightHeight,
				Color barTopColor,
				Color barBottomColor,
				Color textLight,
				Color textDark)
			{
				BorderThickness = borderThickness;
				LeftEdge = leftEdge;
				TopEdge = topEdge;
				RightEdge = rightEdge;
				BottomEdge = bottomEdge;
				TopHighlight = topHighlight;
				TopHighlightHeight = topHighlightHeight;
				BarTopColor = barTopColor;
				BarBottomColor = barBottomColor;
				TextLight = textLight;
				TextDark = textDark;
			}
		}
	}
}

