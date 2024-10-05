using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSpace
    {

        public static readonly int subdivisionShift = 4; // space will be divised in NxNxN cubes N = 2^subdivisionShift
        public static readonly int subdivisionMask = (1 << subdivisionShift) - 1;

        struct SpaceCube<T>
        {
            // Flattened multidimensionnal array
            T[,,] cube;
            public void Init()
            {
                cube = new T[1 << subdivisionShift, 1 << subdivisionShift, 1 << subdivisionShift];
            }
            public bool IsMissing()
            {
                return cube == null;
            }
            public T Get(int x, int y, int z)
            {
                return cube[z, y, x];
            }
            public void Set(int x, int y, int z, T entry)
            {
                cube[z, y, x] = entry;
            }
        } 
        // Dictionary of Cubes
        public struct SpaceMetaCube<T>
        {
            Dictionary<Vector3Int, SpaceCube<T>> cache;
            // One entry cache. For 16x16x16 cubes, I have seen ~80% hit rates
            Vector3Int lastKey;
            SpaceCube<T>? lastCube;
#if DEBUG_HEARING
            int access;
            int hit;
#endif

            internal void Init()
            {
                cache = new Dictionary<Vector3Int, SpaceCube<T>>(128);
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
            public bool TryGetValue(Vector3Int pos, out T entry)
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
                        entry = default;
                        return false;
                    }
                    else
                    {
                        entry = ((SpaceCube<T>) lastCube).Get(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask);
                        return true;
                    }
                }
                else 
                {
                    lastKey = key;
                    if(cache.TryGetValue(key, out SpaceCube<T> cube))
                    {
                        lastCube = cube;
                        entry = cube.Get(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask);
                        return true;
                    }
                    else
                    {
                        lastCube = null;
                        entry = default;
                        return false;
                    }
                }
            }
            public void Set(Vector3Int pos, T entry)
            {
                Vector3Int key = new Vector3Int(pos.z >> subdivisionShift, pos.y >> subdivisionShift, pos.x >> subdivisionShift);
                if (!cache.TryGetValue(key, out SpaceCube<T> cube))
                {
                    cube = new SpaceCube<T>();
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