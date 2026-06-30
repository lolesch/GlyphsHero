using System;
using Code.Data.Enums;
using Code.Runtime.Core.Combat;
using Code.Runtime.Modules.Statistics;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditMode.Combat
{
    /// <summary>
    /// Red-green lock on the pure fail-forward propagation-cost walker (ADR-0006). Pins the four
    /// behaviours the model is built on: the Reactor-scaled root gate (Decision 6), fail-forward subtree
    /// pruning in propagation order (Decision 2), the Splitter fork funding siblings highest-subtree-drain
    /// first off the shared pool without compounding (Decision 3), and marginal costs read live off the
    /// <see cref="MutableFloat"/> pipeline so depth inflates downstream cost (Decision 5).
    /// </summary>
    [TestFixture]
    public sealed class PropagationCostResolverTests
    {
        private static Modifier Flat(float v) => new(v, ModifierType.FlatAdd,     Guid.NewGuid());
        private static Modifier Mult(float v) => new(v, ModifierType.PercentMult, Guid.NewGuid());
        private static CostNode Node(Modifier cost, params CostNode[] children) =>
            new(cost, children.Length == 0 ? null : children);

        private static readonly CostNode[] NoPayloads = Array.Empty<CostNode>();

        // ----- Root gate (Decision 6) -----

        [Test]
        public void RootGate_PoolBelowBase_NothingFires()
        {
            var result = PropagationCostResolver.Resolve(10f, null, NoPayloads, poolBalance: 5f);

            result.RootFired.Should().BeFalse();
            result.FiredNodes.Should().BeEmpty();
            result.TotalSpent.Should().Be(0f);
        }

        [Test]
        public void RootGate_PoolExactlyBase_Fires()
        {
            var result = PropagationCostResolver.Resolve(10f, null, NoPayloads, poolBalance: 10f);

            result.RootFired.Should().BeTrue();
            result.TotalSpent.Should().BeApproximately(10f, 1e-3f);
        }

        [Test]
        public void RootGate_ReactorModRaisesEffectiveBase()
        {
            // A Reactor cost modifier (here +100% mult) is the effective base the pool is gated on
            // (Decision 6). At pool 15 the weapon would fire on its bare base 10 but not on the
            // reactor-scaled base 20 — the mutation lock for "the root gate ignores reactor mods".
            var reactor = new[] { Mult(100f) };

            PropagationCostResolver.Resolve(10f, reactor, NoPayloads, poolBalance: 15f)
                .RootFired.Should().BeFalse();

            var paid = PropagationCostResolver.Resolve(10f, reactor, NoPayloads, poolBalance: 20f);
            paid.RootFired.Should().BeTrue();
            paid.TotalSpent.Should().BeApproximately(20f, 1e-3f);
        }

        // ----- Linear propagation (Decision 2) -----

        [Test]
        public void LinearChain_AllAffordable_AllFire()
        {
            var c = Node(Flat(10f));
            var b = Node(Flat(10f), c);
            var a = Node(Flat(10f), b);

            var result = PropagationCostResolver.Resolve(0f, null, new[] { a }, poolBalance: 100f);

            result.RootFired.Should().BeTrue();
            result.FiredNodes.Should().HaveCount(3).And.Contain(new[] { a, b, c });
            result.TotalSpent.Should().BeApproximately(30f, 1e-3f);
        }

        [Test]
        public void FailForward_UnaffordableNode_PrunesItsWholeSubtree()
        {
            // a fires; b can't be paid; c is dirt-cheap (1) and would be affordable on its own — but it
            // is b's descendant, so the prune takes it too. The lock for "fail-forward stops the branch",
            // not "skip only the unaffordable node".
            var c = Node(Flat(1f));
            var b = Node(Flat(10f), c);
            var a = Node(Flat(10f), b);

            var result = PropagationCostResolver.Resolve(0f, null, new[] { a }, poolBalance: 15f);

            result.FiredNodes.Should().HaveCount(1).And.Contain(a);
            result.FiredNodes.Should().NotContain(c);
            result.TotalSpent.Should().BeApproximately(10f, 1e-3f);
        }

        // ----- Splitter fork (Decision 3) -----

        [Test]
        public void Fork_FundsHighestSubtreeDrainFirst()
        {
            // children listed cheap-first to prove ordering is by drain, not list order. With pool 70 the
            // big centerpiece (60) gets first claim and fires; the cheap sibling (30) is then starved.
            // Lowest-first would fire the cheap one and starve the centerpiece — the ordering mutation lock.
            var small = Node(Flat(30f));
            var big   = Node(Flat(60f));
            var root  = Node(Flat(0f), small, big);

            var result = PropagationCostResolver.Resolve(0f, null, new[] { root }, poolBalance: 70f);

            result.FiredNodes.Should().Contain(big).And.NotContain(small);
            result.TotalSpent.Should().BeApproximately(60f, 1e-3f);
        }

        [Test]
        public void Fork_PrunedSiblingIsAtomic_BudgetSurvivesForNextSibling()
        {
            // The centerpiece (60) is unaffordable at pool 40 and spends nothing; the cheaper sibling (30)
            // still fires from the untouched pool. Locks that a pruned node leaves the budget intact.
            var small = Node(Flat(30f));
            var big   = Node(Flat(60f));
            var root  = Node(Flat(0f), small, big);

            var result = PropagationCostResolver.Resolve(0f, null, new[] { root }, poolBalance: 40f);

            result.FiredNodes.Should().Contain(small).And.NotContain(big);
            result.TotalSpent.Should().BeApproximately(30f, 1e-3f);
        }

        [Test]
        public void Fork_SiblingsDoNotCompound()
        {
            // weapon base 10; a fork of [mult +100%, flat +10]. Each sibling restarts from the running
            // cost R = 10, so the flat sibling's marginal is 10 (total spent 10 base + 10 mult-on-10 + 10
            // flat = 30). If the mult leaked into the flat sibling it would cost 20 (total 40) — so 30 vs
            // 40 is the lock that sibling modifiers are undone between branches (Decision 3).
            var multSibling = Node(Mult(100f));
            var flatSibling = Node(Flat(10f));
            var root        = Node(Flat(0f), multSibling, flatSibling);

            var result = PropagationCostResolver.Resolve(10f, null, new[] { root }, poolBalance: 1000f);

            result.FiredNodes.Should().Contain(new[] { multSibling, flatSibling });
            result.TotalSpent.Should().BeApproximately(30f, 1e-3f);
        }

        // ----- Live aggregate marginals (Decision 5) -----

        [Test]
        public void PercentMultUpstream_InflatesDownstreamMarginal()
        {
            // base 10 → flat +10 (marginal 10) → mult +100% (marginal 20: (10+10)*2-20) → flat +10
            // (marginal 20: (30)*2 - 40). The second flat +10 costs 20, double the first, because the
            // upstream mult is on the running cost — "deeper costs more" emerges from the MutableFloat
            // pipeline. Total 10+10+20+20 = 60. A naive sum of raw modifier values would give 40 — the
            // mutation lock that marginals are read live, not summed.
            var c = Node(Flat(10f));
            var b = Node(Mult(100f), c);
            var a = Node(Flat(10f), b);

            var result = PropagationCostResolver.Resolve(10f, null, new[] { a }, poolBalance: 1000f);

            result.FiredNodes.Should().HaveCount(3);
            result.TotalSpent.Should().BeApproximately(60f, 1e-3f);
        }
    }
}
