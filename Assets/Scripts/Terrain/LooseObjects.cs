using System;
using System.Collections.Generic;
using System.Linq;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace DaggerfallWorkshop
{
    // List of loose objects, such as locations or loot containers
    // These objects are not recycled and will created/destroyed as needed
    public class LooseObjects
    {
        private class Key : IEquatable<Key>
        {
            readonly int x;
            readonly int y;

            public int X { get { return x; } }

            public int Y { get { return y; } }

            public Key(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public override int GetHashCode()
            {
                return X.GetHashCode() * 31 + Y.GetHashCode();
            }

            public bool Equals(Key other)
            {
                return X == other.X && Y == other.Y;
            }
        }

        public struct Desc
        {
            public GameObject gameObject;
            public bool statefulObj;
        }

        private Dictionary<Key, List<Desc>> buckets;

        public LooseObjects()
        {
            buckets = new Dictionary<Key, List<Desc>>();
        }

        public bool IsEmpty { get { return buckets.Count == 0; } }

        public int Count { get { int count = 0; foreach (List<Desc> bucket in buckets.Values) { count += bucket.Count; } return count; } }

        public void DebugLog(int MapPixelX, int MapPixelY, int TerrainDistance)
        {
            int minX = MapPixelX - TerrainDistance, maxX = MapPixelX + TerrainDistance;
            int minY = MapPixelY - TerrainDistance, maxY = MapPixelY + TerrainDistance;
            foreach (Key key in buckets.Keys)
            {
                // Debug.Log(String.Format("({0},{1}) => {2}", key.First, key.Second, looseObjectsList[key].Count));
                if (key.X < minX)
                    minX = key.X;
                if (key.X > maxX)
                    maxX = key.X;
                if (key.Y < minY)
                    minY = key.Y;
                if (key.Y > maxY)
                    maxY = key.Y;
            }
            Debug.Log(String.Format("({0},{1}) - ({2},{3})", minX, minY, maxX, maxY));
            Debug.Log(Enumerable.Range(minX, maxX - minX + 1).Select((x, i) => x.ToString()).Aggregate("\t", (a, b) => a + "\t" + b));
            for (int y = minY; y <= maxY; y++)
            {
                Debug.Log(String.Format("{0}\t{1}", y, Enumerable.Range(minX, maxX - minX + 1)
                .Select((x, i) =>
                {
                    Key key = new Key(x, y);
                    List<Desc> bucket = null;
                    int count = buckets.TryGetValue(key, out bucket) ? bucket.Count : 0;
                    if (count > 0)
                        return count.ToString();
                    else if (Math.Abs(x - MapPixelX) <= TerrainDistance &&
                             Math.Abs(y - MapPixelY) <= TerrainDistance)
                        return "*";
                    else
                        return "-";
                }).Aggregate("", (a, b) => a + "\t" + b)));
            }
        }

        public void Add(int x, int y, Desc desc)
        {
            Key key = new Key(x, y);
            List<Desc> bucket;
            if (!buckets.TryGetValue(key, out bucket))
            {
                bucket = new List<Desc>();
                buckets.Add(key, bucket);
            }
            bucket.Add(desc);
        }

        public void ClearStatefulObjects()
        {
            foreach (List<Desc> bucket in buckets.Values)
            {
                bucket.RemoveAll(x => x.statefulObj);
            }
        }

        public DaggerfallLocation GetLocation(int x, int y)
        {
            Key key = new Key(x, y);
            List<Desc> bucket;
            if (buckets.TryGetValue(key, out bucket))
            {
                foreach (Desc desc in bucket)
                {
                    if (!desc.statefulObj && desc.gameObject)
                    {
                        DaggerfallLocation location = desc.gameObject.GetComponent<DaggerfallLocation>();
                        if (location)
                            return location;
                    }
                }
            }

            return null;
        }

        public void RemoveBuckets(Func<int, int, List<Desc>, bool> match)
        {
            List<Key> dropKeys = new List<Key>(10);
            foreach (KeyValuePair<Key, List<Desc>> kv in buckets)
            {
                if (match(kv.Key.X, kv.Key.Y, kv.Value))
                    dropKeys.Add(kv.Key);
            }
            foreach (Key key in dropKeys)
            {
                buckets.Remove(key);
            }
        }
    }
}
