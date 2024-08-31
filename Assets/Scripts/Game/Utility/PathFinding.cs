using System;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility;
using UnityEngine;

namespace DaggerfallWorkshop.Game.Utility
{

    public static class PathFinding
    {
        protected class ChainedPath : IComparable<ChainedPath> 
        {
            public readonly Vector3Int position;
            public readonly float cost;
            public readonly float estimatedTotalCost; // cost + heuristic cost of remaining path
            public readonly ChainedPath source;

            public ChainedPath(Vector3Int position, float cost, float estimatedTotalCost, ChainedPath source)
            {
                this.position = position;
                this.cost = cost;
                this.estimatedTotalCost = estimatedTotalCost;
                this.source = source;
            }

            public int CompareTo(ChainedPath otherPath)
            {
                return estimatedTotalCost.CompareTo(otherPath.estimatedTotalCost);
            }
        }

        protected class DiscretizedSpace
        {
            private Vector3 origin;
            private Vector3 step;
            internal DiscretizedSpace(Vector3 origin, Vector3 step)
            {
                this.origin = origin;
                this.step = step;
            }

            internal Vector3Int Discretize(Vector3 vector)
            {
                return new Vector3Int((int) Mathf.Floor((vector.x - origin.x) / step.x),
                                    (int) Mathf.Floor((vector.y - origin.y) / step.y),
                                    (int) Mathf.Floor((vector.z - origin.z) / step.z));
            }

            internal Vector3 Reify(Vector3Int gridNode)
            {
                return new Vector3(origin.x + gridNode.x * step.x,
                                origin.y + gridNode.y * step.y,
                                origin.z + gridNode.z * step.z);
            }

            internal bool IsNeighboor(Vector3 source, Vector3 destination)
            {
                return Mathf.Abs(destination.x - source.x) < step.x &&
                    Mathf.Abs(destination.y - source.y) < step.y &&
                    Mathf.Abs(destination.z - source.z) < step.z;
            }
            internal bool IsNeighboor(Vector3Int sourceGridNode, Vector3 destination)
            {
                return IsNeighboor(Reify(sourceGridNode), destination);
            }

            internal float HeuristicCost(Vector3 source, Vector3 destination)
            {
                return Vector3.Distance(source, destination);
            }

            internal float HeuristicCost(Vector3Int sourceGridNode, Vector3 destination)
            {
                return HeuristicCost(Reify(sourceGridNode), destination);
            }
            internal float HeuristicCost(Vector3Int sourceGridNode, Vector3Int destinationGridNode)
            {
                return HeuristicCost(Reify(sourceGridNode), Reify(destinationGridNode));
            }

            internal float MeasuredCost(Vector3 source, Vector3Int destinationGridNode)
            {
                return HeuristicCost(source, Reify(destinationGridNode));
            }
            internal float MeasuredCost(Vector3Int sourceGridNode, Vector3Int destinationGridNode)
            {
                return HeuristicCost(Reify(sourceGridNode), Reify(destinationGridNode));
            }
        }

        
        private static int defaultLayerOnlyMask = 0;
        private static RaycastHit[] hitsBuffer = new RaycastHit[4];

        public static bool IsNavigable(Vector3 source, Vector3 destination)
        {
            Vector3 vector = destination - source;
            Vector3 normalized = vector.normalized;
            // Add some overlap, because paths can get thru walls if a node lands exactly on a wall
            float epsilon = 0.01f;
            if (defaultLayerOnlyMask == 0)
                defaultLayerOnlyMask = 1 << LayerMask.NameToLayer("Default");

            Ray ray = new Ray(source - normalized * epsilon, normalized);
            int nhits;
            while (true) {
                nhits = Physics.RaycastNonAlloc(ray, hitsBuffer, vector.magnitude + 2f * epsilon, defaultLayerOnlyMask);
                if (nhits < hitsBuffer.Length)
                    break;
                // hitsBuffer may have overflowed, retry with a larger buffer
                hitsBuffer = new RaycastHit[nhits + 1];
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
            Debug.DrawLine(source, destination, navigable ? Color.green : Color.red, 1f, true);
            return navigable;
        }

        private static List<Vector3> RebuildPath(ChainedPath path)
        {
            List<Vector3> result = new List<Vector3>();
            while (path != null)
            {
                result.Insert(0, path.position); // is that efficient enough?
                path = path.source;
            }
            return result;
        }

        public static bool FindShortestPath(Vector3 start, Vector3 destination, ref int RaycastBudget, out List<Vector3> path, float weight = 1f)
        {
            Vector3 step = new Vector3(0.5f, 0.5f, 0.5f);
            PriorityQueue<ChainedPath> openList = new PriorityQueue<ChainedPath>();
            ISet<Vector3> closedList = new HashSet<Vector3>();
            DiscretizedSpace space = new DiscretizedSpace(Vector3.zero, step);  // origin should be arbitrary
            Vector3Int discretizedStart = space.Discretize(start);
            for (int x = 0; x <= 1; x++) 
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3Int position = discretizedStart + new Vector3Int(x, y, z);
                        if (--RaycastBudget == 0)
                            goto GIVEUP;
                        openList.Enqueue(new ChainedPath(position, 0f, space.MeasuredCost(start, position) * weight, null));
                    }
                }
            }
            while (openList.Count() > 0)
            {
                ChainedPath Path = openList.Dequeue();
                if (!closedList.Contains(Path.position))
                {
                    if (--RaycastBudget == 0)
                        goto GIVEUP;
                    Vector3 source = Path.source != null ? space.Reify(Path.source.position) : start;
                    if (IsNavigable(source, space.Reify(Path.position)))
                    {
                        // Are we arrived?
                        if (space.IsNeighboor(Path.position, destination))
                        {
                            if (--RaycastBudget == 0)
                                goto GIVEUP;
                            if (IsNavigable(space.Reify(Path.position), destination))
                            {
                                path = RebuildPath(Path);
                                path.Add(destination);
                                return true;
                            }
                        }

                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                for (int z = -1; z <= 1; z++)
                                {
                                    Vector3Int newPosition = new Vector3Int(Path.position.x + x, Path.position.y + y, Path.position.z + z);
                                    float newCost = Path.cost + space.MeasuredCost(Path.position, newPosition);
                                    openList.Enqueue(new ChainedPath(newPosition, newCost, newCost + space.HeuristicCost(newPosition, destination) * weight, Path));
                                }
                            }
                        }
                    }
                    closedList.Add(Path.position);
                }
            }
            GIVEUP:
                path = null;
                return false;
        }
        
    }
}