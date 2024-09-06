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

        public void SetRaycastBudget(int budget)
        {
            raycastBudget = budget;
        }

        private bool DecrRaycastBudget()
        {
            if (raycastBudget == 0)
                return false;
            raycastBudget--;
            return true;
        }

        public int GetLayersMask()
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
    
        private RaycastHit[] hitsBuffer = new RaycastHit[4];

        public PathFindingResult IsNavigable(Vector3 source, Vector3 destination, bool onBudget = true)
        {
            Vector3 vector = destination - source;
            Vector3 normalized = vector.normalized;
            // Add some overlap, because paths can get thru walls if a node lands exactly on a wall
            float epsilon = 0.05f;

            Ray ray = new Ray(source - normalized * epsilon, normalized);
            int nhits;
            while (true) {
                if (onBudget && !DecrRaycastBudget())
                    return PathFindingResult.NotCompleted;
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
            Debug.DrawLine(source, destination, navigable ? Color.green : Color.red, 0.1f, false);
            return navigable ? PathFindingResult.Success : PathFindingResult.Failure;
        }

        static readonly int subdivisionShift = 4; // space will be divised in NxNxN cubes N = 2^subdivisionShift
        static readonly int subdivisionMask = (1 << subdivisionShift) - 1;
        static readonly int resizeExtra = 3; // How many extra rows/columns/... to allocate over what's necessary when resizing

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
            int xLowerBound;
            int yLowerBound;
            int zLowerBound;
            SpaceCube[][][] cache;

            internal void Init()
            {
                cache = new SpaceCube[0][][];
                xLowerBound = yLowerBound = zLowerBound = 0;
            }
            public bool TryGetValue(Vector3Int pos, out NavigableCacheEntry entry)
            {
                entry = NoNavigableCacheEntry;
                int cubeZ = pos.z >> subdivisionShift;
                if (cubeZ < zLowerBound || cubeZ >= zLowerBound + cache.Length)
                    return false;
                SpaceCube[][] plane = cache[cubeZ - zLowerBound];
                if (plane == null)
                    return false;

                int cubeY = pos.y >> subdivisionShift;
                if (cubeY < yLowerBound || cubeY >= yLowerBound + plane.Length)
                    return false;
                SpaceCube[] row = plane[cubeY - yLowerBound];
                if (row == null)
                    return false;

                int cubeX = pos.x >> subdivisionShift;
                if (cubeX < xLowerBound || cubeX >= xLowerBound + row.Length)
                    return false;
                SpaceCube cube = row[cubeX - xLowerBound];
                if (cube.IsMissing())
                    return false;

                entry = cube.Get(pos.x & subdivisionMask, pos.y & subdivisionMask, pos.z & subdivisionMask);
                return true;
            }
            public void Set(Vector3Int pos, NavigableCacheEntry entry)
            {
                int cubeZ = pos.z >> subdivisionShift;
                if (cubeZ < zLowerBound || cubeZ >= zLowerBound + cache.Length)
                {
                    bool isCacheEmpty = cache.Length == 0;
                    int newZLowerBound = isCacheEmpty ? cubeZ - resizeExtra : Math.Min(zLowerBound, cubeZ - resizeExtra);
                    int maxZ = isCacheEmpty ? cubeZ + resizeExtra : Math.Max(zLowerBound + cache.Length - 1, cubeZ + resizeExtra);
                    SpaceCube[][][] newCache = new SpaceCube[maxZ - newZLowerBound + 1][][];
                    if (!isCacheEmpty)
                        Array.Copy(cache, 0, newCache, zLowerBound - newZLowerBound, cache.Length);
                    cache = newCache;
                    zLowerBound = newZLowerBound;
                }
                SpaceCube[][] plane = cache[cubeZ - zLowerBound];
                if (plane == null)
                {
                    cache[cubeZ - zLowerBound] = plane = new SpaceCube[0][];
                }

                int cubeY = pos.y >> subdivisionShift;
                if (cubeY < yLowerBound || cubeY >= yLowerBound + plane.Length)
                {
                    bool isPlaneEmpty = plane.Length == 0;
                    int newYLowerBound = isPlaneEmpty ? cubeY - resizeExtra : Math.Min(yLowerBound, cubeY - resizeExtra);
                    int maxY = isPlaneEmpty ? cubeY + resizeExtra : Math.Max(yLowerBound + plane.Length - 1, cubeY + resizeExtra);
                    SpaceCube[][] newPlane = new SpaceCube[maxY - newYLowerBound + 1][];
                    if (!isPlaneEmpty)
                        Array.Copy(plane, 0, newPlane, yLowerBound - newYLowerBound, plane.Length);
                    cache[cubeZ - zLowerBound] = plane = newPlane;
                    yLowerBound = newYLowerBound;
                }
                SpaceCube[] row = plane[cubeY - yLowerBound];
                if (row == null)
                {
                    plane[cubeY - yLowerBound] = row = new SpaceCube[0];
                }

                int cubeX = pos.x >> subdivisionShift;
                if (cubeX < xLowerBound || cubeX >= xLowerBound + row.Length)
                {
                    bool isRowEmpty = row.Length == 0;
                    int newXLowerBound = isRowEmpty ? cubeX - resizeExtra : Math.Min(xLowerBound, cubeX - resizeExtra);
                    int maxX = isRowEmpty ? cubeX + resizeExtra : Math.Max(xLowerBound + row.Length - 1, cubeX + resizeExtra);
                    SpaceCube[] newRow = new SpaceCube[maxX - newXLowerBound + 1];
                    if (!isRowEmpty)
                        Array.Copy(row, 0, newRow, xLowerBound - newXLowerBound, row.Length);
                    plane[cubeY - yLowerBound] = row = newRow;
                    xLowerBound = newXLowerBound;
                }

                SpaceCube cube = row[cubeX - xLowerBound];
                if (cube.IsMissing())
                {
                    cube.Init();
                    row[cubeX - xLowerBound] = cube;
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
    }
}