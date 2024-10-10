using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;
using UnityEngine;
using UnityEngine.Assertions;
using static DaggerfallWorkshop.Game.Utility.PathFinding;

namespace DaggerfallWorkshop.Game.Utility
{

    public class DiscretizedNavigableSpace
    {
        private Vector3 origin;
        private Vector3 step;
        private Vector3 inverseStep;
        private float radius;
        private static int cyclesBudget = 0;
        private static int LayersMask = 0;
        private static bool opportunisticRaycast = true;

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

        private DiscretizedSpace.SpaceMetaCube<UInt32> spaceCache;
        private DiscretizedSpace.SpaceCubeAllocator<UInt32> spaceAllocator;

        public DiscretizedNavigableSpace(Vector3 origin, Vector3 step, float radius, DiscretizedSpace.SpaceCubeAllocator<UInt32> allocator)
        {
            this.origin = origin;
            this.step = step;
            this.radius = radius;
            inverseStep = new Vector3(1f / step.x, 1f / step.y, 1f / step.z);
            spaceCache = new DiscretizedSpace.SpaceMetaCube<UInt32>();
            spaceAllocator = allocator;
            spaceCache.Init(spaceAllocator);

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

        public static void SetCyclesBudget(int cyclesBudget)
        {
            DiscretizedNavigableSpace.cyclesBudget = cyclesBudget;
        }

        internal static int GetCyclesBudget()
        {
            return cyclesBudget;
        }

        public static bool DecrCyclesBudget()
        {
            if (cyclesBudget == 0)
                return false;
            cyclesBudget--;
            return true;
        }
        public static bool DecrRaycastBudget()
        {
            if (cyclesBudget < DaggerfallUnity.Settings.HearingRaycastCost)
                return false;
            cyclesBudget -= DaggerfallUnity.Settings.HearingRaycastCost;
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
            bool isNavigable = RawIsNavigable(source, destination, radius);
            Debug.DrawLine(source, destination, isNavigable ? Color.green : Color.red, 0.1f, false);
            return isNavigable ? PathFindingResult.Success : PathFindingResult.Failure;
        }

        // Return whether one can navigate between source and destination, but also fill collisions array with the presence
        // of colliders in the same direction over extension times the same length
        public PathFindingResult IsNavigableExtended(Vector3 source, Vector3 destination, int extension, out bool[] collisions)
        {
            if (!DecrRaycastBudget())
            {
                collisions = null;
                return PathFindingResult.NotCompleted;
            }
            collisions = RawIsNavigableExtended(source, destination, extension, radius);
            bool isNavigable = !collisions[0];
            Debug.DrawLine(source, destination, isNavigable ? Color.green : Color.red, 0.1f, false); 
            return isNavigable ? PathFindingResult.Success : PathFindingResult.Failure;
        }

        private static RaycastHit[] hitsBuffer = new RaycastHit[4];
        private static int nhits = 0;

        public static bool RawIsNavigable(Vector3 source, Vector3 destination, float radius)
        {
            RawRaycast(source, destination, radius);
            bool navigable = true;
            for (int i = 0; i < nhits; i++)
            {
                if (GameObjectHelper.IsStaticGeometry(hitsBuffer[i].transform.gameObject))
                {
                    navigable = false;
                    break;
                }
            }
            return navigable;
        }

        public static bool[] RawIsNavigableExtended(Vector3 source, Vector3 destination, int extension, float radius)
        {
            RawRaycast(source, source * (1 - extension) + destination * extension, radius);
            float sliceLengthInv = 1f / (destination - source).magnitude;
            bool[] result = new bool[extension];
            for (int i = 0; i < nhits; i++)
            {
                if (GameObjectHelper.IsStaticGeometry(hitsBuffer[i].transform.gameObject))
                {
                    int index = (int)(hitsBuffer[i].distance * sliceLengthInv);
                    if (index >= 0 && index < extension)
                        result[index] = true;
                    else
                        Debug.LogFormat("Raycast extension OOB extension {0} dist {1} step {2} 1/step {3}", extension, hitsBuffer[i].distance, (destination - source).magnitude, sliceLengthInv);
                }
            }
            return result;
        }

        private static void RawRaycast(Vector3 source, Vector3 destination, float radius)
        {
            Vector3 vector = destination - source;
            Vector3 normalized = vector.normalized;
            // Add some overlap, because paths can get thru walls if a node lands exactly on a wall (for raycasts, at least)
            float epsilon = 0.05f;

            Ray ray = new Ray(source - normalized * epsilon, normalized);
            while (true)
            {
                if (radius == 0f)
                    nhits = Physics.RaycastNonAlloc(ray, hitsBuffer, vector.magnitude + 2f * epsilon, GetLayersMask());
                else
                    nhits = Physics.SphereCastNonAlloc(ray, radius, hitsBuffer, vector.magnitude + 2f * epsilon, GetLayersMask());

                if (nhits < hitsBuffer.Length)
                    break;
                // hitsBuffer may have overflowed, retry with a larger buffer
                hitsBuffer = new RaycastHit[hitsBuffer.Length * 2];
            };
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
            if (spaceCache.TryGetValue(side, out UInt32 entry))
            {
                if ((entry & (computedBit << shift)) != 0)
                {
                    isNavigable = (entry & (navigableBit << shift)) != 0 ? PathFindingResult.Success : PathFindingResult.Failure;
                    Debug.DrawLine(Reify(source), Reify(destination), isNavigable == PathFindingResult.Success ? Color.yellow : Color.magenta, 0.1f, false);
                    return isNavigable;
                }
            }
            else
            {
                entry = 0;
            }

            if (opportunisticRaycast)
            {
                Vector3Int delta = destination - source;
                int extension = spaceCache.GetOptimalExtension(side, delta);
                isNavigable = IsNavigableExtended(Reify(source), Reify(destination), extension, out bool[] collisions);
                if (isNavigable == PathFindingResult.NotCompleted)
                    return isNavigable;
                entry = entry | (isNavigable == PathFindingResult.Success ? computedBit | navigableBit : computedBit) << shift;
                spaceCache.Set(side, entry);
    
                for (int i = 1; i < collisions.Length; i++)
                {
                    side += delta;
                    if (spaceCache.TryGetValue(side, out entry))
                    {
                        entry = entry & ~((computedBit | navigableBit) << shift);
                    }
                    else
                    {
                        entry = 0;
                    }
                    entry = entry | ((collisions[i] ? computedBit : computedBit | navigableBit ) << shift);
                    spaceCache.Set(side, entry); 
                }
            }
            else
            {
                isNavigable = IsNavigable(Reify(source), Reify(destination));
                if (isNavigable == PathFindingResult.NotCompleted)
                    return isNavigable;
                entry = entry | (isNavigable == PathFindingResult.Success ? computedBit | navigableBit : computedBit) << shift;
                spaceCache.Set(side, entry);
            }
            return isNavigable;
        }

        internal object GetCacheCount()
        {
            return spaceCache.Count();
        }

        internal void Clear()
        {
            spaceCache.Clear();
        }
    }
}