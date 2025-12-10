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


public static class Extensions
{
	private const int lineHeight = 6;
	private const int charHeight = 8;
	private const int iconSize = 14;
	private const string evtag = "event_tag";
	private const string evbarea = "event_beat_area";
	internal static void DrawSlice(this SKCanvas canvas, string src, SKRect dest, int scale = 1, PatchStyle style = PatchStyle.Strentch)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return;

		if (info.IsNinePatch)
		{
			DrawNinePatch(canvas, AssetManager._assetFile, info, dest, null, scale, style);
		}
		else
		{
			canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, dest);
		}
	}
	internal static void DrawSlice(this SKCanvas canvas, string src, SKRect dest, SKColor replace, int scale = 1, PatchStyle style = PatchStyle.Strentch)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return;

		if (replace.Alpha == 0)
			return;

		float tr = replace.Red / 255f;
		float tg = replace.Green / 255f;
		float tb = replace.Blue / 255f;
		float ta = replace.Alpha / 255f;

		float[] colorMatrix =
		[
				0.2126f * tr, 0.7152f * tr, 0.0722f * tr, 0, 0,
								0.2126f * tg, 0.7152f * tg, 0.0722f * tg, 0, 0,
								0.2126f * tb, 0.7152f * tb, 0.0722f * tb, 0, 0,
								0,            0,            0,            ta, 0
		];

		using SKPaint paint = new()
		{
			ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix)
		};

		if (info.IsNinePatch)
		{
			DrawNinePatch(canvas, AssetManager._assetFile, info, dest, paint, scale, style);
		}
		else
		{
			canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, dest, paint);
		}
	}
	internal static SKRectI DrawSlice(this SKCanvas canvas, string src, SKPoint dest, int scale = 1)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return default;

		SKRect destRect = SKRect.Create(dest, new(info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale));
		destRect.Offset(-info.Pivot.X * scale, -info.Pivot.Y * scale);
		canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect);

		return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);
	}
	internal static SKRectI DrawSlice(this SKCanvas canvas, string src, SKPoint dest, SKColor replace, int scale = 1)
	{
		if (!AssetManager._slices.TryGetValue(src, out SliceInfo info))
			return default;

		SKRect destRect = SKRect.Create(dest, new(info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale));
		destRect.Offset(-info.Pivot.X * scale, -info.Pivot.Y * scale);
		// 不绘制完全透明的目标色，仍返回期望的 bounds
		if (replace.Alpha == 0)
			return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);

		float tr = replace.Red / 255f;
		float tg = replace.Green / 255f;
		float tb = replace.Blue / 255f;
		float ta = replace.Alpha / 255f;

		float[] colorMatrix =
		[
				0.2126f * tr, 0.7152f * tr, 0.0722f * tr, 0, 0,
								0.2126f * tg, 0.7152f * tg, 0.0722f * tg, 0, 0,
								0.2126f * tb, 0.7152f * tb, 0.0722f * tb, 0, 0,
								0,            0,            0,            ta, 0
		];

		using SKPaint paint = new()
		{
			ColorFilter = SKColorFilter.CreateColorMatrix(colorMatrix)
		};
		canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect, paint);

		return SKRectI.Create(info.Bounds.Left, info.Bounds.Top, info.Bounds.Size.Width * scale, info.Bounds.Size.Height * scale);
	}
	private static readonly ReadOnlyEnumCollection<EventType> _beatTypes = new(2,
		EventType.AddFreeTimeBeat,
		EventType.PulseFreeTimeBeat
	);
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
				SKRectI area = canvas.DrawSlice($"char_{(int)c:x4}", start, color, scale);
				result = SKRect.Union(result, SKRect.Create(start.X, start.Y, area.Width, area.Height));
				start.Offset(area.Width, 0);
			}
		}
		return result;
	}

	public static SKRectI DrawEventIcon<TEvent>(this SKCanvas canvas, TEvent evt, SKPointI dest, IconStyle style)
	where TEvent : IBaseEvent
	{
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

		const uint beatcolor = 0xff60e345;
		switch (evt)
		{
			case AddClassicBeat addClassicBeat:
				{
					float tick = addClassicBeat.Tick;
					float swing = addClassicBeat.Swing;
					float hold = addClassicBeat.Hold;
					SetRowXs? prexs = addClassicBeat.Beat.IsEmpty ? null : addClassicBeat.FrontOrDefault<SetRowXs>();
					if (swing == 0) swing = 1f;
					destRect = SKRect.Create(dest.X, dest.Y, iconSize * style.Scale * (tick * (addClassicBeat.Length - 1 - (prexs?.SyncoSwing ?? 0))), iconSize * style.Scale);
					if (hold > 0)
						canvas.DrawSlice(
							evbarea,
							SKRect.Create(dest.X + destRect.Width, dest.Y, iconSize * style.Scale * hold, iconSize * style.Scale),
							style.Active ? 0xffd046f3 : 0xff7e3990, style.Scale, PatchStyle.Repeat);
					canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					if (style.Hover)
						for (int i = 0; i < addClassicBeat.Length - 1; i++)
							canvas.DrawSlice(pulse, new SKPoint(dest.X + iconSize * style.Scale * (tick * (i + i % 2 * (1 - swing) - (i <= (prexs?.SyncoBeat ?? -1) ? 0 : (prexs?.SyncoSwing ?? 0)))), dest.Y), style.Scale);
					canvas.DrawSlice(hit, new SKPoint(dest.X + style.Scale * (iconSize * (tick * (addClassicBeat.Length - 1 - (prexs?.SyncoSwing ?? 0))) - 1), dest.Y), style.Scale);
				}
				break;
			case AddFreeTimeBeat addFreeTimeBeat:
				{
					float hold = addFreeTimeBeat.Hold - info.Bounds.Width / iconSize;
					destRect = SKRect.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * style.Scale);
					canvas.DrawBack(destRect, new SKColor(beatcolor).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					canvas.DrawRDFontText((addFreeTimeBeat.Pulse + 1).ToString(), new(dest.X + 1.5f * style.Scale, dest.Y + 10 * style.Scale), SKColors.White, style.Scale);
					if (addFreeTimeBeat.Pulse == 6)
						canvas.DrawSlice(hit, new SKPoint(dest.X, dest.Y), style.Scale);
					if (hold > 0)
						canvas.DrawSlice(evbarea,
							SKRect.Create(dest.X + destRect.Width, dest.Y, iconSize * style.Scale * hold, info.Bounds.Height * style.Scale),
							style.Active ? 0xffd046f3 : 0xff7e3990, style.Scale, PatchStyle.Repeat);
				}
				break;
			case AddOneshotBeat oneshotBeat:
				{
					float tick = oneshotBeat.Tick;
					float interval = oneshotBeat.Interval;
					int loop = (int)oneshotBeat.Loops;
					int subdiv = oneshotBeat.Subdivisions;
					float delay = oneshotBeat.FreezeBurnMode is OneshotType.Freezeshot ? oneshotBeat.Delay : 0;
					destRect = SKRect.Create(dest.X, dest.Y, iconSize * style.Scale * (loop * interval + tick + delay), iconSize * style.Scale);
					canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					float subdivwidth = iconSize * style.Scale * (subdiv - 1) / subdiv * tick;
					if (subdiv > 1)
						canvas.DrawSlice(evbarea,
							SKRect.Create(dest.X + destRect.Width, dest.Y, subdivwidth, info.Bounds.Height * style.Scale),
							0xff13B021, style.Scale, PatchStyle.Repeat);
					if (oneshotBeat.Skipshot)
						canvas.DrawSlice(evbarea,
							SKRect.Create(dest.X + destRect.Width + subdivwidth, dest.Y, style.Scale * iconSize * interval - subdivwidth, info.Bounds.Height * style.Scale),
							0xffc53b3b, style.Scale, PatchStyle.Repeat);
					if (style.Hover)
						for (int l = 0; l <= loop; l++)
							for (int i = 0; i < subdiv; i++)
								canvas.DrawSlice(pulse, new SKPoint(dest.X + iconSize * style.Scale * (l * interval + i * tick / subdiv), dest.Y), style.Scale);
					for (int l = 0; l <= loop; l++)
						for (int i = 0; i < subdiv; i++)
							canvas.DrawSlice(hit, new SKPoint(dest.X + style.Scale * (iconSize * (l * interval + delay + tick + (i * tick / subdiv)) - 1), dest.Y), style.Scale);

					float off = interval - tick;
					if (oneshotBeat.FreezeBurnMode is not OneshotType.Wave)
					{
						float posx = dest.X - off * style.Scale * iconSize;
						canvas.DrawSlice(beatcross,
							new SKPoint(posx, dest.Y),
							style.Scale);
						switch (oneshotBeat.FreezeBurnMode)
						{
							case OneshotType.Freezeshot:
								posx += delay * style.Scale * iconSize;
								canvas.DrawSlice(beatcross,
									new SKPoint(posx, dest.Y),
									style.Scale);
								for (int l = 0; l <= loop; l++)
									canvas.DrawSlice(hitf, new SKPoint(dest.X + style.Scale * (iconSize * (l * interval + tick) - 1), dest.Y), style.Scale);
								break;
							case OneshotType.Burnshot:
								posx -= tick * style.Scale * iconSize;
								canvas.DrawSlice(beatcross,
									new SKPoint(posx, dest.Y),
									style.Scale);
								for (int l = 0; l <= loop; l++)
									canvas.DrawSlice(hitb, new SKPoint(dest.X + style.Scale * (iconSize * (l * interval + tick) - 1), dest.Y), style.Scale);
								break;
							default:
								break;
						}
					}
					if(style is { Active:true, Hover: true })
						canvas.DrawSlice("event_beat_loop", new SKPoint(destRect.Right, destRect.Top), style.Scale);
				}
				break;
			case Comment comment:
				{
					SKColor color = (uint)(comment.Color.Value);
					destRect = SKRect.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * style.Scale);
					canvas.DrawBack(
						destRect,
						color.WithState(style.Active, style.Enabled ?? evt.Active),
						style.Active,
						style.Scale);
					canvas.DrawSlice(key, dest, style.Scale);
				}
				break;
			case DesktopColor desktopColor:
				{
					SKColor color = (uint)(desktopColor.StartColor?.Value ?? RDColor.Transparent);
					destRect = SKRect.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * 4 * style.Scale);
					canvas.DrawBack(
						destRect,
						ColorOf(desktopColor.Tab).WithState(style.Active, style.Enabled ?? evt.Active),
						style.Active,
						style.Scale);
					canvas.DrawSlice($"{key}_0", dest with { Y = dest.Y + info.Bounds.Height * style.Scale }, ToSKColor(desktopColor.EndColor?.Value ?? RDColor.Transparent), style.Scale);
					canvas.DrawSlice(key, dest with { Y = dest.Y + info.Bounds.Height * style.Scale }, style.Scale);
					float duration = desktopColor.Duration;
					canvas.DrawSlice(evbarea,
						SKRect.Create(dest.X + destRect.Width, dest.Y, style.Scale * (iconSize * duration - info.Bounds.Width), destRect.Height),
						ColorOf(evt.Tab).WithAlpha(style.Active ? (byte)192 : (byte)91), style.Scale, PatchStyle.Repeat);
				}
				break;
			case PulseFreeTimeBeat pulseFreeTimeBeat:
				{
					float hold = pulseFreeTimeBeat.Hold - info.Bounds.Width / iconSize;
					destRect = SKRect.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * style.Scale);
					canvas.DrawBack(destRect, new SKColor(beatcolor).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					canvas.DrawRDFontText(pulseFreeTimeBeat.Action switch
					{
						PulseActions.Increment => ">",
						PulseActions.Decrement => "<",
						PulseActions.Remove => "x",
						PulseActions.Custom or
						_ => (pulseFreeTimeBeat.CustomPulse + 1).ToString(),
					}, new(dest.X + 1.5f * style.Scale, dest.Y + 8 * style.Scale), SKColors.White, style.Scale);
					if (pulseFreeTimeBeat is { Action: PulseActions.Custom, CustomPulse: 7 })
						canvas.DrawSlice(hit, SKRect.Create(dest.X - 2 * style.Scale, dest.Y, 5 * style.Scale, info.Bounds.Height * style.Scale));
					if (hold > 0)
						canvas.DrawSlice(evbarea,
							SKRect.Create(dest.X + destRect.Width, dest.Y, iconSize * style.Scale * hold, info.Bounds.Height * style.Scale),
							style.Active ? 0xffd046f3 : 0xff7e3990, style.Scale, PatchStyle.Repeat);
				}
				break;
			case ReorderRooms reorderRooms:
				destRect = SKRect.Create(dest.X, dest.Y, iconSize * style.Scale, iconSize * 4 * style.Scale);
				canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
				foreach (var r in reorderRooms.Order.Order)
				{
					canvas.DrawSlice($"{key}_{r}", dest, style.Scale);
					dest.Offset(0, iconSize * style.Scale);
				}
				break;
			case ReorderWindows reorderWindows:
				destRect = SKRect.Create(dest.X, dest.Y, iconSize * style.Scale, iconSize * 4 * style.Scale);
				canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
				foreach (var r in reorderWindows.Order.Order)
				{
					canvas.DrawSlice($"{key}_{r}", dest, style.Scale);
					dest.Offset(0, iconSize * style.Scale);
				}
				break;
			case SayReadyGetSetGo sayReadyGetSetGo:
				{
					float len = LengthOf(sayReadyGetSetGo.PhraseToSay) * sayReadyGetSetGo.Tick + 1;
					destRect = SKRect.Create(dest.X, dest.Y, len * iconSize * style.Scale, iconSize * style.Scale);
					canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
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
							stringToDraw[^1] += ' ' + part;
						}
					}
					stringToDraw = [.. stringToDraw.Take(3)];
					int c = stringToDraw.Count;
					float top = dest.Y + (iconSize - charHeight * c / 2f) * style.Scale / 2;
					for (int i = 0; i < c; i++)
					{
						string line = stringToDraw[i];
						SKPoint p = new(
							dest.X + (len * iconSize * style.Scale - canvas.MeasureRDFontText(line, style.Scale / 2)) / 2,
							top + (i * charHeight + lineHeight) * style.Scale / 2);
						canvas.DrawRDFontText(line, p,
							SKColors.White, style.Scale / 2);
					}
				}
				break;
			case SetRowXs setRowXs:
				{
					destRect = SKRect.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * style.Scale);
					canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					float width = info.Bounds.Width * style.Scale / 6;
					float iconwidth = AssetManager._slices.TryGetValue(beatx, out SliceInfo info2) ? info2.Bounds.Width : throw new NotImplementedException();
					float s = width / iconwidth;
					float left = 0;
					float top = iconSize * style.Scale / 2 - info2.Bounds.Height * s / 2;
					foreach (var p in setRowXs.Pattern)
					{
						if (p is Pattern.X)
							canvas.DrawSlice(beatx, SKRect.Create(
								dest.X + left, dest.Y + top,
								width, info2.Bounds.Height * s), style.Scale);
						else
							canvas.DrawSlice(beatline, SKRect.Create(
								dest.X + left, dest.Y + top,
								width, info2.Bounds.Height * s), style.Scale);
						left += width;
					}
					if (setRowXs.SyncoBeat >= 0)
						canvas.DrawSlice(beatsynco, SKRect.Create(
							dest.X + width * setRowXs.SyncoBeat, dest.Y + top,
							width, info2.Bounds.Height * s), style.Scale);
				}
				break;
			case ShowRooms showRooms:
				{
					destRect = SKRect.Create(dest.X, dest.Y, iconSize * style.Scale, iconSize * 4 * style.Scale);
					canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
					for (int i = 0; i < 4; i++)
						canvas.DrawSlice($"{key}_{(showRooms.Rooms[(byte)i] ? "1" : "0")}", new SKPoint(dest.X, dest.Y + i * iconSize * style.Scale), style.Scale / 2);
				}
				break;
			default:
				destRect = SKRectI.Create(dest.X, dest.Y, info.Bounds.Width * style.Scale, info.Bounds.Height * style.Scale);
				canvas.DrawBack(destRect, ColorOf(evt.Tab).WithState(style.Active, style.Enabled ?? evt.Active), style.Active, style.Scale);
				switch (evt)
				{
					case CustomFlash customFlash:
						canvas.DrawSlice($"{key}_0", dest, ToSKColor(customFlash.StartColor?.Value ?? RDColor.Transparent), style.Scale);
						canvas.DrawSlice($"{key}_1", dest, ToSKColor(customFlash.EndColor?.Value ?? RDColor.Transparent), style.Scale);
						break;
					case FlipScreen flipScreen:
						canvas.DrawSlice($"{key}{((flipScreen.FlipX, flipScreen.FlipY) switch
						{
							(false, false) => "",
							(false, true) => "_0",
							(true, false) => "_1",
							(true, true) => "_2",
						})}", dest, style.Scale);
						break;
					case FloatingText floatingText:
						canvas.DrawSlice($"{key}_0", dest, ToSKColor(floatingText.Color), style.Scale);
						canvas.DrawSlice($"{key}_1", dest, ToSKColor(floatingText.OutlineColor), style.Scale);
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
							float uniform = float.Sqrt(width * width + height * height);
							width /= uniform;
							height /= uniform;
							canvas.Translate(dest.X + destRect.Width / 2, dest.Y + destRect.Height / 2);
							canvas.RotateDegrees(-degree);
							canvas.Scale(width, height);
							canvas.DrawSlice(key, new SKPoint(-destRect.Width / 2, -destRect.Height / 2), style.Scale);
							if (pleft is < 0 or > 1 || ptop is < 0 or > 1) break;
							canvas.DrawPoint((pleft - 0.5f) * destRect.Width, (ptop - 0.5f) * destRect.Height,
								new SKPaint() { Color = SKColors.Red, StrokeWidth = 2 });
						}
						canvas.Restore();
						break;
					case PaintHands paintHands:
						canvas.DrawSlice(key, dest, ToSKColor(paintHands.TintColor), style.Scale);
						switch (paintHands.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(paintHands.BorderColor), style.Scale);
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(paintHands.BorderColor), style.Scale);
								break;
						}
						break;
					case SetBackgroundColor setBackgroundColor:
						{
							canvas.DrawSlice(key, dest, style.Scale);
							switch (setBackgroundColor.BackgroundType)
							{
								case BackgroundType.Color:
									canvas.DrawSlice($"{key}_0", dest,
										ToSKColor(setBackgroundColor.Color),
										style.Scale);
									break;
								case BackgroundType.Image:
									canvas.DrawSlice($"{key}_0", dest, SKColors.White, style.Scale);
									if (setBackgroundColor.Images.Count == 0 || !AssetManager._slices.TryGetValue($"{key}_1", out SliceInfo info2))
										break;
									string path = FilepathOf([], setBackgroundColor.Images[0]);
									if (!File.Exists(path))
										break;
									SKBitmap bitmap = SKBitmap.Decode(path);
									SKRect imgDest = SKRect.Create(dest.X - info2.Pivot.X * style.Scale, dest.Y - info2.Pivot.Y * style.Scale,
										bitmap.Width * style.Scale, bitmap.Height * style.Scale);
									canvas.DrawBitmap(bitmap, imgDest);
									break;
							}
						}
						break;
					case SetCrotchetsPerBar setCrotchetsPerBar:
						int cpb = setCrotchetsPerBar.CrotchetsPerBar;
						canvas.DrawSlice(key, dest, style.Scale);
						canvas.DrawRDFontText(cpb > 9 ? "-" : cpb.ToString(), new(dest.X + 2 * style.Scale, dest.Y + 7 * style.Scale), SKColors.Black, style.Scale);
						canvas.DrawRDFontText("4", new(dest.X + 8 * style.Scale, dest.Y + 12 * style.Scale), SKColors.Black, style.Scale);
						break;
					case SetForeground setForeground:
						{
							canvas.DrawSlice(key, dest, style.Scale);
							if (setForeground.Images.Count == 0 || !AssetManager._slices.TryGetValue($"{key}_1", out SliceInfo info2))
								break;
							string path = FilepathOf([], setForeground.Images[0]);
							if (!File.Exists(path))
								break;
							SKBitmap bitmap = SKBitmap.Decode(path);
							SKRect imgDest = SKRect.Create(dest.X - info2.Pivot.X * style.Scale, dest.Y - info2.Pivot.Y * style.Scale,
								bitmap.Width * style.Scale, bitmap.Height * style.Scale);
							canvas.DrawBitmap(bitmap, imgDest);
						}
						break;
					case Tint tint:
						canvas.DrawSlice(key, dest, ToSKColor(tint.TintColor), style.Scale);
						switch (tint.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(tint.BorderColor), style.Scale);
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(tint.BorderColor), style.Scale);
								break;
						}
						break;
					case TintRows tintRows:
						canvas.DrawSlice(key, dest, ToSKColor(tintRows.TintColor), style.Scale);
						switch (tintRows.Border)
						{
							case Border.Outline:
								canvas.DrawSlice($"{key}_0", dest, ToSKColor(tintRows.BorderColor), style.Scale);
								break;
							case Border.Glow:
								canvas.DrawSlice($"{key}_1", dest, ToSKColor(tintRows.BorderColor), style.Scale);
								break;
						}
						break;
					default:
						canvas.DrawBitmap(AssetManager._assetFile, info.Bounds, destRect);
						break;
				}
				if (evt is IDurationEvent durationEvent && style.Active)
				{
					float duration = durationEvent.Duration;
					canvas.DrawSlice(evbarea,
						SKRect.Create(dest.X + destRect.Width, dest.Y, style.Scale * (iconSize * duration - info.Bounds.Width), info.Bounds.Height * style.Scale),
						ColorOf(evt.Tab).WithAlpha(style.Active ? (byte)192 : (byte)91), style.Scale, PatchStyle.Repeat);
				}
				break;
		}
		if (evt.Condition.HasValue)
		{
			bool hasTrue = evt.Condition.ConditionLists.Any(i => i.Value);
			bool hasFalse = evt.Condition.ConditionLists.Any(i => !i.Value);
			if (hasTrue)
				if (hasFalse)
					canvas.DrawSlice(evtag, dest, 0xffffff00, style.Scale);
				else
					canvas.DrawSlice(evtag, dest, 0xff00ffff, style.Scale);
			else if (hasFalse)
				canvas.DrawSlice(evtag, dest, 0xffff0000, style.Scale);
		}
		if (!string.IsNullOrEmpty(evt.Tag))
		{
			canvas.DrawSlice($"{evtag}_0", dest + new SKPointI(0, 8 * style.Scale), 0xffffc786, style.Scale);
		}
		return SKRectI.Round(destRect);
	}
	internal enum PatchStyle
	{
		Repeat,
		Strentch,
	}
	private static void DrawNinePatch(SKCanvas canvas, SKBitmap srcBitmap, SliceInfo info, SKRect destRect, SKPaint? paint, int scale = 1, PatchStyle style = PatchStyle.Strentch)
	{
		int sx0 = info.Bounds.Left;
		int sx3 = info.Bounds.Right;
		int sy0 = info.Bounds.Top;
		int sy3 = info.Bounds.Bottom;

		int sx1 = sx0 + info.Center.Left;
		int sx2 = sx0 + info.Center.Right;
		int sy1 = sy0 + info.Center.Top;
		int sy2 = sy0 + info.Center.Bottom;

		int swLeft = sx1 - sx0;
		int swCenter = sx2 - sx1;
		int swRight = sx3 - sx2;

		int shTop = sy1 - sy0;
		int shCenter = sy2 - sy1;
		int shBottom = sy3 - sy2;

		int dwLeft = swLeft * Math.Max(1, scale);
		float dwRight = swRight * Math.Max(1, scale);
		float dwCenter = destRect.Width - dwLeft - dwRight;
		if (dwCenter < 0)
		{
			float scaleX = (float)destRect.Width / Math.Max(1, swLeft + swRight);
			dwLeft = Math.Max(0, (int)Math.Round(swLeft * scaleX));
			dwRight = Math.Max(0, destRect.Width - dwLeft);
			dwCenter = 0;
		}

		int dhTop = shTop * Math.Max(1, scale);
		float dhBottom = shBottom * Math.Max(1, scale);
		float dhCenter = destRect.Height - dhTop - dhBottom;
		if (dhCenter < 0)
		{
			float scaleY = (float)destRect.Height / Math.Max(1, shTop + shBottom);
			dhTop = Math.Max(0, (int)Math.Round(shTop * scaleY));
			dhBottom = Math.Max(0, destRect.Height - dhTop);
			dhCenter = 0;
		}

		int[] srcXs = [sx0, sx1, sx2, sx3];
		int[] srcYs = [sy0, sy1, sy2, sy3];

		float dx0 = destRect.Left;
		float dx1 = dx0 + dwLeft;
		float dx2 = dx1 + dwCenter;
		float dx3 = destRect.Right;

		float dy0 = destRect.Top;
		float dy1 = dy0 + dhTop;
		float dy2 = dy1 + dhCenter;
		float dy3 = destRect.Bottom;

		float[] dstXs = [dx0, dx1, dx2, dx3];
		float[] dstYs = [dy0, dy1, dy2, dy3];

		for (int row = 0; row < 3; row++)
		{
			for (int col = 0; col < 3; col++)
			{
				int sLeft = srcXs[col];
				int sTop = srcYs[row];
				int sRight = srcXs[col + 1];
				int sBottom = srcYs[row + 1];
				int sW = sRight - sLeft;
				int sH = sBottom - sTop;
				if (sW <= 0 || sH <= 0)
					continue;

				float dLeft = dstXs[col];
				float dTop = dstYs[row];
				float dRight = dstXs[col + 1];
				float dBottom = dstYs[row + 1];
				float dW = dRight - dLeft;
				float dH = dBottom - dTop;
				if (dW <= 0 || dH <= 0)
					continue;

				switch (style)
				{
					case PatchStyle.Strentch:
						var srcRect = SKRect.Create(sLeft, sTop, sW, sH);
						var dstRect = SKRect.Create(dLeft, dTop, dW, dH);

						canvas.DrawBitmap(srcBitmap, srcRect, dstRect, paint);
						break;
					case PatchStyle.Repeat:
						for (float y = dTop; y < dBottom; y += sH * scale)
						{
							float th = Math.Min(sH, (dBottom - y) / scale);
							for (float x = dLeft; x < dRight; x += sW * scale)
							{
								float tw = Math.Min(sW, (dRight - x) / scale);
								var sRect = SKRect.Create(sLeft, sTop, tw, th);
								var dRect = SKRect.Create(x, y, tw * scale, th * scale);
								canvas.DrawBitmap(srcBitmap, sRect, dRect, paint);
							}
						}
						break;
				}
			}
		}
	}
	internal static SKColor ColorOf(Tab tab)
	{
		return tab switch
		{
			Tab.Sounds => 0xffd82433,
			Tab.Rows => 0xff2c4fd9,
			Tab.Actions => 0xffc543b3,
			Tab.Rooms => 0xffd8b812,
			Tab.Decorations => 0xff00c459,
			Tab.Windows => 0xff50b5d7,
			Tab.Unknown or _ => 0xff50b5d7,
		};
	}
	private static SKColor ToSKColor(RDColor color) => new((uint)color);
	private static int LengthOf(SayReadyGetSetGoWords words)
	{
		return words switch
		{
			SayReadyGetSetGoWords.SayReaDyGetSetGoNew or
			SayReadyGetSetGoWords.SayReaDyGetSetOne or
			SayReadyGetSetGoWords.SayReadyGetSetGo => 4,
			SayReadyGetSetGoWords.SayGetSetGo or
			SayReadyGetSetGoWords.SayGetSetOne => 2,
			_ => 0,
		};
	}
	private static string WordOf(SayReadyGetSetGoWords words)
	{
		return words switch
		{
			SayReadyGetSetGoWords.SayReaDyGetSetGoNew => "Rea, Dy, Get, Set, Go!",
			SayReadyGetSetGoWords.SayReaDyGetSetOne => "Rea, Dy, Get, Set, One!",
			SayReadyGetSetGoWords.SayGetSetGo => "Get, Set, Go!",
			SayReadyGetSetGoWords.SayGetSetOne => "Get, Set, One!",
			SayReadyGetSetGoWords.JustSayRea => "Rea",
			SayReadyGetSetGoWords.JustSayDy => "Dy",
			SayReadyGetSetGoWords.JustSayGet => "Get",
			SayReadyGetSetGoWords.JustSaySet => "Set",
			SayReadyGetSetGoWords.JustSayAnd => "And",
			SayReadyGetSetGoWords.JustSayGo => "Go!",
			SayReadyGetSetGoWords.JustSayStop => "Stop",
			SayReadyGetSetGoWords.JustSayAndStop => "And Stop!",
			SayReadyGetSetGoWords.SaySwitch => "Switch",
			SayReadyGetSetGoWords.SayWatch => "Watch",
			SayReadyGetSetGoWords.SayListen => "Listen",
			SayReadyGetSetGoWords.Count1 => "1",
			SayReadyGetSetGoWords.Count2 => "2",
			SayReadyGetSetGoWords.Count3 => "3",
			SayReadyGetSetGoWords.Count4 => "4",
			SayReadyGetSetGoWords.Count5 => "5",
			SayReadyGetSetGoWords.Count6 => "6",
			SayReadyGetSetGoWords.Count7 => "7",
			SayReadyGetSetGoWords.Count8 => "8",
			SayReadyGetSetGoWords.Count9 => "9",
			SayReadyGetSetGoWords.Count10 => "10",
			SayReadyGetSetGoWords.SayReadyGetSetGo => "Ready, Get, Set, Go!",
			SayReadyGetSetGoWords.JustSayReady => "Ready",
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
	private static void DrawBack(this SKCanvas canvas, SKRect dest, SKColor color, bool active, int scale)
	{
		const string outline = "event_outline";
		const string back = "event_back";
		canvas.DrawSlice(outline, dest, active ? 0xffffffff : 0xffa8a8a8, scale);
		dest.Inflate(-1 * scale, -1 * scale);
		canvas.DrawSlice(back, dest, color, scale, PatchStyle.Strentch);
	}
	private static SKColor WithState(this SKColor color, bool active, bool enabled) => (enabled ? color : 0xff848484).WithAlpha(active ? (byte)192 : (byte)91);
}
public struct IconStyle
{
	public bool? Enabled;
	public bool Active;
	public bool Hover;
	public int Scale;
}