using System.Collections.Generic;
using Code.Runtime.Core;
using UnityEngine;

namespace Code.Runtime.UI.Loot
{
    /// <summary>
    /// Renders the current <see cref="ILootOffer"/> as a row of clickable cards. Subscribes to
    /// GamePhaseController.LootOfferBound instead of being referenced by GameLoop directly — Core
    /// must not depend on UI (same pattern as PlayerStashView/StashBound).
    /// </summary>
    public sealed class LootOfferView : MonoBehaviour
    {
        [SerializeField] private Transform         container;
        [SerializeField] private LootOfferCardView cardPrefab;

        private ILootOffer _offer;
        private readonly List<LootOfferCardView> _liveCards = new();

        private void Awake()
        {
            GamePhaseController.LootOfferBound += Bind;
            if (GamePhaseController.CurrentLootOffer != null)
                Bind(GamePhaseController.CurrentLootOffer);
        }

        private void OnDestroy()
        {
            GamePhaseController.LootOfferBound -= Bind;
            if (_offer != null) _offer.OfferChanged -= Rebuild;
        }

        private void Bind(ILootOffer offer)
        {
            if (_offer != null) _offer.OfferChanged -= Rebuild;
            _offer = offer;
            if (_offer != null) _offer.OfferChanged += Rebuild;
            Rebuild();
        }

        private void Rebuild()
        {
            foreach (var card in _liveCards)
                if (card != null) Destroy(card.gameObject);
            _liveCards.Clear();

            if (_offer == null) return;

            var showCards = _offer.RemainingPicks > 0;
            foreach (var item in _offer.CurrentOffer)
            {
                var card = Instantiate(cardPrefab, container);
                card.Bind(item);
                card.gameObject.SetActive(showCards);
                card.Picked += OnCardPicked;
                _liveCards.Add(card);
            }
        }

        private void OnCardPicked(LootOfferCardView card) => _offer?.TryPick(card.Item);
    }
}
