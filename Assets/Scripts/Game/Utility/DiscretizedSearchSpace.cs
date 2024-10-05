using System;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSearchSpace
    {
        private DiscretizedSpace.SpaceMetaCube<byte> spaceCache;

        public DiscretizedSearchSpace()
        {
            spaceCache = new DiscretizedSpace.SpaceMetaCube<byte>();
            spaceCache.Init();
        }

        static readonly byte doneBit = 0x2;
        static readonly byte closedBit = 0x1;

        public enum CellState {
            Open, Closed, Done
        }

        public CellState GetCellState(Vector3Int pos)
        {
           if (spaceCache.TryGetValue(pos, out byte entry))
           {
                if ((entry & doneBit) != 0)
                    return CellState.Done;
                else if ((entry & closedBit) != 0)
                    return CellState.Closed;
           }
           return CellState.Open;
        }

        public void SetCellClosed(Vector3Int pos)
        {
            spaceCache.Set(pos, closedBit);
        }

        public void SetCellDone(Vector3Int pos)
        {
            spaceCache.Set(pos, doneBit);
        }

        internal void Clear()
        {
            spaceCache.Clear();
        }
    }
}