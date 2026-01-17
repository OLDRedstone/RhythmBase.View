<p align="center">
	<a href="https://www.nuget.org/packages/RhythmBase.View/"><img src="https://img.shields.io/nuget/v/RhythmBase.View?logo=nuget" alt="NuGet Version"></a>
	<img src="https://img.shields.io/nuget/dt/RhythmBase.View" alt="NuGet Downloads"/>
</p>

# RhythmBase.View

A powerful rendering library for Rhythm Doctor events, built with [SkiaSharp](https://github.com/mono/SkiaSharp).

Sponsored by RhythmBase Project. Visit [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) for more info.

## Overview

RhythmBase.View provides a comprehensive solution for visualizing Rhythm Doctor level events. It offers both C# and TypeScript implementations, enabling developers to render event icons, timelines, and interactive visualizations with ease.

**[Core Library →](https://github.com/RDCN-Community-Developers/RhythmToolkit)**  
**[Live Demo (TypeScript) →](https://view.obugs.cn)**

## Quick Start

### C# Example: Rendering Level Events to PNG

This example demonstrates how to load a Rhythm Doctor level file and render all events to a PNG image:

```cs
using RhythmBase.Global.Components;
using RhythmBase.Global.Events;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Utils;
using SkiaSharp;

// Define canvas dimensions
int height = 28 * 60;
string file = @"your\level.rdlevel";

// Load the level
using RDLevel level = RDLevel.FromFile(file);
int width = (int)(level.Length.BeatOnly * 28);
Console.WriteLine($"Level duration: {level.Length.TimeSpan}");

// Create bitmap and canvas
using SKBitmap bitmap = new(width, height);
using SKCanvas canvas = new(bitmap);

// Render each event
foreach (var e in level)
{
	canvas.DrawEventIcon(e, ToLocation(e), new() {
		Scale = 2,
		Active = true,
		Enabled = true,
		Hover = false,
	});
}

// Helper method to convert event position to pixel coordinates
static SKPointI ToLocation(IBaseEvent e)
{
	return new SKPointI(
		(int)(e.Beat.BeatOnly * 28),
		e.Y * 28
	);
}

// Save to file
using Stream stream = File.OpenWrite("output.png");
bitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
```

### TypeScript Example: Interactive Event Elements

This example shows how to create interactive event elements in a web browser:

```typescript
import { EventInfos, EventType, hiddenEventType } from "./definition.ts";
import {
    colorOf,
    createElementEvent,
    HTMLEventElement,
    IconStyle,
} from "./rdtkview.ts";

const rdview = document.createElement("div");

const objs = ...

// Create event elements with interactivity
for (let obj of objs) {
	const style = {
		...eventStyle,
		enabled: obj.active ?? true,
	};
	
	const elem = createElementEvent(obj, style);
	if (!elem) continue;
	
	elem.eventStyle = style;
	elem.style.position = "absolute";
	
	// Calculate position based on bar and beat
	let x = ((((obj.bar ?? 1) - 1) * 8) + (obj.beat ?? 1) - 1) * 14 * eventStyle.scale;
	let y = (obj.y ?? obj.row ?? 0) * 14 * eventStyle.scale;
	elem.moveTo(x, y);
	
	// Add interactive event handlers
	elem.addEventListener("mousedown", (event) => {
		elem.eventStyle.active = true;
		elem.onStateChange(elem.eventStyle);
		elem.style.zIndex = "10";
	});
	
	elem.addEventListener("mouseup", (event) => {
		elem.eventStyle.active = false;
		elem.onStateChange(elem.eventStyle);
	});
	
	elem.addEventListener("mouseover", (event) => {
		elem.eventStyle.hover = true;
		elem.onStateChange(elem.eventStyle);
	});
	
	elem.addEventListener("mouseout", (event) => {
		elem.eventStyle.hover = false;
		elem.eventStyle.active = false;
		elem.onStateChange(elem.eventStyle);
	});
	
	elems.push(elem);
}

// Append all elements to the view
for (const elem of elems) {
	if (elem && rdview) {
		rdview.appendChild(elem);
	}
}
```

## EventIcon Style Options

Customize event rendering with the following style properties:

- `Scale` - Scaling factor for the icon size
- `Active` - Whether the event is currently active/selected
- `Enabled` - Whether the event is enabled or disabled
- `Hover` - Whether the mouse is hovering over the event

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests to improve the library.

## License

This project is licensed under the terms specified in [LICENSE.txt](LICENSE.txt).

## Related Projects

- [RhythmBase](https://github.com/RDCN-Community-Developers/RhythmToolkit) - Core library for Rhythm Doctor level manipulation

---

<p align="center">Made with ❤️ for the Rhythm Doctor community</p>