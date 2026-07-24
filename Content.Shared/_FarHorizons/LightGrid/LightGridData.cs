using System.Numerics;

namespace Content.Shared._FarHorizons.LightGrid;

internal readonly record struct LightSourceData(
    Vector2i Tile,
    float Radius,
    float Brightness,
    Angle Direction,
    bool CastShadows,
    bool Directional);

internal readonly record struct WorldLightSourceData(
    Vector2 Position,
    float Radius,
    float Brightness,
    Angle Direction,
    bool Directional);
