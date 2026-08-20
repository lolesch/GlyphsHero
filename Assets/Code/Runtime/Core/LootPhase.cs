using System;
using System.Collections.Generic;
using Code.Data.Items;
using Code.Runtime.Modules.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.Core
{
    /// <summary>
    /// Offers a small pick from this encounter's scripted loot pool after combat — not an
    /// auto-grant. Player claims up to the encounter's pick count (each claim lands immediately
    /// in the stash); unclaimed offers are discarded on Continue, not banked.
    /// Data-only, like the rest of GameLoop: presentation lives in the UI layer, which reads
    /// <see cref="ILootOffer"/> and calls back through <see cref="TryPick"/> — GameLoop must not
    /// depend on UI (see GamePhaseController.StashBound for the same pattern on the stash).
    /// </summary>
    public sealed class LootPhase : IGamePhase, ILootOffer
    {
        private readonly IPlayerData                     _playerData;
        private readonly Func<IReadOnlyList<ItemConfig>> _getScriptedLoot;
        private readonly Func<int>                       _getPickCount;
        private readonly Button                          _continueButton;
        private readonly Action                          _onContinue;

        private readonly List<ITetrisItem> _offer = new();

        public IReadOnlyList<ITetrisItem> CurrentOffer   => _offer;
        public int                        RemainingPicks { get; private set; }
        public event Action                OfferChanged;

        public LootPhase(
            IPlayerData                     playerData,
            Func<IReadOnlyList<ItemConfig>> getScriptedLoot,
            Func<int>                       getPickCount,
            Button                          continueButton,
            Action                          onContinue)
        {
            _playerData      = playerData;
            _getScriptedLoot = getScriptedLoot;
            _getPickCount    = getPickCount;
            _continueButton  = continueButton;
            _onContinue      = onContinue;
        }

        public void Enter()
        {
            BuildOffer();

            _continueButton.gameObject.SetActive(true);
            _continueButton.onClick.AddListener(OnContinue);

            Debug.Log($"[Phase] Loot — offering {_offer.Count} item(s), {RemainingPicks} pick(s) available.");
        }

        public void Exit()
        {
            _continueButton.onClick.RemoveListener(OnContinue);
            _continueButton.gameObject.SetActive(false);

            _offer.Clear();
            RemainingPicks = 0;
            OfferChanged?.Invoke();
        }

        private void BuildOffer()
        {
            _offer.Clear();
            RemainingPicks = Mathf.Max(0, _getPickCount());

            foreach (var config in _getScriptedLoot())
            {
                if (config == null) continue;
                var item = ItemFactory.Create(config);
                if (item != null) _offer.Add(item);
            }

            OfferChanged?.Invoke();
        }

        /// <summary>Called by the UI layer when the player clicks an offered item's card.</summary>
        public void TryPick(ITetrisItem item)
        {
            if (RemainingPicks <= 0 || !_offer.Contains(item)) return;

            if (_playerData.Stash.TryAdd(item))
                RemainingPicks--;
            else
                Debug.LogWarning($"[LootPhase] Stash full — could not add {item.Name}.");

            _offer.Remove(item);
            OfferChanged?.Invoke();
        }

        private void OnContinue() => _onContinue();
    }

    /// <summary>UI-facing read/act surface for the current loot offer — keeps GameLoop → UI one-way.</summary>
    public interface ILootOffer
    {
        IReadOnlyList<ITetrisItem> CurrentOffer   { get; }
        int                        RemainingPicks { get; }
        event Action                OfferChanged;
        void TryPick(ITetrisItem item);
    }
}
