using RhythmBase.Global.Events;
using RhythmBase.RhythmDoctor;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Extensions;
using RhythmBase.Global.Components;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RhythmBase.View;

public static class LevelViewer
{
	public static RenderConfig Config = new()
	{
		UnitSize = 14,
		PixelSize = 2,
	};
	public record struct RenderConfig
	{
		public int UnitSize;
		public int PixelSize;
	}
	public struct Filter
	{
		public Tab[] Tabs;
		public TickTime StartBeat;
		public TickTime? EndBeat;
		public static Filter All => new()
		{
			Tabs = [Tab.Sounds, Tab.Rows, Tab.Actions, Tab.Decorations, Tab.Rooms, Tab.Windows],
			StartBeat = new(1),
		};
		public static Filter CreateEmpty(Level level) => new()
		{
			Tabs = [],
			StartBeat = new(1),
			EndBeat = new(1),
		};
	}
	public struct TabData
	{
		internal Tab Tab { get; init; }
		public int MinY { get; init; }
		public int MaxY { get; init; }
		internal TabData ConcatY(int y)
		{
			return new TabData()
			{
				Tab = this.Tab,
				MinY = Math.Min(this.MinY, y),
				MaxY = Math.Max(this.MaxY, y),
			};
		}
	}
	public struct RenderData
	{
		internal Chart Chart { get; init; }
		internal Tab[] Tabs { get; init; }
		internal float StartBpm { get; init; }
		public TickTime StartBeat { get; init; }
		public TickTime EndBeat { get; init; }
		public IEnumerable<IBaseEvent> Events { get; init; }
		internal SortedDictionary<TickTime, float> TempoChanges { get; init; }
		public Dictionary<Tab, TabData> TabDataCol { get; init; }
		public SKImageInfo Info { get; init; }
	}
	internal static Color GetColor(this PaletteColor color)
	{
		return color.EnablePanel ? View.Palette[color.PaletteIndex] : color.Color;
	}
	internal static Color GetColor(this PaletteColorWithAlpha color)
	{
		return color.EnablePanel ? View.Palette[color.PaletteIndex] : color.Color;
	}
	public static void ProcessDefault(string filepath)
	{
		Chart chart = Chart.FromFile(filepath);
		Filter filter = Filter.All;
		RenderData data = GetRenderData(chart, filter);

		View.Directory = data.Chart.ResolvedDirectory;
		View.Palette = data.Chart.ColorPalette;
		View.Conditionals = data.Chart.Conditionals;

		SKFontManager manager = SKFontManager.Default;
		var tf = manager.MatchFamily("DinkieBitmap", new SKFontStyle(400, 5, SKFontStyleSlant.Upright));

		using SKSurface surface = SKSurface.Create(data.Info);
		if (surface is null)
		{
			data.Chart.Dispose();
			return;
		}
		using SKCanvas canvas = surface.Canvas;

		DrawBackground(data, canvas, tf);
		using SKImage background = surface.Snapshot();

		DrawEvents(data, canvas, tf);
		using SKImage foreground = surface.Snapshot();

		using FileStream backgroundFs = new("background.png", FileMode.Create, FileAccess.Write);
		using FileStream foregroundFs = new("foreground.png", FileMode.Create, FileAccess.Write);
		background.Encode(SKEncodedImageFormat.Png, 100).SaveTo(backgroundFs);
		foreground.Encode(SKEncodedImageFormat.Png, 100).SaveTo(foregroundFs);
	}
	public static RenderData GetRenderData(Chart chart, Filter filter)
	{
		TickTime start = filter.StartBeat.WithLink(chart);
		TickTime end = filter.EndBeat?.WithLink(chart) ?? chart.Duration;
		List<IBaseEvent> events = [];
		float? startBpm = null;
		SortedDictionary<TickTime, float> tempoChanges = [];
		Dictionary<Tab, TabData> tabDataCol = [];
		foreach (var e in chart.InRange(start, end + 1))
		{
			(int bar, _) = e.TickTime;
			if (e is BaseBeatsPerMinute bbpm)
			{
				startBpm ??= e.TickTime.Bpm;
				if (tempoChanges.ContainsKey(e.TickTime))
					tempoChanges[e.TickTime] = bbpm.BeatsPerMinute;
				else
					tempoChanges.Add(e.TickTime, bbpm.BeatsPerMinute);
			}
			if (filter.Tabs.Contains(e.Tab))
			{
				if (!tabDataCol.ContainsKey(Tab.Rooms) && e.Tab is Tab.Rooms)
					tabDataCol[Tab.Rooms] = new TabData()
					{
						Tab = Tab.Rooms,
						MinY = 0,
						MaxY = 3,
					};
				else if (e.Tab is Tab.Windows)
				{
					const int windowY = 3;
					int y = int.Max(e.Y, windowY);
					tabDataCol[Tab.Windows] = tabDataCol[Tab.Windows].ConcatY(y);
				}
				else
					if (tabDataCol.TryGetValue(e.Tab, out var data1))
						tabDataCol[e.Tab] = data1.ConcatY(e.Y);
					else
						tabDataCol[e.Tab] = new TabData()
						{
							Tab = e.Tab,
							MinY = e.Y,
							MaxY = e.Y,
						};
				events.Add(e);
			}
		}
		RenderData data = new()
		{
			Chart = chart,
			Tabs = filter.Tabs,
			StartBpm = startBpm ?? 0,
			StartBeat = start,
			EndBeat = end,
			Events = events,
			TempoChanges = tempoChanges,
			TabDataCol = tabDataCol,
			Info = new SKImageInfo(
						(int)((end.Tick - start.Tick) * Config.PixelSize * Config.UnitSize + 2 * Config.PixelSize * Config.UnitSize),
						(tabDataCol.Values.Sum(i => i.MaxY - i.MinY + 1) + 1) * Config.PixelSize * Config.UnitSize)
		};
		return data;
	}
	public static void DrawBackground(RenderData renderData, SKCanvas canvas, SKTypeface typeface)
	{
		View.Directory = renderData.Chart.ResolvedDirectory;
		View.Palette = renderData.Chart.ColorPalette;

		TickTime start = renderData.StartBeat;
		TickTime end = renderData.EndBeat;

		int unitSize = Config.UnitSize;
		int pixelSize = Config.PixelSize;
		int currentY = renderData.TabDataCol.Values.Sum(i => i.MaxY - i.MinY + 1);

		canvas.Translate(pixelSize * unitSize, pixelSize * unitSize);
		canvas.Clear(0xff2C2C2C);
		canvas.Save();
		canvas.RotateDegrees(-90);
		canvas.DrawText($"{renderData.Chart.Settings.Artist} - {renderData.Chart.Settings.Song} by {renderData.Chart.Settings.Author}",
				pixelSize, -pixelSize * 2, SKTextAlign.Right, new SKFont(typeface, 24), new SKPaint
				{
					Color = 0xffffffff,
					Style = SKPaintStyle.Fill,
				});
		canvas.Restore();


		int index = 0;
		List<KeyValuePair<TickTime, float>> tempoList = [.. renderData.TempoChanges];
		if (tempoList.Count == 0 || tempoList[0].Key != start)
			tempoList.Insert(0, new KeyValuePair<TickTime, float>(start, renderData.StartBpm));
		foreach (var tempo in tempoList)
		{
			int x = (int)((tempo.Key.Tick - start.Tick) * pixelSize * unitSize);
			using var paint = new SKPaint
			{
				Color = 0xff7a7a7a,
				Style = SKPaintStyle.Fill,
			};
			canvas.DrawText($"{tempo.Value} BPM", x + pixelSize, -pixelSize - 12, new SKFont(typeface), paint);
			if (index > 0 && index % 2 == 0)
			{
				var prevTempo = tempoList[index - 1];
				int prevX = (int)((prevTempo.Key.Tick - start.Tick) * pixelSize * unitSize);
				canvas.DrawRect(prevX, 0, x - prevX, (currentY + 1) * pixelSize * unitSize, new SKPaint
				{
					Color = 0x227a7a7a,
					Style = SKPaintStyle.Fill,
				});
			}

			index++;
		}
		if (renderData.TempoChanges.Count > 0)
		{
			var lastTempo = tempoList.Last();
			int lastX = (int)((lastTempo.Key.Tick - start.Tick) * pixelSize * unitSize);
			canvas.DrawRect(lastX, 0, renderData.Info.Width - lastX - 2 * pixelSize * unitSize, (currentY + 1) * pixelSize * unitSize, new SKPaint
			{
				Color = 0x227a7a7a,
				Style = SKPaintStyle.Fill,
			});
		}

		for (TickTime b = start; b < end; b += 1)
		{
			Bookmark bookmark = renderData.Chart.Bookmarks.FirstOrDefault(m => m.Tick.QuantizeBeat(1) == b);
			int x = (int)((b.Tick - start.Tick) * pixelSize * unitSize);
			(int bar, float beat) = b;
			uint color = beat == 1
					? 0xff7a7a7a
					: 0xff3C3C3C;
			using var paint = new SKPaint
			{
				Color = color,
				Style = SKPaintStyle.Fill,
			};
			if (beat == 1)
			{
				canvas.DrawText(bar.ToString(), x + pixelSize, -pixelSize, new SKFont(typeface), paint);
			}
			canvas.DrawLine(x, 0, x, (currentY + 1) * pixelSize * unitSize, paint);
		}
		foreach (var bookmark in renderData.Chart.Bookmarks)
		{
			TickTime b = bookmark.Tick.WithLink(renderData.Chart);
			int x = (int)((b.Tick - start.Tick) * pixelSize * unitSize);
			(int bar, float beat) = bookmark.Tick;
			uint color = bookmark.Color.ToColor();
			using var paint = new SKPaint
			{
				Style = SKPaintStyle.Fill,
				Shader = SKShader.CreateLinearGradient(
							new SKPoint(x, 0),
							new SKPoint(x + pixelSize * unitSize, 0),
							[new SKColor(color).WithAlpha(192), new SKColor(color).WithAlpha(0)],
							null,
							SKShaderTileMode.Clamp)
			};
			canvas.DrawRect(x, 0, pixelSize * unitSize, (currentY + 1) * pixelSize * unitSize, paint);
		}
		int top = 0;
		foreach (var tab in renderData.Tabs)
		{
			if (!renderData.TabDataCol.ContainsKey(tab))
				continue;
			var range = renderData.TabDataCol[tab];
			using var paint = new SKPaint
			{
				Color = View.ColorOf(tab).WithAlpha(32),
				Style = SKPaintStyle.Fill,
			};

			int y = top * pixelSize * unitSize;
			int height = range.MaxY - range.MinY + 1;
			int heightPixel = height * pixelSize * unitSize;
			canvas.DrawRect(0, y, (end.Tick - start.Tick) * pixelSize * unitSize, heightPixel, paint);
			top += height;
		}

	}
	public static void DrawEvents(RenderData renderData, SKCanvas canvas, SKTypeface typeface)
	{
		TickTime start = renderData.StartBeat;
		int unitSize = Config.UnitSize;
		int pixelSize = Config.PixelSize;
		Dictionary<Tab, int> tabTop = [];
		int top = 0;
		foreach (var tab in renderData.Tabs)
		{
			if (!renderData.TabDataCol.ContainsKey(tab))
				continue;
			var range = renderData.TabDataCol[tab];
			tabTop[tab] = top;
			top += range.MaxY - range.MinY + 1;
		}
		List<(IBaseEvent evt, SKPoint dest, IconStyleConfig config)> icons = [];
		foreach (var e in renderData.Events)
		{
			icons.Add((e, ToLocation(e, start.Tick, renderData.TabDataCol[e.Tab].MinY - tabTop[e.Tab]), new()
			{
				Scale = pixelSize,
				ShowDuration = true,
				Active = false,
				Hover = e is AddClassicBeat,
				Enabled = e.Active,
			}));
		}
		canvas.DrawEventIcons(icons);
	}
	private static SKPointI ToLocation(IBaseEvent e, float left, int top)
	{
		return new SKPointI(
				(int)((e.TickTime.Tick - left) * 28),
				((e is ReorderRooms or ShowRooms ? 0 : e.Y) - top) * 28
		);
	}
}