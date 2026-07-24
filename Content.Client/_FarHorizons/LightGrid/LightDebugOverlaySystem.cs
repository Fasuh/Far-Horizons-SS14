using System.Numerics;
using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._FarHorizons.LightGrid;

internal sealed class LightDebugOverlaySystem : EntitySystem
{
    public const int LocalViewRange = 16;
    private const float UpdateRate = 20f;

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly LightGridSystem _lightGrid = default!;

    public readonly Dictionary<EntityUid, LightOverlayGridData> TileData = new();
    private List<Entity<MapGridComponent>> _grids = new();

    private float _accumulatedFrameTime;
    private readonly float _updateCooldown = 1f / UpdateRate;

    public bool Enabled { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (!overlayManager.HasOverlay<LightDebugOverlay>())
            overlayManager.AddOverlay(new LightDebugOverlay(this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        var overlayManager = IoCManager.Resolve<IOverlayManager>();
        if (overlayManager.HasOverlay<LightDebugOverlay>())
            overlayManager.RemoveOverlay<LightDebugOverlay>();
    }

    public bool Toggle()
    {
        Enabled = !Enabled;
        if (!Enabled)
            TileData.Clear();
        return Enabled;
    }

    public bool HasData(EntityUid gridId) => TileData.ContainsKey(gridId);

    public override void Update(float frameTime)
    {
        if (!Enabled)
            return;

        _accumulatedFrameTime += frameTime;
        if (_accumulatedFrameTime < _updateCooldown)
            return;

        _accumulatedFrameTime -= _updateCooldown;

        if (_playerManager.LocalEntity is not { } player || !Exists(player))
            return;

        var xform = Transform(player);
        var pos = _transform.GetWorldPosition(xform);
        var worldBounds = Box2.CenteredAround(pos, new Vector2(LocalViewRange, LocalViewRange));

        _grids.Clear();
        _mapManager.FindGridsIntersecting(xform.MapID, worldBounds, ref _grids);

        var seen = new HashSet<EntityUid>(_grids.Count);
        foreach (var grid in _grids)
        {
            if (!Exists(grid.Owner))
                continue;

            seen.Add(grid.Owner);

            var entityTile = _mapSystem.GetTileRef(grid, grid, xform.Coordinates).GridIndices;
            var baseTile = new Vector2i(entityTile.X - LocalViewRange / 2, entityTile.Y - LocalViewRange / 2);
            var overlayData = new byte[LocalViewRange * LocalViewRange];

            var index = 0;
            for (var y = 0; y < LocalViewRange; y++)
            {
                for (var x = 0; x < LocalViewRange; x++)
                {
                    var tile = new Vector2i(baseTile.X + x, baseTile.Y + y);
                    overlayData[index++] = _lightGrid.GetTileLight(grid.Owner, tile);
                }
            }

            TileData[grid.Owner] = new LightOverlayGridData(baseTile, overlayData);
        }

        // Drop grids we left behind so the overlay doesn't render stale tiles.
        if (TileData.Count != seen.Count)
        {
            var stale = new List<EntityUid>();
            foreach (var key in TileData.Keys)
            {
                if (!seen.Contains(key))
                    stale.Add(key);
            }
            foreach (var key in stale)
                TileData.Remove(key);
        }
    }

    private void OnGridRemoved(GridRemovalEvent ev) => TileData.Remove(ev.EntityUid);

    private void Reset(RoundRestartCleanupEvent ev) => TileData.Clear();
}

public sealed record LightOverlayGridData(Vector2i BaseIdx, byte[] OverlayData);