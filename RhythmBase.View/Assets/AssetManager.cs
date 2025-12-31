using System.Collections;
using SkiaSharp;
using System.Text.Json;
using System.Reflection;

namespace RhythmBase.View.Assets;

internal static class AssetManager
{
	private const string AssetFilePath = "assets.png";
	private const string SlicesFilePath = "assets.json";
	private const string LangDirPath = "Lang";
	private const string ConfigFilePath = "config.yaml";
	internal static readonly Dictionary<string, SliceInfo> _slices;
	internal static readonly SKImage _assetFile;
	static AssetManager()
	{
		using Stream stream1 = GetAssemblyStream(AssetFilePath);
		_assetFile = SKImage.FromBitmap(SKBitmap.Decode(stream1)!);
		using Stream stream2 = GetAssemblyStream(SlicesFilePath);
		_slices = LoadSlices(stream2);
	}
	private static Stream GetAssemblyStream(string path)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		return assembly.GetManifestResourceStream($"RhythmBase.View.Assets.{path}")!;
	}
	public static Dictionary<string, SliceInfo> LoadSlices(Stream stream)
	{
		JsonDocument document = JsonDocument.Parse(stream);
		var slices = document.RootElement.GetProperty("meta").GetProperty("slices");
		Dictionary<string, SliceInfo> sliceInfos = [];
		foreach (var slice in slices.EnumerateArray())
		{
			string name = slice.GetProperty("name").GetString() ?? "";
			var keys = slice.GetProperty("keys");
			foreach (var key in keys.EnumerateArray())
			{
				int frame = key.GetProperty("frame").GetInt32();
				var boundsProp = key.GetProperty("bounds");
				SKRectI bounds = SKRectI.Create(
					boundsProp.GetProperty("x").GetInt32(),
					boundsProp.GetProperty("y").GetInt32(),
					boundsProp.GetProperty("w").GetInt32(),
					boundsProp.GetProperty("h").GetInt32()
				);
				SliceInfo sliceInfo = new()
				{
					Bounds = bounds,
				};
				if (key.TryGetProperty("center", out var centerProp))
				{
					SKRectI center = SKRectI.Create(
						centerProp.GetProperty("x").GetInt32(),
						centerProp.GetProperty("y").GetInt32(),
						centerProp.GetProperty("w").GetInt32(),
						centerProp.GetProperty("h").GetInt32()
					);
					sliceInfo.Center = center;
				}
				if (key.TryGetProperty("pivot", out var pivotProp))
				{
					SKPointI pivot = new(
						pivotProp.GetProperty("x").GetInt32(),
						pivotProp.GetProperty("y").GetInt32()
					);
					sliceInfo.Pivot = pivot;
				}
				if (slice.TryGetProperty("data", out var dataProp))
				{
					string data = dataProp.GetString() ?? "";
					if (data.Length >= 3)
						sliceInfo.Scale = data[0..3] switch
						{
							"@2x" => 2,
							_ => 1,
						};
					if (data.Length > 3)
						sliceInfo.HasSpace = data[3] is 'T';
				}
				sliceInfos[name] = sliceInfo;
			}
		}
		return sliceInfos;
	}
	//public static SKColor GetColor(string src, SKPointI offset)
	//{
	//	if (!_slices.TryGetValue(src, out SliceInfo info))
	//		return SKColors.Transparent;
	//	SKPointI pixel = new(info.Bounds.Left + offset.X, info.Bounds.Top + offset.Y);
	//	if (pixel.X < info.Bounds.Left || pixel.X >= info.Bounds.Right || pixel.Y < info.Bounds.Top || pixel.Y >= info.Bounds.Bottom)
	//		return SKColors.Transparent;
	//	return _assetFile.GetPixel(pixel.X, pixel.Y);
	//}
}
