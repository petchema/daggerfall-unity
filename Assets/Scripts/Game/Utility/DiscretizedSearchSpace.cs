using System;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSearchSpace
    {
        private DiscretizedSpace.SpaceMetaCube spaceCache;

        public DiscretizedSearchSpace()
        {
            spaceCache = new DiscretizedSpace.SpaceMetaCube();
            spaceCache.Init();
        }

        // FIXME not very information packed, uh
        static readonly uint doneBit = 0x2;
        static readonly uint closedBit = 0x1;
        static readonly DiscretizedSpace.SpaceCacheEntry doneCacheEntry = new DiscretizedSpace.SpaceCacheEntry(doneBit); 
        static readonly DiscretizedSpace.SpaceCacheEntry closedCacheEntry = new DiscretizedSpace.SpaceCacheEntry(closedBit); 

        public enum CellState {
            Open, Closed, Done
        }

        public CellState GetCellState(Vector3Int pos)
        {
           if (spaceCache.TryGetValue(pos, out DiscretizedSpace.SpaceCacheEntry entry))
           {
                if ((entry.flags & doneBit) != 0)
                    return CellState.Done;
                else if ((entry.flags & closedBit) != 0)
                    return CellState.Closed;
           }
           return CellState.Open;
        }

        public void SetCellClosed(Vector3Int pos)
        {
            spaceCache.Set(pos, closedCacheEntry);
        }

        public void SetCellDone(Vector3Int pos)
        {
            spaceCache.Set(pos, doneCacheEntry);
        }

        internal void Clear()
        {
            spaceCache.Clear();
        }
    }
}