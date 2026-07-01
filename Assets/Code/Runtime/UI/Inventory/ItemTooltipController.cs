using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Code.Data.Enums;
using Code.Data.Items.Weapon;
using Code.Runtime.Modules.Inventory;
using Code.Runtime.Modules.Statistics;
using Submodules.Utility.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Runtime.UI.Inventory
{
    [RequireComponent(typeof(Canvas))]
    public sealed class ItemTooltipController : MonoBehaviour, IItemTooltipController
    {
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Image         _panelFrame;
        [SerializeField] private TMP_Text      _text;
        [SerializeField] private Canvas        _canvas;
        [SerializeField] private float         _showDelay = 0.4f;

        private float         _anchoredX;
        private Coroutine     _pendingShow;
        private ITetrisItem   _pendingItem;
        private ITetrisItem   _visibleItem;
        private ITetrisItem   _pendingHideItem;
        private bool          _hideScheduled;
        private ITetrisItem   _cachedItem;
        private ChainTopology _cachedTopology;
        private bool          _altWasPressed;

        private static readonly Color LightGray = new(0.7f, 0.7f, 0.7f);

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                Debug.LogWarning("Assign _canvas in Inspector.", this);
            }
            _panel.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_hideScheduled)
            {
                _hideScheduled = false;
                ExecuteHide(_pendingHideItem);
                _pendingHideItem = null;
            }

            if (!_panel.gameObject.activeSelf) return;

            var altNow = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (altNow != _altWasPressed)
            {
                _altWasPressed = altNow;
                if (_cachedItem != null && _cachedTopology != null)
                {
                    _text.text = BuildTooltip(_cachedItem, _cachedTopology, altNow);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
                }
            }

            _panel.anchoredPosition = ClampedPosition();
        }

        // ── IItemTooltipController ────────────────────────────────────────

        public void RequestShow(ITetrisItem item, ITetrisContainer container, float anchorScreenX, bool onRight)
        {
            if (item == null) { Hide(null); return; }

            _hideScheduled   = false;
            _pendingHideItem = null;

            if (item == _visibleItem || item == _pendingItem) return;

            if (_pendingShow != null) StopCoroutine(_pendingShow);
            _pendingItem = item;
            _pendingShow = StartCoroutine(ShowAfterDelay(item, container, anchorScreenX, onRight));
        }

        public void Hide(ITetrisItem leavingItem)
        {
            if (leavingItem != null && leavingItem != _visibleItem && leavingItem != _pendingItem) return;

            _pendingHideItem = leavingItem;
            _hideScheduled   = true;
        }

        // ── Internals ─────────────────────────────────────────────────────

        private IEnumerator ShowAfterDelay(ITetrisItem item, ITetrisContainer container,
            float anchorScreenX, bool onRight)
        {
            if (_visibleItem == null)
                yield return new WaitForSeconds(_showDelay);

            _pendingShow  = null;
            _pendingItem  = null;
            _visibleItem  = item;

            var topology    = container.Topology;   // container-owned, resolved once — no per-hover re-resolve
            _cachedItem     = item;
            _cachedTopology = topology;
            _altWasPressed  = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            _text.text      = BuildTooltip(item, topology, _altWasPressed);

            var primaryChain  = PrimaryChain(item, topology);
            var isWeaponRoot  = item is IWeaponItem && primaryChain != null && !IsPayload(item, primaryChain);
            _panelFrame.color = ChainComponentColors.GetColor(item, isWeaponRoot);

            _panel.pivot = new Vector2(onRight ? 1f : 0f, 1f);
            _panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);

            _anchoredX              = CanvasX(anchorScreenX);
            _panel.anchoredPosition = ClampedPosition();
        }

        private void ExecuteHide(ITetrisItem leavingItem)
        {
            if (leavingItem != null && leavingItem != _visibleItem && leavingItem != _pendingItem) return;

            if (_pendingShow != null)
            {
                StopCoroutine(_pendingShow);
                _pendingShow = null;
                _pendingItem = null;
            }
            _visibleItem    = null;
            _cachedItem     = null;
            _cachedTopology = null;
            _panel.gameObject.SetActive(false);
        }

        // ── Position helpers ──────────────────────────────────────────────

        private Vector2 ClampedPosition()
        {
            var canvasRT = (RectTransform)_canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, Input.mousePosition, null, out var mouse);

            var half      = canvasRT.rect.size * 0.5f;
            var panelSize = _panel.rect.size;
            var clampedY  = Mathf.Clamp(mouse.y, Mathf.Min(-half.y + panelSize.y, half.y), half.y);

            return new Vector2(_anchoredX, clampedY);
        }

        private float CanvasX(float screenX)
        {
            var canvasRT = (RectTransform)_canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, new Vector2(screenX, 0f), null, out var local);

            return Mathf.Clamp(local.x, -canvasRT.rect.width * 0.5f, canvasRT.rect.width * 0.5f);
        }

        // ── Tooltip text ──────────────────────────────────────────────────

        private static string BuildTooltip(ITetrisItem item, ChainTopology topology, bool detailed)
        {
            var sb = new StringBuilder();

            var chains       = topology.Chains
                .Where(c => c.Root == item || c.Modifiers.Contains(item))
                .ToList();
            var isChained    = chains.Count > 0;
            var primaryChain = PrimaryChain(item, topology);
            var isPayload    = item is IWeaponItem && primaryChain != null && IsPayload(item, primaryChain);
            var isWeaponRoot = item is IWeaponItem && !isPayload;

            // Header: name + component tag, coloured by true root/payload state (red root, purple
            // payload) — not by "is a weapon", which mis-painted every payload with the root colour.
            var labelColor   = ChainComponentColors.GetColor(item, isWeaponRoot);
            var componentStr = $"[{ComponentLabel(item, isPayload)}]".Colored(labelColor);
            // Type channel (tooltip-redesign slice 1): a leading role glyph next to the name. Color
            // stays reserved for direction, so type is the glyph's job (TypeGlyphs).
            var typeGlyph    = TypeGlyphs.For(item, isPayload);
            sb.AppendLine($"<align=left>{typeGlyph} <b>{item.Name}</b><align=right> {componentStr}</align>");
            sb.AppendLine(new string('─', 24));

            // Attachments (Amplifier/Shifter/Reactor/Converter) carry an intrinsic affix identity;
            // weapons describe themselves through their firing (standalone / payload / chain output).
            AppendAttachmentIdentity(sb, item, isChained);

            if (!isChained)
            {
                if (item is IWeaponItem w)
                    AppendStandaloneWeapon(sb, w);
                return sb.ToString().TrimEnd();
            }

            foreach (var chain in chains)
            {
                sb.AppendLine();
                sb.AppendLine(new string('─', 24));
                if (detailed)
                {
                    sb.Append(BuildChainSentence(chain, item));
                    sb.AppendLine();
                }

                // Weapon-redesign slice 3: the weapon is the chain's terminal readout (final totals +
                // piece list). A payload weapon shows its own child delivery. Only non-weapon pieces
                // (amp/shifter/reactor/converter) still fall through to the per-piece marginal view.
                if (item is IWeaponItem weaponItem)
                {
                    if (IsPayload(item, chain))
                        AppendPayloadOutput(sb, chain, weaponItem);
                    else
                        AppendWeaponTerminal(sb, chain, detailed);
                }
                else
                    AppendChainOutput(sb, chain, item, detailed);
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildChainSentence(IItemChain chain, ITetrisItem hovered)
        {
            var ordered  = OrderedItems(chain);
            var hovIndex = ordered.IndexOf(hovered);
            var sb       = new StringBuilder();
            var isFirst  = true;

            void AppendEntry(ITetrisItem item)
            {
                var isPayload    = IsPayload(item, chain);
                var isWeaponRoot = item is IWeaponItem && !isPayload;
                var color        = ChainComponentColors.GetColor(item, isWeaponRoot);
                var name         = item == hovered ? $"<b>{item.Name}</b>" : item.Name;
                var prefix       = isFirst ? "" : "  ↓\n";
                sb.Append($"{prefix}{name.Colored(color)}\n");
                isFirst = false;
            }

            var isTrigger = hovered is IShifterItem or IReactorItem;
            if (isTrigger)
            {
                AppendEntry(hovered);
                for (var i = hovIndex + 1; i < ordered.Count; i++)
                {
                    AppendEntry(ordered[i]);
                    if (ordered[i] is IWeaponItem) break;
                }
            }
            else
            {
                AppendEntry(hovered);
                for (var i = hovIndex - 1; i >= 0; i--)
                    AppendEntry(ordered[i]);
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// The driving weapon is the chain's <em>terminal readout</em> (tooltip-redesign slice 3): the
        /// final resolved totals (not a delta) followed by an enumerated piece list — one line per
        /// contributing piece, <c>glyph + name + that piece's marginal delta</c> (coloured by direction).
        /// The math is factored into <see cref="PositionalDelta"/>; this method only formats it.
        /// </summary>
        private static void AppendWeaponTerminal(StringBuilder sb, IItemChain chain, bool detailed)
        {
            var totals = PositionalDelta.Totals(chain);

            var reactorDriven = chain.Root is IReactorItem;
            sb.AppendLine(reactorDriven ? "<b>Attack:</b>  (reactor-driven)" : "<b>Attack:</b>");
            sb.AppendLine($"  dmg  {(float)totals.Damage:F1}   {TerminalRate(chain, totals.AttackSpeed)}");
            sb.AppendLine($"  {DeliverySentence.Build(totals.Delivery, totals.Affinity, totals.Anchor, 0)}");
            // The weapon's resolved cost is the fail-forward root gate (ADR-0006): if the pool can't
            // cover it, nothing fires.
            sb.AppendLine($"  cost {(float)totals.ResourceCost:F1} [{totals.CostResource}]" +
                          "   (root gate)".Colored(LightGray));

            foreach (var piece in PositionalDelta.Pieces(chain))
                sb.AppendLine(PieceLine(piece, detailed));

            AppendPayloadSummary(sb, chain, totals.CostResource);
        }

        /// <summary>One piece-list row: the piece's type glyph, its name, and its marginal delta.</summary>
        private static string PieceLine(PieceDelta piece, bool detailed)
        {
            var glyph = TypeGlyphs.For(piece.Item, isPayload: false); // pieces are never payload weapons
            return $"  {glyph} {piece.Item.Name}  {PieceDeltaText(piece, detailed)}";
        }

        // A piece's marginal effect. Slice 3 keeps this generic (the numeric stat/axis deltas that
        // changed, plus the reactor's firing condition, which has no numeric readout); slice 4 refines
        // it into the full per-attachment views from the spec's §3 table.
        private static string PieceDeltaText(PieceDelta p, bool detailed)
        {
            if (p.Item is IReactorItem reactor)
                return $"fires {PositionalDelta.FiringCondition(reactor.ReactorType)}".Colored(LightGray);

            var parts = new List<string>();
            if (!Mathf.Approximately(p.Before.Damage, p.With.Damage))
                parts.Add($"{Stat(p.Before.Damage, p.With.Damage, detailed)} dmg");
            if (!Mathf.Approximately(p.Before.AttackSpeed, p.With.AttackSpeed))
            {
                // Show the attack interval (1/speed), where a shorter interval is the improvement.
                var beforeInt = p.Before.AttackSpeed > 0f ? 1f / p.Before.AttackSpeed : 0f;
                var withInt   = p.With.AttackSpeed   > 0f ? 1f / p.With.AttackSpeed   : 0f;
                parts.Add($"rate {Stat(beforeInt, withInt, detailed, invert: true)}s");
            }
            if (!Mathf.Approximately(p.Before.ResourceCost, p.With.ResourceCost))
                parts.Add($"cost {Stat(p.Before.ResourceCost, p.With.ResourceCost, detailed, invert: true)}");
            if (p.Before.Delivery     != p.With.Delivery)     parts.Add($"→ {p.With.Delivery}");
            if (p.Before.Affinity     != p.With.Affinity)     parts.Add($"→ {p.With.Affinity}");
            if (p.Before.Anchor       != p.With.Anchor)       parts.Add($"→ {p.With.Anchor}");
            if (p.Before.CostResource != p.With.CostResource) parts.Add($"pool → {p.With.CostResource}");

            return parts.Count > 0 ? string.Join("   ", parts) : "—".Colored(LightGray);
        }

        /// <summary>The terminal fire-rate readout: reactor-driven chains show the firing condition, else
        /// the resolved attack interval.</summary>
        private static string TerminalRate(IItemChain chain, float attackSpeed)
        {
            var reactor = chain.Root as IReactorItem
                          ?? chain.Modifiers.OfType<IReactorItem>().FirstOrDefault();
            return reactor != null
                ? $"fires {PositionalDelta.FiringCondition(reactor.ReactorType)}"
                : $"every {Interval(attackSpeed)}";
        }

        private static void AppendChainOutput(StringBuilder sb, IItemChain chain,
            ITetrisItem hovered, bool detailed)
        {
            var weapon = chain.Weapon;
            if (weapon == null) return;

            // Diff two resolved snapshots: the chain up to (but excluding) the hovered item, and the
            // chain including it. The difference is exactly what the hovered item contributes — no
            // weapon mutation. OrderedItems puts the root first, so the prefix is the upstream chain.
            var ordered  = OrderedItems(chain);
            var hovIndex = ordered.IndexOf(hovered);

            var before = WeaponStatResolver.Resolve(weapon, ordered.Take(hovIndex));
            var with   = WeaponStatResolver.Resolve(weapon, ordered.Take(hovIndex + 1));

            var reactorDriven = chain.Root is IReactorItem;
            sb.AppendLine(reactorDriven ? "<b>Attack:</b>  (reactor-driven)" : "<b>Attack:</b>");
            sb.AppendLine($"  dmg  {Stat(before.Damage, with.Damage, detailed)}   " +
                          $"{FireRate(chain, before.AttackSpeed, with.AttackSpeed, detailed)}");
            sb.AppendLine($"  {DeliverySentence.Build(with.Delivery, with.Affinity, with.Anchor, 0)}");

            var poolStr = with.CostResource != before.CostResource
                ? $" [{before.CostResource}→{with.CostResource}]"
                : $" [{with.CostResource}]";
            // The weapon's resolved cost is the fail-forward root gate (ADR-0006): if the pool can't
            // cover it nothing fires. Payload marginals are summarised below and detailed per-payload.
            sb.AppendLine($"  cost {Stat(before.ResourceCost, with.ResourceCost, detailed, invert: true)}{poolStr}" +
                          "   (root gate)".Colored(LightGray));

            AppendPayloadSummary(sb, chain, with.CostResource);
        }

        /// <summary>
        /// One line summarising the weapon's downstream payloads (ADR-0006): how many child deliveries
        /// the firing carries and what they add to the shared cost pool. Flat costs sum cleanly; a mix of
        /// modifier types can't be one honest number, so we say "mixed" and defer detail to each payload's
        /// own tooltip. Silent when the chain has no payload weapons.
        /// </summary>
        private static void AppendPayloadSummary(StringBuilder sb, IItemChain chain, ResourceType pool)
        {
            var payloads = chain.Modifiers.OfType<IWeaponItem>()
                .Where(w => w != chain.Weapon)
                .ToList();
            if (payloads.Count == 0) return;

            var allFlat = payloads.All(w => (w.Payload?.CostType ?? ModifierType.FlatAdd) == ModifierType.FlatAdd);
            var flatSum = payloads.Sum(w => w.Payload?.CostValue ?? 0f);
            var costStr = allFlat ? $"+{flatSum:0.###} [{pool}]" : "mixed cost";

            sb.AppendLine($"  {payloads.Count} payload{(payloads.Count > 1 ? "s" : "")}: {costStr}".Colored(LightGray));
        }

        /// <summary>
        /// A hovered payload weapon describes its <em>own</em> child delivery (ADR-0004 §4 / ADR-0006),
        /// not the root's stats — combat fires it with its own Damage, its PayloadBehavior delivery axes,
        /// and charges its authored cost modifier against the chain's shared pool. The old diff path
        /// showed the root's unchanged numbers here (a payload weapon isn't a WeaponStats contributor),
        /// which was simply wrong.
        ///
        /// Tooltip-redesign slice 3: the root name and the "(#n in propagation)" slot text are dropped —
        /// a payload's tooltip is about its own delivery + cost-to-pool, not its position in the root.
        /// </summary>
        private static void AppendPayloadOutput(StringBuilder sb, IItemChain chain, IWeaponItem payload)
        {
            sb.AppendLine("<b>Payload</b>");

            var b         = payload.Payload;
            var delivery  = b?.Delivery  ?? DeliveryPattern.Single;
            var affinity  = b?.Affinity  ?? Affinity.Hostile;
            var anchor    = b?.Anchor    ?? Anchor.Target;
            var shapeSize = b?.ShapeSize ?? 1;
            sb.AppendLine($"  {(float)payload.Damage:F1} dmg   ·   {DeliverySentence.Build(delivery, affinity, anchor, shapeSize)}");

            // What including this payload adds to the one shared pool — the chain root's CostResource
            // (ADR-0006 Decision 4). Type tells the player how it scales: flat, % of base, or compounding.
            var pool     = WeaponStatResolver.Resolve(chain).CostResource;
            var costVal  = b?.CostValue ?? 0f;
            var costType = b?.CostType  ?? ModifierType.FlatAdd;
            if (Mathf.Approximately(costVal, 0f))
                sb.AppendLine($"  free to add   [{pool}]".Colored(LightGray));
            else
            {
                var costStr = new Modifier(costVal, costType, Guid.Empty).ToString();
                sb.AppendLine($"  cost {costStr} [{pool}]   ·   {CostNote(costType).Colored(LightGray)}");
            }

            if (b != null && b.Timing != PayloadTiming.Instant)
                sb.AppendLine($"  {b.Timing.ToString().ToLowerInvariant()} ({b.TimingValue:0.###})".Colored(LightGray));
        }

        // ── Stat formatting ───────────────────────────────────────────────

        private static string Stat(float before, float after, bool detailed, bool invert = false)
        {
            if (Mathf.Approximately(before, after))
                return $"{after:F1}";

            var improved  = invert ? after < before : after > before;
            var color     = improved ? new Color(0f, 1f, 0.53f) : new Color(1f, 0.27f, 0.27f);
            var resultStr = $"{after:F1}".Colored(color);

            return detailed ? $"{before:F1} → {resultStr}" : resultStr;
        }

        // ── Item stats display ────────────────────────────────────────────

        private static void AppendAttachmentIdentity(StringBuilder sb, ITetrisItem item, bool isChained)
        {
            if (item is not (IAmplifierItem or IShifterItem or IReactorItem or IConverterItem))
                return;

            var chainedDesc  = ChainedDescription(item);
            var sm           = item as IAttachmentItem;
            var unchainedStr = sm?.affixes.Count > 0
                ? $"unchained: {sm.affixes[0].PawnStat} {sm.affixes[0].Modifier}"
                : null;

            // Bold the active half: in a chain the chained effect is live and the loose affix is greyed,
            // and vice-versa when the item sits alone in the grid (ADR-0004 item roles).
            if (isChained)
            {
                sb.AppendLine($"  <b>{chainedDesc}</b>");
                if (unchainedStr != null)
                    sb.AppendLine($"  {unchainedStr.Colored(LightGray)}");
            }
            else
            {
                sb.AppendLine($"  {chainedDesc.Colored(LightGray)}");
                if (unchainedStr != null)
                    sb.AppendLine($"  <b>{unchainedStr}</b>");
            }
        }

        /// <summary>A weapon sitting alone (no chain): it fires on its own timer with its base stats.</summary>
        private static void AppendStandaloneWeapon(StringBuilder sb, IWeaponItem w)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Attack:</b>");
            sb.AppendLine($"  {(float)w.Damage:F1} dmg   ·   {DeliverySentence.Build(w.Delivery, w.Affinity, w.Anchor, 0)}");
            sb.AppendLine($"  every {Interval((float)w.AttackSpeed)}   ·   cost {(float)w.ResourceCost:F1} [{w.CostResource}]");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        // AttackSpeed is attacks-per-second (combat fires every 1/AttackSpeed s — CombatClock). The old
        // tooltip printed the rate value as if it were seconds, so a fast weapon read as "every 2.5s"
        // when it actually fired every 0.4s. Convert to the interval and treat lower as the improvement.
        private static string FireRate(IItemChain chain, float beforeSpd, float afterSpd, bool detailed)
        {
            var reactor = chain.Root as IReactorItem
                          ?? chain.Modifiers.OfType<IReactorItem>().FirstOrDefault();
            if (reactor != null)
                return $"fires {PositionalDelta.FiringCondition(reactor.ReactorType)}"; // reactor-driven: the timer is suppressed

            var before = beforeSpd > 0f ? 1f / beforeSpd : 0f;
            var after  = afterSpd  > 0f ? 1f / afterSpd  : 0f;

            var valueStr = Mathf.Approximately(before, after)
                ? $"{after:0.00}s"
                : detailed
                    ? $"{before:0.00}s → {Stat(before, after, detailed, invert: true)}s"
                    : $"{Stat(before, after, detailed, invert: true)}s";

            return $"every {valueStr}";
        }

        private static string Interval(float attackSpeed) =>
            attackSpeed > 0f ? $"{1f / attackSpeed:0.00}s" : "—";

        // The attachment's own §3 active-delta content (tooltip-redesign slice 4). Built intrinsically
        // by PositionalDelta.Describe — additive lines (a numeric line only when non-default), including
        // the reactor's input modifier that the old per-type switch dropped. Joined onto one "chained:"
        // line; the bold/dim two-state framing lives in AppendAttachmentIdentity.
        private static string ChainedDescription(ITetrisItem item)
        {
            var lines = PositionalDelta.Describe(item);
            return lines.Count > 0 ? "chained:   " + string.Join("   ·   ", lines) : "chained:";
        }

        private static List<ITetrisItem> OrderedItems(IItemChain chain)
        {
            var list = new List<ITetrisItem> { chain.Root };
            list.AddRange(chain.Modifiers);
            return list;
        }

        private static IItemChain PrimaryChain(ITetrisItem item, ChainTopology topology) =>
            topology.Chains.FirstOrDefault(
                c => (c.Root == item || c.Modifiers.Contains(item)) && !IsPayload(item, c))
            ?? topology.Chains.FirstOrDefault(c => c.Root == item || c.Modifiers.Contains(item));

        private static bool IsPayload(ITetrisItem item, IItemChain chain)
        {
            if (item is not IWeaponItem) return false;
            foreach (var ordered in OrderedItems(chain))
            {
                if (ordered == item)        return false; // no weapon found before self
                if (ordered is IWeaponItem) return true;  // a weapon precedes self
            }
            return false;
        }

        private static string ComponentLabel(ITetrisItem item, bool isPayload) => item switch
        {
            IWeaponItem when isPayload => "Payload",
            IWeaponItem                => "Weapon",
            IAmplifierItem             => "Amplifier",
            IConverterItem             => "Converter",
            IShifterItem               => "Shifter",
            IReactorItem               => "Reactor",
            _                          => item.GetType().Name,
        };

        // ── Player-facing word maps ───────────────────────────────────────

        // Delivery axes (Pattern × Affinity × Anchor + Aoe radius) are now rendered as one verb-led
        // sentence by DeliverySentence.Build (tooltip-redesign slice 2). The old AxesLine/DeliveryWord/
        // AffinityWord/AnchorWord robot-output maps were deleted with their callers.

        // ReactorWhen moved to PositionalDelta.FiringCondition (slice 4) so the attachment view, the
        // terminal rate line, and the piece list share one firing-condition map.

        /// <summary>How a payload's cost modifier scales (ADR-0006 Decision 5) — the player's read on
        /// whether stacking it deep gets expensive.</summary>
        private static string CostNote(ModifierType type) => type switch
        {
            ModifierType.FlatAdd     => "flat",
            ModifierType.PercentAdd  => "% of base",
            ModifierType.PercentMult => "deeper-costs-more",
            ModifierType.Overwrite   => "fixed",
            _                        => string.Empty,
        };
    }

    public interface IItemTooltipController
    {
        void RequestShow(ITetrisItem item, ITetrisContainer container, float anchorScreenX, bool onRight);
        void Hide(ITetrisItem leavingItem);
    }
}