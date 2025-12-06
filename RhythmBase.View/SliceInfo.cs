using SkiaSharp;

namespace RhythmBase.View;

internal record struct SliceInfo()
{
	public SKRectI Bounds { get; set; }
	public SKRectI Center { get; set; }
	public readonly bool IsNinePatch => Center != SKRectI.Empty;
	public SKPointI Pivot { get; set; }
	public bool HasSpace { get; set; }
	public int Scale { get; set; } = 1;
}
