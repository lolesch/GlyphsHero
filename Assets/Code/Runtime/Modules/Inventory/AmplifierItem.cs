using Code.Data.Items.Amplifier;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class AmplifierItem : AttachmentItem, IAmplifierItem
    {
        public WeaponOutputModifier outputMod { get; }
        public WeaponInputModifier  inputMod  { get; }

        public AmplifierItem(AmplifierConfig config, RotationType rotation = RotationType.None) : base(config, rotation)
        {
            outputMod = new WeaponOutputModifier(
                config.outputStatMod.stat,
                new Modifier(config.outputStatMod.value, config.outputStatMod.type, Guid));

            inputMod = new WeaponInputModifier(
                config.inputStatMod.stat,
                new Modifier(config.inputStatMod.value, config.inputStatMod.type, Guid));
        }
    }

    public interface IAmplifierItem : ITetrisItem
    {
        WeaponOutputModifier outputMod { get; }
        WeaponInputModifier  inputMod  { get; }
    }
}