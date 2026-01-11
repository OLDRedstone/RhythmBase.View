using System.Collections;
using SkiaSharp;
using System.Text.Json;
using System.Reflection;

namespace RhythmBase.View.Assets;

internal static class AssetManager
{
	private const string AssetFilePath = "assets.png";
	private const string SlicesFilePath = "assets";
	internal static readonly Dictionary<string, SliceInfo> _slices;
	internal static readonly SKImage _assetFile;
	public static readonly SKColor[] Colors;
	static AssetManager()
	{
		using Stream stream1 = GetAssemblyStream(AssetFilePath);
		_assetFile = SKImage.FromEncodedData(stream1);
		using Stream stream2 = GetAssemblyStream(SlicesFilePath);
		_slices = ReadFromStream(stream2);
		using SKBitmap bitmap = SKBitmap.FromImage(_assetFile);
		Colors = new SKColor[8];
		for (int i = 0; i < 8; i++)
			Colors[i] = GetColor(bitmap, "tab_colors", new(i, 0));
	}
	private static Stream GetAssemblyStream(string path)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		return assembly.GetManifestResourceStream($"RhythmBase.View.Assets.{path}")!;
	}
	static Dictionary<string, SliceInfo> ReadFromStream(Stream stream)
	{
		BinaryReader reader = new(stream);
		int count = reader.ReadByte();
		Dictionary<string, SliceInfo> sliceInfos = [];
		for (int i = 0; i < count; i++)
		{
			SliceInfo info = new();
			int keyLength = reader.ReadByte();
			string key = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(keyLength));
			int left = reader.ReadByte();
			int top = reader.ReadByte();
			short wh = reader.ReadInt16();
			int width = wh & 0x1F;
			int height = (wh >> 5) & 0x3F;
			info.Bounds = SKRectI.Create(left, top, width, height);
			bool hasCenter = (wh & 0x8000) != 0;
			bool hasPivot = (wh & 0x4000) != 0;
			if (hasCenter)
			{
				byte centerByte = reader.ReadByte();
				int centerLeft = centerByte & 0x3;
				int centerTop = (centerByte >> 2) & 0x3;
				int centerWidth = (centerByte >> 4) & 0x3;
				int centerHeight = (centerByte >> 6) & 0x3;
				info.Center = SKRectI.Create(centerLeft, centerTop, centerWidth, centerHeight);
			}
			if (hasPivot)
			{
				byte pivotByte = reader.ReadByte();
				int pivotLeft = pivotByte & 0x7;
				int pivotTop = (pivotByte >> 3) & 0x7;
				if ((pivotByte & 0x80) != 0)
					pivotLeft = -pivotLeft;
				if ((pivotByte & 0x40) != 0)
					pivotTop = -pivotTop;
				info.Pivot = new SKPointI(pivotLeft, pivotTop);
			}
			sliceInfos[key] = info;
		}
		return sliceInfos;
	}
	public static SKColor GetColor(SKBitmap bitmap, string src, SKPointI offset)
	{
		if (!_slices.TryGetValue(src, out SliceInfo info))
			return SKColors.Transparent;
		SKPointI pixel = new(info.Bounds.Left + offset.X, info.Bounds.Top + offset.Y);
		if (pixel.X < info.Bounds.Left || pixel.X >= info.Bounds.Right || pixel.Y < info.Bounds.Top || pixel.Y >= info.Bounds.Bottom)
			return SKColors.Transparent;
		return bitmap.GetPixel(pixel.X, pixel.Y);
	}
}
