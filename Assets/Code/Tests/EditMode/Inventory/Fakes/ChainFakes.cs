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

#pragma warning disable 0067 // event required by interface, unused by the resolver
        public event Action<IReadOnlyDictionary<Vector2Int, ITetrisItem>> OnContentsChanged;
#pragma warning restore 0067

        public bool TryAdd(ITetrisItem item) => throw new NotSupportedException();
        public bool TryAddAt(Vector2Int position, ref ITetrisItem arrival) => throw new NotSupportedException();
        public bool TryRemove(Vector2Int position, out ITetrisItem removed) => throw new NotSupportedException();
        public bool TryRemove(ITetrisItem item) => throw new NotSupportedException();
        public bool CanAddAt(Vector2Int position, ITetrisItem item, out List<Vector2Int> overlapping)
            => throw new NotSupportedException();
    }

    /// <summary>1x1 item with a configurable set of outgoing connector directions.</summary>
    internal abstract class FakeItem : ITetrisItem
    {
        private readonly List<Vector2Int> _connectorDirections;

        protected FakeItem(string name, params Vector2Int[] connectorDirections)
        {
            Name = name;
            Guid = Guid.NewGuid();
            _connectorDirections = connectorDirections?.ToList() ?? new List<Vector2Int>();
        }

        public Guid         Guid     { get; }
        public Sprite       Icon     => null;
        public string       Name     { get; }
        public RotationType rotation { get; set; }

        public List<Vector2Int> GetPointers(Vector2Int position) => new() { position };
        public Vector2Int GetShapeOrigin(List<Vector2Int> normalized = null) => Vector2Int.zero;
        public Vector2Int GetDimensions() => Vector2Int.one;
        public Vector2Int GetVisualDimensions() => Vector2Int.one;

        // For a 1x1 item the single occupied cell is the placement itself.
        public List<(Vector2Int slotPos, Vector2Int direction)> GetGridConnectors(Vector2Int placement)
            => _connectorDirections.Select(dir => (placement, dir)).ToList();
    }

    internal sealed class FakeWeapon : FakeItem, IWeaponItem
    {
        public FakeWeapon(string name, params Vector2Int[] connectorDirections)
            : base(name, connectorDirections) { }

        public MutableFloat    Damage           { get; } = new(1f);
        public MutableFloat    AttackSpeed      { get; } = new(1f);
        public MutableFloat    ResourceCost     { get; } = new(0f);
        public MutableFloat    ResourceGenOnHit { get; } = new(0f);
        public PayloadBehavior Payload          => null;
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
}
