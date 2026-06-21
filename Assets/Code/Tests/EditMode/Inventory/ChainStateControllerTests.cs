using System.Collections.Generic;
using System.Linq;
using Code.Runtime.Modules.Inventory;
using Code.Tests.EditMode.Inventory.Fakes;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Inventory
{
    /// <summary>
    /// Locks the chain-state seam (Candidate 4): an attachment grants its passive pawn-stat affix
    /// while it is NOT part of a weapon chain, and loses it once chained. ChainStateController owns
    /// these transitions, diffing the container's owned topology against the previous state.
    /// </summary>
    [TestFixture]
    public sealed class ChainStateControllerTests
    {
        // First item is the chain root, the rest are its downstream modifiers — every item in the
        // chain counts as "chained".
        private static IItemChain Chain(params ITetrisItem[] items)
            => new ItemChain(items[0], items.Skip(1).ToList());

        [Test]
        public void Bootstrap_LooseAttachment_AppliesPassive()
        {
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(att);
            var stats     = new RecordingStats();

            _ = new ChainStateController(container, stats);

            stats.Active.Should().ContainSingle().Which.Should().Be(att.Affix);
        }

        [Test]
        public void Bootstrap_ChainedAttachment_DoesNotApplyPassive()
        {
            var weapon    = new FakeWeapon("W");
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(weapon).Add(att);
            container.SetChains(Chain(weapon, att));
            var stats = new RecordingStats();

            _ = new ChainStateController(container, stats);

            stats.Active.Should().BeEmpty();
        }

        [Test]
        public void Chaining_LooseAttachment_RemovesPassive()
        {
            var weapon    = new FakeWeapon("W");
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(weapon).Add(att); // both loose at bootstrap
            var stats     = new RecordingStats();
            _ = new ChainStateController(container, stats);
            stats.Active.Should().ContainSingle("the attachment is loose at bootstrap");

            container.SetChains(Chain(weapon, att));
            container.RaiseChanged();

            stats.Active.Should().BeEmpty();
        }

        [Test]
        public void Unchaining_Attachment_ReappliesPassive()
        {
            var weapon    = new FakeWeapon("W");
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(weapon).Add(att);
            container.SetChains(Chain(weapon, att));
            var stats = new RecordingStats();
            _ = new ChainStateController(container, stats);
            stats.Active.Should().BeEmpty("the attachment is chained at bootstrap");

            container.SetChains(); // break the chain
            container.RaiseChanged();

            stats.Active.Should().ContainSingle().Which.Should().Be(att.Affix);
        }

        [Test]
        public void RemovingLooseAttachment_RemovesPassive()
        {
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(att);
            var stats     = new RecordingStats();
            _ = new ChainStateController(container, stats);
            stats.Active.Should().ContainSingle();

            container.Remove(att);
            container.RaiseChanged();

            stats.Active.Should().BeEmpty("a removed attachment must not leak its passive");
        }

        [Test]
        public void ContentsChanged_WithoutStateChange_DoesNotReapply()
        {
            var att       = new FakeAttachment("Amp");
            var container = new FakeStateContainer().Add(att);
            var stats     = new RecordingStats();
            _ = new ChainStateController(container, stats);

            container.RaiseChanged();
            container.RaiseChanged();

            stats.Active.Should().ContainSingle("an unchanged item must not re-apply its passive");
        }
    }
}
