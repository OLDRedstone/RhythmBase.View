<p align="center">
	<a href="https://www.nuget.org/packages/RhythmBase.View/"><img src="https://img.shields.io/nuget/v/RhythmBase.View?logo=nuget" alt="Nuget Download"></a>
	<img src="https://img.shields.io/nuget/dt/RhythmBase.View" alt="Downloads"/>
</p>

# RhythmBase.View

This library renders Rhythm Doctor events with [SkiaSharp](https://github.com/mono/SkiaSharp).  
See the [example](#example) below for a quick usage sketch.

| Project             | Description                                         | Status           | Link                                                                       | 
|---------------------|-----------------------------------------------------|------------------|----------------------------------------------------------------------------|
| RhythmBase          | Core library for level editing.                     | WIP              | [Go There](https://github.com/RDCN-Community-Developers/RhythmToolkit)                      |
| RhythmBase.View     | Draw all Rhythm Doctor event elements in SkiaSharp. | WIP              | **You are here**                                                           |
| RhythmBase.Addition | Extensions for levels.                              | *Not disclosed*  | -                                                                          |
| RhythmBase.Interact | Interact with Level editor.                         | *Not disclosed*  | -                                                                          |
| RhythmBase.Hospital | Judgement logic for levels.                         | *Not disclosed*  | -                                                                          |
| RhythmBase.Lite     | Lightweight version of RhythmBase.                  | WIP              | [Go there](https://github.com/RDCN-Community-Developers/RhythmToolkitLite) |
| RhythmBase.Control  | Custom controls.                                    | *Not disclosed*  | -                                                                          |

```mermaid
flowchart RL
RBLite[RhythmBase.Lite]
subgraph RD[Rhythm Doctor]
	RDLE[Rhythm Doctor Level Editor]
end
subgraph AD[Adofai]
	ADLE[Adofai Level Editor]
end
RDL([Rhythm Doctor Level])
ADL([Adofai Level])
subgraph RBTitle[RhythmBase]
	RB[RhythmBase]
	RBAdd[RhythmBase.Addition]
	RBInt[RhythmBase.Interact]
	RBHos[RhythmBase.Hospital]
	RBV[RhythmBase.View]
	subgraph RBC[RhythmBase.Control]
		RBCCore[RhythmBase.Control.Core]
		RBCWPF[RhythmBase.Control.WPF]
		RBCWF[RhythmBase.Control.WinForm]
		RBCAva[RhythmBase.Control.Avalonia]
	end
end

RBLite ---> RDL
RBCWPF & RBCWF & RBCAva --> RBCCore --> RBV
RBV & RBHos & RBAdd & RBInt --> RB ---> RDL & ADL
RBInt ---> RDLE --> RDL
RBInt ---> ADLE --> ADL
```

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
	canvas.DrawEventIcon(e, ToLocation(e),   // Provided by this library.
		false,                               // The event is not selected.
		2                                    // The scale of the event.
	);                                       // Returns the hit area used for selection.
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