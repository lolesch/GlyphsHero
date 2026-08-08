using Code.Runtime.UI.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.UI
{
    /// <summary>
    /// Locks the tooltip header row (tooltip-redesign v2 slice 5, issue #21): the type text label
    /// ("[Weapon]", "[Amplifier]", ...) is Details-mode-only — hidden by default so it doesn't compete
    /// with the header icon (Leonid: "it belongs to the icon, not the name"). The name and type glyph
    /// (<see cref="TypeGlyphs"/>) always render regardless of mode.
    ///
    /// Red-green: written against the pre-fix behavior (label always rendered), so
    /// <see cref="Default_HidesTypeLabel"/> would have failed before the toggle existed.
    /// </summary>
    [TestFixture]
    public sealed class HeaderLineTests
    {
        [Test]
        public void Default_HidesTypeLabel() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: false)
                .Should().NotContain("[Weapon]");

        [Test]
        public void Details_ShowsTypeLabel() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: true)
                .Should().Contain("[Weapon]");

        [Test]
        public void Details_PayloadWeapon_LabelIsPayload() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: true, isWeaponRoot: false, detailed: true)
                .Should().Contain("[Payload]");

        [Test]
        public void Details_Amplifier_LabelIsAmplifier() =>
            HeaderLine.Build(new FakeAmplifier("Glow"), isPayload: false, isWeaponRoot: false, detailed: true)
                .Should().Contain("[Amplifier]");

        [Test]
        public void Default_ContainsName() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: false)
                .Should().Contain("Blade");

        [Test]
        public void Details_ContainsName() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: true)
                .Should().Contain("Blade");

        // Header glyph channel (issue #27): the type glyph reflects the item's current chain
        // membership, via TypeGlyphs' sprite-tag map — locks that HeaderLine actually forwards
        // isChained rather than dropping it on the floor.
        [Test]
        public void Chained_HeaderGlyphIsChainedSprite() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: false,
                    isChained: true)
                .Should().Contain("Weapon_Chained");

        [Test]
        public void Unchained_HeaderGlyphIsUnchainedSprite() =>
            HeaderLine.Build(new FakeWeapon("Blade"), isPayload: false, isWeaponRoot: true, detailed: false,
                    isChained: false)
                .Should().Contain("Weapon_Unchained");
    }
}
