using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using NewDawn.Models;

string archive = Path.GetFullPath(args[0]);
string working = Path.GetFullPath(args[1]);
Directory.CreateDirectory(working);
Environment.CurrentDirectory = working;
using var zip = ZipFile.OpenRead(archive);
foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".map")).OrderBy(e => e.FullName))
{
    string input = Path.Combine(working, Path.GetFileName(entry.FullName));
    using (var source = entry.Open())
    using (var target = File.Create(input)) source.CopyTo(target);
    try
    {
        Map map = SaveAndLoaderHandler.LoadMap(input);
        string output = Path.Combine(working, "roundtrip.map");
        SaveAndLoaderHandler.SaveMap(map, output);
        byte[] first = File.ReadAllBytes(output);
        Map restored = SaveAndLoaderHandler.LoadMap(output);
        SaveAndLoaderHandler.SaveMap(restored, output);
        if (!first.SequenceEqual(File.ReadAllBytes(output))) throw new Exception("Round trip unstable");
        Console.WriteLine($"{entry.Name}: {map.Width}x{map.Height} {Convert.ToHexString(SHA256.HashData(first))}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{entry.Name}: {ex.GetType().Name}: {ex.Message.Replace(working, "<test>")}");
    }
}
// Check terrain transitions too: retain the legacy effects rather than resetting them.
var tile = new Tile(0, 0);
foreach (Tile.TerrainType terrain in Enum.GetValues<Tile.TerrainType>())
{
    tile.SetTerrain(terrain);
    Console.WriteLine($"{terrain}: {tile.Blocked}, {tile.Slows}, {tile.RangedDebuff}, {tile.DamageModifier}, {tile.DamageReductionModifier}");
}
