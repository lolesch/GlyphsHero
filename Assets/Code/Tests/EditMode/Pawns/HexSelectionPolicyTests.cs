using System;
using Code.Data.Enums;
using Code.Data.Pawns;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using Code.Runtime.Pawns;
using Code.Runtime.UI.Inventory;
using FluentAssertions;
using NUnit.Framework;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Code.Tests.EditMode.Pawns
{
    /// <summary>
    /// Locks the pure click-selection policy of <see cref="HexSelectionHandler.ResolveSelection"/>
    /// (pawn-ui spec §Decisions 1): clicking a pawn selects it, clicking another switches, clicking
    /// empty keeps the current selection, and re-clicking the same pawn is a no-op (no deselect, no toggle).
    ///
    /// Red-green — these turn red under the plausible mutations:
    ///  - returning <c>clicked</c> unconditionally → <see cref="ClickEmpty_KeepsCurrentSelection"/> fails
    ///    (would clear on an empty-hex click);
    ///  - a toggle (<c>clicked == current ? null : clicked</c>) → <see cref="ReClickSamePawn_IsNoToggle"/>
    ///    fails (would deselect on re-click).
    /// The MonoBehaviour/Input wiring around it (mouse-down, event fire, Q/E) is human-verified in-editor.
    /// </summary>
    [TestFixture]
    public sealed class HexSelectionPolicyTests
    {
        [Test]
        public void ClickPawn_WhenNothingSelected_SelectsIt()
        {
            var pawn = new StubPawn();

            HexSelectionHandler.ResolveSelection(current: null, clicked: pawn)
                .Should().BeSameAs(pawn);
        }

        [Test]
        public void ClickAnotherPawn_SwitchesSelection()
        {
            IPawn current = new StubPawn();
            IPawn other   = new StubPawn();

            HexSelectionHandler.ResolveSelection(current, clicked: other)
                .Should().BeSameAs(other);
        }

        [Test]
        public void ClickEmpty_KeepsCurrentSelection()
        {
            IPawn current = new StubPawn();

            HexSelectionHandler.ResolveSelection(current, clicked: null)
                .Should().BeSameAs(current);
        }

        [Test]
        public void ReClickSamePawn_IsNoToggle()
        {
            IPawn current = new StubPawn();

            HexSelectionHandler.ResolveSelection(current, clicked: current)
                .Should().BeSameAs(current);
        }

        /// <summary>Reference-identity-only <see cref="IPawn"/>; members are never exercised by the policy.</summary>
        private sealed class StubPawn : IPawn
        {
            public IPawnEffect      PawnEffects   => throw new NotImplementedException();
            public IPawnStats       Stats         => throw new NotImplementedException();
            public ITetrisContainer Inventory     => throw new NotImplementedException();
            public PawnTeam         Team          => throw new NotImplementedException();
            public Hex              HexPosition   => throw new NotImplementedException();
            public TerrainCostConfig MovementCosts => throw new NotImplementedException();
            public Sprite           Icon          => throw new NotImplementedException();
            public string           DisplayName   => throw new NotImplementedException();

            public event Action OnDefeated { add { } remove { } }

            public void TakeDamage(float damage) => throw new NotImplementedException();
            public void MoveTo(Hex hex) => throw new NotImplementedException();
            public void MoveTo(Hex hex, float duration) => throw new NotImplementedException();
        }
    }
}
