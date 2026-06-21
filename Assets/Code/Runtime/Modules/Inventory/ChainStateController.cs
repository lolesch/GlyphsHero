using System.Collections.Generic;
using Code.Runtime.Modules.Statistics;

namespace Code.Runtime.Modules.Inventory
{
    public sealed class ChainStateController
    {
        private readonly ITetrisContainer _inventory;
        private readonly IPawnStats       _stats;

        private HashSet<ITetrisItem> _chained    = new();
        private HashSet<ITetrisItem> _allTracked = new();

        public ChainStateController(ITetrisContainer inventory, IPawnStats stats)
        {
            _inventory = inventory;
            _stats     = stats;

            Bootstrap();

            _inventory.OnContentsChanged += _ => Refresh();
        }
        
        // Bootstrap from whatever is already in the inventory (e.g. starter weapon).
        private void Bootstrap()
        {
            var topology   = _inventory.Topology;
            var nowChained = CollectChained(topology);

            foreach (var item in _inventory.Contents.Values)
            {
                _allTracked.Add(item);
                if (!nowChained.Contains(item) && item is IAttachmentItem att)
                    att.OnUnchained(_stats);
            }

            _chained = nowChained;
        }

        private void Refresh()
        {
            var topology   = _inventory.Topology;
            var nowChained = CollectChained(topology);
            var nowAll     = new HashSet<ITetrisItem>(_inventory.Contents.Values);

            foreach (var item in _allTracked)
            {
                if (nowAll.Contains(item)) continue;
                if (!_chained.Contains(item) && item is IAttachmentItem att)
                    att.OnChained(_stats);
            }
            
            foreach (var item in nowAll)
            {
                if (_allTracked.Contains(item)) continue;
                if (!nowChained.Contains(item) && item is IAttachmentItem att)
                    att.OnUnchained(_stats);
            }
            
            foreach (var item in nowAll)
            {
                if (!_allTracked.Contains(item)) continue; // already handled above
                if (item is not IAttachmentItem att) continue;

                var wasChained = _chained.Contains(item);
                var isChained  = nowChained.Contains(item);
                
                if (!wasChained && isChained)
                    att.OnChained(_stats);
                else if (wasChained && !isChained )
                    att.OnUnchained(_stats);
            }

            _allTracked = nowAll;
            _chained    = nowChained;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static HashSet<ITetrisItem> CollectChained(ChainTopology topology)
        {
            var set = new HashSet<ITetrisItem>();
            foreach (var chain in topology.Chains)
            {
                set.Add(chain.Root);
                foreach (var mod in chain.Modifiers)
                    set.Add(mod);
            }
            return set;
        }
    }
}