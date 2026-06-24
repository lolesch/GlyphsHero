using System;
using System.Collections.Generic;
using System.Linq;
using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using UnityEngine;

namespace Code.Tests.EditMode.Inventory.Fakes
{
    /// <summary>
    /// Minimal in-memory ITetrisContainer. Every fake item is treated as a 1x1 cell,
    /// so a placement position maps to exactly one content pointer. Mutation methods
    /// are unused by ChainResolver and intentionally unsupported.
    /// </summary>
    internal sealed class FakeContainer : ITetrisContainer
    {
        private readonly Dictionary<Vector2Int, ITetrisItem> _contents = new();
        private readonly Dictionary<Vector2Int, Vector2Int>  _pointer  = new();

        public FakeContainer(Vector2Int gridSize) => GridSize = gridSize;

        public FakeContainer Place(Vector2Int pos, ITetrisItem item)
        {
            _contents[pos] = item;
            _pointer[pos]  = pos;
            return this;
        }

        public Vector2Int GridSize { get; }
        public IReadOnlyDictionary<Vector2Int, ITetrisItem> Contents       => _contents;
        public IReadOnlyDictionary<Vector2Int, Vector2Int>  ContentPointer => _pointer;

        // The fake recomputes; caching is exercised on the real TetrisContainer.
        public ChainTopology Topology => ChainResolver.ResolveTopology(this);

#pragma warning disable 0067 // event required by interface, unused by the resolver
        public event Action<IReadOnlyDictionary<Vector2Int, ITetrisItem>> OnContentsChanged;
#pragma warning restore 0067

        public bool TryAdd(ITetrisItem item) => throw new NotSupportedException();
        public bool TryAddAt(Vector2Int position, ref ITetrisItem arrival) => throw new NotSupportedException();
        public bool TrySwapInto(Vector2Int anchor, ref ITetrisItem incoming,
            ITetrisContainer source, Vector2Int sourceAnchor) => throw new NotSupportedException();
        public bool TryRemove(Vector2Int position, out ITetrisItem removed) => throw new NotSupportedException();
        public bool TryRemove(ITetrisItem item) => throw new NotSupportedException();
        public bool CanAddAt(Vector2Int position, ITetrisItem item, out List<Vector2Int> overlapping)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Rectangular item (default 1x1) with a configurable set of outgoing connector directions.
    /// A multi-cell size lets placement/swap collision be exercised; connectors stay rooted at the
    /// placement cell (the resolver only cares about 1x1 fakes).
    /// </summary>
    internal abstract class FakeItem : ITetrisItem
    {
        private readonly List<Vector2Int> _connectorDirections;
        private readonly Vector2Int       _size;

        protected FakeItem(string name, params Vector2Int[] connectorDirections)
            : this(name, 1, 1, connectorDirections) { }

        // Width/height (not a Vector2Int) so this never collides with the direction-array overload
        // when called as (name, oneDirection) — that ambiguity silently zero-sizes the item.
        protected FakeItem(string name, int width, int height, params Vector2Int[] connectorDirections)
        {
            Name = name;
            Guid = Guid.NewGuid();
            _size = new Vector2Int(width, height);
            _connectorDirections = connectorDirections?.ToList() ?? new List<Vector2Int>();
        }

        public Guid         Guid     { get; }
        public Sprite       Icon     => null;
        public string       Name     { get; }
        public RotationType rotation { get; set; }

        public List<Vector2Int> GetPointers(Vector2Int position)
        {
            var pointers = new List<Vector2Int>();
            for (var x = 0; x < _size.x; x++)
                for (var y = 0; y < _size.y; y++)
                    pointers.Add(position + new Vector2Int(x, y));
            return pointers;
        }

        public Vector2Int GetShapeOrigin(List<Vector2Int> normalized = null) => Vector2Int.zero;
        public Vector2Int GetDimensions() => _size;
        public Vector2Int GetVisualDimensions() => _size;

        // For a 1x1 item the single occupied cell is the placement itself.
        public List<(Vector2Int slotPos, Vector2Int direction)> GetGridConnectors(Vector2Int placement)
            => _connectorDirections.Select(dir => (placement, dir)).ToList();
    }

    internal sealed class FakeWeapon : FakeItem, IWeaponItem
    {
        public FakeWeapon(string name, params Vector2Int[] connectorDirections)
            : base(name, connectorDirections) { }

        public FakeWeapon(string name, int width, int height, params Vector2Int[] connectorDirections)
            : base(name, width, height, connectorDirections) { }

        public MutableFloat    Damage       { get; } = new(1f);
        public MutableFloat    AttackSpeed  { get; } = new(1f);
        public MutableFloat    ResourceCost { get; } = new(0f);
        public ResourceType    CostResource => ResourceType.Mana;
        public DeliveryPattern Delivery     => DeliveryPattern.Single;
        public Affinity        Affinity     => Affinity.Hostile;
        public Anchor          Anchor       => Anchor.Target;
        public PayloadBehavior Payload      => null;
    }

    internal sealed class FakeAmplifier : FakeItem, IAmplifierItem
    {
        public FakeAmplifier(string name, params Vector2Int[] connectorDirections)
            : base(name, connectorDirections) =>
            outputMod = new WeaponOutputModifier(
                WeaponOutputStat.Damage,
                new Modifier(1f, ModifierType.FlatAdd, Guid.NewGuid()));

        public WeaponOutputModifier outputMod { get; }
    }

    /// <summary>
    /// Dual-purpose amplifier mirroring the real AmplifierItem: it is BOTH an IAmplifierItem (a
    /// weapon-output contributor when chained) and an IAttachmentItem (a passive pawn-stat affix when
    /// loose). Lets a test exercise the chaining-vs-passive tradeoff on a single item.
    /// </summary>
    internal sealed class FakeDualAmplifier : FakeItem, IAmplifierItem, IAttachmentItem
    {
        public FakeDualAmplifier(string name, float damageBonus = 2f, float lifeBonus = 5f)
            : base(name)
        {
            outputMod = new WeaponOutputModifier(WeaponOutputStat.Damage,
                new Modifier(damageBonus, ModifierType.FlatAdd, Guid.NewGuid()));
            Affix = new PawnStatModifier(PawnStat.LifeMax,
                new Modifier(lifeBonus, ModifierType.FlatAdd, Guid.NewGuid()));
        }

        public WeaponOutputModifier outputMod { get; }
        public PawnStatModifier     Affix     { get; }
        public IReadOnlyList<PawnStatModifier> affixes => new[] { Affix };

        public void OnUnchained(IPawnStats stats) => stats.ApplyMod(Affix);
        public void OnChained(IPawnStats stats)   => stats.RemoveMod(Affix);
    }

    internal sealed class FakeShifter : FakeItem, IShifterItem
    {
        public FakeShifter(string name, params Vector2Int[] connectorDirections)
            : base(name, connectorDirections)
        {
            inputMod  = new WeaponInputModifier(WeaponInputStat.AttackSpeed,
                new Modifier(1f, ModifierType.FlatAdd, Guid.NewGuid()));
            outputMod = new WeaponOutputModifier(WeaponOutputStat.Damage,
                new Modifier(1f, ModifierType.FlatAdd, Guid.NewGuid()));
        }

        public WeaponInputModifier  inputMod  { get; }
        public WeaponOutputModifier outputMod { get; }
    }

    internal sealed class FakeReactor : FakeItem, IReactorItem
    {
        public FakeReactor(string name, params Vector2Int[] connectorDirections)
            : this(name, ReactorType.OnSelfHit, connectorDirections) { }

        public FakeReactor(string name, ReactorType reactorType, params Vector2Int[] connectorDirections)
            : base(name, connectorDirections)
        {
            ReactorType = reactorType;
            inputMod    = new WeaponInputModifier(WeaponInputStat.AttackSpeed,
                new Modifier(1f, ModifierType.FlatAdd, Guid.NewGuid()));
        }

        public ReactorType        ReactorType { get; }
        public WeaponInputModifier inputMod   { get; }
    }

    /// <summary>
    /// Container double for the chain-state seam: contents and the chain topology are set directly
    /// (no resolver), so a test can declare exactly which items are "chained" and raise
    /// <see cref="OnContentsChanged"/> on demand. The resolver itself is locked by ChainResolverTests.
    /// </summary>
    internal sealed class FakeStateContainer : ITetrisContainer
    {
        private readonly Dictionary<Vector2Int, ITetrisItem> _contents = new();
        private List<IItemChain> _chains = new();
        private int _nextCell;

        public FakeStateContainer Add(ITetrisItem item)
        {
            _contents[new Vector2Int(_nextCell++, 0)] = item;
            return this;
        }

        public void Remove(ITetrisItem item)
        {
            foreach (var key in _contents.Where(kvp => kvp.Value == item).Select(kvp => kvp.Key).ToList())
                _contents.Remove(key);
        }

        public void SetChains(params IItemChain[] chains) => _chains = chains.ToList();
        public void RaiseChanged() => OnContentsChanged?.Invoke(_contents);

        public Vector2Int GridSize => new(6, 3);
        public IReadOnlyDictionary<Vector2Int, ITetrisItem> Contents       => _contents;
        public IReadOnlyDictionary<Vector2Int, Vector2Int>  ContentPointer => throw new NotSupportedException();
        public ChainTopology Topology => new(_chains, new(), new(), new(), new());

        public event Action<IReadOnlyDictionary<Vector2Int, ITetrisItem>> OnContentsChanged;

        public bool TryAdd(ITetrisItem item) => throw new NotSupportedException();
        public bool TryAddAt(Vector2Int position, ref ITetrisItem arrival) => throw new NotSupportedException();
        public bool TrySwapInto(Vector2Int anchor, ref ITetrisItem incoming,
            ITetrisContainer source, Vector2Int sourceAnchor) => throw new NotSupportedException();
        public bool TryRemove(Vector2Int position, out ITetrisItem removed) => throw new NotSupportedException();
        public bool TryRemove(ITetrisItem item) => throw new NotSupportedException();
        public bool CanAddAt(Vector2Int position, ITetrisItem item, out List<Vector2Int> overlapping)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Attachment item carrying one passive pawn-stat affix. Mirrors the real AttachmentItem:
    /// OnUnchained applies the affix, OnChained removes it.
    /// </summary>
    internal sealed class FakeAttachment : FakeItem, IAttachmentItem
    {
        public FakeAttachment(string name)
            : base(name) =>
            Affix = new PawnStatModifier(PawnStat.LifeMax,
                new Modifier(5f, ModifierType.FlatAdd, Guid.NewGuid()));

        public PawnStatModifier Affix { get; }
        public IReadOnlyList<PawnStatModifier> affixes => new[] { Affix };

        public void OnUnchained(IPawnStats stats) => stats.ApplyMod(Affix);
        public void OnChained(IPawnStats stats)   => stats.RemoveMod(Affix);
    }

    /// <summary>
    /// IPawnStats double that records the currently-applied pawn-stat modifiers. Only ApplyMod /
    /// RemoveMod are exercised by the chain-state seam; the stat pools are unused.
    /// </summary>
    internal sealed class RecordingStats : IPawnStats
    {
        public readonly List<PawnStatModifier> Active = new();

        public void ApplyMod(PawnStatModifier mod)  => Active.Add(mod);
        public void RemoveMod(PawnStatModifier mod) => Active.Remove(mod);

        public Resource health        => throw new NotSupportedException();
        public Resource mana          => throw new NotSupportedException();
        public Stat     healthRegen   => throw new NotSupportedException();
        public Stat     manaRegen     => throw new NotSupportedException();
        public Stat     movementSpeed => throw new NotSupportedException();
        public Stat     range         => throw new NotSupportedException();
    }
}
