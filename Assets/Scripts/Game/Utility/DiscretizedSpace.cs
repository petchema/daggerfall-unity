using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;
using UnityEngine;
using UnityEngine.Assertions;
using static DaggerfallWorkshop.Game.Utility.PathFinding;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedSpace
    {
        private Vector3 origin;
        private Vector3 step;
        private Vector3 inverseStep;
        private int raycastBudget = 0;
        private static int LayersMask = 0;

        public struct Movement
        {
            public readonly Vector3Int delta;
            public readonly float cost;
            public readonly int shift;
            public readonly bool side;

            public Movement(int x, int y, int z, float cost, int shift, bool side)
            {
                delta = new Vector3Int(x, y, z);
                this.cost = cost;
                this.shift = shift;
                this.side = side;
            }
        }
        private static Movement[] movements = null;

        private SpaceMetaCube spaceCache;

        public DiscretizedSpace(Vector3 origin, Vector3 step)
        {
            this.origin = origin;
            this.step = step;
            inverseStep = new Vector3(1f / step.x, 1f / step.y, 1f / step.z);
            raycastBudget = 0;
            spaceCache = new SpaceMetaCube();
            spaceCache.Init();

            if (movements == null)
            {
                movements = new Movement[26];
                int shift = 0;
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            if (x != 0 || y != 0 || z != 0)
                            {
                                bool found = false;
                                for (int i = 0; i < shift * 2; i++)
                                {
                                    if (movements[i].delta.x == x && movements[i].delta.y == y && movements[i].delta.z == z)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    float cost = Mathf.Sqrt(Mathf.Pow(x * step.x, 2) + Mathf.Pow(y * step.y, 2) + Mathf.Pow(z * step.z, 2));
                                    movements[shift * 2] = new Movement(x, y, z, cost, shift, false);
                                    movements[shift * 2 + 1] = new Movement(-x, -y, -z, cost, shift, true);
                                    shift++;
                                }
                            }
                        }
                    }
                }
                Assert.AreEqual(shift, 13);
            }
        }

        internal int GetRaycastBudget()
        {
            return raycastBudget;
        }
        public void SetRaycastBudget(int raycastBudget)
        {
            this.raycastBudget = raycastBudget;
        }

        private bool DecrRaycastBudget()
        {
            if (raycastBudget == 0)
                return false;
            raycastBudget--;
            return true;
        }

        public static int GetLayersMask()
        {
            if (LayersMask == 0)
                LayersMask = ~((1 << LayerMask.NameToLayer("Automap")) |
                               (1 << LayerMask.NameToLayer("Enemies")) |
                               (1 << LayerMask.NameToLayer("Player")) |
                               (1 << LayerMask.NameToLayer("SpellMissiles")) ) ;; // 1 << LayerMask.NameToLayer("Default");
            return LayersMask;
        }

        public Vector3Int Discretize(Vector3 vector)
        {
            return new Vector3Int((int) Mathf.Floor((vector.x - origin.x) * inverseStep.x),
                                (int) Mathf.Floor((vector.y - origin.y) * inverseStep.y),
                                (int) Mathf.Floor((vector.z - origin.z) * inverseStep.z));
        }

        public Vector3 Reify(Vector3Int gridNode)
        {
            return new Vector3(origin.x + gridNode.x * step.x,
                            origin.y + gridNode.y * step.y,
                            origin.z + gridNode.z * step.z);
        }

        public float HeuristicCost(Vector3 source, Vector3 destination)
        {
            return Vector3.Distance(source, destination);
        }
        public float MeasuredCost(Vector3 source, Vector3 destination)
        {
            return Vector3.Distance(source, destination);
        }
        public float MeasuredCostInt(Vector3Int sourceGridNode, Vector3Int destinationGridNode)
        {
            return Vector3Int.Distance(sourceGridNode, destinationGridNode);
        }
        public Movement[] GetMovements()
        {
            return movements;
        }
    
        public PathFindingResult IsNavigable(Vector3 source, Vector3 destination)
        {
            if (!DecrRaycastBudget())
                return PathFindingResult.NotCompleted;
            PathFindingResult isNavigable = RawIsNavigable(source, destination);
            Debug.DrawLine(source, destination, isNavigable == PathFindingResult.Success ? Color.green : Color.red, 0.1f, false);
            return isNavigable;
        }

        private static RaycastHit[] hitsBuffer = new RaycastHit[4];

        public static PathFindingResult RawIsNavigable(Vector3 source, Vector3 destination)
        {
            Vector3 vector = destination - source;
            Vector3 normalized = vector.normalized;
            // Add some overlap, because paths can get thru walls if a node lands exactly on a wall
            float epsilon = 0.05f;

            Ray ray = new Ray(source - normalized * epsilon, normalized);
            int nhits;
            while (true) {
                nhits = Physics.SphereCastNonAlloc(ray, 0.25f, hitsBuffer, vector.magnitude + 2f * epsilon, GetLayersMask());
                // nhits = Physics.RaycastNonAlloc(ray, hitsBuffer, vector.magnitude + 2f * epsilon, GetLayersMask());
                if (nhits < hitsBuffer.Length)
                    break;
                // hitsBuffer may have overflowed, retry with a larger buffer
                hitsBuffer = new RaycastHit[hitsBuffer.Length * 2];
            };
            bool navigable = true;
            for (int i = 0; i < nhits; i++)
            {
                if (GameObjectHelper.IsStaticGeometry(hitsBuffer[i].transform.gameObject))
                {
                    navigable = false;
                    break;
                }
            }
            return navigable ? PathFindingResult.Success : PathFindingResult.Failure;
        }

        static readonly int subdivisionShift = 4; // space will be divised in NxNxN cubes N = 2^subdivisionShift
        static readonly int subdivisionMask = (1 << subdivisionShift) - 1;

        public struct NavigableCacheEntry
        {
            public UInt32 flags; // Bitfield of directions that have been already computed (movementIndex-based)
                                 // 32 is sufficient for storing 2 bits for 13 orientations

            public NavigableCacheEntry(uint flags)
            {
                this.flags = flags;
            }
        }

        static NavigableCacheEntry NoNavigableCacheEntry = new NavigableCacheEntry(); // use "default" instead?

        struct SpaceCube
        {
            // Flattened multidimensionnal array
            NavigableCacheEntry[,,] cube;
            public void Init()
            {
                cube = new NavigableCacheEntry[1 << subdivisionShift, 1 << subdivisionShift, 1 << subdivisionShift];
            }
            public bool IsMissing()
            {
                return cube == null;
            }
            public NavigableCacheEntry Get(int x, int y, int z)
            {
                return cube[z, y, x];
            }
            public void Set(int x, int y, int z, NavigableCacheEntry entry)
            {
                cube[z, y, x] = entry;
            }
        } 
        // Cube of Cubes
        struct SpaceMetaCube
        {
            Dictionary<Vector3Int, SpaceCube> cache;
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
            internal int Count()
            {
                return cache.Count;
            }
            public bool TryGetValue(Vector3Int pos, out NavigableCacheEntry entry)
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
                        entry = NoNavigableCacheEntry;
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
                        entry = NoNavigableCacheEntry;
                        return false;
                    }
                }
            }
            public void Set(Vector3Int pos, NavigableCacheEntry entry)
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

        static readonly int bitsPerMovement = 2; /* 00 = unknown
                                                    01 = (unused)
                                                    10 = not navigable
                                                    11 = navigable */
        static readonly uint computedBit = 0x2;
        static readonly uint navigableBit = 0x1;

        internal PathFindingResult IsNavigableInt(Vector3Int source, Vector3Int destination, int movementIndex)
        {
            int shift = movements[movementIndex].shift * bitsPerMovement;
            Vector3Int side = movements[movementIndex].side ? destination : source;
            PathFindingResult isNavigable;
            if (spaceCache.TryGetValue(side, out NavigableCacheEntry entry))
            {
                if ((entry.flags & (computedBit << shift)) != 0)
                {
                    isNavigable = (entry.flags & (navigableBit << shift)) != 0 ? PathFindingResult.Success : PathFindingResult.Failure;
                    Debug.DrawLine(Reify(source), Reify(destination), isNavigable == PathFindingResult.Success ? Color.cyan : Color.blue, 0.1f, false);
                }
                else
                {
                    isNavigable = IsNavigable(Reify(source), Reify(destination));
                    if (isNavigable == PathFindingResult.NotCompleted)
                        return isNavigable;
                    entry.flags = entry.flags | (isNavigable == PathFindingResult.Success ? computedBit | navigableBit : computedBit) << shift;
                    spaceCache.Set(side, entry);
                }
            }
            else
            {
                isNavigable = IsNavigable(Reify(source), Reify(destination));
                if (isNavigable == PathFindingResult.NotCompleted)
                    return isNavigable;
                entry = new NavigableCacheEntry((isNavigable == PathFindingResult.Success ? computedBit | navigableBit : computedBit) << shift);
                spaceCache.Set(side, entry);
            }
            return isNavigable;
        }

        internal object GetCacheCount()
        {
            return spaceCache.Count();
        }
    }
}