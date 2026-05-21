namespace Content.Client._FarHorizons.Spatial;

/// <summary>
/// Spatial layer, it determines layers of space per context, which is a smart way of saying,
/// that there is different ways you can detect rooms, if you are looking at light, windows are transparent, they don't count as walls to you
/// if you are looking at sound, you most likely want to exclude windows, as they also block sounds, or at least muffle them.
/// This aims to provide us with different layers depending on what data we need.
/// </summary>
[Flags]
public enum SpatialLayer : byte
{
    None  = 0,
    Light = 1 << 0,   // bit 0
    Sound = 1 << 1,   // bit 1
}