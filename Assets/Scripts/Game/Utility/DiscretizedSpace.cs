using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSpace
    {

        public static readonly int subdivisionShift = 4; // space will be divised in NxNxN cubes N = 2^subdivisionShift
        public static readonly int subdivisionMask = (1 << subdivisionShift) - 1;

        public class SpaceCubeAllocator<T>
        {
            private readonly Stack<T[,,]> freeList = new Stack<T[,,]>();
            private readonly Stack<T[,,]> dirtyFreeList = new Stack<T[,,]>();
            private int requests = 0;
            private int realAllocations = 0;
            private int asyncLaundry = 0;
            private int syncLaundry = 0;

            private T[,,] AllocateNew()
            {
                realAllocations++;
                return new T[1 << subdivisionShift, 1 << subdivisionShift, 1 << subdivisionShift];
            }

            public T[,,] Borrow()
            {
                requests++;
                if (freeList.Count > 0)
                {
                    return freeList.Pop();
                }
                else if (dirtyFreeList.Count > 0)
                {
                    syncLaundry++;
                    T[,,] cube = dirtyFreeList.Pop();
                    Clean(cube);
                    return cube;
                }
                else
                    return AllocateNew();
            }

            public void Restore(T[,,] cube)
            {
                dirtyFreeList.Push(cube);
            }
            private static void Clean(T[,,] cube)
            {
                Array.Clear(cube, 0, cube.Length);
            }

            public void Update()
            {
                if (dirtyFreeList.Count > 0)
                {
                    asyncLaundry++;
                    T[,,] cube = dirtyFreeList.Pop();
                    Clean(cube);
                    freeList.Push(cube);
                }
                else if (freeList.Count < 2)
                {
                    freeList.Push(AllocateNew());
                }
            }

            public string Stats()
            {
                return string.Format("Requests {0} Real Allocations {1} Laundry Async {2} Sync {3}", requests, realAllocations, asyncLaundry, syncLaundry);
            }
        }

        struct SpaceCube<T>
        {
            // Flattened multidimensionnal array
            T[,,] cube;
            private readonly SpaceCubeAllocator<T> allocator;

            public SpaceCube(SpaceCubeAllocator<T> allocator)
            {
                this.allocator = allocator;
                cube = null;
            }
            public void Init()
            {
                cube = allocator.Borrow();
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

            internal void Clear()
            {
                allocator.Restore(cube);
                cube = null;
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
            SpaceCubeAllocator<T> allocator;

            internal void Init(SpaceCubeAllocator<T> allocator)
            {
                cache = new Dictionary<Vector3Int, SpaceCube<T>>(128);
                this.allocator = allocator;
                lastKey = Vector3Int.zero;
                lastCube = null;
#if DEBUG_HEARING
                access = 0;
                hit = 0;
#endif
            }
            internal void Clear()
            {
                foreach (SpaceCube<T> entry in cache.Values)
                    entry.Clear();
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
                    cube = new SpaceCube<T>(allocator);
                    cube.Init();
                    cache.Add(key, cube);
                    if (lastKey == key)
                        lastCube = cube;
                }
                cube.Set(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask, entry);
            }

            // Starting from pos in direction direction, how many hits can be done in the same SpaceCube?
            internal int GetOptimalExtension(Vector3Int pos, Vector3Int direction)
            {
                int extension = int.MaxValue;
                if (direction.x < 0) extension = Math.Min(extension, 1 + (pos.x & subdivisionMask));
                if (direction.x > 0) extension = Math.Min(extension, (1 << subdivisionShift) - (pos.x & subdivisionMask));
                if (direction.y < 0) extension = Math.Min(extension, 1 + (pos.y & subdivisionMask));
                if (direction.y > 0) extension = Math.Min(extension, (1 << subdivisionShift) - (pos.y & subdivisionMask));
                if (direction.z < 0) extension = Math.Min(extension, 1 + (pos.z & subdivisionMask));
                if (direction.z > 0) extension = Math.Min(extension, (1 << subdivisionShift) - (pos.z & subdivisionMask));
                // Assert.IsTrue(extension > 0 && extension <= (1 << subdivisionShift));
                return extension;
            }
        }
    }
}