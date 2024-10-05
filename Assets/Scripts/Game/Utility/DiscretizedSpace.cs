using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSpace
    {

        public static readonly int subdivisionShift = 4; // space will be divised in NxNxN cubes N = 2^subdivisionShift
        public static readonly int subdivisionMask = (1 << subdivisionShift) - 1;

        public struct SpaceCacheEntry
        {
            public UInt32 flags; // Bitfield of directions that have been already computed (movementIndex-based)
                                 // 32 is sufficient for storing 2 bits for 13 orientations

            public SpaceCacheEntry(uint flags)
            {
                this.flags = flags;
            }
        }

        public static readonly SpaceCacheEntry NoSpaceCacheEntry = new SpaceCacheEntry(); // use "default" instead?

        struct SpaceCube
        {
            // Flattened multidimensionnal array
            SpaceCacheEntry[,,] cube;
            public void Init()
            {
                cube = new SpaceCacheEntry[1 << subdivisionShift, 1 << subdivisionShift, 1 << subdivisionShift];
            }
            public bool IsMissing()
            {
                return cube == null;
            }
            public SpaceCacheEntry Get(int x, int y, int z)
            {
                return cube[z, y, x];
            }
            public void Set(int x, int y, int z, SpaceCacheEntry entry)
            {
                cube[z, y, x] = entry;
            }
        } 
        // Dictionary of Cubes
        public struct SpaceMetaCube
        {
            Dictionary<Vector3Int, SpaceCube> cache;
            // One entry cache. For 16x16x16 cubes, I have seen ~80% hit rates
            Vector3Int lastKey;
            SpaceCube? lastCube;
#if DEBUG_HEARING
            int access;
            int hit;
#endif

            internal void Init()
            {
                cache = new Dictionary<Vector3Int, SpaceCube>(128);
                lastKey = Vector3Int.zero;
                lastCube = null;
#if DEBUG_HEARING
                access = 0;
                hit = 0;
#endif
            }
            internal void Clear()
            {
                cache.Clear();
                lastKey = Vector3Int.zero;
                lastCube = null;
            }
            internal int Count()
            {
                return cache.Count;
            }
            public bool TryGetValue(Vector3Int pos, out SpaceCacheEntry entry)
            {
#if DEBUG_HEARING
                if (access % 1024 == 0)
                {
                    Debug.LogFormat("SpaceMetaCube {0} access {1} hits ({2} success rate)", access, hit, 100f * hit / access);
                }
                access++;
#endif
                Vector3Int key = new Vector3Int(pos.z >> subdivisionShift, pos.y >> subdivisionShift, pos.x >> subdivisionShift);
                if (key == lastKey)
                {
#if DEBUG_HEARING
                    hit++;
#endif
                    if (lastCube == null)
                    {
                        entry = NoSpaceCacheEntry;
                        return false;
                    }
                    else
                    {
                        entry = ((SpaceCube) lastCube).Get(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask);
                        return true;
                    }
                }
                else 
                {
                    lastKey = key;
                    if(cache.TryGetValue(key, out SpaceCube cube))
                    {
                        lastCube = cube;
                        entry = cube.Get(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask);
                        return true;
                    }
                    else
                    {
                        lastCube = null;
                        entry = NoSpaceCacheEntry;
                        return false;
                    }
                }
            }
            public void Set(Vector3Int pos, SpaceCacheEntry entry)
            {
                Vector3Int key = new Vector3Int(pos.z >> subdivisionShift, pos.y >> subdivisionShift, pos.x >> subdivisionShift);
                if (!cache.TryGetValue(key, out SpaceCube cube))
                {
                    cube = new SpaceCube();
                    cube.Init();
                    cache.Add(key, cube);
                    if (lastKey == key)
                        lastCube = cube;
                }
                cube.Set(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask, entry);
            }
        }
    }
}