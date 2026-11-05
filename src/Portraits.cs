using UnityEngine;
using Object = UnityEngine.Object;
namespace PlantLibrary;
internal static class Portraits {
    private static readonly Dictionary<PlantDefinition, Sprite> sprites = new();
    private static Sprite? packetBackground;
    internal static Sprite PacketBackground {
        get {
            if (packetBackground != null) return packetBackground;
            using var stream = typeof(Portraits).Assembly.GetManifestResourceStream("PlantLibrary.Assets.packet-background.png")
                ?? throw new InvalidDataException("Missing native packet background.");
            return packetBackground = Load(stream);
        }
    }
    internal static Sprite? Get(PlantDefinition plant) {
        if (plant.Portrait != null) return plant.Portrait();
        if (sprites.TryGetValue(plant, out var sprite)) return sprite;
        using var stream = plant.Mod.Resource(plant.Entry.Portrait);
        sprite = Load(stream); sprites.Add(plant, sprite); return sprite;
    }
    private static Sprite Load(Stream stream) {
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try {
            if (!ImageConversion.LoadImage(texture, bytes.ToArray(), true)) throw new InvalidDataException("Invalid plant portrait.");
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset; return sprite;
        } catch { Object.Destroy(texture); throw; }
    }
    internal static void Dispose() {
        foreach (var sprite in sprites.Values) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
        sprites.Clear();
        if (packetBackground != null) { Object.Destroy(packetBackground.texture); Object.Destroy(packetBackground); packetBackground = null; }
    }
}
