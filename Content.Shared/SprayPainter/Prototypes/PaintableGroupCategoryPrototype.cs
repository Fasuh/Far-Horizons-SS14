using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SprayPainter.Prototypes;

/// <summary>
/// Defines the style of the paintings group categories, 
/// this allows us to split paintables into multiple versions for different factions and such
/// </summary>
[Serializable, NetSerializable]
public enum PaintingStyle : byte
{
    NanoTrasen,
    Syndicate,
}

/// <summary>
/// A category of spray paintable items (e.g. airlocks, crates)
/// </summary>
[Prototype]
public sealed partial class PaintableGroupCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Each group that makes up this category.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<PaintableGroupPrototype>> Groups = new();
}
