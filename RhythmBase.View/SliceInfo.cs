using SkiaSharp;

namespace RhythmBase.View;

internal record class SliceInfo()
{
	public SKRectI Bounds { get; internal set; }
	public SKRectI Center { get; internal set; }
	public bool IsNinePatch => Center != SKRectI.Empty;
	public SKImage? Image { get; internal set; }
	public SKShader? Shader { get; internal set; }
	public SKImage[]? NinePatchImages { get; internal set; }
	public SKShader[]? NinePatchShaders { get; internal set; }
	public SKPointI Pivot { get; internal set; }
	public bool HasSpace { get; internal set; }
	public int Scale { get; internal set; } = 1;
}
