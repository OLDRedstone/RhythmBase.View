using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;

string dir = args.Length > 0 ? args[0] : "../../../../RhythmBase.View/Assets/";
string jsonpath = dir + "assets.json";
string outputpath = dir + "assets";
Dictionary<string, SliceInfo> value;
using (FileStream fs = new(jsonpath, FileMode.Open, FileAccess.Read))
{
	value = LoadSlices(fs);
}
using (FileStream fs2 = new(outputpath, FileMode.Create, FileAccess.Write))
{
	SaveToStream(fs2, value);
}
using (FileStream fs3 = new(outputpath, FileMode.Open, FileAccess.Read))
{
	var readValue = ReadFromStream(fs3);
	foreach (var pair in readValue)
	{
		var original = value[pair.Key];
		var read = pair.Value;
		if (original.Bounds != read.Bounds ||
			original.Center != read.Center ||
			original.Pivot != read.Pivot)
		{
			Console.WriteLine($"""
				Mismatch in key: {pair.Key}
				- {original}
				- {read}
				""");
		}
	}
}



static void SaveToStream(Stream stream, Dictionary<string, SliceInfo> data)
{
	BinaryWriter writer = new(stream);
	writer.Write((byte)data.Count);
	foreach (var pair in data)
	{
		string key = pair.Key;
		SliceInfo value = pair.Value;
		byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(key);
		writer.Write((byte)utf8.Length);
		writer.Write(utf8);
		writer.Write((byte)value.Bounds.Left);
		writer.Write((byte)value.Bounds.Top);
		byte width = (byte)(value.Bounds.Width & 0x1F);
		byte height = (byte)(value.Bounds.Height & 0x3F);
		bool hasCenter = value.IsNinePatch;
		bool hasPivot = value.Pivot != SKPointI.Empty;
		short wh = (short)(width | (height << 5) | (hasCenter ? 0x8000 : 0) | (hasPivot ? 0x4000 : 0));
		writer.Write(wh);
		if (hasCenter)
		{
			writer.Write((byte)(
				value.Center.Left |
				value.Center.Top << 2 |
				value.Center.Width << 4 |
				value.Center.Height << 6
				));
		}
		if (hasPivot)
		{
			writer.Write((byte)(
				(Math.Abs(value.Pivot.X) & 0x7) |
				(Math.Abs(value.Pivot.Y) & 0x7) << 3 |
				(value.Pivot.X >= 0 ? 0 : 0x80) |
				(value.Pivot.Y >= 0 ? 0 : 0x40)
				));
		}
	}
	Console.WriteLine($"""
			lm:{data.Values.Max(i => i.Bounds.Left)}
			tm:{data.Values.Max(i => i.Bounds.Top)}
			wm:{data.Values.Max(i => i.Bounds.Width)}
			hm:{data.Values.Max(i => i.Bounds.Height)}
			clm:{data.Values.Max(i => i.Center.Left)}
			ctm:{data.Values.Max(i => i.Center.Top)}
			cwm:{data.Values.Max(i => i.Center.Width)}
			chm:{data.Values.Max(i => i.Center.Height)}
			plm:{data.Values.Max(i => i.Pivot.X)}
			ptm:{data.Values.Max(i => i.Pivot.Y)}

			lm:{data.Values.Min(i => i.Bounds.Left)}
			tm:{data.Values.Min(i => i.Bounds.Top)}
			wm:{data.Values.Min(i => i.Bounds.Width)}
			hm:{data.Values.Min(i => i.Bounds.Height)}
			clm:{data.Values.Min(i => i.Center.Left)}
			ctm:{data.Values.Min(i => i.Center.Top)}
			cwm:{data.Values.Min(i => i.Center.Width)}
			chm:{data.Values.Min(i => i.Center.Height)}
			plm:{data.Values.Min(i => i.Pivot.X)}
			ptm:{data.Values.Min(i => i.Pivot.Y)}
			""");
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

static Dictionary<string, SliceInfo> LoadSlices(Stream stream)
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
			sliceInfos[name] = sliceInfo;
		}
	}
	return sliceInfos;
}
internal record struct SliceInfo()
{
	public SKRectI Bounds { get; set; }
	public SKRectI Center { get; set; }
	public readonly bool IsNinePatch => Center != SKRectI.Empty;
	public SKPointI Pivot { get; set; }
	public bool HasSpace { get; set; }
	public int Scale { get; set; } = 1;
}
