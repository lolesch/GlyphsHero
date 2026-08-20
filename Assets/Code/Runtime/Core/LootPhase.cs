using System;
using System.Collections.Generic;
using Code.Data.Items;
using Code.Runtime.Modules.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.Core
{
    /// <summary>
    /// Generates loot items into the player stash after combat.
    /// Player reviews the stash and presses Continue to return to Placement,
    /// where they can assign new items to unit inventories.
    /// </summary>
    public sealed class LootPhase : IGamePhase
    {
        private readonly IPlayerData                    _playerData;
        private readonly Func<IReadOnlyList<ItemConfig>> _getScriptedLoot;
        private readonly Button                          _continueButton;
        private readonly Action                          _onContinue;

        public LootPhase(
            IPlayerData                     playerData,
            Func<IReadOnlyList<ItemConfig>> getScriptedLoot,
            Button                          continueButton,
            Action                          onContinue)
        {
            _playerData      = playerData;
            _getScriptedLoot = getScriptedLoot;
            _continueButton  = continueButton;
            _onContinue      = onContinue;
        }

        public void Enter()
        {
            GenerateLoot();

            _continueButton.gameObject.SetActive(true);
            _continueButton.onClick.AddListener(OnContinue);

            Debug.Log("[Phase] Loot — new items added to stash.");
        }

        public void Exit()
        {
            _continueButton.onClick.RemoveListener(OnContinue);
            _continueButton.gameObject.SetActive(false);
        }

        private void GenerateLoot()
        {
            var loot  = _getScriptedLoot();
            var added = 0;

            foreach (var config in loot)
            {
                if (config == null)
                    continue;

                var item = ItemFactory.Create(config);

                if (_playerData.Stash.TryAdd(item))
                    added++;
                else
                    Debug.LogWarning($"[LootPhase] Stash full — could not add {item.Name}.");
            }

            Debug.Log($"[LootPhase] Added {added}/{loot.Count} items to stash.");
        }

        private void OnContinue() => _onContinue();
    }
}