using Content.Shared._Starlight.Shadekin;
using Robust.Server.GameObjects;

namespace Content.Server._Starlight.Shadekin;

public sealed class LightGridSystem : SharedLightGridSystem
{
    protected override void EnumeratePointLights()
    {
        var query = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var lightUid, out var lightComp, out var xform))
        {
            ProcessLight(lightUid, lightComp, xform);
        }
    }
}