using Content.Shared._FarHorizons.LightGrid;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.Server._FarHorizons.LightGrid;

public sealed partial class LightGridSystem : SharedLightGridSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<MindContainerComponent> _mindQuery;
    private EntityQuery<GhostComponent> _ghostQuery;

    public override void Initialize()
    {
        base.Initialize();
        _mindQuery = GetEntityQuery<MindContainerComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
    }

    protected override void EnumeratePointLights()
    {
        var query = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var lightUid, out var lightComp, out var xform))
        {
            ProcessLight(lightUid, lightComp, xform);
        }
    }
}
