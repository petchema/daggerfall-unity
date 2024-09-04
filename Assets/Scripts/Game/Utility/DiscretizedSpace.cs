using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    [System.Serializable]
    public class OverRaycastBudgetException : Exception
    {
        public OverRaycastBudgetException() { }
        public OverRaycastBudgetException(string message) : base(message) { }
        public OverRaycastBudgetException(string message, System.Exception inner) : base(message, inner) { }
        protected OverRaycastBudgetException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

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

            public Movement(int x, int y, int z, float cost)
            {
                delta = new Vector3Int(x, y, z);
                this.cost = cost;
            }
        }
        private static List<Movement> movements = null;
        public DiscretizedSpace(Vector3 origin, Vector3 step)
        {
            this.origin = origin;
            this.step = step;
            inverseStep = new Vector3(1f / step.x, 1f / step.y, 1f / step.z);
            raycastBudget = 0;
            if (movements == null)
            {
                movements = new List<Movement>();
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            if (x != 0 || y != 0 || z != 0)
                            {
                                float cost = Mathf.Sqrt(Mathf.Pow(x * step.x, 2) + Mathf.Pow(y * step.y, 2) + Mathf.Pow(z * step.z, 2));
                                Movement movement = new Movement(x, y, z, cost);
                                movements.Add(movement);
                            }
                        }
                    }
                }
            }
        }

        public void SetRaycastBudget(int budget)
        {
            raycastBudget = budget;
        }

        private void DecrRaycastBudget()
        {
            if (raycastBudget > 0)
                raycastBudget--;
            else 
                throw new OverRaycastBudgetException();
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
        public List<Movement> GetMovements()
        {
            return movements;
        }
    
        private RaycastHit[] hitsBuffer = new RaycastHit[4];

        public bool IsNavigable(Vector3 source, Vector3 destination)
        {
            Vector3 vector = destination - source;
            Vector3 normalized = vector.normalized;
            // Add some overlap, because paths can get thru walls if a node lands exactly on a wall
            float epsilon = 0.05f;

            Ray ray = new Ray(source - normalized * epsilon, normalized);
            int nhits;
            while (true) {
                DecrRaycastBudget();
                nhits = Physics.SphereCastNonAlloc(ray, 0.2f, hitsBuffer, vector.magnitude + 2f * epsilon, GetLayersMask());
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
            return navigable;
        }
        public struct NavigableCacheEntry
        {
            public int computed; // Bitfield of directions that have been already computed (movementIndex-based)
            public int navigable; // If computed bit is set, bitfield of directions that are navigable (movementIndex-based)

            public NavigableCacheEntry(int computed, int navigable)
            {
                this.computed = computed;
                this.navigable = navigable;
            }
        }
        public Dictionary<Vector3Int, NavigableCacheEntry> NavigableCache = new Dictionary<Vector3Int, NavigableCacheEntry>();

        internal bool IsNavigableInt(Vector3Int source, Vector3Int destination, int movementIndex)
        {
            int shift = 1 << movementIndex;
            bool isNavigable;
            if (NavigableCache.TryGetValue(source, out NavigableCacheEntry entry))
            {
                if ((entry.computed & shift) != 0)
                {
                    isNavigable = (entry.navigable & shift) != 0;
                    Debug.DrawLine(Reify(source), Reify(destination), isNavigable ? Color.cyan : Color.blue, 0.1f, false);
                }
                else
                {
                    isNavigable = IsNavigable(Reify(source), Reify(destination));
                    entry.computed |= shift;
                    if (isNavigable)
                        entry.navigable |= shift;
                    NavigableCache[source] = entry;
                }
            }
            else
            {
                isNavigable = IsNavigable(Reify(source), Reify(destination));
                entry = new NavigableCacheEntry(shift, isNavigable ? shift : 0);
                NavigableCache[source] = entry;
            }
            return isNavigable;
        }
    }
}