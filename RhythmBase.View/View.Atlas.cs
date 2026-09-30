using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Events;
using RhythmBase.RhythmDoctor;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Extensions;
using RhythmBase.View.Assets;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RhythmBase.View;

public static partial class View
{
	public enum BandMode
	{
		Single,
		Segmented,
		Shader,
	}
	internal static BandMode BandRenderMode { get; set; } = BandMode.Single;

	public static SKRect[] DrawEventIcons(this SKCanvas canvas, IEnumerable<(IBaseEvent evt, SKPoint dest, IconStyleConfig config)> evts)
	{
		(IBaseEvent evt, SKPoint dest, IconStyleConfig config)[] items = new List<(IBaseEvent, SKPoint, IconStyleConfig)>(evts).ToArray();
		SKRect[] rects = new SKRect[items.Length];
		if (items.Length == 0)
			return rects;

		IconBatch batch = new();
		EventPlan[] plans = new EventPlan[items.Length];
		Parallel.For(0, items.Length, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
		{
			plans[i] = BuildEventPlan(batch, items[i].evt, items[i].config);
		});
		batch.Compile();

		List<PendingSprite> pending = [];
		for (int i = 0; i < items.Length; i++)
		{
			EventPlan plan = plans[i];
			if (plan.Legacy)
			{
				FlushAtlas(canvas, batch, pending);
				rects[i] = canvas.DrawEventIcon(items[i].evt, items[i].dest, items[i].config);
				continue;
			}
			SKRect rect = plan.Rect;
			rect.Location = items[i].dest;
			rects[i] = rect;
			foreach (Layer layer in plan.Layers)
				pending.Add(new PendingSprite(layer.Tile, items[i].dest.X + layer.X, items[i].dest.Y + layer.Y));
		}
		FlushAtlas(canvas, batch, pending);
		return rects;
	}

	private static EventPlan BuildEventPlan(IconBatch batch, IBaseEvent evt, IconStyleConfig config)
	{
		IconStyle style = config.WithEventState(evt);
		if (style.Scale <= 0)
			return new EventPlan();
		switch (evt)
		{
			case AddClassicBeat classic:
				return BuildBeatPlan(batch, classic, style, BuildClassicBeat);
			case AddOneshotBeat oneshot:
				return BuildBeatPlan(batch, oneshot, style, BuildOneshotBeat);
			case Comment comment:
				return BuildComment(batch, comment, style);
			case DesktopColor desktopColor:
				return BuildDesktopColor(batch, desktopColor, style);
			case ReorderRooms reorderRooms:
				return BuildReorder(batch, reorderRooms, style);
			case ReorderWindows reorderWindows:
				return BuildReorder(batch, reorderWindows, style);
			case AddFreeTimeBeat freeTime:
				return BuildFreeTime(batch, freeTime, style);
			case PulseFreeTimeBeat pulseFreeTime:
				return BuildPulseFreeTime(batch, pulseFreeTime, style);
			case SetRowXs setRowXs:
				return BuildSetRowXs(batch, setRowXs, style);
			case ShowRooms showRooms:
				return BuildShowRooms(batch, showRooms, style);
			case SetCrotchetsPerBar setCrotchetsPerBar:
				return BuildSetCrotchetsPerBar(batch, setCrotchetsPerBar, style);
			default:
				if (evt is SayReadyGetSetGo or MoveRoom or SetBackgroundColor or SetForeground)
					return new EventPlan { Legacy = true };
				return BuildDefaultIcon(batch, evt, style);
		}
	}

	private static EventPlan BuildBeatPlan(IconBatch batch, IBaseEvent evt, IconStyle style, Action<IconBatch, EventPlan, IBaseEvent, IconStyle> build)
	{
		EventPlan plan = new();
		build(batch, plan, evt, style);
		AddBeatBadges(batch, plan, evt, style, plan.ContentWidth);
		return plan;
	}

	private static EventPlan BuildDefaultIcon(IconBatch batch, IBaseEvent evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.ContainsKey(key))
			key = "event_Unknown";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		switch (evt)
		{
			case CustomFlash customFlash:
				AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(customFlash.StartColor?.GetColor() ?? Color.Transparent));
				AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(customFlash.EndColor?.GetColor() ?? Color.Transparent));
				break;
			case FlipScreen flipScreen:
				AddMark(batch, plan, $"{key}{((flipScreen.FlipX, flipScreen.FlipY) switch
				{
					(false, false) => "",
					(false, true) => "_0",
					(true, false) => "_1",
					(true, true) => "_2",
				})}", 0, 0, S);
				break;
			case FloatingText floatingText:
				AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(floatingText.Color));
				AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(floatingText.OutlineColor));
				break;
			case PaintHands paintHands:
				AddMark(batch, plan, key, 0, 0, S, ToSKColor(paintHands.TintColor));
				switch (paintHands.Border)
				{
					case Border.Outline:
						AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(paintHands.BorderColor));
						break;
					case Border.Glow:
						AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(paintHands.BorderColor));
						break;
				}
				break;
			case SetText setText:
				AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(setText.Color));
				AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(setText.OutlineColor));
				break;
			case Tint tint:
				AddMark(batch, plan, key, 0, 0, S, ToSKColor(tint.TintColor));
				switch (tint.Border)
				{
					case Border.Outline:
						AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(tint.BorderColor));
						break;
					case Border.Glow:
						AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(tint.BorderColor));
						break;
				}
				break;
			case TintRows tintRows:
				AddMark(batch, plan, key, 0, 0, S, ToSKColor(tintRows.TintColor));
				switch (tintRows.Border)
				{
					case Border.Outline:
						AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(tintRows.BorderColor));
						break;
					case Border.Glow:
						AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(tintRows.BorderColor));
						break;
				}
				break;
			case TintText tintText:
				if (tintText.TintColor is PaletteColorWithAlpha tintColor)
					AddMark(batch, plan, $"{key}_0", 0, 0, S, ToSKColor(tintColor));
				else
					AddMark(batch, plan, key, 0, 0, S, SKColors.White);
				if (tintText.BorderColor is PaletteColorWithAlpha borderColor)
					AddMark(batch, plan, $"{key}_1", 0, 0, S, ToSKColor(borderColor));
				break;
			default:
				AddPlainArt(batch, plan, key, S);
				break;
		}
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static void AddPlainArt(IconBatch batch, EventPlan plan, string slice, int scale)
	{
		int tile = batch.SliceSprite(slice, scale);
		if (tile >= 0)
			plan.Layers.Add(new Layer(tile, 0, 0));
	}

	private static void AddGlyphText(IconBatch batch, EventPlan plan, string text, float destX, float destY, int scale, int textScale, SKColor color)
	{
		float x = destX;
		float y = destY - lineHeight * textScale;
		foreach (char c in text)
		{
			if (c == '\n')
			{
				x = destX;
				y += lineHeight * textScale;
				continue;
			}
			string glyph = $"char_{(int)c:x4}";
			if (!AssetManager._slices.TryGetValue(glyph, out SliceInfo info))
				continue;
			float devScale = scale * textScale;
			int tile = batch.SpriteScaled(glyph, devScale, color);
			plan.Layers.Add(new Layer(tile, x * scale - info.Pivot.X * devScale, y * scale - info.Pivot.Y * devScale));
			x += info.Bounds.Width * textScale;
		}
	}

	private static void AddRectImage(IconBatch batch, EventPlan plan, string slice, float x, float y, float w, float h, int scale, SKColor? tint)
	{
		if (!AssetManager._slices.TryGetValue(slice, out SliceInfo info))
			return;
		int pw = Math.Max(1, (int)Math.Ceiling((double)w * scale));
		int ph = Math.Max(1, (int)Math.Ceiling((double)h * scale));
		int tile = batch.RectImage(slice, pw, ph, tint);
		plan.Layers.Add(new Layer(tile, x * scale, y * scale));
	}

	private static EventPlan BuildFreeTime(IconBatch batch, AddFreeTimeBeat evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		float hold = evt.Hold - w / iconSize;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, Colors[7], style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		AddGlyphText(batch, plan, (evt.Pulse + 1).ToString(), 1.5f, 10, S, 1, SKColors.White);
		if (evt.Pulse == 6)
			AddMark(batch, plan, "event_beat_hit", 0, 0, S);
		if (hold > 0)
			AddBandSegments(batch, plan, "event_beat_area", w, 0, iconSize * hold, h, style.Active ? 0xffd046f3 : 0xff7e3990, S);
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static EventPlan BuildPulseFreeTime(IconBatch batch, PulseFreeTimeBeat evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		float hold = evt.Hold - w / iconSize;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, Colors[7], style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		AddGlyphText(batch, plan, evt.Action switch
		{
			PulseAction.Increment => ">",
			PulseAction.Decrement => "<",
			PulseAction.Remove => "x",
			PulseAction.Custom or _ => (evt.CustomPulse + 1).ToString(),
		}, 1.5f, 8, S, 1, SKColors.White);
		if (evt is { Action: PulseAction.Custom, CustomPulse: 7 })
			AddRectImage(batch, plan, "event_beat_hit", -2, 0, 5, h, S, null);
		if (hold > 0)
			AddBandSegments(batch, plan, "event_beat_area", w, 0, iconSize * hold, h, style.Active ? 0xffd046f3 : 0xff7e3990, S);
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static EventPlan BuildSetRowXs(IconBatch batch, SetRowXs evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		if (!AssetManager._slices.TryGetValue("event_beat_x", out SliceInfo beatx))
			return plan;
		float width = w / 6f;
		float s = width / beatx.Bounds.Width;
		float left = 0;
		float top = iconSize / 2f - beatx.Bounds.Height * s / 2f;
		foreach (var p in evt.Pattern)
		{
			string slice = p is Pattern.X ? "event_beat_x" : "event_beat_line";
			AddRectImage(batch, plan, slice, left, top, width, beatx.Bounds.Height * s, S, null);
			left += width;
		}
		if (evt.SyncoBeat >= 0)
			AddRectImage(batch, plan, "event_beat_synco", width * evt.SyncoBeat, top, width, beatx.Bounds.Height * s, S, null);
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static EventPlan BuildShowRooms(IconBatch batch, ShowRooms evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		plan.Rect = SKRect.Create(0, 0, iconSize * S, iconSize * 4 * S);
		plan.ContentWidth = iconSize;
		int back = batch.Back(0, 0, iconSize, iconSize * 4, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		for (int i = 0; i < 4; i++)
			AddMarkScaled(batch, plan, $"{key}_{(evt.Rooms[(byte)i] ? "1" : "0")}", 0, i * iconSize, S, 0.5f, null);
		AddDuration(batch, plan, evt, style, iconSize, iconSize * 4);
		AddBeatBadges(batch, plan, evt, style, iconSize);
		return plan;
	}

	private static EventPlan BuildSetCrotchetsPerBar(IconBatch batch, SetCrotchetsPerBar evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		AddMark(batch, plan, key, 0, 0, S);
		int cpb = evt.CrotchetsPerBar;
		AddGlyphText(batch, plan, cpb > 9 ? "-" : cpb.ToString(), 2, 7, S, 1, SKColors.Black);
		AddGlyphText(batch, plan, "4", 8, 12, S, 1, SKColors.Black);
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static EventPlan BuildComment(IconBatch batch, Comment evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		plan.Rect = SKRect.Create(0, 0, iconSize * S, iconSize * S);
		plan.ContentWidth = iconSize;
		SKColor color = (uint)(evt.Color.GetColor());
		int back = batch.Back(0, 0, iconSize, iconSize, color, style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		AddPlainArt(batch, plan, key, S);
		AddDuration(batch, plan, evt, style, iconSize, iconSize);
		AddBeatBadges(batch, plan, evt, style, iconSize);
		return plan;
	}

	private static EventPlan BuildDesktopColor(IconBatch batch, DesktopColor evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
			return plan;
		float w = info.Bounds.Width;
		float h = info.Bounds.Height;
		plan.Rect = SKRect.Create(0, 0, w * S, h * S);
		plan.ContentWidth = w;
		int back = batch.Back(0, 0, w, h, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		AddMark(batch, plan, $"{key}_0", 0, iconSize, S, ToSKColor(evt.EndColor?.GetColor() ?? Color.Transparent));
		AddMark(batch, plan, $"{key}_1", 0, iconSize, S);
		AddDuration(batch, plan, evt, style, w, h);
		AddBeatBadges(batch, plan, evt, style, w);
		return plan;
	}

	private static EventPlan BuildReorder(IconBatch batch, IBaseEvent evt, IconStyle style)
	{
		EventPlan plan = new();
		int S = style.Scale;
		string key = $"event_{evt.Type}";
		IEnumerable<int> order = evt switch
		{
			ReorderRooms rooms => rooms.Order.Take(4),
			ReorderWindows windows => windows.Order.Take(4),
			_ => [],
		};
		plan.Rect = SKRect.Create(0, 0, iconSize * S, iconSize * 4 * S);
		plan.ContentWidth = iconSize;
		int back = batch.Back(0, 0, iconSize, iconSize * 4, ColorOf(evt.Tab), style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		int i = 0;
		foreach (int room in order)
			AddMark(batch, plan, $"{key}_{room}", 0, iconSize * i++, S);
		AddDuration(batch, plan, evt, style, iconSize, iconSize * 4);
		AddBeatBadges(batch, plan, evt, style, iconSize);
		return plan;
	}

	private static void BuildClassicBeat(IconBatch batch, EventPlan plan, IBaseEvent eventBase, IconStyle style)
	{
		AddClassicBeat evt = (AddClassicBeat)eventBase;
		int S = style.Scale;
		float tick = evt.Tick;
		float swing = evt.Swing;
		if (swing == 0)
			swing = 1f;
		float hold = evt.Hold;
		float length = evt.Length;
		SetRowXs? prexs = evt.TickTime.IsEmpty ? null : evt.FrontOrDefault<SetRowXs>();
		float iconWidth = iconSize * tick * (length - 1 - (prexs?.SyncoSwing ?? 0));
		plan.Rect = SKRect.Create(0, 0, iconWidth * S, iconSize * S);
		plan.ContentWidth = iconWidth;
		SKColor fill = ColorOf(evt.Tab);
		if (hold > 0)
			AddBandSegments(batch, plan, "event_beat_area", iconWidth, 0, iconSize * hold, iconSize, style.Active ? 0xffd046f3 : 0xff7e3990, S);
		int back = batch.Back(0, 0, iconWidth, iconSize, fill, style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		if (style.Hover)
			for (int i = 0; i < length - 1; i++)
			{
				float x = iconSize * (tick * (i + i % 2 * (1 - swing) - (i <= (prexs?.SyncoBeat ?? -1) ? 0 : (prexs?.SyncoSwing ?? 0))));
				AddMark(batch, plan, "event_beat_pulse", x, 0, S);
			}
		{
			float x = iconSize * (tick * (length - 1 - (prexs?.SyncoSwing ?? 0))) - 1;
			AddMark(batch, plan, "event_beat_hit", x, 0, S);
		}
		AddDuration(batch, plan, evt, style, iconWidth);
	}

	private static void BuildOneshotBeat(IconBatch batch, EventPlan plan, IBaseEvent eventBase, IconStyle style)
	{
		AddOneshotBeat evt = (AddOneshotBeat)eventBase;
		int S = style.Scale;
		float tick = evt.Tick;
		float interval = evt.Interval;
		int loop = (int)evt.Loop;
		int subdiv = evt.Subdivisions;
		float delay = evt.FreezeBurnMode is OneshotType.Freezeshot ? evt.Delay : 0;
		float eventWidth = iconSize * (loop * interval + tick + delay);
		plan.Rect = SKRect.Create(0, 0, eventWidth * S, iconSize * S);
		plan.ContentWidth = eventWidth;
		SKColor fill = ColorOf(evt.Tab);
		int back = batch.Back(0, 0, eventWidth, iconSize, fill, style.Active, style.Enabled, S);
		AddRectLayer(batch, plan, back, 0, 0, S);
		float subdivWidth = iconSize * (subdiv - 1) / subdiv * tick;
		float off = interval - tick;
		float holdWidth = evt.Hold ? iconSize * (interval - tick - delay) : 0;
		if (holdWidth - subdivWidth > 0)
		{
			int tile = batch.Band("event_beat_area", eventWidth + subdivWidth, 0, holdWidth - subdivWidth, iconSize, 0xffd046f3, S);
			AddRectLayer(batch, plan, tile, eventWidth + subdivWidth, 0, S);
		}
		if (subdiv > 1)
		{
			int tile = batch.Band("event_beat_area", eventWidth, 0, subdivWidth, iconSize, 0xff13B021, S);
			AddRectLayer(batch, plan, tile, eventWidth, 0, S);
		}
		if (evt.Skipshot)
		{
			float skipWidth = iconSize * (interval - delay) - Math.Max(subdivWidth, holdWidth);
			int tile = batch.Band("event_beat_area", eventWidth + Math.Max(subdivWidth, holdWidth), 0, skipWidth, iconSize, 0xffc53b3b, S);
			AddRectLayer(batch, plan, tile, eventWidth + Math.Max(subdivWidth, holdWidth), 0, S);
			AddMark(batch, plan, "event_beat_skip", eventWidth + iconSize * (interval - delay) - 1, 0, S);
		}
		for (int l = 0; l <= loop; l++)
		{
			for (int i = 0; i < subdiv; i++)
			{
				float pulseX = iconSize * (l * interval + i * tick / subdiv);
				float hitX = iconSize * (l * interval + delay + tick + i * tick / subdiv) - 1;
				AddMark(batch, plan, "event_beat_pulse", pulseX, 0, S);
				AddMark(batch, plan, "event_beat_hit", hitX, 0, S);
				if (style.Hover)
					AddMark(batch, plan, "event_beat_pulse", pulseX, 0, S);
			}
			if (evt.FreezeBurnMode is OneshotType.Freezeshot or OneshotType.Burnshot || evt.Hold)
			{
				float x = iconSize * (l * interval - off) - 1;
				AddMark(batch, plan, "event_beat_cross", x, 0, S);
			}
			if (evt.FreezeBurnMode is OneshotType.Freezeshot or OneshotType.Burnshot)
			{
				bool freeze = evt.FreezeBurnMode is OneshotType.Freezeshot;
				float x = iconSize * (l * interval - off + (freeze ? delay : -tick)) + (freeze ? -1 : 0);
				AddMark(batch, plan, "event_beat_cross", x, 0, S);
				AddMark(batch, plan, freeze ? "event_beat_hit_freeze" : "event_beat_hit_burn", iconSize * (l * interval + tick) - 1, 0, S);
			}
		}
		if (style is { Active: true, Hover: true })
			AddMark(batch, plan, "event_beat_loop", eventWidth, 0, S);
		AddDuration(batch, plan, evt, style, eventWidth);
	}

	private static void AddBandSegments(IconBatch batch, EventPlan plan, string slice, float x, float y, float w, float h, SKColor color, int scale)
	{
		int totalPx = (int)Math.Ceiling((double)w * scale);
		if (totalPx <= 0)
			return;
		if (BandRenderMode == BandMode.Segmented && totalPx > IconBatch.MaxBandSegmentPx)
		{
			float seg = IconBatch.MaxBandSegmentPx / (float)scale;
			int unit = batch.Band(slice, x, y, seg, h, color, scale);
			float left = 0;
			while (w - left > seg)
			{
				AddRectLayer(batch, plan, unit, x + left, y, scale);
				left += seg;
			}
			if (w > left)
			{
				int tile = batch.Band(slice, x, y, w - left, h, color, scale);
				AddRectLayer(batch, plan, tile, x + left, y, scale);
			}
			return;
		}
		if (totalPx <= IconBatch.MaxTextureDimension)
		{
			int tile = batch.Band(slice, x, y, w, h, color, scale);
			AddRectLayer(batch, plan, tile, x, y, scale);
			return;
		}
		plan.Legacy = true;
	}

	private static void AddDuration(IconBatch batch, EventPlan plan, IBaseEvent evt, IconStyle style, float width, float height = iconSize)
	{
		if (evt is not IDurationEvent duration || !style.Enabled || (!style.Active && !style.ShowDuration))
			return;
		float durWidth = iconSize * duration.Duration - width;
		if (durWidth <= 0)
			return;
		if (BandRenderMode == BandMode.Shader)
		{
			plan.Legacy = true;
			return;
		}
		if ((int)Math.Ceiling((double)durWidth * style.Scale) > IconBatch.MaxTextureDimension)
		{
			plan.Legacy = true;
			return;
		}
		SKColor color = ColorOf(evt.Tab).WithAlpha(style.Active ? (byte)192 : (byte)91);
		int tile = batch.Band("event_beat_area", width, 0, durWidth, height, color, style.Scale);
		AddRectLayer(batch, plan, tile, width, 0, style.Scale);
	}

	private static void AddBeatBadges(IconBatch batch, EventPlan plan, IBaseEvent evt, IconStyle style, float width)
	{
		int S = style.Scale;
		if ((evt is IRoomEvent or ISingleRoomEvent) && style.Active)
		{
			Room room = evt switch
			{
				IRoomEvent roomEvent => roomEvent.Rooms,
				ISingleRoomEvent singleRoomEvent => singleRoomEvent.Room,
				_ => throw new NotImplementedException(),
			};
			const uint roomEnabled = 0xffd8b811;
			const uint roomDisabled = 0xff5b5b5b;
			AddMark(batch, plan, "room_0", width, 0, S, (SKColor)(room.Contains(RoomIndex.Room1) ? roomEnabled : roomDisabled));
			AddMark(batch, plan, "room_1", width, 0, S, (SKColor)(room.Contains(RoomIndex.Room2) ? roomEnabled : roomDisabled));
			AddMark(batch, plan, "room_2", width, 0, S, (SKColor)(room.Contains(RoomIndex.Room3) ? roomEnabled : roomDisabled));
			AddMark(batch, plan, "room_3", width, 0, S, (SKColor)(room.Contains(RoomIndex.Room4) ? roomEnabled : roomDisabled));
			if (room.Contains(RoomIndex.RoomTop))
				AddMark(batch, plan, "room_top", width, 0, S, (SKColor)roomEnabled);
		}
		if (!evt.Condition.IsEmpty)
		{
			bool hasTrue = Conditionals.Any(c => evt.Condition[c] is true);
			bool hasFalse = Conditionals.Any(c => evt.Condition[c] is false);
			if (hasTrue)
			{
				if (hasFalse)
					AddMark(batch, plan, "event_tag", 0, 0, S, 0xffffff00);
				else
					AddMark(batch, plan, "event_tag", 0, 0, S, 0xff00ffff);
			}
			else if (hasFalse)
				AddMark(batch, plan, "event_tag", 0, 0, S, 0xffff0000);
		}
		if (!string.IsNullOrEmpty(evt.Tag))
			AddMark(batch, plan, "event_tag_0", 0, iconSize, S, 0xffffc786);
	}

	private static void AddMark(IconBatch batch, EventPlan plan, string slice, float pointX, float pointY, int scale)
	{
		AddMark(batch, plan, slice, pointX, pointY, scale, null);
	}

	private static void AddMark(IconBatch batch, EventPlan plan, string slice, float pointX, float pointY, int scale, SKColor? tint)
	{
		AddMarkScaled(batch, plan, slice, pointX, pointY, scale, 1f, tint);
	}

	private static void AddMarkScaled(IconBatch batch, EventPlan plan, string slice, float pointX, float pointY, float scale, float scaleParam, SKColor? tint)
	{
		if (!AssetManager._slices.TryGetValue(slice, out SliceInfo info))
			return;
		float devScale = scale * scaleParam;
		int tile = batch.SpriteScaled(slice, devScale, tint);
		plan.Layers.Add(new Layer(tile, pointX * scale - info.Pivot.X * devScale, pointY * scale - info.Pivot.Y * devScale));
	}

	private static void AddRectLayer(IconBatch batch, EventPlan plan, int tile, float localX, float localY, int scale)
	{
		if (tile >= 0)
			plan.Layers.Add(new Layer(tile, localX * scale, localY * scale));
	}

	private static void FlushAtlas(SKCanvas canvas, IconBatch batch, List<PendingSprite> pending)
	{
		if (pending.Count == 0)
			return;
		int start = 0;
		while (start < pending.Count)
		{
			int page = batch.PageOf(pending[start].Tile);
			int end = start + 1;
			while (end < pending.Count && batch.PageOf(pending[end].Tile) == page)
				end++;
			int count = end - start;
			SKRect[] srcs = new SKRect[count];
			SKRotationScaleMatrix[] transforms = new SKRotationScaleMatrix[count];
			for (int i = 0; i < count; i++)
			{
				PendingSprite sprite = pending[start + i];
				SKRectI rect = batch.SrcRect(sprite.Tile);
				srcs[i] = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
				transforms[i] = new SKRotationScaleMatrix(1, 0, sprite.X, sprite.Y);
			}
			using SKPaint paint = new();
			canvas.DrawAtlas(batch.PageImage(page), srcs, transforms, paint);
			start = end;
		}
		pending.Clear();
	}

	private sealed class IconBatch : IDisposable
	{
		public const int MaxTextureDimension = 8192;
		public const int MaxBandSegmentPx = 128;
		private const int PageWidth = 2048;
		private const int PageHeight = 2048;
		private readonly Dictionary<SliceKey, int> _sliceIds = [];
		private readonly Dictionary<BackKey, int> _backIds = [];
		private readonly Dictionary<BandKey, int> _bandIds = [];
		private readonly List<SKImage> _tiles = [];
		private readonly List<Placement> _placements = [];
		private readonly List<SKImage> _pages = [];
		private readonly object _sync = new();

		public int SliceSprite(string slice, int scale)
		{
			return SpriteScaled(slice, scale, null);
		}

		public int SliceSprite(string slice, int scale, SKColor? tint)
		{
			return SpriteScaled(slice, scale, tint);
		}

		public int SpriteScaled(string slice, float devScale, SKColor? tint)
		{
			if (!AssetManager._slices.TryGetValue(slice, out SliceInfo info))
				return -1;
			int pw = Math.Max(1, (int)Math.Ceiling((double)info.Bounds.Width * devScale));
			int ph = Math.Max(1, (int)Math.Ceiling((double)info.Bounds.Height * devScale));
			SliceKey key = new(slice, pw, ph, tint);
			lock (_sync)
			{
				if (_sliceIds.TryGetValue(key, out int id))
					return id;
			}
			SKImage image;
			if (tint is SKColor color)
				image = Raster(pw, ph, c =>
				{
					using SKPaint paint = new()
					{
						ColorFilter = CreateFilter(color),
					};
					c.DrawImage(AssetManager._assetFile, info.Bounds, new SKRect(0, 0, pw, ph), paint);
				});
			else
				image = Raster(pw, ph, c => c.DrawImage(AssetManager._assetFile, info.Bounds, new SKRect(0, 0, pw, ph)));
			return GetOrAdd(_sliceIds, key, image);
		}

		public int RectImage(string slice, int pw, int ph, SKColor? tint)
		{
			if (!AssetManager._slices.TryGetValue(slice, out SliceInfo info))
				return -1;
			SliceKey key = new(slice, pw, ph, tint);
			lock (_sync)
			{
				if (_sliceIds.TryGetValue(key, out int id))
					return id;
			}
			SKImage image;
			if (tint is SKColor color)
				image = Raster(pw, ph, c =>
				{
					using SKPaint paint = new()
					{
						ColorFilter = CreateFilter(color),
					};
					c.DrawImage(AssetManager._assetFile, info.Bounds, new SKRect(0, 0, pw, ph), paint);
				});
			else
				image = Raster(pw, ph, c => c.DrawImage(AssetManager._assetFile, info.Bounds, new SKRect(0, 0, pw, ph)));
			return GetOrAdd(_sliceIds, key, image);
		}

		public int Back(float x, float y, float w, float h, SKColor color, bool active, bool enabled, int scale)
		{
			int pw = (int)Math.Ceiling((double)w * scale);
			int ph = (int)Math.Ceiling((double)h * scale);
			if (pw <= 0 || ph <= 0)
				return -1;
			BackKey key = new(pw, ph, color, active, enabled, scale);
			lock (_sync)
			{
				if (_backIds.TryGetValue(key, out int id))
					return id;
			}
			IconStyle style = new() { Enabled = enabled, Active = active };
			SKImage image = Raster(pw, ph, c =>
			{
				c.Translate(-x * scale, -y * scale);
				c.Scale(scale);
				DrawBack(c, SKRect.Create(x, y, w, h), color, style);
			});
			return GetOrAdd(_backIds, key, image);
		}

		public int Band(string slice, float x, float y, float w, float h, SKColor color, int scale)
		{
			if (!AssetManager._slices.TryGetValue(slice, out SliceInfo info))
				return -1;
			int pw = (int)Math.Ceiling((double)w * scale);
			int ph = (int)Math.Ceiling((double)h * scale);
			if (pw <= 0 || ph <= 0)
				return -1;
			BandKey key = new(slice, pw, ph, color, scale);
			lock (_sync)
			{
				if (_bandIds.TryGetValue(key, out int id))
					return id;
			}
			SKImage image = Raster(pw, ph, c =>
			{
				c.Translate(-x * scale, -y * scale);
				c.Scale(scale);
				DrawSlice(c, slice, SKRect.Create(x, y, w, h), color, 1f, PatchStyle.Repeat);
			});
			return GetOrAdd(_bandIds, key, image);
		}

		public int PageOf(int tile) => _placements[tile].Page;
		public SKRectI SrcRect(int tile) => _placements[tile].Rect;
		public SKImage PageImage(int page) => _pages[page];

		private int GetOrAdd<TKey>(Dictionary<TKey, int> map, TKey key, SKImage image)
		{
			lock (_sync)
			{
				if (map.TryGetValue(key, out int id))
				{
					image.Dispose();
					return id;
				}
				id = AddUnlocked(image);
				map[key] = id;
				return id;
			}
		}

		private int Add(SKImage image)
		{
			lock (_sync)
				return AddUnlocked(image);
		}

		private int AddUnlocked(SKImage image)
		{
			_tiles.Add(image);
			_placements.Add(default);
			return _tiles.Count - 1;
		}

		private static SKImage Raster(int pw, int ph, Action<SKCanvas> draw)
		{
			using SKSurface surface = SKSurface.Create(new SKImageInfo(pw, ph, SKColorType.Rgba8888, SKAlphaType.Premul));
			using SKCanvas canvas = surface.Canvas;
			canvas.Clear(SKColors.Transparent);
			draw(canvas);
			return surface.Snapshot();
		}

		public void Compile()
		{
			int count = _tiles.Count;
			List<PageSurface> surfaces = [];
			int x = 0;
			int y = 0;
			int rowHeight = 0;
			bool freshPage = true;
			for (int i = 0; i < count; i++)
			{
				SKImage tile = _tiles[i];
				int w = tile.Width;
				int h = tile.Height;
				if (w > PageWidth || h > PageHeight)
				{
					surfaces.Add(new PageSurface(w, h));
					surfaces[surfaces.Count - 1].Canvas.DrawImage(tile, new SKRect(0, 0, w, h));
					_placements[i] = new Placement(surfaces.Count - 1, SKRectI.Create(0, 0, w, h));
					freshPage = true;
					continue;
				}
				if (freshPage)
				{
					surfaces.Add(new PageSurface(PageWidth, PageHeight));
					x = 0;
					y = 0;
					rowHeight = 0;
					freshPage = false;
				}
				if (x + w > PageWidth)
				{
					x = 0;
					y += rowHeight;
					rowHeight = 0;
				}
				if (y + h > PageHeight)
				{
					surfaces.Add(new PageSurface(PageWidth, PageHeight));
					x = 0;
					y = 0;
					rowHeight = 0;
				}
				surfaces[surfaces.Count - 1].Canvas.DrawImage(tile, new SKRect(x, y, x + w, y + h));
				_placements[i] = new Placement(surfaces.Count - 1, SKRectI.Create(x, y, w, h));
				x += w;
				rowHeight = Math.Max(rowHeight, h);
			}
			foreach (PageSurface surface in surfaces)
			{
				surface.Canvas.Flush();
				_pages.Add(surface.Surface.Snapshot());
				surface.Dispose();
			}
			foreach (SKImage tile in _tiles)
				tile.Dispose();
			_tiles.Clear();
		}

		public void Dispose()
		{
			foreach (SKImage page in _pages)
				page.Dispose();
			_pages.Clear();
			foreach (SKImage tile in _tiles)
				tile.Dispose();
			_tiles.Clear();
		}

		private sealed class PageSurface : IDisposable
		{
			public PageSurface(int width, int height)
			{
				Surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
				Canvas = Surface.Canvas;
				Canvas.Clear(SKColors.Transparent);
			}
			public SKSurface Surface { get; }
			public SKCanvas Canvas { get; }
			public void Dispose()
			{
				Canvas.Dispose();
				Surface.Dispose();
			}
		}
		private readonly record struct Placement(int Page, SKRectI Rect);
		private readonly record struct SliceKey(string Slice, int Pw, int Ph, SKColor? Tint);
		private readonly record struct BackKey(int Width, int Height, SKColor Color, bool Active, bool Enabled, int Scale);
		private readonly record struct BandKey(string Slice, int Width, int Height, SKColor Color, int Scale);
	}

	private readonly record struct Layer(int Tile, float X, float Y);
	private readonly record struct PendingSprite(int Tile, float X, float Y);
	private sealed record EventPlan
	{
		public bool Legacy { get; set; }
		public SKRect Rect { get; set; }
		public float ContentWidth { get; set; }
		public List<Layer> Layers { get; } = [];
	}
}
