using System;
using Code.Data.Enums;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Statistics
{
    /// <summary>
    /// The public <see cref="Stat.OnTotalChanged"/> forwarding event: UI (in the UI asmdef) needs a
    /// notification hook when a modifier changes a stat's total, without being handed the underlying
    /// <see cref="MutableFloat"/> (which would bypass <see cref="Stat"/> as the sole modifier gate).
    /// </summary>
    [TestFixture]
    public sealed class StatTests
    {
        private static Modifier Mod(float value, ModifierType type) =>
            new(value, type, Guid.NewGuid());

        [Test]
        public void OnTotalChanged_FiresWithNewTotal_OnModifierAdd()
        {
            var range = new Stat(PawnStat.Range, 5f);
            float? observed = null;
            range.OnTotalChanged += total => observed = total;

            range.AddModifier(Mod(3f, ModifierType.FlatAdd)); // 5 -> 8

            observed.Should().Be(8f);
            ((float) range).Should().Be(8f);
        }

        [Test]
        public void OnTotalChanged_FiresWithNewTotal_OnModifierRemove()
        {
            var range = new Stat(PawnStat.Range, 5f);
            var mod = Mod(3f, ModifierType.FlatAdd);
            range.AddModifier(mod); // 5 -> 8

            float? observed = null;
            range.OnTotalChanged += total => observed = total;

            range.TryRemoveModifier(mod); // 8 -> 5

            observed.Should().Be(5f);
        }

        [Test]
        public void OnTotalChanged_AfterUnsubscribe_DoesNotFire()
        {
            var range = new Stat(PawnStat.Range, 5f);
            var fired = false;
            Action<float> handler = _ => fired = true;

            range.OnTotalChanged += handler;
            range.OnTotalChanged -= handler;

            range.AddModifier(Mod(3f, ModifierType.FlatAdd));

            fired.Should().BeFalse();
        }
    }
}
