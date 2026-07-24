using Content.Client._FarHorizons.Spatial;
using Content.Shared._FarHorizons.LightGrid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client._FarHorizons.LightGrid;

public sealed class LightGridSystem : SharedLightGridSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SpatialSystem _spatialSystem = default!;

    private const float ViewportMargin = 20f;

    protected override TimeSpan UpdateInterval => TimeSpan.FromSeconds(1f / 30f);

    private readonly HashSet<Entity<PointLightComponent>> _visibleLights = new();
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    protected override void EnumeratePointLights()
    {
        var mapId = _eyeManager.CurrentEye.Position.MapId;
        if (mapId == MapId.Nullspace)
            return;

        var bounds = _eyeManager.GetWorldViewbounds().Enlarged(ViewportMargin);

        _visibleLights.Clear();
        _lookup.GetEntitiesIntersecting(mapId, bounds, _visibleLights);

        foreach (var light in _visibleLights)
        {
            if (!_xformQuery.TryGetComponent(light.Owner, out var xform))
                continue;

            ProcessLight(light.Owner, light.Comp, xform);
        }
    }

    public float GetRoomLightLevel(int radius = 16)
    {
        if (_spatialSystem.CachedGrid is not { } gridEnt)
            return MaxExposure;
        
        var center = _spatialSystem.CachedCenter;
        var sum = 0f;
        var count = 0;

        for (var rx = -SpatialSystem.MaxRadius; rx <= SpatialSystem.MaxRadius; rx++)
        {
            for (var ry = -SpatialSystem.MaxRadius; ry <= SpatialSystem.MaxRadius; ry++)
            {
                if (!_spatialSystem.IsReachable(rx, ry, SpatialLayer.Light))
                    continue;
  
                var tile = center + new Vector2i(rx, ry);
                sum += DecodeLight(GetTileLight(gridEnt, tile));
                count++;
            }
        }

        return count == 0 ? 0f : sum / count;
    }
}