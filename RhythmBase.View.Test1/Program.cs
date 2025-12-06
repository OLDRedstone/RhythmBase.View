using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.View;
using SkiaSharp;
using System.Numerics;
using RhythmBase.RhythmDoctor.Utils;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Extensions;

int height = 28 * 60;

string file = @"O:\RhythmDoctor\main.rdlevel";

using RDLevel level = RDLevel.FromFile(file);
int width = (int)(level.Length.BeatOnly * 28);
Console.WriteLine(level.Length.TimeSpan);

using SKBitmap bitmap = new(width, height);
using SKCanvas canvas = new(bitmap);

foreach (var e in level)
{
	if(e.Tab is Tabs.Actions)
	canvas.DrawEventIcon(e, ToLocation(e), false, 2);
}

static SKPointI ToLocation(IBaseEvent e)
{
	return new SKPointI(
		(int)(e.Beat.BeatOnly * 28),
		(e.Y) * 28);
}

using Stream stream = File.OpenWrite("output.png");
bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);

