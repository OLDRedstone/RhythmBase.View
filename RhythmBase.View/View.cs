using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Events;
using RhythmBase.RhythmDoctor.Extensions;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.View.Assets;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.View;


public static class View
{
	private const int lineHeight = 6;
	private const int charHeight = 8;
	private const int iconSize = 14;
	public static readonly SKColor[] Colors = AssetManager.Colors;

	private static SKColorFilter CreateFilter(SKColor replace)
	{
		float r = replace.Red / 255f;
		float g = replace.Green / 255f;
		float b = replace.Blue / 255f;
		float a = replace.Alpha / 255f;

		float[] colorMatrix =
		[
			r,0,0,0,0,
			0,g,0,0,0,
			0,0,b,0,0,
			0,0,0,a,0
		];
		SKColorFilter filter = SKColorFilter.CreateColorMatrix(colorMatrix);
		return filter;
	}
	internal static void DrawSlice(this SKCanvas canvas, string src, SKRect dest, SKColor? replace = null, float scale = 1, PatchStyle style = PatchStyle.Stretch)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return;

		if (replace?.Alpha == 0)
			return;

		using SKPaint paint = new();
		if (replace is SKColor color)
			paint.ColorFilter = CreateFilter(color);

		if (info.IsNinePatch)
		{
			canvas.Save();
			canvas.Translate(dest.Location);
			DrawNinePatch(canvas, AssetManager._assetFile, info, dest.Size, paint, scale, style);
			canvas.Restore();
		}
		else
		{
			canvas.DrawImage(AssetManager._assetFile, info.Bounds, dest, paint);
		}
	}
	internal static SKRect DrawSlice(this SKCanvas canvas, string src, SKPoint dest, SKColor? replace = null, float scale = 1)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return default;

		SKRect destRect = SKRect.Create(dest, new(info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale));
		destRect.Offset(-info.Pivot.X * scale, -info.Pivot.Y * scale);
		if (replace?.Alpha == 0)
			return SKRect.Create(dest.X, dest.Y, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);

		using SKPaint paint = new();
		if (replace is SKColor color)
			paint.ColorFilter = CreateFilter(color);

		canvas.DrawImage(AssetManager._assetFile, info.Bounds, destRect, paint);

		return SKRect.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);
	}
	public static int MeasureRDFontText(this SKCanvas canvas, char c, int scale = 1)
	{
		if (c == '\n')
			return 0;
		if (AssetManager._slices.TryGetValue($"char_{(int)c:x4}", out SliceInfo info))
			return info.Bounds.Width * scale;
		return 0;
	}
	public static int MeasureRDFontText(this SKCanvas canvas, string text, int scale = 1)
	{
		int len = 0;
		foreach (var c in text)
		{
			if (c == '\n')
				len = 0;
			else if (AssetManager._slices.TryGetValue($"char_{(int)c:x4}", out SliceInfo info))
				len += info.Bounds.Width * scale;
		}
		return len;
	}
	public static SKRect DrawRDFontText(this SKCanvas canvas, string text, in SKPoint dest, SKColor color, int scale = 1)
	{
		SKPoint start = dest;
		SKRect result = default;
		start.Offset(0, -lineHeight * scale);
		foreach (char c in text)
		{
			if (c == '\n')
			{
				start.Offset(0, lineHeight * scale);
				start.X = dest.X;
			}
			else
			{
				SKRect area = canvas.DrawSlice($"char_{(int)c:x4}", start, color, scale);
				result = SKRect.Union(result, SKRect.Create(start.X, start.Y, area.Width, area.Height));
				start.Offset(area.Width, 0);
			}
		}
		return result;
	}

	public static SKRect DrawEventIcon<TEvent>(this SKCanvas canvas, TEvent evt, SKPoint dest, IconStyle style)
	where TEvent : IBaseEvent
	{
		canvas.Save();
		canvas.Translate(dest.X, dest.Y);
		canvas.Scale(style.Scale, style.Scale);
		SKRect destRect = default;
		string key = $"event_{evt.Type}";
		EventType evttype = evt.Type;
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
		{
			if (!AssetManager._slices.TryGetValue($"event_Unknown", out info))
			{
				throw new NotImplementedException();
			}
		}
		const string pulse = "event_beat_pulse";
		const string hit = "event_beat_hit";
		const string hitf = "event_beat_hit_freeze";
		const string hitb = "event_beat_hit_burn";
		const string beatcross = "event_beat_cross";
		const string beatx = "event_beat_x";
		const string beatsynco = "event_beat_synco";
		const string beatline = "event_beat_line";
		const string evtag = "event_tag";
		const string evbarea = "event_beat_area";
		const string evbskip = "event_beat_skip";

		SKColor beatcolor = AssetManager.Colors[7];
		switch (evt)
		{
			case AddClassicBeat addClassicBeat:
				{
					float tick = addClassicBeat.Tick;
					float swing = addClassicBeat.Swing;
					if (swing == 0) swing = 1f;
					float hold = addClassicBeat.Hold;
					float length = addClassicBeat.Length;
					SetRowXs? prexs = addClassicBeat.Beat.IsEmpty ? null : addClassicBeat.FrontOrDefault<SetRowXs>();
					float iconWidth = iconSize * tick * (length - 1 - (prexs?.SyncoSwing ?? 0));
					destRect = SKRect.Create(0, 0, iconWidth, iconSize);
					if (hold > 0)
						canvas.DrawSlice(
							evbarea,
							SKRect.Create(iconWidth, 0, iconSize * hold, iconSize),
							style.Active ? 0xffd046f3 : 0xff7e3990, style: PatchStyle.Repeat);
					canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
					if (style.Hover)
						for (int i = 0; i < addClassicBeat.Length - 1; i++)
						{
							Console.WriteLine(iconSize * (tick *
(i + i % 2 * (1 - swing) - (i <= (prexs?.SyncoBeat ?? -1) ? 0 : (prexs?.SyncoSwing ?? 0)))));
							canvas.DrawSlice(pulse,
							new SKPoint(iconSize * (tick *
(i + i % 2 * (1 - swing) - (i <= (prexs?.SyncoBeat ?? -1) ? 0 : (prexs?.SyncoSwing ?? 0)))), 0));
						}
					canvas.DrawSlice(hit, new SKPoint(
					(iconSize * (tick * (addClassicBeat.Length - 1 - (prexs?.SyncoSwing ?? 0))) - 1), 0));
				}
				break;
			case AddFreeTimeBeat addFreeTimeBeat:
				{
					float hold = addFreeTimeBeat.Hold - info.Bounds.Width / iconSize;
					destRect = SKRect.Create(0, 0, info.Bounds.Width, info.Bounds.Height);
					canvas.DrawBack(destRect, beatcolor, style);
					canvas.DrawRDFontText((addFreeTimeBeat.Pulse + 1).ToString(), new(1.5f, 10), SKColors.White);
					if (addFreeTimeBeat.Pulse == 6)
						canvas.DrawSlice(hit, new SKPoint());
					if (hold > 0)
						canvas.DrawSlice(evbarea,
							SKRect.Create(destRect.Width, 0, iconSize * hold, info.Bounds.Height),
							style.Active ? 0xffd046f3 : 0xff7e3990, style: PatchStyle.Repeat);
				}
				break;
			case AddOneshotBeat oneshotBeat:
				{
					float tick = oneshotBeat.Tick;
					float interval = oneshotBeat.Interval;
					int loop = (int)oneshotBeat.Loops;
					int subdiv = oneshotBeat.Subdivisions;
					float delay = oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? oneshotBeat.Delay : 0;
					destRect = SKRect.Create(0, 0, iconSize * (loop * interval + tick + delay), iconSize);
					canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
					float subdivWidth = iconSize * (subdiv - 1) / subdiv * tick;
					float eventWidth = iconSize * (loop * interval + tick + delay);
					float off = interval - tick;
					float holdWidth = oneshotBeat.Hold ? iconSize * (interval - tick - delay) : 0;
					if (holdWidth - subdivWidth > 0)
						canvas.DrawSlice(evbarea,
							SKRect.Create(eventWidth + subdivWidth, 0, holdWidth - subdivWidth, iconSize),
							0xffd046f3, style: PatchStyle.Repeat);
					if (subdiv > 1)
						canvas.DrawSlice(evbarea,
							SKRect.Create(eventWidth, 0, subdivWidth, iconSize),
							0xff13B021, style: PatchStyle.Repeat);
					if (oneshotBeat.Skipshot)
					{
						canvas.DrawSlice(evbarea,
							SKRect.Create(eventWidth + Math.Max(subdivWidth, holdWidth), 0, iconSize * (interval - delay) -
														Math.Max(subdivWidth, holdWidth), iconSize),
							0xffc53b3b, style: PatchStyle.Repeat);
						canvas.DrawSlice(evbskip, new SKPoint(eventWidth + iconSize * (interval - delay) - 1, 0));
					}
					for (int l = 0; l <= loop; l++)
					{
						for (int i = 0; i < subdiv; i++)
						{
							canvas.DrawSlice(pulse, new SKPoint(iconSize * (l * interval + i * tick / subdiv), 0));
							canvas.DrawSlice(hit, new SKPoint(iconSize * (l * interval + delay + tick + (i * tick / subdiv)) - 1, 0));
							if (style.Hover)
								canvas.DrawSlice(pulse, new SKPoint(iconSize * (l * interval + i * tick / subdiv), 0));
						}
						if (oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot or OneshotType.Burnshot || oneshotBeat.Hold)
							canvas.DrawSlice(beatcross, new SKPoint(iconSize * (l * interval - off) - 1, 0));
						if (oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot or OneshotType.Burnshot)
						{
							canvas.DrawSlice(beatcross, new SKPoint(iconSize * (l * interval - off + (oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? delay : -tick)) +
								(oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? -1 : 0), 0));
							canvas.DrawSlice(oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? hitf : hitb, new SKPoint(iconSize * (l * interval + tick) - 1, 0));
						}
					}
					if (style is { Active: true, Hover: true })
						canvas.DrawSlice("event_beat_loop", new SKPoint(destRect.Right, 0));
				}
				break;
			case Comment comment:
				{
					SKColor color = (uint)(comment.Color.Value);
					destRect = SKRect.Create(0, 0, iconSize, iconSize);
					canvas.DrawBack(
						destRect,
						color,
						style);
					canvas.DrawSlice(key, dest);
				}
				break;
			case DesktopColor desktopColor:
				{
					SKColor color = (uint)(desktopColor.StartColor?.Value ?? RDColor.Transparent);
					destRect = SKRect.Create(0, 0, info.Bounds.Width, info.Bounds.Height);
					canvas.DrawBack(
						destRect,
						ColorOf(desktopColor.Tab),
						style);
					canvas.DrawSlice($"{key}_0", dest with { Y = iconSize }, ToSKColor(desktopColor.EndColor?.Value ?? RDColor.Transparent));
					canvas.DrawSlice($"{key}_1", dest with { Y = iconSize });
				}
				break;
			case PulseFreeTimeBeat pulseFreeTimeBeat:
				{
					float hold = pulseFreeTimeBeat.Hold - info.Bounds.Width / iconSize;
					destRect = SKRect.Create(0, 0, info.Bounds.Width, info.Bounds.Height);
					canvas.DrawBack(destRect, beatcolor, style);
					canvas.DrawRDFontText(pulseFreeTimeBeat.Action switch
					{
						PulseAction.Increment => ">",
						PulseAction.Decrement => "<",
						PulseAction.Remove => "x",
						PulseAction.Custom or
						_ => (pulseFreeTimeBeat.CustomPulse + 1).ToString(),
					}, new(1.5f, 8), SKColors.White);
					if (pulseFreeTimeBeat is { Action: PulseAction.Custom, CustomPulse: 7 })
						canvas.DrawSlice(hit, SKRect.Create(-2, 0, 5, info.Bounds.Height));
					if (hold > 0)
						canvas.DrawSlice(evbarea,
							SKRect.Create(destRect.Width, 0, iconSize * hold, info.Bounds.Height),
							style.Active ? 0xffd046f3 : 0xff7e3990, style: PatchStyle.Repeat);
				}
				break;
			case ReorderRooms reorderRooms:
				destRect = SKRect.Create(0, 0, iconSize, iconSize * 4);
				canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
				foreach (var r in reorderRooms.Order.Order)
				{
					canvas.DrawSlice($"{key}_{r}", dest);
					dest.Offset(0, iconSize);
				}
				break;
			case ReorderWindows reorderWindows:
				destRect = SKRect.Create(0, 0, iconSize, iconSize * 4);
				canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
				foreach (var r in reorderWindows.Order.Order)
				{
					canvas.DrawSlice($"{key}_{r}", dest);
					dest.Offset(0, iconSize);
				}
				break;
			case SayReadyGetSetGo sayReadyGetSetGo:
				{
					float len = LengthOf(sayReadyGetSetGo.PhraseToSay) * sayReadyGetSetGo.Tick + 1;
					destRect = SKRect.Create(0, 0, len * iconSize, iconSize);
					canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
					string[] stringToJoin = WordOf(sayReadyGetSetGo.PhraseToSay).Split(' ');
					List<string> stringToDraw = [stringToJoin[0]];
					int lw = canvas.MeasureRDFontText(stringToJoin[0]);
					int sw = canvas.MeasureRDFontText(' ');
					for (int i = 1; i < stringToJoin.Length; i++)
					{
						string? part = stringToJoin[i];
						int w = canvas.MeasureRDFontText(part);
						if (lw + w + sw > (len * iconSize - 2) * style.Scale)
						{
							lw = w;
							stringToDraw.Add(part);
						}
						else
						{
							lw += w + sw;
							stringToDraw[stringToDraw.Count - 1] += ' ' + part;
						}
					}
					stringToDraw = [.. stringToDraw.Take(3)];
					int c = stringToDraw.Count;
					float top = (iconSize - charHeight * c / 2f) * style.Scale / 2;
					for (int i = 0; i < c; i++)
					{
						string line = stringToDraw[i];
						SKPoint p = new(
							(len * iconSize - canvas.MeasureRDFontText(line, style.Scale / 2)) / 2,
							top + (i * charHeight + lineHeight) * style.Scale / 2);
						canvas.DrawRDFontText(line, p,
							SKColors.White, style.Scale / 2);
					}
				}
				break;
			case SetRowXs setRowXs:
				{
					destRect = SKRect.Create(0, 0, info.Bounds.Width, info.Bounds.Height);
					canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
					float width = info.Bounds.Width / 6;
					float iconwidth = AssetManager._slices.TryGetValue(beatx, out SliceInfo info2) ? info2.Bounds.Width : throw new NotImplementedException();
					float s = width / iconwidth;
					float left = 0;
					float top = iconSize / 2 - info2.Bounds.Height * s / 2;
					foreach (var p in setRowXs.Pattern)
					{
						if (p is Pattern.X)
							canvas.DrawSlice(beatx, SKRect.Create(
								left, top,
								width, info2.Bounds.Height * s));
						else
							canvas.DrawSlice(beatline, SKRect.Create(
								left, top,
								width, info2.Bounds.Height * s));
						left += width;
					}
					if (setRowXs.SyncoBeat >= 0)
						canvas.DrawSlice(beatsynco, SKRect.Create(
								width * setRowXs.SyncoBeat, top,
							width, info2.Bounds.Height * s));
				}
				break;
			case ShowRooms showRooms:
				{
					destRect = SKRect.Create(0, 0, iconSize, iconSize * 4);
					canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
					for (int i = 0; i < 4; i++)
						canvas.DrawSlice($"{key}_{(showRooms.Rooms[(byte)i] ? "1" : "0")}", new SKPoint(0, i * iconSize), scale: 0.5f);
				}
				break;
			default:
				destRect = SKRect.Create(0, 0, info.Bounds.Width, info.Bounds.Height);
				canvas.DrawBack(destRect, ColorOf(evt.Tab), style);
				switch (evt)
				{
					case CustomFlash customFlash:
						canvas.DrawSlice($"{key}_0", dest, ToSKColor(customFlash.StartColor?.Value ?? RDColor.Transparent));
						canvas.DrawSlice($"{key}_1", dest, ToSKColor(customFlash.EndColor?.Value ?? RDColor.Transparent));
						break;
					case FlipScreen flipScreen:
						canvas.DrawSlice($"{key}{((flipScreen.FlipX, flipScreen.FlipY) switch
						{
							(false, false) => "",
							(false, true) => "_0",
							(true, false) => "_1",
							(true, true) => "_2",
						})}", dest);
						break;
					case FloatingText floatingText:
						canvas.DrawSlice($"{key}_0", dest, ToSKColor(floatingText.Color));
						canvas.DrawSlice($"{key}_1", dest, ToSKColor(floatingText.OutlineColor));
						break;
					case MoveRoom moveRoom:
						canvas.Save();
						{
							float degree = moveRoom.Angle ?? 0;
							float width = moveRoom.Scale?.Width ?? 0;
							float height = moveRoom.Scale?.Height ?? 0;
							float pleft = (moveRoom.Pivot?.X ?? 0) / 100f;
							float ptop = 1f - (moveRoom.Pivot?.Y ?? 0) / 100f;
							if (width == 0 || height == 0) break;
							float uniform =
#if NET8_0_OR_GREATER
								float.Sqrt
#else
								(float)Math.Sqrt
#endif
								(width * width + height * height);
							width /= uniform;
							height /= uniform;
							canvas.Translate(destRect.Width / 2, destRect.Height / 2);
							canvas.RotateDegrees(-degree);
							canvas.Scale(width, height);
							canvas.DrawSlice(key, new SKPoint(-destRect.Width / 2, -destRect.Height / 2));
							if (pleft is < 0 or > 1 || ptop is < 0 or > 1) break;
							canvas.DrawPoint((pleft - 0.5f) * destRect.Width, (ptop - 0.5f) * destRect.Height,
								new SKPaint() { Color = SKColors.Red, StrokeWidth = 2 });
						}
						canvas.Restore();
						break;
					case PaintHands paintHands:
						canvas.DrawSlice(key, dest, ToSKColor(paintHands.TintColor));
						switch (paintHands.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(paintHands.BorderColor));
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(paintHands.BorderColor));
								break;
						}
						break;
					case SetBackgroundColor setBackgroundColor:
						{
							canvas.DrawSlice(key, dest);
							switch (setBackgroundColor.BackgroundType)
							{
								case BackgroundType.Color:
									canvas.DrawSlice($"{key}_0", dest,
										ToSKColor(setBackgroundColor.Color));
									break;
								case BackgroundType.Image:
									canvas.DrawSlice($"{key}_0", dest, SKColors.White);
									if (setBackgroundColor.Images.Count == 0 || !AssetManager._slices.TryGetValue($"{key}_1", out SliceInfo info2))
										break;
									string path = FilepathOf([], setBackgroundColor.Images[0]);
									if (!File.Exists(path))
										break;
									SKBitmap bitmap = SKBitmap.Decode(path);
									SKRect imgDest = SKRect.Create(-info2.Pivot.X, -info2.Pivot.Y,
										bitmap.Width, bitmap.Height);
									canvas.DrawBitmap(bitmap, imgDest);
									break;
							}
						}
						break;
					case SetCrotchetsPerBar setCrotchetsPerBar:
						int cpb = setCrotchetsPerBar.CrotchetsPerBar;
						canvas.DrawSlice(key, dest);
						canvas.DrawRDFontText(cpb > 9 ? "-" : cpb.ToString(), new(2, 7), SKColors.Black);
						canvas.DrawRDFontText("4", new(8, 12), SKColors.Black);
						break;
					case SetForeground setForeground:
						{
							canvas.DrawSlice(key, dest);
							if (setForeground.Images.Count == 0 || !AssetManager._slices.TryGetValue($"{key}_1", out SliceInfo info2))
								break;
							string path = FilepathOf([], setForeground.Images[0]);
							if (!File.Exists(path))
								break;
							SKBitmap bitmap = SKBitmap.Decode(path);
							SKRect imgDest = SKRect.Create(-info2.Pivot.X, -info2.Pivot.Y,
								bitmap.Width, bitmap.Height);
							canvas.DrawBitmap(bitmap, imgDest);
						}
						break;
					case Tint tint:
						canvas.DrawSlice(key, dest, ToSKColor(tint.TintColor));
						switch (tint.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(tint.BorderColor));
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(tint.BorderColor));
								break;
						}
						break;
					case TintRows tintRows:
						canvas.DrawSlice(key, dest, ToSKColor(tintRows.TintColor));
						switch (tintRows.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(tintRows.BorderColor));
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(tintRows.BorderColor));
								break;
						}
						break;
					default:
						canvas.DrawImage(AssetManager._assetFile, info.Bounds, destRect);
						break;
				}
				break;
		}
		if (evt is IDurationEvent durationEvent && (style.Active || style.ShowDuration))
		{
			float duration = durationEvent.Duration;
			float durwidth = iconSize * duration - destRect.Width;
			if (durwidth > 0)
				canvas.DrawSlice(evbarea,
					SKRect.Create(destRect.Width, 0, durwidth, destRect.Height),
					ColorOf(evt.Tab).WithAlpha(style.Active ? (byte)192 : (byte)91), style: PatchStyle.Repeat);
		}
		if ((evt is IRoomEvent || evt is ISingleRoomEvent) && style.Active)
		{
			RDRoom room = evt switch
			{
				IRoomEvent roomEvent => roomEvent.Rooms,
				ISingleRoomEvent singleRoomEvent => singleRoomEvent.Room,
				_ => throw new NotImplementedException(),
			};
			const uint roomEnabled = 0xffd8b811;
			const uint roomDisabled = 0xff5b5b5b;
			canvas.DrawSlice("room_0", new SKPoint(destRect.Right, 0), (SKColor)(room.Contains(RDRoomIndex.Room1) ? roomEnabled : roomDisabled));
			canvas.DrawSlice("room_1", new SKPoint(destRect.Right, 0), (SKColor)(room.Contains(RDRoomIndex.Room2) ? roomEnabled : roomDisabled));
			canvas.DrawSlice("room_2", new SKPoint(destRect.Right, 0), (SKColor)(room.Contains(RDRoomIndex.Room3) ? roomEnabled : roomDisabled));
			canvas.DrawSlice("room_3", new SKPoint(destRect.Right, 0), (SKColor)(room.Contains(RDRoomIndex.Room4) ? roomEnabled : roomDisabled));
			if (room.Contains(RDRoomIndex.RoomTop))
				canvas.DrawSlice("room_top", new SKPoint(destRect.Right, 0), (SKColor)(roomEnabled));
		}
		if (evt.Condition.HasValue)
		{
			bool hasTrue = evt.Condition.ConditionLists.Any(i => i.Value);
			bool hasFalse = evt.Condition.ConditionLists.Any(i => !i.Value);
			if (hasTrue)
				if (hasFalse)
					canvas.DrawSlice(evtag, dest, 0xffffff00);
				else
					canvas.DrawSlice(evtag, dest, 0xff00ffff);
			else if (hasFalse)
				canvas.DrawSlice(evtag, dest, 0xffff0000);
		}
		if (!string.IsNullOrEmpty(evt.Tag))
		{
			canvas.DrawSlice($"{evtag}_0", dest + new SKPointI(0, 8), 0xffffc786);
		}
		canvas.Restore();
		destRect.Right *= style.Scale;
		destRect.Bottom *= style.Scale;
		destRect.Location = dest;
		return destRect;
	}
	public static SKSize MeasureEventIcon<TEvent>(this TEvent evt, IconStyle style)
	where TEvent : IBaseEvent
	{
		SKSize dest = default;
		string key = $"event_{evt.Type}";
		EventType evttype = evt.Type;
		if (!AssetManager._slices.TryGetValue(key, out SliceInfo info))
		{
			if (!AssetManager._slices.TryGetValue($"event_Unknown", out info))
			{
				throw new NotImplementedException();
			}
		}
		switch (evt)
		{
			case AddClassicBeat addClassicBeat:
				{
					float tick = addClassicBeat.Tick;
					float swing = addClassicBeat.Swing;
					SetRowXs? prexs = addClassicBeat.Beat.IsEmpty ? null : addClassicBeat.FrontOrDefault<SetRowXs>();
					if (swing == 0) swing = 1f;
					dest = new(iconSize * (tick * (addClassicBeat.Length - 1 - (prexs?.SyncoSwing ?? 0))), iconSize);
				}
				break;
			case AddOneshotBeat oneshotBeat:
				{
					float tick = oneshotBeat.Tick;
					float interval = oneshotBeat.Interval;
					int loop = (int)oneshotBeat.Loops;
					float delay = oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? oneshotBeat.Delay : 0;
					dest = new(iconSize * (loop * interval + tick + delay), iconSize);
				}
				break;
			case SayReadyGetSetGo sayReadyGetSetGo:
				{
					float len = LengthOf(sayReadyGetSetGo.PhraseToSay) * sayReadyGetSetGo.Tick + 1;
					dest = new(len * iconSize, iconSize);
				}
				break;
			case ReorderRooms or ReorderWindows or ShowRooms or DesktopColor:
				dest = new(info.Bounds.Width, info.Bounds.Width * 4);
				break;
			default:
				dest = new(info.Bounds.Width, info.Bounds.Height);
				break;
		}
		return dest;
	}
	internal enum PatchStyle
	{
		Repeat,
		Stretch,
	}
	private static void DrawNinePatch(SKCanvas canvas, SKImage srcBitmap, SliceInfo info, SKSize dest, SKPaint? paint, float scale = 1, PatchStyle style = PatchStyle.Stretch)
	{
		int[] srcXs = [0, info.Center.Left, info.Center.Right, info.Bounds.Width];
		int[] srcYs = [0, info.Center.Top, info.Center.Bottom, info.Bounds.Height];
		float[] dstXs = [0, info.Center.Left, dest.Width - (info.Bounds.Width - info.Center.Right), dest.Width];
		float[] dstYs = [0, info.Center.Top, dest.Height - (info.Bounds.Height - info.Center.Bottom), dest.Height];
		for (int row = 0; row < 3; row++)
		{
			for (int col = 0; col < 3; col++)
			{
				int srcWidth = srcXs[row + 1] - srcXs[row];
				int srcHeight = srcYs[col + 1] - srcYs[col];
				float dstWidth = dstXs[row + 1] - dstXs[row];
				float dstHeight = dstYs[col + 1] - dstYs[col];
				if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0)
					continue;
				SKRectI srcRect = new(srcXs[row], srcYs[col], srcXs[row + 1], srcYs[col + 1]);
				SKRect dstRect = new(dstXs[row], dstYs[col], dstXs[row + 1], dstYs[col + 1]);
				srcRect.Offset(info.Bounds.Location);
				if (row == 1 && col == 1 && (dstRect.Width > 0 || dstRect.Height > 0) && style is PatchStyle.Repeat)
				{
					for (float x = 0; x < dstWidth; x += srcWidth)
					{
						float tw = Math.Min(srcWidth, dstWidth - x);
						for (float y = 0; y < dstHeight; y += srcHeight)
						{
							float th = Math.Min(srcHeight, dstHeight - y);
							var sRect = SKRectI.Create(srcRect.Left, srcRect.Top, (int)tw, (int)th);
							var dRect = SKRect.Create(dstRect.Left + x, dstRect.Top + y, tw, th);
							canvas.DrawImage(srcBitmap, sRect, dRect, paint);
						}
					}
				}
				else
				{
					canvas.DrawImage(srcBitmap, srcRect, dstRect, paint);
				}
			}
		}
		//int sx0 = info.Bounds.Left;
		//int sx3 = info.Bounds.Right;
		//int sy0 = info.Bounds.Top;
		//int sy3 = info.Bounds.Bottom;

		//int sx1 = sx0 + info.Center.Left;
		//int sx2 = sx0 + info.Center.Right;
		//int sy1 = sy0 + info.Center.Top;
		//int sy2 = sy0 + info.Center.Bottom;

		//int swLeft = sx1 - sx0;
		//int swCenter = sx2 - sx1;
		//int swRight = sx3 - sx2;

		//int shTop = sy1 - sy0;
		//int shCenter = sy2 - sy1;
		//int shBottom = sy3 - sy2;

		//float dwLeft = swLeft * Math.Max(1, scale);
		//float dwRight = swRight * Math.Max(1, scale);
		//float dwCenter = destRect.Width - dwLeft - dwRight;
		//if (dwCenter < 0)
		//{
		//	float scaleX = (float)destRect.Width / Math.Max(1, swLeft + swRight);
		//	dwLeft = Math.Max(0, (int)Math.Round(swLeft * scaleX));
		//	dwRight = Math.Max(0, destRect.Width - dwLeft);
		//	dwCenter = 0;
		//}

		//float dhTop = shTop * Math.Max(1, scale);
		//float dhBottom = shBottom * Math.Max(1, scale);
		//float dhCenter = destRect.Height - dhTop - dhBottom;
		//if (dhCenter < 0)
		//{
		//	float scaleY = (float)destRect.Height / Math.Max(1, shTop + shBottom);
		//	dhTop = Math.Max(0, (int)Math.Round(shTop * scaleY));
		//	dhBottom = Math.Max(0, destRect.Height - dhTop);
		//	dhCenter = 0;
		//}

		//int[] srcXs = [sx0, sx1, sx2, sx3];
		//int[] srcYs = [sy0, sy1, sy2, sy3];

		//float dx0 = destRect.Left;
		//float dx1 = dx0 + dwLeft;
		//float dx2 = dx1 + dwCenter;
		//float dx3 = destRect.Right;

		//float dy0 = destRect.Top;
		//float dy1 = dy0 + dhTop;
		//float dy2 = dy1 + dhCenter;
		//float dy3 = destRect.Bottom;

		//float[] dstXs = [dx0, dx1, dx2, dx3];
		//float[] dstYs = [dy0, dy1, dy2, dy3];

		//for (int row = 0; row < 3; row++)
		//{
		//	for (int col = 0; col < 3; col++)
		//	{
		//		int sLeft = srcXs[col];
		//		int sTop = srcYs[row];
		//		int sRight = srcXs[col + 1];
		//		int sBottom = srcYs[row + 1];
		//		int sW = sRight - sLeft;
		//		int sH = sBottom - sTop;
		//		if (sW <= 0 || sH <= 0)
		//			continue;

		//		float dLeft = dstXs[col];
		//		float dTop = dstYs[row];
		//		float dRight = dstXs[col + 1];
		//		float dBottom = dstYs[row + 1];
		//		float dW = dRight - dLeft;
		//		float dH = dBottom - dTop;
		//		if (dW <= 0 || dH <= 0)
		//			continue;

		//		switch (style)
		//		{
		//			case PatchStyle.Stretch:
		//				var srcRect = SKRect.Create(sLeft, sTop, sW, sH);
		//				var dstRect = SKRect.Create(dLeft, dTop, dW, dH);

		//				canvas.DrawImage(srcBitmap, srcRect, dstRect, paint);
		//				break;
		//			case PatchStyle.Repeat:
		//				for (float y = dTop; y < dBottom; y += sH * scale)
		//				{
		//					float th = Math.Min(sH, (dBottom - y) / scale);
		//					for (float x = dLeft; x < dRight; x += sW * scale)
		//					{
		//						float tw = Math.Min(sW, (dRight - x) / scale);
		//						var sRect = SKRect.Create(sLeft, sTop, tw, th);
		//						var dRect = SKRect.Create(x, y, tw * scale, th * scale);
		//						canvas.DrawImage(srcBitmap, sRect, dRect, paint);
		//					}
		//				}
		//				break;
		//		}
		//	}
		//}
	}
	internal static SKColor ColorOf(Tab tab)
	{
		return tab switch
		{
			Tab.Sounds => AssetManager.Colors[0],
			Tab.Rows => AssetManager.Colors[1],
			Tab.Actions => AssetManager.Colors[2],
			Tab.Rooms => AssetManager.Colors[3],
			Tab.Decorations => AssetManager.Colors[4],
			Tab.Windows => AssetManager.Colors[5],
			Tab.Unknown or _ => AssetManager.Colors[6],
		};
	}
	private static SKColor ToSKColor(RDColor color) => (uint)color;
	private static int LengthOf(SayReadyGetSetGoWord words)
	{
		return words switch
		{
			SayReadyGetSetGoWord.SayReaDyGetSetGoNew or
			SayReadyGetSetGoWord.SayReaDyGetSetOne or
			SayReadyGetSetGoWord.SayReadyGetSetGo => 4,
			SayReadyGetSetGoWord.SayGetSetGo or
			SayReadyGetSetGoWord.SayGetSetOne => 2,
			_ => 0,
		};
	}
	private static string WordOf(SayReadyGetSetGoWord words)
	{
		return words switch
		{
			SayReadyGetSetGoWord.SayReaDyGetSetGoNew => "Rea, Dy, Get, Set, Go!",
			SayReadyGetSetGoWord.SayReaDyGetSetOne => "Rea, Dy, Get, Set, One!",
			SayReadyGetSetGoWord.SayGetSetGo => "Get, Set, Go!",
			SayReadyGetSetGoWord.SayGetSetOne => "Get, Set, One!",
			SayReadyGetSetGoWord.JustSayRea => "Rea",
			SayReadyGetSetGoWord.JustSayDy => "Dy",
			SayReadyGetSetGoWord.JustSayGet => "Get",
			SayReadyGetSetGoWord.JustSaySet => "Set",
			SayReadyGetSetGoWord.JustSayAnd => "And",
			SayReadyGetSetGoWord.JustSayGo => "Go!",
			SayReadyGetSetGoWord.JustSayStop => "Stop",
			SayReadyGetSetGoWord.JustSayAndStop => "And Stop!",
			SayReadyGetSetGoWord.SaySwitch => "Switch",
			SayReadyGetSetGoWord.SayWatch => "Watch",
			SayReadyGetSetGoWord.SayListen => "Listen",
			SayReadyGetSetGoWord.Count1 => "1",
			SayReadyGetSetGoWord.Count2 => "2",
			SayReadyGetSetGoWord.Count3 => "3",
			SayReadyGetSetGoWord.Count4 => "4",
			SayReadyGetSetGoWord.Count5 => "5",
			SayReadyGetSetGoWord.Count6 => "6",
			SayReadyGetSetGoWord.Count7 => "7",
			SayReadyGetSetGoWord.Count8 => "8",
			SayReadyGetSetGoWord.Count9 => "9",
			SayReadyGetSetGoWord.Count10 => "10",
			SayReadyGetSetGoWord.SayReadyGetSetGo => "Ready, Get, Set, Go!",
			SayReadyGetSetGoWord.JustSayReady => "Ready",
			_ => throw new NotImplementedException(),
		};
	}
	private static string FilepathOf(RDLevel level, string relativePath)
	{
		if (Path.IsPathRooted(relativePath))
			return relativePath;
		else
			return Path.Combine(Path.GetDirectoryName(level.Directory) ?? string.Empty, relativePath ?? string.Empty);
	}
	private static void DrawBack(this SKCanvas canvas, SKRect dest, SKColor color, IconStyle style)
	{
		color = color.WithState(style.Active, style.Enabled ?? true);
		const string outline = "event_outline";
		const string back = "event_back";
		canvas.DrawSlice(outline, dest, (SKColor)(style.Active ? 0xffffffff : 0xffa8a8a8));
		dest.Inflate(-1, -1);
		canvas.DrawSlice(back, dest, color, style: PatchStyle.Stretch);
	}
	private static SKColor WithState(this SKColor color, bool active, bool enabled) => (enabled ? color : 0xff848484).WithAlpha(active ? (byte)192 : (byte)91);
}
public record struct IconStyle
{
	public bool? Enabled { get; set; }
	public bool Active { get; set; }
	public bool Hover { get; set; }
	public int Scale { get; set; }
	public bool ShowDuration { get; set; }
}