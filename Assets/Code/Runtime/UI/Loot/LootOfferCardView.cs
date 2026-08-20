using System;
using Code.Runtime.Modules.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.UI.Loot
{
    /// <summary>
    /// One clickable card in the Loot phase's offer row. Purely presentational — LootPhase owns
    /// the offer pool, the remaining-picks count, and what clicking a card actually does.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class LootOfferCardView : MonoBehaviour
    {
        [SerializeField] private Image             icon;
        [SerializeField] private TextMeshProUGUI   nameText;
        [SerializeField] private Button            button;

        public ITetrisItem Item { get; private set; }
        public event Action<LootOfferCardView> Picked;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            button.onClick.AddListener(() => Picked?.Invoke(this));
        }

        public void Bind(ITetrisItem item)
        {
            Item        = item;
            icon.sprite = item.Icon;
            icon.color  = item.Icon != null ? Color.white : Color.clear;
            nameText.text = item.Name;
        }
    }
}
