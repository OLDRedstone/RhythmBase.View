<p align="center">
	<a href="https://www.nuget.org/packages/RhythmBase.View/"><img src="https://img.shields.io/nuget/v/RhythmBase.View?logo=nuget" alt="Nuget Download"></a>
	<img src="https://img.shields.io/nuget/dt/RhythmBase.View" alt="Downloads"/>
</p>

# RhythmBase.View

[Go to Core Library](https://github.com/RDCN-Community-Developers/RhythmToolkit)

This library renders Rhythm Doctor events with [SkiaSharp](https://github.com/mono/SkiaSharp).  
See the [example](#example) below for a quick usage sketch.

# Example

```cs
int height = 28 * 60;

string file = @"your\level.rdlevel";

using RDLevel level = RDLevel.FromFile(file);
int width = (int)(level.Length.BeatOnly * 28);
Console.WriteLine(level.Length.TimeSpan);

using SKBitmap bitmap = new(width, height);
using SKCanvas canvas = new(bitmap);

foreach (var e in level)
{
	canvas.DrawEventIcon(e, ToLocation(e), new() { // Provided by this library.
		Scale = 2,
		Active = true,
		Enabled = true,
		Hover = false,
		ShowDuration = true,
	});
}

static SKPointI ToLocation(IBaseEvent e)
{
	return new SKPointI(
		(int)(e.Beat.BeatOnly * 28),
		(e.Y) * 28);
}

using Stream stream = File.OpenWrite("output.png");
bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);

```