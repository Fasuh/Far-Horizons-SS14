using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Client._FarHorizons.Spatial;

/// <summary>
/// This is a system used for flood filling and figuring out the shape and size of a room the user is in.
/// This is very useful for effects that require knowledge of how the space around us looks like.
/// Examples, light eye adjustment, sound reverberations.
/// </summary>
public sealed class SpatialSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1f / 30f);
    private TimeSpan _nextRefresh;

    public const int MaxRadius = 16;
    private const int Side = (MaxRadius * 2) + 1;
    private const byte LightBit = (byte)SpatialLayer.Light;
    private const byte SoundBit = (byte)SpatialLayer.Sound; 

    public EntityUid? CachedGrid { get; private set; }
    public Vector2i CachedCenter { get; private set; }

    /// <summary>
    /// Save reachable as bytes for a bit faster compute, they are saved in two layers, <see cref="SpatialLayer"/>
    /// This is done because sound follows different rules than light, sound might not pass through windows for example, where light does.
    /// </summary>
    private readonly byte[] _reachable = new byte[Side*Side];
    
    /// <summary>
    /// A mask of all blocking objects, separated by <see cref="SpatialLayer"/>
    /// </summary>
    private readonly byte[] _blockerMask = new byte[Side * Side];
    
    private readonly Vector2i[] _bfsQueue = new Vector2i[Side*Side];
    private int _bfsHead;
    private int _bfsTail;
    
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<OccluderComponent> _occluderQuery;
    private readonly HashSet<Entity<OccluderComponent>> _occluderBuffer = new();
    
    public override void Initialize()
    {
        base.Initialize();
        _occluderQuery = GetEntityQuery<OccluderComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
  
        if (_timing.CurTime < _nextRefresh)
            return;
        
        _nextRefresh = _timing.CurTime + RefreshInterval;
        Refresh();
    }

    public bool IsReachable(int rx, int ry, SpatialLayer layer)
    {
        if (CachedGrid == null) return false;
        if (Math.Abs(rx) > MaxRadius || Math.Abs(ry) > MaxRadius) return false;
        return (_reachable[ToIndex(rx, ry)] & (byte)layer) != 0;
    }
    
    private void Refresh()
    {
        var origin = _eyeManager.CurrentEye.Position;

        if (!_mapManager.TryFindGridAt(origin, out var gridEnt, out var gridComp))
        {
            CachedGrid = null;
            return;
        }
        
        var localPos = Vector2.Transform(origin.Position,                                                                                                                                                                                                                                      
            _transform.GetInvWorldMatrix(Transform(gridEnt)));
        var center = _maps.LocalToTile(gridEnt, gridComp, new EntityCoordinates(gridEnt, localPos));

        BuildLocalBlockerMask(gridEnt, gridComp, center, MaxRadius);
        FloodFillReachable(center, MaxRadius, LightBit);

        CachedGrid = gridEnt;                                                                                                                                                                                                                                                                  
        CachedCenter = center;
    }
    
    private void BuildLocalBlockerMask(EntityUid gridUid, MapGridComponent grid, Vector2i center, int radius)
    {
        Array.Clear(_blockerMask);

        var minTile = center - new Vector2i(radius, radius);
        var maxTile = center + new Vector2i(radius, radius);
        var bounds = new Box2(
            _lookup.GetLocalBounds(minTile, grid.TileSize).BottomLeft,
            _lookup.GetLocalBounds(maxTile, grid.TileSize).TopRight);

        _occluderBuffer.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            (gridUid, Comp<BroadphaseComponent>(gridUid)),
            bounds, _occluderBuffer,
            query: _occluderQuery,
            flags: LookupFlags.Static | LookupFlags.Approximate);

        foreach (var occ in _occluderBuffer)
        {
            if (!occ.Comp.Enabled)
                continue;

            var occTile = _maps.LocalToTile(gridUid, grid, _xformQuery.GetComponent(occ.Owner).Coordinates);
            var dx = occTile.X - center.X;
            var dy = occTile.Y - center.Y;
            if (Math.Abs(dx) > radius || Math.Abs(dy) > radius)
                continue;

            var idx = ToIndex(dx, dy);
            _blockerMask[idx] |= CategorizeOccluder(occ);
            
        }
    }
    
    private static int ToIndex(int rx, int ry) => ((rx + MaxRadius) * Side) + ry + MaxRadius;

    private byte CategorizeOccluder(Entity<OccluderComponent> occluder)
    {
        // TODO - Add logic to figure out if the occluder stops sound
        return LightBit | SoundBit;
    }
    
    private void FloodFillReachable(Vector2i center, int radius, byte layerBit)
    {
        Array.Clear(_reachable);
        _bfsHead = 0;
        _bfsTail = 0;

        _reachable[ToIndex(0, 0)] |= layerBit;
        
        while (_bfsHead < _bfsTail)
        {
            var tile = _bfsQueue[_bfsHead];
            var dx = tile.X - center.X;
            var dy = tile.Y - center.Y;
            var dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (dist >= radius)
                continue;

            ExpandNeighbor(tile + new Vector2i(1, 0), center, radius, layerBit);
            ExpandNeighbor(tile + new Vector2i(-1, 0), center, radius, layerBit);
            ExpandNeighbor(tile + new Vector2i(0, 1), center, radius, layerBit);
            ExpandNeighbor(tile + new Vector2i(0, -1), center, radius, layerBit);
        }
    }

    private void ExpandNeighbor(Vector2i tile, Vector2i center, int radius, byte layerBit)
    {
        var rx = tile.X - center.X;
        var ry = tile.Y - center.Y;
        if (Math.Abs(rx) > radius || Math.Abs(ry) > radius)
            return;
        
        var idx = ToIndex(rx, ry);
        if ((_reachable[idx] & layerBit) != 0) return;
        if ((_blockerMask[idx] & layerBit) != 0) return;
        
        _reachable[idx] |= layerBit;
        _bfsQueue[_bfsTail++] = tile;
    }
}