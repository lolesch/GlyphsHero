using Code.Data.Enums;
using UnityEngine;

namespace Code.Data.Items.Amplifier
{
    [CreateAssetMenu(fileName = "AmplifierConfig", menuName = Const.ItemConfig + "Amplifier")]
    public sealed class AmplifierConfig : AttachmentItemConfig
    {
        [field: Header("Chained")]
        [field: SerializeField] public WeaponOutputStatModConfig outputStatMod { get; private set; }

        // Optional secondary Cost axis (ADR-0009) — independent of outputStatMod's Magnitude job.
        // Default type FlatAdd + value 0 so an asset that never authors this field resolves as a
        // true no-op, not Overwrite's implicit 0 (see WeaponInputStatModConfig/ModifierType default).
        [field: SerializeField]
        public WeaponInputStatModConfig inputStatMod { get; private set; } = new() { type = ModifierType.FlatAdd };

        public override int MaxConnectors => 2;
    }
}