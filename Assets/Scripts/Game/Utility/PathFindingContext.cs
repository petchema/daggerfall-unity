using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{
    // Whatever data we may find interesting to keep persistent for an enemy
    // For example, we could save intermediate results of a A* research interrupted because of resources
    public class PathFindingContext
    {
        public readonly struct CacheKey
        {
            public readonly Vector3Int discretizedStart;
            public readonly Vector3Int discretizedDestination;
            public readonly float maxLength;
            public readonly float weight;

            public CacheKey(Vector3Int discretizedStart, Vector3Int discretizedDestination, float maxLength, float weight)
            {
                this.discretizedStart = discretizedStart;
                this.discretizedDestination = discretizedDestination;
                this.maxLength = maxLength;
                this.weight = weight;
            }
        }
        public readonly struct CacheValue
        {
            public readonly bool pathFound;
            public readonly List<Vector3> path;

            public CacheValue(bool pathFound, List<Vector3> path) : this()
            {
                this.pathFound = pathFound;
                this.path = path;
            }
        }

        private static CacheKey NoKey = new CacheKey(Vector3Int.zero, Vector3Int.zero, 0f, 0f);
        private static CacheValue NoValue = new CacheValue(false, null);
        private static float TTL = 30f;

        private CacheKey cacheKey = NoKey;
        private CacheValue cacheValue = NoValue;
        private float cacheTTL = 0f;

        internal bool HasCachedShortestPath(CacheKey cacheKey, out CacheValue cachedShortestPath)
        {
            // Information too old, parameters changed or target has moved: we don't have a cached answer
            if (Time.time >= cacheTTL
                || cacheKey.maxLength != this.cacheKey.maxLength
                || cacheKey.weight != this.cacheKey.weight
                || ((cacheKey.discretizedDestination - this.cacheKey.discretizedDestination).magnitude > 2f))
            {
                cachedShortestPath = NoValue;
                return false;
            }
            if (cacheValue.pathFound)
            {
                // Can we reuse part of the cached path?
                int closest = 0;
                float minDist = (cacheKey.discretizedStart - this.cacheValue.path[closest]).sqrMagnitude;
                while (closest < this.cacheValue.path.Count - 1)
                {
                    int nextClosest = closest + 1;
                    float nextMinDist = (cacheKey.discretizedStart - this.cacheValue.path[nextClosest]).sqrMagnitude;
                    if (nextMinDist < minDist)
                    {
                        closest = nextClosest;
                        minDist = nextMinDist;
                    }
                    else
                        break;
                }
                if (minDist < 1f)
                {
                    List<Vector3> path = this.cacheValue.path;
                    cachedShortestPath = new CacheValue(true, path.GetRange(closest, path.Count - closest));
                    return true;
                }
            }
            else // cacheValue.pathFound
            {
                // Cached answer was that target was unreachable and we haven't moved much: probably still true
                if ((cacheKey.discretizedStart - this.cacheKey.discretizedStart).magnitude < 2f)
                {
                    cachedShortestPath = this.cacheValue;
                    return true;
                }
            }
            // Otherwise we don't have a canned answer
            cachedShortestPath = NoValue;
            return false;
        }

        internal void CacheShortestPath(CacheKey cacheKey, CacheValue cachedShortestPath)
        {
            this.cacheKey = cacheKey;
            this.cacheValue = cachedShortestPath;
            cacheTTL = Time.time + TTL;
        }
    }
}
