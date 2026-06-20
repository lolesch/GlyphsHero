using System.Collections.Generic;
using System.Linq;

namespace Code.Runtime.Modules.Inventory
{
    /// <summary>
    /// Pure topology: a chain's firing source (<see cref="Root"/>), its ordered downstream
    /// <see cref="Modifiers"/>, and the <see cref="Weapon"/> they resolve around. It holds no stat
    /// state and mutates nothing — effective stats are computed on demand by
    /// <see cref="WeaponStatResolver"/>.
    /// </summary>
    public sealed class ItemChain : IItemChain
    {
        public static readonly IItemChain Empty = new ItemChain(null, new List<ITetrisItem>());

        public ITetrisItem                Root      { get; }
        public IReadOnlyList<ITetrisItem> Modifiers { get; }
        public bool                       IsValid   => Root != null;
        public IWeaponItem                Weapon    => Root as IWeaponItem
                                                    ?? Modifiers.OfType<IWeaponItem>().FirstOrDefault();

        public ItemChain(ITetrisItem root, List<ITetrisItem> modifiers)
        {
            Root      = root;
            Modifiers = modifiers;
        }
    }

    public interface IItemChain
    {
        ITetrisItem                Root      { get; }
        IReadOnlyList<ITetrisItem> Modifiers { get; }
        bool                       IsValid   { get; }
        IWeaponItem                Weapon    { get; }
    }
}